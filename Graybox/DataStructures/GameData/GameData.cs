
using Graybox.Utility;
using Newtonsoft.Json;

namespace Graybox.DataStructures.GameData;

public class GameData
{
	public int MapSizeLow { get; set; }
	public int MapSizeHigh { get; set; }
	public List<GameDataObject> Classes { get; private set; }
	public List<string> Includes { get; private set; }
	public List<string> MaterialExclusions { get; private set; }

	public List<string> CustomEntityErrors { get; private set; }

	public GameData()
	{
		MapSizeHigh = 16384;
		MapSizeLow = -16384;
		Classes = new List<GameDataObject>();

		CustomEntityErrors = new List<string>();

		var jsonFiles = Directory.EnumerateFiles( "Assets/Entities", "*.json" );

		foreach ( string jsonFile in jsonFiles )
		{
			var jsonFilename = Path.GetFileName( jsonFile );
			var jsonContent = File.ReadAllText( jsonFile );
			var entities = JsonConvert.DeserializeObject<List<CustomEntity>>( jsonContent );

			foreach ( var customEntity in entities )
			{
				if ( string.IsNullOrEmpty( customEntity.Name ) )
				{
					CustomEntityErrors.Add( $"[{jsonFilename}] Entity class has no valid name. Skipping." );

					continue;
				}
				if ( Classes.Any( x => x.Name == customEntity.Name ) )
				{
					//CustomEntityErrors.Add( $"[{jsonFilename}] Entity class with name \"{customEntity.Name}\" already exists. Skipping." );

					continue;
				}

				var gameDataObj = new GameDataObject( customEntity.Name, customEntity.Description, ClassType.Point, true );
				gameDataObj.DisplayName = customEntity.DisplayName;

				foreach ( var customProperty in customEntity.Properties )
				{
					if ( string.IsNullOrEmpty( customProperty.Name ) )
					{
						CustomEntityErrors.Add( $"[{jsonFilename}] Property has no valid name. Skipping property." );

						continue;
					}

					if ( !GetPropertyType( customProperty, out var varType ) )
					{
						CustomEntityErrors.Add( $"[{jsonFilename}] Could not read type \"{customProperty.Type}\" of property \"{customProperty.Name}\". Skipping property." );

						continue;
					}

					var actualProperty = new Property( customProperty.Name, varType )
					{
						ShortDescription = customProperty.SmartEditName,
						DefaultValue = ParseUtility.NormalizeValue( varType, customProperty.DefaultValue ?? string.Empty ),
						Description = customProperty.HelpText
					};

					if ( varType == VariableType.Choices && customProperty.EnumValues != null )
					{
						if ( actualProperty.Options == null )
						{
							actualProperty.Options = new List<Option>();
						}
						actualProperty.Options.Clear();

						foreach ( var e in customProperty.EnumValues )
						{
							actualProperty.Options.Add( new Option()
							{
								Key = e,
								Description = e,
								On = e == customProperty.EnumValues[0]
							} );
						}
					}

					actualProperty.AssetType = customProperty.AssetType;

					gameDataObj.Properties.Add( actualProperty );
				}

				if ( !string.IsNullOrWhiteSpace( customEntity.Sprite ) )
				{
					gameDataObj.Behaviours.Add( new Behaviour( "sprite", customEntity.Sprite ) );
				}

				if ( customEntity.Name.StartsWith( "trigger_" ) || customEntity.Name.StartsWith( "func_" ) )
				{
					gameDataObj.ClassType = ClassType.Solid;
				}
				else if ( customEntity.Name.StartsWith( "brush_" ) )
				{
					gameDataObj.ClassType = ClassType.Solid;
				}
				else
				{
					gameDataObj.ClassType = ClassType.Point;
				}

				if ( gameDataObj.Properties.Any( x => string.Equals( "modelpath", x.Name, StringComparison.InvariantCultureIgnoreCase ) ) )
				{
					gameDataObj.Behaviours.Add( new Behaviour( "useModels" ) );
				}

				Classes.Add( gameDataObj );
			}
		}

		//GameDataObject lightDataObj = new GameDataObject( "light", "Point light source.", ClassType.Point );
		//lightDataObj.Properties.Add( new Property( "color", VariableType.Color255 ) { ShortDescription = "Color", DefaultValue = "255 255 255" } );
		//lightDataObj.Properties.Add( new Property( "intensity", VariableType.Float ) { ShortDescription = "Intensity", DefaultValue = "1.0" } );
		//lightDataObj.Properties.Add( new Property( "range", VariableType.Float ) { ShortDescription = "Range", DefaultValue = "1.0" } );
		//lightDataObj.Properties.Add( new Property( "hassprite", VariableType.Bool ) { ShortDescription = "Has sprite", DefaultValue = "Yes" } );
		//lightDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/lightbulb" ) );
		//Classes.Add( lightDataObj );

		//GameDataObject spotlightDataObj = new GameDataObject( "spotlight", "Self-explanatory.", ClassType.Point );
		//spotlightDataObj.Properties.Add( new Property( "color", VariableType.Color255 ) { ShortDescription = "Color", DefaultValue = "255 255 255" } );
		//spotlightDataObj.Properties.Add( new Property( "intensity", VariableType.Float ) { ShortDescription = "Intensity", DefaultValue = "1.0" } );
		//spotlightDataObj.Properties.Add( new Property( "range", VariableType.Float ) { ShortDescription = "Range", DefaultValue = "1.0" } );
		//spotlightDataObj.Properties.Add( new Property( "hassprite", VariableType.Bool ) { ShortDescription = "Has sprite", DefaultValue = "Yes" } );
		//spotlightDataObj.Properties.Add( new Property( "innerconeangle", VariableType.Float ) { ShortDescription = "Inner cone angle", DefaultValue = "45" } );
		//spotlightDataObj.Properties.Add( new Property( "outerconeangle", VariableType.Float ) { ShortDescription = "Outer cone angle", DefaultValue = "90" } );
		//spotlightDataObj.Properties.Add( new Property( "angles", VariableType.Vector ) { ShortDescription = "Rotation", DefaultValue = "0 0 0" } );
		//spotlightDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/spotlight" ) );
		//Classes.Add( spotlightDataObj );

		//GameDataObject waypointDataObj = new GameDataObject( "waypoint", "AI waypoint.", ClassType.Point );
		//waypointDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/waypoint" ) );
		//Classes.Add( waypointDataObj );

		//GameDataObject soundEmitterDataObj = new GameDataObject( "soundemitter", "Self-explanatory.", ClassType.Point );
		//soundEmitterDataObj.Properties.Add( new Property( "sound", VariableType.Integer ) { ShortDescription = "Ambience index", DefaultValue = "1" } );
		//soundEmitterDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/speaker" ) );
		//Classes.Add( soundEmitterDataObj );

		//GameDataObject modelDataObj = new GameDataObject( "model", "Self-explanatory.", ClassType.Point );
		//modelDataObj.Properties.Add( new Property( "file", VariableType.String ) { ShortDescription = "File", DefaultValue = "" } );
		//modelDataObj.Properties.Add( new Property( "angles", VariableType.Vector ) { ShortDescription = "Rotation", DefaultValue = "0 0 0" } );
		//modelDataObj.Properties.Add( new Property( "scale", VariableType.Vector ) { ShortDescription = "Scale", DefaultValue = "1 1 1" } );
		//modelDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/model" ) );
		//modelDataObj.Behaviours.Add( new Behaviour( "useModels" ) );
		//Classes.Add( modelDataObj );

		//GameDataObject screenDataObj = new GameDataObject( "screen", "Savescreen.", ClassType.Point );
		//screenDataObj.Properties.Add( new Property( "imgpath", VariableType.String ) { ShortDescription = "Image Path", DefaultValue = "" } );
		//screenDataObj.Behaviours.Add( new Behaviour( "sprite", "sprites/screen" ) );
		//Classes.Add( screenDataObj );

		//GameDataObject noShadowObj = new GameDataObject( "noshadow", "Disables shadow casting for this brush.", ClassType.Solid );
		//Classes.Add( noShadowObj );

		var p = new Property( "position", VariableType.Vector ) { ShortDescription = "Position", DefaultValue = "0 0 0" };
		foreach ( var gdo in Classes )
		{
			if ( gdo.ClassType != ClassType.Solid )
			{
				gdo.Properties.Add( p );
			}
		}

		Includes = new List<string>();
		MaterialExclusions = new List<string>();
	}

	bool GetPropertyType( CustomEntityProperty property, out VariableType varType )
	{
		if ( string.Equals( property.Name, "script", StringComparison.InvariantCultureIgnoreCase ) )
		{
			varType = VariableType.Script;
			return true;
		}

		var input = property.Type;

		if ( Enum.TryParse( input, out varType ) )
		{
			return true;
		}

		var sample = input.ToLower().Trim();

		switch ( sample )
		{
			case "vector3":
			case "angles":
				varType = VariableType.Vector;
				return true;
			case "single":
				varType = VariableType.Float;
				return true;
			case "color32":
				varType = VariableType.Color255;
				return true;
			case "boolean":
				varType = VariableType.Bool;
				return true;
			case "int32":
			case "byte":
				varType = VariableType.Integer;
				return true;
			case "enum":
				varType = VariableType.Choices;
				return true;
		}

		if ( sample.StartsWith( "asset" ) )
		{
			varType = VariableType.Asset;
			return true;
		}

		return false;
	}

	public void CreateDependencies()
	{
		List<string> resolved = new List<string>();
		List<GameDataObject> unresolved = new List<GameDataObject>( Classes );
		while ( unresolved.Any() )
		{
			List<GameDataObject> resolve = unresolved.Where( x => x.BaseClasses.All( resolved.Contains ) ).ToList();
			if ( !resolve.Any() ) throw new Exception( "Circular dependencies: " + String.Join( ", ", unresolved.Select( x => x.Name ) ) );
			resolve.ForEach( x => x.Inherit( Classes.Where( y => x.BaseClasses.Contains( y.Name ) ) ) );
			unresolved.RemoveAll( resolve.Contains );
			resolved.AddRange( resolve.Select( x => x.Name ) );
		}
	}

	public void RemoveDuplicates()
	{
		foreach ( IGrouping<string, GameDataObject> g in Classes.Where( x => x.ClassType != ClassType.Base ).GroupBy( x => x.Name.ToLowerInvariant() ).Where( g => g.Count() > 1 ).ToList() )
		{
			foreach ( GameDataObject obj in g.Skip( 1 ) ) Classes.Remove( obj );
		}
	}
}
