﻿
using Graybox.DataStructures.Geometric;
using Graybox.DataStructures.MapObjects;
using Graybox.Utility;
using System.Diagnostics;
using System.Drawing;

namespace Graybox.Providers.Map
{
	public class VmfProvider : MapProvider
	{

		protected override bool IsValidForFileName( string filename )
		{
			return filename.EndsWith( ".vmf", StringComparison.OrdinalIgnoreCase )
				|| filename.EndsWith( ".vmx", StringComparison.OrdinalIgnoreCase );
		}

		private static long GetObjectID( GenericStructure gs, IDGenerator generator )
		{
			long id = gs.PropertyLong( "id" );
			if ( id == 0 ) id = generator.GetNextObjectID();
			return id;
		}

		private static void FlattenTree( MapObject parent, List<Solid> solids, List<Entity> entities, List<Group> groups )
		{
			foreach ( MapObject mo in parent.GetChildren() )
			{
				if ( mo is Solid )
				{
					solids.Add( (Solid)mo );
				}
				else if ( mo is Entity )
				{
					entities.Add( (Entity)mo );
				}
				else if ( mo is Group )
				{
					groups.Add( (Group)mo );
					FlattenTree( mo, solids, entities, groups );
				}
			}
		}

		private static string FormatCoordinate( OpenTK.Mathematics.Vector3 c )
		{
			return c.X.ToString( "0.00000000" )
				+ " " + c.Y.ToString( "0.00000000" )
				+ " " + c.Z.ToString( "0.00000000" );
		}

		private static readonly string[] ExcludedKeys = new[] { "id", "spawnflags", "classname", "origin", "wad", "mapversion" };

		private static EntityData ReadEntityData( GenericStructure structure )
		{
			EntityData ret = new EntityData();
			foreach ( string key in structure.GetPropertyKeys() )
			{
				if ( ExcludedKeys.Contains( key.ToLower() ) ) continue;
				ret.SetPropertyValue( key, structure[key] );
			}
			ret.Name = structure["classname"];
			ret.Flags = structure.PropertyInteger( "spawnflags" );
			return ret;
		}

		private static void WriteEntityData( GenericStructure obj, EntityData data )
		{
			foreach ( Property property in data.Properties/*.OrderBy(x => x.Key)*/)
			{
				obj[property.Key] = property.Value;
			}
			obj["spawnflags"] = data.Flags.ToString();
		}

		private static GenericStructure WriteEditor( MapObject obj )
		{
			GenericStructure editor = new GenericStructure( "editor" );
			editor["color"] = ParseUtility.ColorToString( obj.Colour );
			editor["visgroupshown"] = "1";
			editor["visgroupautoshown"] = "1";
			if ( obj.Parent is Group ) editor["groupid"] = obj.Parent.ID.ToString();
			if ( obj.Parent != null ) editor["parentid"] = obj.Parent.ID.ToString();
			return editor;
		}

		private static Displacement ReadDisplacement( long id, GenericStructure dispinfo )
		{
			Displacement disp = new Displacement( id );
			// power, startposition, flags, elevation, subdiv, normals{}, distances{},
			// offsets{}, offset_normals{}, alphas{}, triangle_tags{}, allowed_verts{}
			disp.SetPower( dispinfo.PropertyInteger( "power", 3 ) );
			disp.StartPosition = dispinfo.PropertyCoordinate( "startposition" );
			disp.Elevation = dispinfo.PropertySingle( "elevation" );
			disp.SubDiv = dispinfo.PropertyInteger( "subdiv" ) > 0;
			int size = disp.Resolution + 1;
			GenericStructure normals = dispinfo.GetChildren( "normals" ).FirstOrDefault();
			GenericStructure distances = dispinfo.GetChildren( "distances" ).FirstOrDefault();
			GenericStructure offsets = dispinfo.GetChildren( "offsets" ).FirstOrDefault();
			GenericStructure offsetNormals = dispinfo.GetChildren( "offset_normals" ).FirstOrDefault();
			GenericStructure alphas = dispinfo.GetChildren( "alphas" ).FirstOrDefault();
			//var triangleTags = dispinfo.GetChildren("triangle_tags").First();
			//var allowedVerts = dispinfo.GetChildren("allowed_verts").First();
			for ( int i = 0; i < size; i++ )
			{
				string row = "row" + i;
				Vector3[] norm = normals != null ? normals.PropertyCoordinateArray( row, size ) : Enumerable.Range( 0, size ).Select( x => Vector3.Zero ).ToArray();
				float[] dist = distances != null ? distances.PropertySingleArray( row, size ) : Enumerable.Range( 0, size ).Select( x => 0f ).ToArray();
				Vector3[] offn = offsetNormals != null ? offsetNormals.PropertyCoordinateArray( row, size ) : Enumerable.Range( 0, size ).Select( x => Vector3.Zero ).ToArray();
				float[] offs = offsets != null ? offsets.PropertySingleArray( row, size ) : Enumerable.Range( 0, size ).Select( x => 0f ).ToArray();
				float[] alph = alphas != null ? alphas.PropertySingleArray( row, size ) : Enumerable.Range( 0, size ).Select( x => 0f ).ToArray();
				for ( int j = 0; j < size; j++ )
				{
					disp.Points[i, j].Displacement = new DisplacementVector( norm[j], dist[j] );
					disp.Points[i, j].OffsetDisplacement = new DisplacementVector( offn[j], offs[j] );
					disp.Points[i, j].Alpha = alph[j];
				}
			}
			return disp;
		}

		private static GenericStructure WriteDisplacement( Displacement disp )
		{
			throw new NotImplementedException();
		}

		private static Face ReadFace( GenericStructure side, IDGenerator generator )
		{
			long id = side.PropertyLong( "id" );
			if ( id == 0 ) id = generator.GetNextFaceID();
			GenericStructure dispinfo = side.GetChildren( "dispinfo" ).FirstOrDefault();
			Face ret = dispinfo != null ? ReadDisplacement( id, dispinfo ) : new Face( id );
			// id, plane, material, uaxis, vaxis, rotation, lightmapscale, smoothing_groups
			Tuple<OpenTK.Mathematics.Vector3, float, float> uaxis = side.PropertyTextureAxis( "uaxis" );
			Tuple<OpenTK.Mathematics.Vector3, float, float> vaxis = side.PropertyTextureAxis( "vaxis" );
			ret.TextureRef.AssetPath = side["material"];
			ret.TextureRef.UAxis = uaxis.Item1;
			ret.TextureRef.XShift = uaxis.Item2;
			ret.TextureRef.XScale = uaxis.Item3;
			ret.TextureRef.VAxis = vaxis.Item1;
			ret.TextureRef.YShift = vaxis.Item2;
			ret.TextureRef.YScale = vaxis.Item3;
			ret.TextureRef.Rotation = side.PropertySingle( "rotation" );
			ret.Plane = side.PropertyPlane( "plane" );

			GenericStructure verts = side.Children.FirstOrDefault( x => x.Name == "vertex" );
			if ( verts != null )
			{
				int count = verts.PropertyInteger( "count" );
				for ( int i = 0; i < count; i++ )
				{
					var position = verts.PropertyCoordinate( "vertex" + i );
					var vert = new Vertex( position, ret );
					vert.LightmapU = verts.PropertySingle( "vertex_lmu" + i );
					vert.LightmapV = verts.PropertySingle( "vertex_lmv" + i );
					ret.Vertices.Add( vert );
				}
			}

			return ret;
		}

		private static GenericStructure WriteFace( Face face )
		{
			GenericStructure ret = new GenericStructure( "side" );
			ret["id"] = face.ID.ToString();
			ret["plane"] = String.Format( "({0}) ({1}) ({2})",
										 FormatCoordinate( face.Vertices[0].Position ),
										 FormatCoordinate( face.Vertices[1].Position ),
										 FormatCoordinate( face.Vertices[2].Position ) );
			ret["material"] = face.TextureRef.AssetPath;
			ret["uaxis"] = String.Format( "[{0} {1}] {2}", FormatCoordinate( face.TextureRef.UAxis ), face.TextureRef.XShift, face.TextureRef.XScale );
			ret["vaxis"] = String.Format( "[{0} {1}] {2}", FormatCoordinate( face.TextureRef.VAxis ), face.TextureRef.YShift, face.TextureRef.YScale );
			ret["rotation"] = face.TextureRef.Rotation.ToString();
			// ret["lightmapscale"]
			// ret["smoothing_groups"]

			GenericStructure verts = new GenericStructure( "vertex" );
			verts["count"] = face.Vertices.Count.ToString();
			for ( int i = 0; i < face.Vertices.Count; i++ )
			{
				verts["vertex" + i] = FormatCoordinate( face.Vertices[i].Position );
				verts["vertex_lmu" + i] = face.Vertices[i].LightmapU.ToString();
				verts["vertex_lmv" + i] = face.Vertices[i].LightmapV.ToString();
			}
			ret.Children.Add( verts );

			Displacement disp = face as Displacement;
			if ( disp != null )
			{
				ret.Children.Add( WriteDisplacement( disp ) );
			}

			return ret;
		}

		private static Solid ReadSolid( GenericStructure solid, IDGenerator generator )
		{
			GenericStructure editor = solid.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
			List<Face> faces = solid.GetChildren( "side" ).Select( x => ReadFace( x, generator ) ).ToList();
			Solid ret;

			if ( faces.All( x => x.Vertices.Count >= 3 ) )
			{
				// Vertices were stored in the VMF
				ret = new Solid( GetObjectID( solid, generator ) );
				ret.Faces.AddRange( faces );
			}
			else
			{
				// Need to grab the vertices using plane intersections
				IDGenerator idg = new IDGenerator(); // No need to increment the id generator if it doesn't have to be
				ret = Solid.CreateFromIntersectingPlanes( faces.Select( x => x.Plane ), idg );
				ret.ID = GetObjectID( solid, generator );

				for ( int i = 0; i < ret.Faces.Count; i++ )
				{
					Face face = ret.Faces[i];
					Face f = faces.FirstOrDefault( x => x.Plane.Normal.EquivalentTo( ret.Faces[i].Plane.Normal ) );
					if ( f == null )
					{
						// TODO: Report invalid solids
						Graybox.Debug.Log( "Invalid solid! ID: " + solid["id"] );
						return null;
					}
					face.TextureRef = f.TextureRef;

					Displacement disp = f as Displacement;
					if ( disp == null ) continue;

					disp.Plane = face.Plane;
					disp.Vertices = face.Vertices;
					disp.TextureRef = f.TextureRef;
					disp.AlignTextureToWorld();
					try
					{
						disp.CalculatePoints();
						ret.Faces[i] = disp;
					}
					catch { continue; }
				}
			}

			ret.Colour = editor.PropertyColour( "color", ColorUtility.GetRandomBrushColour() );
			foreach ( Face face in ret.Faces )
			{
				face.Parent = ret;
				face.Colour = ret.Colour;
				face.UpdateBoundingBox();
			}

			if ( ret.Faces.Any( x => x is Displacement ) )
			{
				ret.Faces.ForEach( x => x.IsHidden = !(x is Displacement) );
			}

			ret.UpdateBoundingBox( false );

			if ( Math.Abs( ret.BoundingBox.Dimensions.X ) < 0.001f ||
				Math.Abs( ret.BoundingBox.Dimensions.Y ) < 0.001f ||
				Math.Abs( ret.BoundingBox.Dimensions.Z ) < 0.001f )
			{
				return null;
			}

			return ret;
		}

		private static GenericStructure WriteSolid( Solid solid )
		{
			GenericStructure ret = new GenericStructure( "solid" );
			ret["id"] = solid.ID.ToString();

			foreach ( Face face in solid.Faces.OrderBy( x => x.ID ) )
			{
				ret.Children.Add( WriteFace( face ) );
			}

			GenericStructure editor = WriteEditor( solid );
			ret.Children.Add( editor );

			if ( solid.IsVisgroupHidden )
			{
				GenericStructure hidden = new GenericStructure( "hidden" );
				hidden.Children.Add( ret );
				ret = hidden;
			}

			return ret;
		}

		private static Entity ReadEntity( GenericStructure entity, IDGenerator generator )
		{
			Entity ret = new Entity( GetObjectID( entity, generator ) )
			{
				ClassName = entity["classname"],
				EntityData = ReadEntityData( entity ),
				Origin = entity.PropertyCoordinate( "origin" )
			};
			GenericStructure editor = entity.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
			ret.Colour = editor.PropertyColour( "color", ColorUtility.GetRandomBrushColour() );
			foreach ( Solid child in entity.GetChildren( "solid" ).Select( solid => ReadSolid( solid, generator ) ).Where( s => s != null ) )
			{
				child.SetParent( ret, false );
			}
			ret.UpdateBoundingBox( false );
			return ret;
		}

		private static GenericStructure WriteEntity( Entity ent )
		{
			GenericStructure ret = new GenericStructure( "entity" );
			ret["id"] = ent.ID.ToString();
			ret["classname"] = ent.EntityData.Name;
			WriteEntityData( ret, ent.EntityData );
			if ( !ent.HasChildren ) ret["origin"] = FormatCoordinate( ent.Origin );

			GenericStructure editor = WriteEditor( ent );
			ret.Children.Add( editor );

			foreach ( Solid solid in ent.GetChildren().SelectMany( x => x.FindAll() ).OfType<Solid>().OrderBy( x => x.ID ) )
			{
				ret.Children.Add( WriteSolid( solid ) );
			}

			return ret;
		}

		private static Group ReadGroup( GenericStructure group, IDGenerator generator )
		{
			Group g = new Group( GetObjectID( group, generator ) );
			GenericStructure editor = group.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
			g.Colour = editor.PropertyColour( "color", ColorUtility.GetRandomBrushColour() );
			return g;
		}

		private static GenericStructure WriteGroup( Group group )
		{
			GenericStructure ret = new GenericStructure( "group" );
			ret["id"] = group.ID.ToString();

			GenericStructure editor = WriteEditor( group );
			ret.Children.Add( editor );

			return ret;
		}

		private static World ReadWorld( GenericStructure world, IDGenerator generator )
		{
			World ret = new World( GetObjectID( world, generator ) )
			{
				ClassName = "worldspawn",
				EntityData = ReadEntityData( world )
			};

			// Load groups
			Dictionary<Group, long> groups = new Dictionary<Group, long>();
			foreach ( GenericStructure group in world.GetChildren( "group" ) )
			{
				Group g = ReadGroup( group, generator );
				GenericStructure editor = group.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
				long gid = editor.PropertyLong( "groupid" );
				groups.Add( g, gid );
			}

			// Build group tree
			List<Group> assignedGroups = groups.Where( x => x.Value == 0 ).Select( x => x.Key ).ToList();
			foreach ( Group ag in assignedGroups )
			{
				// Add the groups with no parent
				ag.SetParent( ret, false );
				groups.Remove( ag );
			}

			while ( groups.Any() )
			{
				List<KeyValuePair<Group, long>> canAssign = groups.Where( x => assignedGroups.Any( y => y.ID == x.Value ) ).ToList();
				if ( !canAssign.Any() )
				{
					break;
				}
				foreach ( KeyValuePair<Group, long> kv in canAssign )
				{
					// Add the group to the tree and the assigned list, remove it from the groups list
					Group parent = assignedGroups.First( y => y.ID == kv.Value );
					kv.Key.SetParent( parent, false );
					assignedGroups.Add( kv.Key );
					groups.Remove( kv.Key );
				}
			}

			// Load visible solids
			foreach ( var read in world.GetChildren( "solid" ).Select( x => new { Solid = ReadSolid( x, generator ), Structure = x } ) )
			{
				Solid s = read.Solid;
				GenericStructure solid = read.Structure;
				if ( s == null ) continue;

				GenericStructure editor = solid.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
				long gid = editor.PropertyLong( "groupid" );
				MapObject parent = gid > 0 ? assignedGroups.FirstOrDefault( x => x.ID == gid ) ?? (MapObject)ret : ret;
				s.SetParent( parent, false );
			}

			// Load hidden solids
			foreach ( GenericStructure hidden in world.GetChildren( "hidden" ) )
			{
				foreach ( var read in hidden.GetChildren( "solid" ).AsParallel().Select( x => new { Solid = ReadSolid( x, generator ), Structure = x } ) )
				{
					Solid s = read.Solid;
					GenericStructure solid = read.Structure;
					if ( s == null ) continue;

					s.IsVisgroupHidden = true;

					GenericStructure editor = solid.GetChildren( "editor" ).FirstOrDefault() ?? new GenericStructure( "editor" );
					long gid = editor.PropertyLong( "groupid" );
					MapObject parent = gid > 0 ? assignedGroups.FirstOrDefault( x => x.ID == gid ) ?? (MapObject)ret : ret;
					s.SetParent( parent, false );
				}
			}

			assignedGroups.ForEach( x => x.UpdateBoundingBox() );
			ret.UpdateBoundingBox();

			return ret;
		}

		private static GenericStructure WriteWorld( DataStructures.MapObjects.Map map, IEnumerable<Solid> solids, IEnumerable<Group> groups )
		{
			World world = map.WorldSpawn;
			GenericStructure ret = new GenericStructure( "world" );
			ret["id"] = world.ID.ToString();
			ret["classname"] = "worldspawn";
			ret["mapversion"] = map.Version.ToString();
			WriteEntityData( ret, world.EntityData );

			foreach ( Solid solid in solids.OrderBy( x => x.ID ) )
			{
				ret.Children.Add( WriteSolid( solid ) );
			}

			foreach ( Group group in groups.OrderBy( x => x.ID ) )
			{
				ret.Children.Add( WriteGroup( group ) );
			}

			return ret;
		}

		private static void Reindex( IEnumerable<MapObject> objs, IDGenerator generator )
		{
			foreach ( MapObject o in objs )
			{
				if ( o is Solid ) ((Solid)o).Faces.ForEach( x => x.ID = generator.GetNextFaceID() );

				// Remove the children
				List<MapObject> children = o.GetChildren().ToList();
				children.ForEach( x => x.SetParent( null ) );

				// re-index the children
				Reindex( children, generator );

				// Change the ID
				o.ID = generator.GetNextObjectID();

				// Re-add the children
				children.ForEach( x => x.SetParent( o ) );

				if ( !o.HasChildren ) o.UpdateBoundingBox();
			}
		}

		protected override DataStructures.MapObjects.Map GetFromStream( Stream stream )
		{
			using ( StreamReader reader = new StreamReader( stream ) )
			{
				GenericStructure parent = new GenericStructure( "Root" );
				parent.Children.AddRange( GenericStructure.Parse( reader ) );
				// Sections from a Hammer map:
				// - world
				// - entity
				// - visgroups
				// - cordon
				// Not done yet
				// - versioninfo
				// - viewsettings
				// - cameras

				DataStructures.MapObjects.Map map = new DataStructures.MapObjects.Map();

				GenericStructure world = parent.GetChildren( "world" ).FirstOrDefault();
				IEnumerable<GenericStructure> entities = parent.GetChildren( "entity" );
				IEnumerable<GenericStructure> visgroups = parent.GetChildren( "visgroups" ).SelectMany( x => x.GetChildren( "visgroup" ) );
				GenericStructure cameras = parent.GetChildren( "cameras" ).FirstOrDefault();
				GenericStructure cordon = parent.GetChildren( "cordon" ).FirstOrDefault();
				GenericStructure viewsettings = parent.GetChildren( "viewsettings" ).FirstOrDefault();

				if ( world != null ) map.WorldSpawn = ReadWorld( world, map.IDGenerator );
				foreach ( GenericStructure entity in entities )
				{
					Entity ent = ReadEntity( entity, map.IDGenerator );
					int groupid = entity.Children.Where( x => x.Name == "editor" ).Select( x => x.PropertyInteger( "groupid" ) ).FirstOrDefault();
					MapObject entParent = groupid > 0 ? map.WorldSpawn.Find( x => x.ID == groupid && x is Group ).FirstOrDefault() ?? map.WorldSpawn : map.WorldSpawn;
					ent.SetParent( entParent );
				}

				int activeCamera = 0;
				if ( cameras != null )
				{
					activeCamera = cameras.PropertyInteger( "activecamera" );
					foreach ( GenericStructure cam in cameras.GetChildren( "camera" ) )
					{
						var pos = cam.PropertyCoordinate( "position" );
						var look = cam.PropertyCoordinate( "look" );
						if ( pos != default && look != default )
						{
							map.Cameras.Add( new Camera { EyePosition = pos, LookPosition = look } );
						}
					}
				}
				if ( !map.Cameras.Any() )
				{
					map.Cameras.Add( new Camera { EyePosition = OpenTK.Mathematics.Vector3.Zero, LookPosition = OpenTK.Mathematics.Vector3.UnitY } );
				}
				if ( activeCamera < 0 || activeCamera >= map.Cameras.Count )
				{
					activeCamera = 0;
				}
				map.ActiveCamera = map.Cameras[activeCamera];

				if ( cordon != null )
				{
					OpenTK.Mathematics.Vector3 start = cordon.PropertyCoordinate( "mins", map.CordonBounds.Start );
					OpenTK.Mathematics.Vector3 end = cordon.PropertyCoordinate( "maxs", map.CordonBounds.End );
					map.CordonBounds = new Box( start, end );
					map.Cordon = cordon.PropertyBoolean( "active", map.Cordon );
				}

				if ( viewsettings != null )
				{
					map.SnapToGrid = viewsettings.PropertyBoolean( "bSnapToGrid", map.SnapToGrid );
					map.Show2DGrid = viewsettings.PropertyBoolean( "bShowGrid", map.Show2DGrid );
					map.Show3DGrid = viewsettings.PropertyBoolean( "bShow3DGrid", map.Show3DGrid );
					map.GridSpacing = viewsettings.PropertySingle( "nGridSpacing", map.GridSpacing );
					map.IgnoreGrouping = viewsettings.PropertyBoolean( "bIgnoreGrouping", map.IgnoreGrouping );
					map.HideFaceMask = viewsettings.PropertyBoolean( "bHideFaceMask", map.HideFaceMask );
					map.HideToolTextures = viewsettings.PropertyBoolean( "bHideToolTextures", map.HideToolTextures );
					map.HideMapOrigin = viewsettings.PropertyBoolean( "bHideMapOrigin", map.HideMapOrigin );
					map.HideEntitySprites = viewsettings.PropertyBoolean( "bHideEntitySprites", map.HideEntitySprites );
					map.TextureLock = viewsettings.PropertyBoolean( "bTextureLock", map.TextureLock );
					map.TextureScalingLock = viewsettings.PropertyBoolean( "bTextureScalingLock", map.TextureScalingLock );
				}

				return map;
			}
		}

		protected override void SaveToStream( Stream stream, DataStructures.MapObjects.Map map, AssetSystem assetSystem )
		{
			List<Group> groups = new List<Group>();
			List<Solid> solids = new List<Solid>();
			List<Entity> ents = new List<Entity>();
			FlattenTree( map.WorldSpawn, solids, ents, groups );

			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo( typeof( VmfProvider ).Assembly.Location );
			GenericStructure versioninfo = new GenericStructure( "versioninfo" );
			versioninfo.AddProperty( "editorname", "Chisel" );
			versioninfo.AddProperty( "editorversion", fvi.ProductMajorPart.ToString() + "." + fvi.ProductMinorPart.ToString() );
			versioninfo.AddProperty( "editorbuild", fvi.ProductBuildPart.ToString() );
			versioninfo.AddProperty( "mapversion", map.Version.ToString() );
			versioninfo.AddProperty( "formatversion", "100" );
			versioninfo.AddProperty( "prefab", "0" );

			GenericStructure viewsettings = new GenericStructure( "viewsettings" );

			viewsettings.AddProperty( "bSnapToGrid", map.SnapToGrid ? "1" : "0" );
			viewsettings.AddProperty( "bShowGrid", map.Show2DGrid ? "1" : "0" );
			viewsettings.AddProperty( "bShow3DGrid", map.Show3DGrid ? "1" : "0" );
			viewsettings.AddProperty( "nGridSpacing", map.GridSpacing.ToString() );
			viewsettings.AddProperty( "bIgnoreGrouping", map.IgnoreGrouping ? "1" : "0" );
			viewsettings.AddProperty( "bHideFaceMask", map.HideFaceMask ? "1" : "0" );
			viewsettings.AddProperty( "bHideToolTextures", map.HideToolTextures ? "1" : "0" );
			viewsettings.AddProperty( "bHideEntitySprites", map.HideEntitySprites ? "1" : "0" );
			viewsettings.AddProperty( "bHideMapOrigin", map.HideMapOrigin ? "1" : "0" );
			viewsettings.AddProperty( "bTextureLock", map.TextureLock ? "1" : "0" );
			viewsettings.AddProperty( "bTextureScalingLock", map.TextureScalingLock ? "1" : "0" );

			GenericStructure world = WriteWorld( map, solids, groups );

			List<GenericStructure> entities = ents.OrderBy( x => x.ID ).Select( WriteEntity ).ToList();

			GenericStructure cameras = new GenericStructure( "cameras" );
			cameras.AddProperty( "activecamera", map.Cameras.IndexOf( map.ActiveCamera ).ToString() );
			foreach ( Camera cam in map.Cameras )
			{
				GenericStructure camera = new GenericStructure( "camera" );
				camera.AddProperty( "position", "[" + FormatCoordinate( cam.EyePosition ) + "]" );
				camera.AddProperty( "look", "[" + FormatCoordinate( cam.LookPosition ) + "]" );
				cameras.Children.Add( camera );
			}

			GenericStructure cordon = new GenericStructure( "cordon" );
			cordon.AddProperty( "mins", map.CordonBounds.Start.ToString() );
			cordon.AddProperty( "maxs", map.CordonBounds.End.ToString() );
			cordon.AddProperty( "active", map.Cordon ? "1" : "0" );

			using ( StreamWriter sw = new StreamWriter( stream ) )
			{
				versioninfo.PrintToStream( sw );
				viewsettings.PrintToStream( sw );
				world.PrintToStream( sw );
				entities.ForEach( e => e.PrintToStream( sw ) );
				cameras.PrintToStream( sw );
				cordon.PrintToStream( sw );
			}
		}
	}
}
