
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Drawing;

namespace Graybox.Providers.Map.RMap
{
	internal class MapObject
	{
		public List<MapObject> Children { get; set; } = new List<MapObject>();
		public int UniqueId { get; set; }
		public virtual string Name { get; set; } = "New MapObject";
		public AABB Bounds { get; set; } = new AABB();
		public EntityMapObject Entity { get; set; }

		public IEnumerable<T> GetAllDescendants<T>() where T : MapObject
		{
			foreach ( var child in Children )
			{
				if ( child is T t ) yield return t;
				foreach ( var descendant in child.GetAllDescendants<T>() )
				{
					yield return descendant;
				}
			}
		}
	}

	internal class RMapDocument
	{
		public string Name { get; set; }
		public string AbsFilePath { get; set; }
		public int FormatVersion { get; set; }
		public MapObject Root { get; set; }
		public List<string> PackageRoots { get; set; } = new List<string>();

		public static RMapDocument FromJson( string json )
		{
			var settings = new JsonSerializerSettings();
			settings.Converters.Add( new Vector2Converter() );
			settings.Converters.Add( new Vector3Converter() );
			settings.Converters.Add( new ColorConverter() );
			settings.SerializationBinder = new MapObjectSerializationBinder();
			settings.TypeNameHandling = TypeNameHandling.Auto;
			return JsonConvert.DeserializeObject<RMapDocument>( json, settings );
		}

		public string ToJson()
		{
			var settings = new JsonSerializerSettings();
			settings.Converters.Add( new Vector2Converter() );
			settings.Converters.Add( new Vector3Converter() );
			settings.Converters.Add( new ColorConverter() );
			settings.SerializationBinder = new MapObjectSerializationBinder();
			settings.TypeNameHandling = TypeNameHandling.Auto;
			return JsonConvert.SerializeObject( this, settings );
		}

		public static RMapDocument FromMap( DataStructures.MapObjects.Map map, Func<string, string> findTexturePath = null )
		{
			var result = new RMapDocument();
			result.Root = new MapObject();
			result.Root.Children = new List<MapObject>();
			result.Root.UniqueId = 0;

			var objectId = 1;

			var entityMap = new Dictionary<Graybox.DataStructures.MapObjects.Entity, EntityMapObject>();
			var entities = map.WorldSpawn.GetAllDescendants<DataStructures.MapObjects.Entity>();
			{
				foreach ( var entity in entities )
				{
					var rmapEntity = new EntityMapObject();
					rmapEntity.KeyValuePairs = new Dictionary<string, string>();
					rmapEntity.KeyValuePairs.Add( "classname", entity.ClassName );
					rmapEntity.Name = entity.ClassName;
					rmapEntity.Bounds = new AABB();
					foreach ( var kvp in entity.EntityData.Properties )
					{
						if ( rmapEntity.KeyValuePairs.ContainsKey( kvp.Key ) ) continue;
						rmapEntity.KeyValuePairs.Add( kvp.Key, kvp.Value );
					}
					var pos = entity.EntityData.GetPropertyCoordinate( "position" );
					if ( pos != default )
					{
						var mins = new Vector3()
						{
							x = (float)pos.X - 16,
							y = (float)pos.Y - 16,
							z = (float)pos.Z - 16,
						};
						var maxs = new Vector3()
						{
							x = (float)pos.X + 16,
							y = (float)pos.Y + 16,
							z = (float)pos.Z + 16,
						};
						rmapEntity.Bounds.Min = mins;
						rmapEntity.Bounds.Max = maxs;
					}
					result.Root.Children.Add( rmapEntity );

					if ( !entityMap.ContainsKey( entity ) )
					{
						entityMap.Add( entity, rmapEntity );
					}
				}
			}

			var solids = map.WorldSpawn.GetAllDescendants<DataStructures.MapObjects.Solid>();
			{
				foreach ( var solid in solids )
				{
					var rmapSolid = new Solid();
					rmapSolid.VertexPositions = new List<Vector3>();
					rmapSolid.UniqueId = objectId++;
					rmapSolid.Faces = new List<SolidFace>();

					if ( solid.Parent is Graybox.DataStructures.MapObjects.Entity ent && entityMap.ContainsKey( ent ) )
					{
						result.Root.Children.Remove( entityMap[ent] );
						rmapSolid.Entity = entityMap[ent];
					}

					var index = 0;

					foreach ( var face in solid.Faces )
					{
						var verts = face.GetIndexedVertices().Reverse().ToList();
						var newFace = new SolidFace();
						newFace.Indices = new List<SolidIndex>();
						newFace.Texture = new Texture2D();

						if ( face.TextureRef?.Texture != null )
						{
							var uaxis = new Vector3()
							{
								x = face.TextureRef.UAxis.X,
								z = face.TextureRef.UAxis.Y,
								y = face.TextureRef.UAxis.Z,
							};

							var vaxis = new Vector3()
							{
								x = face.TextureRef.VAxis.X,
								z = face.TextureRef.VAxis.Y,
								y = face.TextureRef.VAxis.Z,
							};

							newFace.TextureMapping = new TextureMapping()
							{
								Rotation = (float)face.TextureRef.Rotation,
								UAxis = uaxis,
								VAxis = vaxis,
								UShift = (float)face.TextureRef.XShift,
								VShift = (float)face.TextureRef.YShift,
								UScale = (float)face.TextureRef.XScale,
								VScale = (float)face.TextureRef.YScale
							};

							newFace.Texture.Width = face.TextureRef.Texture.Width;
							newFace.Texture.Height = face.TextureRef.Texture.Height;
							newFace.Texture.Identifier = face.TextureRef.AssetPath;
							newFace.Texture.FilePath = findTexturePath?.Invoke( face.TextureRef.AssetPath ) ?? face.TextureRef.AssetPath;
						}

						newFace.Normal = new Vector3()
						{
							x = (float)face.Plane.Normal.X,
							z = (float)face.Plane.Normal.Y,
							y = (float)face.Plane.Normal.Z,
						};

						foreach ( var v in verts )
						{
							var duv = new Vector2();
							duv.x = (float)v.TextureU;
							duv.y = (float)v.TextureV;

							newFace.Indices.Add( new SolidIndex()
							{
								Index = index++,
								DiffuseUv = duv
							} );

							var vertPos = new Vector3();
							vertPos.x = (float)v.Position.X;
							vertPos.z = (float)v.Position.Y;
							vertPos.y = (float)v.Position.Z;

							rmapSolid.VertexPositions.Add( vertPos );
						}

						rmapSolid.Faces.Add( newFace );
					}

					var mins = new Vector3()
					{
						x = (float)solid.BoundingBox.Start.X,
						z = (float)solid.BoundingBox.Start.Y,
						y = (float)solid.BoundingBox.Start.Z,
					};

					var maxs = new Vector3()
					{
						x = (float)solid.BoundingBox.End.X,
						z = (float)solid.BoundingBox.End.Y,
						y = (float)solid.BoundingBox.End.Z,
					};

					rmapSolid.Bounds = new AABB()
					{
						Min = mins,
						Max = maxs,
					};

					result.Root.Children.Add( rmapSolid );
				}
			}


			return result;
		}
	}

	internal class AABB
	{
		public Vector3 Min { get; set; } = new Vector3();
		public Vector3 Max { get; set; } = new Vector3();

		public Vector3 Center
		{
			get
			{
				var result = new Vector3();

				result.x = (Min.x + Max.x) / 2;
				result.y = (Min.y + Max.y) / 2;
				result.z = (Min.z + Max.z) / 2;

				return result;
			}
		}
	}

	internal class Vector3
	{
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
	}

	internal class Vector2
	{
		public float x { get; set; }
		public float y { get; set; }
	}

	internal class EntityMapObject : MapObject
	{
		public string ClassName
		{
			get
			{
				if ( KeyValuePairs.TryGetValue( "classname", out var result ) )
					return result;

				return "unknown";
			}
		}
		public Dictionary<string, string> KeyValuePairs { get; set; } = new Dictionary<string, string>();
	}

	internal class Solid : MapObject
	{
		public Color Color { get; set; }
		public bool Detail { get; set; }
		public bool Hidden { get; set; }
		public List<Vector3> VertexPositions { get; set; } = new List<Vector3>();
		public List<SolidFace> Faces { get; set; } = new List<SolidFace>();
	}

	internal class SolidFace
	{
		public Vector3 Normal { get; set; } = new Vector3();
		public Texture2D Texture { get; set; } = new Texture2D();
		public List<SolidIndex> Indices { get; set; } = new List<SolidIndex>();

		public TextureMapping TextureMapping { get; set; } = new TextureMapping();
	}

	internal class SolidIndex
	{
		public int Index { get; set; }
		public Vector2 DiffuseUv { get; set; } = new Vector2();
	}

	internal class Texture2D
	{
		public string Identifier { get; set; }
		public string FilePath { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public bool IsTransparent { get; set; }
	}

	internal class TextureMapping
	{
		public Vector3 UAxis { get; set; } = new Vector3();
		public Vector3 VAxis { get; set; } = new Vector3();
		public float UScale { get; set; }
		public float VScale { get; set; }
		public float UShift { get; set; }
		public float VShift { get; set; }
		public float Rotation { get; set; }
		public bool TextureLocked { get; set; }
	}

	internal class Vector2Converter : JsonConverter<Vector2>
	{
		public override void WriteJson( JsonWriter writer, Vector2 value, JsonSerializer serializer )
		{
			writer.WriteStartObject();
			writer.WritePropertyName( "x" );
			writer.WriteValue( value.x );
			writer.WritePropertyName( "y" );
			writer.WriteValue( value.y );
			writer.WriteEndObject();
		}

		public override Vector2 ReadJson( JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer )
		{
			JObject obj = JObject.Load( reader );
			return new Vector2()
			{
				x = obj["x"].Value<float>(),
				y = obj["y"].Value<float>()
			};
		}
	}

	internal class Vector3Converter : JsonConverter<Vector3>
	{
		public override void WriteJson( JsonWriter writer, Vector3 value, JsonSerializer serializer )
		{
			writer.WriteStartObject();
			writer.WritePropertyName( "x" );
			writer.WriteValue( value.x );
			writer.WritePropertyName( "y" );
			writer.WriteValue( value.y );
			writer.WritePropertyName( "z" );
			writer.WriteValue( value.z );
			writer.WriteEndObject();
		}

		public override Vector3 ReadJson( JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer )
		{
			JObject obj = JObject.Load( reader );
			return new Vector3()
			{
				x = obj["x"].Value<float>(),
				y = obj["y"].Value<float>(),
				z = obj["z"].Value<float>()
			};
		}
	}

	internal class ColorConverter : JsonConverter<Color>
	{
		public override void WriteJson( JsonWriter writer, Color value, JsonSerializer serializer )
		{
			writer.WriteStartObject();
			writer.WritePropertyName( "r" );
			writer.WriteValue( value.R );
			writer.WritePropertyName( "g" );
			writer.WriteValue( value.G );
			writer.WritePropertyName( "b" );
			writer.WriteValue( value.B );
			writer.WritePropertyName( "a" );
			writer.WriteValue( value.A );
			writer.WriteEndObject();
		}

		public override Color ReadJson( JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer )
		{
			JObject obj = JObject.Load( reader );
			var a = 255;
			var r = 255;
			var g = 255;
			var b = 255;

			if ( obj.ContainsKey( "r" ) ) r = obj["r"].Value<byte>();
			if ( obj.ContainsKey( "g" ) ) g = obj["g"].Value<byte>();
			if ( obj.ContainsKey( "b" ) ) b = obj["b"].Value<byte>();
			if ( obj.ContainsKey( "a" ) ) a = obj["a"].Value<byte>();

			return Color.FromArgb( a, r, g, b );
		}
	}

	internal class MapObjectSerializationBinder : ISerializationBinder
	{

		public void BindToName( Type serializedType, out string assemblyName, out string typeName )
		{
			assemblyName = null;
			typeName = null;

			if ( serializedType == typeof( Solid ) )
			{
				typeName = "Solid";
			}
			else if ( serializedType == typeof( EntityMapObject ) )
			{
				typeName = "Entity";
			}
		}

		public Type BindToType( string assemblyName, string typeName )
		{
			switch ( typeName )
			{
				case "Solid": return typeof( Solid );
				case "Entity": return typeof( EntityMapObject );
				default: return null;
			}
		}

	}
}
