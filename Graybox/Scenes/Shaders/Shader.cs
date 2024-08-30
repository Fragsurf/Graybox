
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Graybox.Scenes.Shaders
{
	public class Shader : IDisposable
	{
		public int Id { get; private set; }

		protected Shader( int id )
		{
			Id = id;
		}

		public void Dispose()
		{
			GL.DeleteShader( Id );
		}

		public static Shader FromSource( string source, ShaderType type )
		{
			int shaderId = GL.CreateShader( type );
			GL.ShaderSource( shaderId, source );
			GL.CompileShader( shaderId );

			GL.GetShader( shaderId, ShaderParameter.CompileStatus, out int status );
			if ( status == 0 )
			{
				string infoLog = GL.GetShaderInfoLog( shaderId );
				throw new Exception( $"Shader compilation failed: {infoLog}" );
			}

			return new Shader( shaderId );
		}
	}

}
