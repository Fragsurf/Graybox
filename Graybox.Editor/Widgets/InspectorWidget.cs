

using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.GameData;
using Graybox.Editor.Documents;
using ImGuiNET;
using Graybox.Utility;
using Graybox.Editor.Inspectors;
using System.Reflection;

namespace Graybox.Editor.Widgets;

internal class InspectorWidget : BaseWidget
{

	public override string Title => "Inspector";

	protected override void OnUpdate( FrameInfo frameInfo )
	{
		base.OnUpdate( frameInfo );

		var selection = Selection.SelectedObjects;
		if ( selection == null || !selection.Any() )
		{
			ImGui.Text( "Nothing selected." );
			return;
		}

		if ( selection.Count() > 1 )
		{
			ImGui.Text( "Multiple objects selected." );
			return;
		}

		var item = selection.First();
		var inspector = GetInspector( item );

		if ( inspector != null )
		{
			inspector.Target = item;
			inspector.DrawInspector();
		}
		else
		{
			ImGui.Text( $"No inspector found for {item.GetType().Name}." );
		}
	}

	void DrawProperties( object obj )
	{
		ImGui.Text( obj.GetType().Name + " Properties" );
		ImGui.Separator();

		var properties = obj.GetType().GetProperties();
		foreach ( var prop in properties )
		{
			object value = prop.GetValue( obj, null );
			if ( value != null )
			{
				ImGui.Text( $"{prop.Name}: {value}" );
			}
			else
			{
				ImGui.Text( $"{prop.Name}: null" );
			}
		}

		// Similarly for fields, if needed
		var fields = obj.GetType().GetFields();
		foreach ( var field in fields )
		{
			object value = field.GetValue( obj );
			if ( value != null )
			{
				ImGui.Text( $"{field.Name}: {value}" );
			}
			else
			{
				ImGui.Text( $"{field.Name}: null" );
			}
		}
	}

	private static Dictionary<Type, Type> inspectorCache = new Dictionary<Type, Type>();
	public static BaseInspector GetInspector( object item )
	{
		Type assetType = item.GetType();

		if ( !inspectorCache.TryGetValue( assetType, out Type inspectorType ) )
		{
			// Look for a BaseInspector subclass capable of handling the asset type
			var baseInspectorGenericType = typeof( BaseInspector<> ).MakeGenericType( assetType );
			inspectorType = Assembly.GetExecutingAssembly().GetTypes()
				.FirstOrDefault( t => t.IsClass && !t.IsAbstract && baseInspectorGenericType.IsAssignableFrom( t ) );

			if ( inspectorType != null )
			{
				inspectorCache[assetType] = inspectorType;
				return (BaseInspector)Activator.CreateInstance( inspectorType );
			}

			// Cache null if no suitable inspector is found
			inspectorCache[assetType] = null;
		}
		else if ( inspectorType != null )
		{
			return (BaseInspector)Activator.CreateInstance( inspectorType );
		}

		return null;
	}

}
