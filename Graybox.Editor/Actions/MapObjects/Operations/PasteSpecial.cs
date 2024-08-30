
using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Transformations;
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions.MapObjects.Operations
{

	public enum PasteSpecialGrouping
	{
		None,
		Individual,
		All
	}

	public enum PasteSpecialStartPoint
	{
		Origin,
		CenterOriginal,
		CenterSelection
	}

	public class PasteSpecial : CreateEditDelete
	{
		private IEnumerable<MapObject> _objectsToPaste;
		private readonly int _numCopies;
		private readonly PasteSpecialStartPoint _startPoint;
		private readonly Vector3 _offset;
		private readonly Vector3 _rotation;
		private PasteSpecialGrouping _grouping;
		private readonly bool _makeEntitesUnique;
		private readonly bool _prefixEntityNames;
		private readonly string _entityNamePrefix;

		private bool _firstRun;

		public PasteSpecial( IEnumerable<MapObject> objectsToPaste, int numCopies,
			PasteSpecialStartPoint startPoint, PasteSpecialGrouping grouping,
			OpenTK.Mathematics.Vector3 offset, OpenTK.Mathematics.Vector3 rotation, bool makeEntitesUnique,
			bool prefixEntityNames, string entityNamePrefix )
		{
			_objectsToPaste = objectsToPaste;
			_numCopies = numCopies;
			_startPoint = startPoint;
			_grouping = grouping;
			_offset = offset;
			_rotation = rotation;
			_makeEntitesUnique = makeEntitesUnique;
			_prefixEntityNames = prefixEntityNames;
			_entityNamePrefix = entityNamePrefix;
			_firstRun = true;

			if ( _numCopies == 1 && grouping == PasteSpecialGrouping.All )
			{
				// Only one copy - individual will give the same result (this makes the below comparison easier)
				_grouping = PasteSpecialGrouping.Individual;
			}
			if ( _objectsToPaste.Count() == 1 && _grouping == PasteSpecialGrouping.Individual )
			{
				// Only one object - no need to group.
				_grouping = PasteSpecialGrouping.None;
			}
		}

		public override void Perform( Documents.Document document )
		{
			if ( _firstRun )
			{
				OpenTK.Mathematics.Vector3 origin = GetPasteOrigin( document );
				List<MapObject> objects = new List<MapObject>();

				if ( _objectsToPaste.Count() == 1 )
				{
					// Only one object - no need to group.
					_grouping = PasteSpecialGrouping.None;
				}

				Group allGroup = null;
				if ( _grouping == PasteSpecialGrouping.All )
				{
					// Use one group for all copies
					allGroup = new Group( document.Map.IDGenerator.GetNextObjectID() );
					// Add the group to the tree
					objects.Add( allGroup );
				}

				// Get a list of all entity names if needed
				List<string> names = new List<string>();
				if ( _makeEntitesUnique )
				{
					names = document.Map.WorldSpawn.Find( x => x is Entity )
						.Select( x => x.GetEntityData() )
						.Where( x => x != null )
						.Select( x => x.Properties.FirstOrDefault( y => y.Key == "targetname" ) )
						.Where( x => x != null )
						.Select( x => x.Value )
						.ToList();
				}

				// Start at i = 1 so the original isn't duped with no offets
				for ( int i = 1; i <= _numCopies; i++ )
				{
					OpenTK.Mathematics.Vector3 copyOrigin = origin + (_offset * i);
					OpenTK.Mathematics.Vector3 copyRotation = _rotation * i;
					List<MapObject> copy = CreateCopy( document.Map.IDGenerator, copyOrigin, copyRotation, names, document.Map.GetTransformFlags() ).ToList();
					IEnumerable<MapObject> grouped = GroupCopy( document.Map.IDGenerator, allGroup, copy );
					objects.AddRange( grouped );
				}

				// Mark the objects to be created
				Create( document.Map.WorldSpawn.ID, objects );

				// We don't need to calculate this again.
				_firstRun = false;
				_objectsToPaste = null;
			}
			base.Perform( document );
		}

		private OpenTK.Mathematics.Vector3 GetPasteOrigin( Document document )
		{
			// Find the starting point of the paste
			OpenTK.Mathematics.Vector3 origin;
			switch ( _startPoint )
			{
				case PasteSpecialStartPoint.CenterOriginal:
					// Use the original origin
					Box box = new Box( _objectsToPaste.Select( x => x.BoundingBox ) );
					origin = box.Center;
					break;
				case PasteSpecialStartPoint.CenterSelection:
					// Use the selection origin
					origin = document.Selection.GetSelectionBoundingBox().Center;
					break;
				default:
					// Use the map origin
					origin = OpenTK.Mathematics.Vector3.Zero;
					break;
			}
			return origin;
		}

		private IEnumerable<MapObject> CreateCopy( IDGenerator gen, OpenTK.Mathematics.Vector3 origin, OpenTK.Mathematics.Vector3 rotation, List<string> names, TransformFlags transformFlags )
		{
			Box box = new Box( _objectsToPaste.Select( x => x.BoundingBox ) );

			var mov = Matrix4.CreateTranslation( -box.Center ); // Move to zero
			var rot = Matrix4.CreateFromQuaternion( Quaternion.FromEulerAngles( rotation * MathF.PI / 180 ) ); // Do rotation
			var fin = Matrix4.CreateTranslation( origin ); // Move to final origin
			UnitMatrixMult transform = new UnitMatrixMult( fin * rot * mov );

			foreach ( MapObject mo in _objectsToPaste )
			{
				// Copy, transform and fix entity names
				MapObject copy = mo.Copy( gen );
				copy.Transform( transform, transformFlags );
				FixEntityNames( copy, names );
				yield return copy;
			}
		}

		private void FixEntityNames( MapObject obj, List<string> names )
		{
			if ( !_makeEntitesUnique && !_prefixEntityNames ) return;

			IEnumerable<Entity> ents = obj.Find( x => x is Entity )
				.OfType<Entity>()
				.Where( x => x.EntityData != null );
			foreach ( Entity entity in ents )
			{
				// Find the targetname property
				Property prop = entity.EntityData.Properties.FirstOrDefault( x => x.Key == "targetname" );
				if ( prop == null ) continue;

				// Skip unnamed entities
				if ( String.IsNullOrWhiteSpace( prop.Value ) ) continue;

				// Add the prefix before the unique check
				if ( _prefixEntityNames )
				{
					prop.Value = _entityNamePrefix + prop.Value;
				}

				// Make the name unique
				if ( _makeEntitesUnique )
				{
					string name = prop.Value;

					// Find a unique new name for the entity
					string newName = name;
					int counter = 1;
					while ( names.Contains( newName ) )
					{
						newName = name + "_" + counter;
						counter++;
					}

					// Set the new name and add it into the list
					prop.Value = newName;
					names.Add( newName );
				}
			}
		}

		private IEnumerable<MapObject> GroupCopy( IDGenerator gen, MapObject allGroup, List<MapObject> copy )
		{
			switch ( _grouping )
			{
				case PasteSpecialGrouping.None:
					// No grouping - add directly to tree
					return copy;
				case PasteSpecialGrouping.Individual:
					// Use one group per copy
					Group group = new Group( gen.GetNextObjectID() );
					copy.ForEach( x => x.SetParent( group ) );
					return new List<MapObject> { group };
				case PasteSpecialGrouping.All:
					// Use one group for all copies
					copy.ForEach( x => x.SetParent( allGroup ) );
					return new MapObject[0];
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
