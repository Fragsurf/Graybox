
using Graybox.Scenes.Shaders;
using Newtonsoft.Json;

namespace Graybox;

public class MaterialAsset : Asset
{

	public override AssetTypes AssetType => AssetTypes.Material;

	public TextureAsset MainTexture => GetTexture( ShaderConstants.MainTexture );
	public TextureAsset NormalTexture => GetTexture( ShaderConstants.NormalMap );
	public IReadOnlyDictionary<string, string> Properties => materialProperties;

	Dictionary<string, string> materialProperties = new();

	TextureAsset GetTexture( string key )
	{
		if ( AssetSystem == null )
			return null;

		foreach ( var kvp in materialProperties )
		{
			if ( string.Equals( kvp.Key, key, StringComparison.OrdinalIgnoreCase ) )
			{
				var texturePath = kvp.Value;
				return AssetSystem.FindAsset<TextureAsset>( texturePath );
			}
		}

		return null;
	}

	internal void Apply( ShaderProgram program )
	{
		program.ResetTexturePosition();

		var mainTex = GetTexture( ShaderConstants.MainTexture );
		var normalTex = GetTexture( ShaderConstants.NormalMap );

		if ( mainTex != null )
		{
			program.SetTexture( ShaderConstants.MainTexture, mainTex.GraphicsID );
		}

		if ( normalTex != null )
		{
			program.SetTexture( ShaderConstants.NormalMap, normalTex.GraphicsID );
			program.SetUniform( ShaderConstants.HasNormalMap, true );
		}
	}

	protected override void Load()
	{
		if ( !File.Exists( AbsolutePath ) )
			return;

		try
		{
			var json = File.ReadAllText( AbsolutePath );
			materialProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>( json );
		}
		catch ( Exception e )
		{
			Debug.Log( "Failed to deserialize material: " + e.Message );
			materialProperties = new();
		}
	}

}
