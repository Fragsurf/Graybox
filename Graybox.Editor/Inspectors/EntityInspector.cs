
using Graybox.DataStructures.GameData;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Entities;
using Graybox.Editor.Documents;
using Graybox.Utility;
using ImGuiNET;

namespace Graybox.Editor.Inspectors
{
	internal class EntityInspector : BaseInspector<Entity>
	{

		static Entity Entity;

		public override object Target
		{
			get => Entity;
			set
			{
				if ( Entity == value )
				{
					return;
				}
				Entity = value as Entity;
			}
		}

		public override void DrawInspector()
		{
			var ent = Entity;
			var doc = DocumentManager.CurrentDocument;

			if ( ent?.GameData == null ) return;
			if ( doc?.AssetSystem == null ) return;

			var type = ent.GameData.ClassType;
			var isBrush = type == ClassType.Solid;

			var entClasses = doc.GameData.Classes.Where( x => x.ClassType == type ).Select( y => y.Name );
			var assetSystem = doc.AssetSystem;
			var entChoices = entClasses.ToArray();
			int entCurrentChoice = Array.IndexOf( entChoices, ent.ClassName );
			ImGui.SetNextItemWidth( -1 );

			var entData = ent.EntityData;
			var gameData = DocumentManager.CurrentDocument?.GameData?.Classes?.FirstOrDefault( x => x.Name == ent.ClassName );

			if ( gameData != null )
			{
				if ( ImGui.Combo( "##" + ent.ID, ref entCurrentChoice, entChoices, entChoices.Length ) )
				{
					SetEntityClass( ent, entChoices[entCurrentChoice] );
					return;
				}

				if ( ImGui.BeginTable( "EntityEditor", 2, ImGuiTableFlags.SizingStretchProp ) )
				{
					foreach ( var prop in gameData.Properties )
					{
						ImGui.TableNextRow();
						ImGui.TableSetColumnIndex( 0 );
						ImGui.AlignTextToFramePadding();
						ImGui.Text( prop.Name );
						ImGui.TableSetColumnIndex( 1 );

						ImGui.SetNextItemWidth( -1 );

						var value = entData.GetPropertyValue( prop.Name );
						var propId = $"##{prop.Name}";
						switch ( prop.VariableType )
						{
							case VariableType.Color255:
								var color = ParseUtility.StringToColor( value, default );
								var colorVec = new SVector3( color.R, color.G, color.B );
								if ( ImGui.ColorEdit3( propId, ref colorVec ) )
								{
									entData.SetPropertyColor( prop.Name, new( (byte)(colorVec.X * 255), (byte)(colorVec.Y * 255), (byte)(colorVec.Z * 255), 255 ) );
								}
								break;
							case VariableType.Float:
								float floatValue = float.Parse( value );
								if ( ImGui.DragFloat( propId, ref floatValue ) )
								{
									entData.SetPropertyValue( prop.Name, floatValue.ToString() );
								}
								break;
							case VariableType.Bool:
								bool boolValue = bool.Parse( value );
								if ( ImGui.Checkbox( propId, ref boolValue ) )
								{
									entData.SetPropertyValue( prop.Name, boolValue.ToString() );
								}
								break;
							case VariableType.Integer:
								int intValue = int.Parse( value );
								if ( ImGui.DragInt( propId, ref intValue ) )
								{
									entData.SetPropertyValue( prop.Name, intValue.ToString() );
								}
								break;
							case VariableType.String:
								string stringValue = value;
								if ( ImGui.InputText( propId, ref stringValue, 100 ) )
								{
									entData.SetPropertyValue( prop.Name, stringValue );
								}
								break;
							case VariableType.Vector:
								var vector = ParseUtility.StringToVector3( value, default );
								var sVector = new SVector3( vector.X, vector.Y, vector.Z );
								if ( ImGui.DragFloat3( propId, ref sVector ) )
								{
									entData.SetPropertyValue( prop.Name, ParseUtility.Vector3ToString( new Vector3( sVector.X, sVector.Y, sVector.Z ) ) );
								}
								break;
							case VariableType.Choices:
								var choices = prop.Options.Select( x => x.Key ).ToArray();
								int currentChoice = Array.IndexOf( choices, value );
								if ( ImGui.Combo( propId, ref currentChoice, choices, choices.Length ) )
								{
									entData.SetPropertyValue( prop.Name, choices[currentChoice] );
								}
								break;
							case VariableType.Asset:
								var asset = assetSystem?.FindAsset( value );
								int assetThumb = 0;
								if ( asset != null )
								{
									assetThumb = asset.GetGLThumbnail();
								}
								string assetPath = value;
								ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 3, 3 ) );
								if ( ImGui.ImageButton( $"##{prop.Name}AssetThumb", assetThumb, new SVector2( 50, 50 ) ) ) // Placeholder thumbnail ID and button size
								{
									Debug.LogError( "Open asset browser" );
								}
								ImGui.PopStyleVar( 1 );

								unsafe
								{
									if ( ImGui.BeginDragDropTarget() )
									{
										ImGuiPayload* payload = ImGui.AcceptDragDropPayload( "ASSET" );
										if ( payload != null && payload->Data != null )
										{
											unsafe
											{
												var b = (byte*)payload->Data;
												var droppedData = System.Text.Encoding.UTF8.GetString( b, payload->DataSize );
												ent.EntityData.SetPropertyValue( prop.Name, droppedData );
											}
										}
										ImGui.EndDragDropTarget();
									}
								}

								ImGui.SameLine();
								ImGui.AlignTextToFramePadding();
								ImGui.TextDisabled( assetPath );

								break;
							default:
								ImGui.TextDisabled( "Unsupported type: " + prop.VariableType );
								break;
						}
					}
					ImGui.EndTable();

					if ( ImGui.Button( "Reset to Default" ) )
					{
						entData = new( gameData );
					}
				}

				if ( entData.Differs( ent.EntityData ) )
				{
					ent.EntityData = entData;
					//var edit = new EditEntityData();
					//edit.AddEntity( ent, entData );
					//doc.PerformAction( "Edit Entity Property", edit );
					//Entity = null;
				}
			}
		}

		void SetEntityClass( Entity ent, string classname )
		{
			var doc = DocumentManager.CurrentDocument;
			if ( doc?.GameData == null ) return;

			var gameData = doc.GameData.Classes.FirstOrDefault( x => x.Name == classname );
			if ( gameData == null )
			{
				Debug.LogError( "Entity GameData not found: " + classname );
				return;
			}

			var edit = new EditEntityData();
			var newData = new EntityData( gameData );
			edit.AddEntity( ent, newData );
			doc.PerformAction( "Change Entity Class", edit );
		}

	}
}
