
using Graybox.Utility;

namespace Graybox.DataStructures.MapObjects;

public class EntityData 
{

	public string Name { get; set; }
	public int Flags { get; set; }
	public List<Property> Properties { get; private set; }
	public List<Output> Outputs { get; private set; }

	public EntityData()
	{
		Properties = new List<Property>();
		Outputs = new List<Output>();
		Name = "";
	}

	public EntityData( GameData.GameDataObject gd )
	{
		Properties = new List<Property>();
		Outputs = new List<Output>();
		if ( gd == null ) return;
		Name = gd.Name;
		foreach ( GameData.Property prop in gd.Properties.Where( x => x.Name != "spawnflags" ) )
		{
			Properties.Add( new Property { Key = prop.Name, Value = prop.DefaultValue ?? string.Empty } );
		}
	}

	public EntityData Clone()
	{
		EntityData ed = new EntityData { Name = Name, Flags = Flags };
		ed.Properties.AddRange( Properties.Select( x => x.Clone() ) );
		ed.Outputs.AddRange( Outputs.Select( x => x.Clone() ) );
		return ed;
	}

	public string GetPropertyValue( string key )
	{
		Property prop = Properties.FirstOrDefault( x => String.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		return prop == null ? null : prop.Value;
	}

	public void SetPropertyValue( string key, string value )
	{
		Property prop = Properties.FirstOrDefault( x => String.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		if ( prop == null )
		{
			prop = new Property { Key = key };
			Properties.Add( prop );
		}
		prop.Value = value;
	}

	public float GetPropertyFloat( string key, float def = 0f )
	{
		var prop = Properties.FirstOrDefault( x => string.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		return prop == null ? def : ParseUtility.StringToFloat( prop.Value, def );
	}

	public Vector3 GetPropertyCoordinate( string key, Vector3 def = default )
	{
		Property prop = Properties.FirstOrDefault( x => String.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		return prop == null ? def : ParseUtility.StringToVector3( prop.Value, def );
	}

	public Color4 GetPropertyColor( string key, Color4 def )
	{
		Property prop = Properties.FirstOrDefault( x => String.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		return prop == null ? def : ParseUtility.StringToColor( prop.Value, def );
	}

	public void SetPropertyColor( string key, Color4 value )
	{
		Property prop = Properties.FirstOrDefault( x => String.Equals( key, x.Key, StringComparison.OrdinalIgnoreCase ) );
		if ( prop == null ) return;

		prop.Value = ParseUtility.ColorToString( value );
	}

	public bool Differs( EntityData other )
	{
		var hash1 = Properties.Select( x => x.Key + x.Value ).Aggregate( 0, ( a, b ) => a ^ b.GetHashCode() );
		var hash2 = other.Properties.Select( x => x.Key + x.Value ).Aggregate( 0, ( a, b ) => a ^ b.GetHashCode() );

		hash1 = HashCode.Combine( hash1, Name );
		hash2 = HashCode.Combine( hash2, Name );

		return hash1 != hash2;
	}

}
