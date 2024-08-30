
using Assimp;
using Graybox.DataStructures.Geometric;
using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Models;
using Graybox.FileSystem;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Face = Graybox.DataStructures.MapObjects.Face;
using Mesh = Assimp.Mesh;

namespace Graybox.Providers.Model
{
	public class AssimpProvider : ModelProvider
	{

		protected static AssimpContext importer = null;

		protected override bool IsValidForFile( IFile file )
		{
			return file.Extension.ToLowerInvariant() == "fbx" ||
				   file.Extension.ToLowerInvariant() == "glb" ||
				   file.Extension.ToLowerInvariant() == "gltf";
		}

		protected override DataStructures.Models.Model LoadFromFile( IFile file )
		{
			if ( importer == null )
			{
				importer = new AssimpContext();
				//importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
			}

			var model = new DataStructures.Models.Model();
			var bone = new DataStructures.Models.Bone( 0, -1, null, "rootBone", OpenTK.Mathematics.Vector3.Zero, OpenTK.Mathematics.Vector3.Zero, OpenTK.Mathematics.Vector3.One, OpenTK.Mathematics.Vector3.One );
			model.Bones.Add( bone );

			var scene = importer.ImportFile( file.FullPathName, PostProcessSteps.PreTransformVertices | PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder );

			AddNode( scene, scene.RootNode, model, Matrix4x4.Identity );

			if ( !model.Textures.Any() )
			{
				model.Textures.Add( MissingTexture() );
			}

			return model;
		}

		static DataStructures.Models.Texture GetDiffuseTexture( DataStructures.Models.Model model, Scene scene, Mesh mesh )
		{
			var idx = mesh.MaterialIndex;
			if ( idx < 0 || idx >= scene.MaterialCount )
				return null;

			var material = scene.Materials[idx];
			if ( material.HasTextureDiffuse )
			{
				var existing = model.Textures.FirstOrDefault( x => x.Name == material.TextureDiffuse.FilePath );
				if ( existing != null )
				{
					return existing;
				}

				var tex = scene.GetEmbeddedTexture( material.TextureDiffuse.FilePath );
				if ( tex != null )
				{
					using ( var ms = new MemoryStream( tex.CompressedData ) )
					{
						var bmp = new Bitmap( ms );
						var result = new DataStructures.Models.Texture()
						{
							Width = bmp.Width,
							Height = bmp.Height,
							Image = bmp,
							Name = material.TextureDiffuse.FilePath,
							Index = model.Textures.Count
						};
						model.Textures.Add( result );
						return result;
					}
				}
			}

			return null;
		}

		protected static void AddNode( Scene scene, Node node, DataStructures.Models.Model model, Matrix4x4 parentMatrix )
		{
			var selfMatrix = node.Transform * parentMatrix;
			selfMatrix = Matrix4x4.Identity;

			foreach ( int meshIndex in node.MeshIndices )
			{
				var sledgeMesh = AddMesh( model, scene.Meshes[meshIndex], selfMatrix );
				var tex = GetDiffuseTexture( model, scene, scene.Meshes[meshIndex] );

				if ( tex == null ) tex = MissingTexture();

				sledgeMesh.SkinRef = tex.Index;

				foreach ( var v in sledgeMesh.Vertices )
				{
					v.TextureU *= tex.Width;
					v.TextureV *= tex.Height;
				}

				model.AddMesh( "mesh", 0, sledgeMesh );
			}

			foreach ( var subNode in node.Children )
			{
				AddNode( scene, subNode, model, selfMatrix );
			}
		}

		protected static DataStructures.Models.Mesh AddMesh( DataStructures.Models.Model sledgeModel, Assimp.Mesh assimpMesh, Matrix4x4 selfMatrix )
		{
			var sledgeMesh = new DataStructures.Models.Mesh( 0 );
			var vertices = new List<MeshVertex>();
			var normals = new List<Vector3D>();

			if ( assimpMesh.HasNormals )
			{
				normals.AddRange( assimpMesh.Normals );
			}
			else
			{
				List<Vector3D> assimpVertices = assimpMesh.Vertices;
				for ( int i = 0; i < assimpMesh.VertexCount; i++ )
				{
					normals.Add( new Vector3D( 0, 0, 0 ) );
				}

				foreach ( Assimp.Face face in assimpMesh.Faces )
				{
					List<int> triInds = face.Indices;
					for ( int i = 1; i < triInds.Count - 1; i++ )
					{
						Vector3D normal = Vector3D.Cross( assimpVertices[triInds[0]] - assimpVertices[triInds[i]], assimpVertices[triInds[0]] - assimpVertices[triInds[i + 1]] );
						normal.Normalize();

						normals[triInds[0]] += normal;
						normals[triInds[i]] += normal;
						normals[triInds[i + 1]] += normal;
					}
				}

				for ( int i = 0; i < assimpMesh.VertexCount; i++ )
				{
					normals[i].Normalize();
				}
			}

			for ( int i = 0; i < assimpMesh.VertexCount; i++ )
			{
				var position = assimpMesh.Vertices[i];
				var normal = normals[i];
				Vector3D uv = default;

				if ( assimpMesh.HasTextureCoords( 0 ) )
				{
					uv = assimpMesh.TextureCoordinateChannels[0][i];
				}

				position = selfMatrix * position;
				normal = selfMatrix * normal;

				vertices.Add( new MeshVertex( new OpenTK.Mathematics.Vector3( position.X, position.Z, position.Y ),
											new OpenTK.Mathematics.Vector3( normal.X, normal.Z, normal.Y ),
											sledgeModel.Bones[0], uv.X, -uv.Y ) );
			}

			if ( false && assimpMesh.HasVertexColors( 0 ) )
			{
				var colors = assimpMesh.VertexColorChannels[0];
				for ( int i = 0; i < colors.Count; i++ )
				{
					var v = vertices[i];
					v.Color = new OpenTK.Mathematics.Quaternion( colors[i].R, colors[i].G, colors[i].B, colors[i].A );
				}
			}

			//selfMatrix.Decompose( out var scale, out _, out _ );
			var isNegativeScale = selfMatrix.Determinant() < 0;

			foreach ( var face in assimpMesh.Faces )
			{
				var triInds = face.Indices;
				for ( int i = 1; i < triInds.Count - 1; i++ )
				{
					var v0 = vertices[triInds[0]];
					var v1 = vertices[triInds[i + (isNegativeScale ? 1 : 0)]];
					var v2 = vertices[triInds[i + (isNegativeScale ? 0 : 1)]];

					sledgeMesh.Vertices.Add( v0 );
					sledgeMesh.Vertices.Add( v1 );
					sledgeMesh.Vertices.Add( v2 );
				}
			}

			return sledgeMesh;
		}

		public static void SaveToFile( string filename, DataStructures.MapObjects.Map map, string format )
		{
			Scene scene = new Scene();

			Node rootNode = new Node();
			rootNode.Name = "root";
			scene.RootNode = rootNode;

			Node newNode = new Node();

			Mesh mesh;
			int vertOffset;
			string[] textures = map.GetAllTextures().ToArray();
			foreach ( string texture in textures )
			{
				if ( texture == "tooltextures/remove_face" ) { continue; }

				Material material = new Material();
				material.Name = texture;
				TextureSlot textureSlot = new TextureSlot( texture +
					(File.Exists( texture + ".png" ) ? ".png" : (File.Exists( texture + ".jpeg" ) ? ".jpeg" : ".jpg")),
					TextureType.Diffuse,
					0,
					TextureMapping.Plane,
					0,
					1.0f,
					TextureOperation.Multiply,
					Assimp.TextureWrapMode.Wrap,
					Assimp.TextureWrapMode.Wrap,
					0 );
				material.AddMaterialTexture( in textureSlot );
				scene.Materials.Add( material );

				mesh = new Mesh();
				if ( format != "obj" ) // .obj files should have no mesh names so they are one proper mesh
				{
					mesh.Name = texture + "_mesh";
				}
				mesh.MaterialIndex = scene.MaterialCount - 1;
				vertOffset = 0;

				List<int> indices = new List<int>();

				IEnumerable<Face> faces = map.WorldSpawn.Find( x => x is Solid ).
					OfType<Solid>().
					SelectMany( x => x.Faces ).
					Where( x => x.TextureRef.AssetPath == texture );

				foreach ( Face face in faces )
				{
					foreach ( Vertex v in face.Vertices )
					{
						mesh.Vertices.Add( new Vector3D( (float)v.Position.X, (float)v.Position.Z, (float)v.Position.Y ) );
						mesh.Normals.Add( new Vector3D( (float)face.Plane.Normal.X, (float)face.Plane.Normal.Z, (float)face.Plane.Normal.Y ) );
						mesh.TextureCoordinateChannels[0].Add( new Vector3D( (float)v.TextureU, (float)v.TextureV, 0 ) );
					}
					mesh.UVComponentCount[0] = 2;
					foreach ( uint ind in face.GetTriangleIndices() )
					{
						indices.Add( (int)ind + vertOffset );
					}

					vertOffset += face.Vertices.Count;
				}

				mesh.SetIndices( indices.ToArray(), 3 );
				scene.Meshes.Add( mesh );

				newNode.MeshIndices.Add( scene.MeshCount - 1 );
			}

			rootNode.Children.Add( newNode );

			new AssimpContext().ExportFile( scene, filename, format );
		}

		static DataStructures.Models.Texture MissingTexture()
		{
			var bmp = new Bitmap( 8, 8 );
			for ( int i = 0; i < 8; i++ )
			{
				for ( int j = 0; j < 8; j++ )
				{
					bmp.SetPixel( i, j, Color.DarkGray );
				}
			}
			return new DataStructures.Models.Texture
			{
				Name = "Missing Texture",
				Index = 0,
				Width = bmp.Width,
				Height = bmp.Height,
				Flags = 0,
				Image = bmp
			};
		}

	}
}
