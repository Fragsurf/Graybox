
namespace Graybox.Scenes.Shaders
{
	public class ShaderProgram : IDisposable
	{
		int handle;
		int nextTextureUnit;
		int textureOffset;
		Dictionary<string, ShaderVariable> uniformLocations;

		static int maxCombinedTextureUnits;
		static ShaderProgram()
		{
			maxCombinedTextureUnits = GL.GetInteger( GetPName.MaxCombinedTextureImageUnits );
		}

		public ShaderProgram( string vertex, string fragment )
		{
			var vertShader = Shader.FromSource( vertex, ShaderType.VertexShader );
			var fragShader = Shader.FromSource( fragment, ShaderType.FragmentShader );
			Initialize( vertShader, fragShader );
		}

		public ShaderProgram( Shader vertexShader, Shader fragmentShader )
		{
			Initialize( vertexShader, fragmentShader );
		}

		void Initialize( Shader vertexShader, Shader fragmentShader )
		{
			handle = GL.CreateProgram();
			GL.AttachShader( handle, vertexShader.Id );
			GL.AttachShader( handle, fragmentShader.Id );

			GL.BindAttribLocation( handle, 0, "inPosition" );
			GL.BindAttribLocation( handle, 1, "inNormal" );
			GL.BindAttribLocation( handle, 2, "inTangent" );
			GL.BindAttribLocation( handle, 3, "inTexCoords" );
			GL.BindAttribLocation( handle, 4, "inTexCoordsLM" );
			GL.BindAttribLocation( handle, 5, "inVertexColor" );
			GL.BindAttribLocation( handle, 6, "inIsSelected" );

			GL.LinkProgram( handle );

			GL.GetProgram( handle, GetProgramParameterName.LinkStatus, out int status );
			if ( status == 0 )
			{
				string infoLog = GL.GetProgramInfoLog( handle );
				throw new Exception( $"Shader program linking failed: {infoLog}" );
			}

			GL.DetachShader( handle, vertexShader.Id );
			GL.DetachShader( handle, fragmentShader.Id );

			CacheUniformLocations();
		}

		void CacheUniformLocations()
		{
			uniformLocations = new Dictionary<string, ShaderVariable>();
			int uniformCount;
			GL.GetProgram( handle, GetProgramParameterName.ActiveUniforms, out uniformCount );

			for ( int i = 0; i < uniformCount; i++ )
			{
				int size;
				ActiveUniformType type;
				string name = GL.GetActiveUniform( handle, i, out size, out type );
				int location = GL.GetUniformLocation( handle, name );
				uniformLocations[name] = new ShaderVariable( location, type );
			}
		}

		public void Bind()
		{
			GL.UseProgram( handle );
			textureOffset = 0;
		}

		public void Unbind()
		{
			GL.UseProgram( 0 );
			GL.ActiveTexture( TextureUnit.Texture0 );
			GL.BindTexture( TextureTarget.Texture2D, 0 );
			textureOffset = 0;
		}

		public void Dispose()
		{
			GL.DeleteProgram( handle );
		}

		public void SetUniform( string name, Vector2 value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.FloatVec2 ) return;
			GL.Uniform2( variable.Location, value );
		}

		public void SetUniform( string name, Vector3 value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.FloatVec3 ) return;
			GL.Uniform3( variable.Location, value );
		}

		public void SetUniform( string name, Vector4 value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.FloatVec4 ) return;
			GL.Uniform4( variable.Location, value );
		}

		public void SetUniform( string name, Matrix4 value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.FloatMat4 ) return;
			GL.UniformMatrix4( variable.Location, false, ref value );
		}

		public void SetUniform( string name, float value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.Float ) return;
			GL.Uniform1( variable.Location, value );
		}

		public void SetUniform( string name, int value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || (variable.Type != ActiveUniformType.Int && variable.Type != ActiveUniformType.Sampler2D) ) return;
			GL.Uniform1( variable.Location, value );
		}

		public void SetUniform( string name, bool value )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.Bool ) return;
			GL.Uniform1( variable.Location, value ? 1 : 0 );
		}

		public void SetTexture( string name, int textureId )
		{
			if ( !uniformLocations.TryGetValue( name, out ShaderVariable variable ) || variable.Type != ActiveUniformType.Sampler2D ) return;

			var unit = TextureUnit.Texture0 + nextTextureUnit;

			GL.ActiveTexture( unit );
			GL.BindTexture( TextureTarget.Texture2D, textureId );
			GL.Uniform1( variable.Location, nextTextureUnit );

			nextTextureUnit++;

			if ( nextTextureUnit >= maxCombinedTextureUnits )
			{
				Debug.LogWarning( "Warning: Exceeded maximum number of texture units." );
				nextTextureUnit = textureOffset;
			}
		}

		internal void SetTextureOffset( int offset )
		{
			textureOffset = offset;
		}

		internal void ResetTexturePosition()
		{
			nextTextureUnit = textureOffset;
		}

		struct ShaderVariable
		{
			public int Location { get; }
			public ActiveUniformType Type { get; }

			public ShaderVariable( int location, ActiveUniformType type )
			{
				Location = location;
				Type = type;
			}
		}
	}
}
