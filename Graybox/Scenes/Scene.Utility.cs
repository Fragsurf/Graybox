
using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Models;
using Graybox.Graphics.Helpers;
using Graybox.Graphics.Immediate;
using Graybox.Scenes.Shaders;
using System.Drawing;

namespace Graybox.Scenes
{
	public partial class Scene
	{

		public bool ShowTriggers { get; set; } = false;
		public bool ShowEntities { get; set; } = true;
		public bool ShowLights { get; set; } = true;

		void RenderUtility()
		{
			if ( ShowTriggers ) RenderTriggers();
			if ( ShowEntities ) RenderEntities();
			//if ( ShowLights ) RenderLights();
			if ( DebugVisualizeTexels ) RenderTexels();
		}

		public void RenderTexels()
		{
			_texelShader.Bind();

			var view = Camera.GetViewMatrix();
			var proj = Camera.GetProjectionMatrix();

			_texelShader.SetUniform( ShaderConstants.CameraView, view );
			_texelShader.SetUniform( ShaderConstants.CameraProjection, proj );
			_texelShader.SetUniform( ShaderConstants.ModelMatrix, Matrix4.Identity );

			foreach ( var obj in _objects )
			{
				if ( obj is not Solid s ) continue;

				foreach ( var f in s.Faces )
				{
					_texelShader.SetUniform( "_Normal", f.Plane.Normal );
					_texelShader.SetUniform( ShaderConstants.TexelSize, (float)f.TexelSize );
					_texelShader.SetUniform( "_DisabledInLightmap", f.DisableInLightmap );

					GL.Begin( PrimitiveType.Triangles );

					foreach ( var tri in f.GetTriangles() )
					{
						GL.Vertex3( tri[0].Position.X, tri[0].Position.Y, tri[0].Position.Z );
						GL.Vertex3( tri[1].Position.X, tri[1].Position.Y, tri[1].Position.Z );
						GL.Vertex3( tri[2].Position.X, tri[2].Position.Y, tri[2].Position.Z );
					}

					GL.End();
				}
			}

			_texelShader.Unbind();
		}

		public void RenderTriggers()
		{
			if ( Camera.Orthographic ) return;

			foreach ( var obj in _objects )
			{
				if ( obj is not Solid s ) continue;
				if ( obj.Parent is not Entity e ) continue;
				if ( !e.ClassName.StartsWith( "trigger_" ) ) continue;

				var fillColor = Color.FromArgb( 100, Color.Orange );
				var wireColor = Color.FromArgb( 255, Color.Black );
				MapObjectRenderer.DrawFilledNoFucks( s.Faces, fillColor, false, false );
				GL.Color3( wireColor );
				MapObjectRenderer.DrawWireframe( s.Faces, true, false );
			}
		}

		public void RenderEntities()
		{
			foreach ( var obj in _objects )
			{
				if ( obj?.BoundingBox == null ) continue;

				var bounds = new Bounds( obj.BoundingBox.Start, obj.BoundingBox.End );
				var dir = (Camera.Position - bounds.Center).Normalized();
				if ( Camera.Orthographic )
				{
					dir = Camera.Forward;
				}

				Vector3 angles = default;

				if ( obj is Light l )
				{
					if ( l.LightType == Lightmapper.LightTypes.Directional )
					{
						angles = l.LightInfo.Direction;
					}

					var texPath = l.LightType == Lightmapper.LightTypes.Point ? "icons/icon_pointlight" : "icons/icon_sun";
					var lightIcon = AssetSystem?.FindAsset<TextureAsset>( texPath );
					if ( lightIcon != null )
					{
						var color = l.LightType == Lightmapper.LightTypes.Directional ? Color4.White : l.LightInfo.Color;
						GraphicsHelper.DrawTexturedQuad( lightIcon.GraphicsID, bounds.Center, new( 32, 32, 32 ), dir, color );
					}
				}

				if ( obj is Entity e )
				{
					var entityIcon = AssetSystem?.FindAsset<TextureAsset>( "icons/icon_entity" );
					if ( entityIcon != null )
					{
						GraphicsHelper.DrawTexturedQuad( entityIcon.GraphicsID, bounds.Center, new( 32, 32, 32 ), dir, Color4.White );
					}

					angles = e.Angles;
				}

				if ( angles != default )
				{
					var forward = angles.EulerToForward();

					var arrowStart = bounds.Center;
					var arrowLength = bounds.Size.Length * 0.75f;
					var arrowEnd = arrowStart + forward * arrowLength;

					GL.Color4( 1.0f, 1.0f, 0.0f, 1.0f );
					Gizmos.DrawCone( arrowEnd, forward, 6f, 16f );

					GL.Begin( PrimitiveType.Lines );
					GL.Vertex3( arrowStart );
					GL.Vertex3( arrowEnd );
					GL.End();
				}

				if ( Camera.Orthographic )
				{
					if ( obj is Light || obj is Entity )
					{
						var faces = obj.CollectFaces();
						GL.Color4( obj.Colour );
						MapObjectRenderer.DrawWireframe( faces, true, false );
					}
				}
			}
		}

		public void RenderLights()
		{
			foreach ( var obj in _objects )
			{
				if ( obj is not Light light ) continue;

				var bounds = new Bounds( light.BoundingBox.Start, light.BoundingBox.End );
				var fillColor = Color.FromArgb( 100, Color.Magenta );
				var wireColor = Color.FromArgb( 255, Color.Black );
				var fcv = new Vector4( fillColor.R / 255f, fillColor.G / 255f, fillColor.B / 255f, fillColor.A / 255f );
				var wcv = new Vector4( wireColor.R / 255f, wireColor.G / 255f, wireColor.B / 255f, wireColor.A / 255f );

				Gizmos.Box( bounds.Mins, bounds.Maxs, fcv, 0 );
				Gizmos.WireBox( bounds.Mins, bounds.Maxs, wcv, 0 );
			}
		}

	}
}
