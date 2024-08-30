
using Graybox.Editor.Widgets;
using ImGuiNET;
using System.Reflection;
using System.Text.Json;

namespace Graybox.Editor;

internal partial class EditorWindow
{

	internal static string DefaultLayout => @"{""Name"":null,""Widgets"":[{""TypeName"":""Graybox.Editor.Widgets.ConsoleWidget"",""LayoutId"":""ConsoleWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.SceneWidget"",""LayoutId"":""SceneWidget#1"",""WidgetData"":{""Config"":""{\u0022Orthographic\u0022:true,\u0022View\u0022:2,\u0022Wireframe\u0022:true,\u0022GridEnabled\u0022:true,\u0022GridSize\u0022:32}""}},{""TypeName"":""Graybox.Editor.Widgets.SceneWidget"",""LayoutId"":""SceneWidget#2"",""WidgetData"":{""Config"":""{\u0022Orthographic\u0022:false,\u0022View\u0022:2,\u0022Wireframe\u0022:false,\u0022GridEnabled\u0022:true,\u0022GridSize\u0022:32}""}},{""TypeName"":""Graybox.Editor.Widgets.LightmapWidget"",""LayoutId"":""LightmapWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.HierarchyWidget"",""LayoutId"":""HierarchyWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.ProfilerWidget"",""LayoutId"":""ProfilerWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.InspectorWidget"",""LayoutId"":""InspectorWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.AssetBrowserWidget"",""LayoutId"":""AssetBrowserWidget#1"",""WidgetData"":{}},{""TypeName"":""Graybox.Editor.Widgets.SceneWidget"",""LayoutId"":""SceneWidget#5"",""WidgetData"":{""Config"":""{\u0022Orthographic\u0022:true,\u0022View\u0022:1,\u0022Wireframe\u0022:true,\u0022GridEnabled\u0022:true,\u0022GridSize\u0022:64}""}}],""ImGuiConfig"":""[Window][Graybox Window]\nPos=0,0\nSize=2560,1494\nCollapsed=0\n\n[Window][Debug##Default]\nPos=60,60\nSize=400,400\nCollapsed=0\n\n[Window][TabMenu]\nPos=0,37\nSize=2560,45\nCollapsed=0\n\n[Window][Tools##Tools_1]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Console##Console_2]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Scene##Scene_3]\nPos=60,60\nSize=48,48\nCollapsed=0\n\n[Window][Scene##Scene_4]\nPos=60,60\nSize=48,48\nCollapsed=0\n\n[Window][Lightmap##Lightmap_5]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Hierarchy##Hierarchy_6]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Profiler##Profiler_7]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Inspector##Inspector_8]\nPos=60,60\nSize=48,67\nCollapsed=0\n\n[Window][Asset Browser##Asset Browser_9]\nPos=60,60\nSize=268,237\nCollapsed=0\n\n[Window][Asset Browser##AssetBrowserWidget#1]\nPos=80,1176\nSize=2067,318\nCollapsed=0\nDockId=0x00000002,0\n\n[Window][Tools##ToolListWidget#1]\nPos=0,82\nSize=83,1412\nCollapsed=0\nDockId=0x00000009,0\n\n[Window][Console##ConsoleWidget#1]\nPos=80,1176\nSize=2067,318\nCollapsed=0\nDockId=0x00000002,2\n\n[Window][Scene##SceneWidget#1]\nPos=1141,82\nSize=1006,579\nCollapsed=0\nDockId=0x0000000B,0\n\n[Window][Scene##SceneWidget#2]\nPos=80,82\nSize=1058,1091\nCollapsed=0\nDockId=0x00000007,0\n\n[Window][Lightmap##LightmapWidget#1]\nPos=2150,82\nSize=410,680\nCollapsed=0\nDockId=0x00000005,1\n\n[Window][Hierarchy##HierarchyWidget#1]\nPos=2150,765\nSize=410,729\nCollapsed=0\nDockId=0x00000006,0\n\n[Window][Profiler##ProfilerWidget#1]\nPos=80,1176\nSize=2067,318\nCollapsed=0\nDockId=0x00000002,1\n\n[Window][Inspector##InspectorWidget#1]\nPos=2150,82\nSize=410,680\nCollapsed=0\nDockId=0x00000005,0\n\n[Window][Profiler##ProfilerWidget#3]\nPos=60,60\nSize=340,287\nCollapsed=0\n\n[Window][Hierarchy##HierarchyWidget#2]\nPos=347,228\nSize=413,398\nCollapsed=0\n\n[Window][Hierarchy##HierarchyWidget#3]\nPos=320,202\nSize=598,466\nCollapsed=0\n\n[Window][Hierarchy##HierarchyWidget#4]\nPos=60,60\nSize=273,230\nCollapsed=0\n\n[Window][Scene##SceneWidget#3]\nPos=1075,599\nSize=979,519\nCollapsed=0\n\n[Window][Scene##SceneWidget#5]\nPos=1141,664\nSize=1006,509\nCollapsed=0\nDockId=0x0000000C,0\n\n[Docking][Data]\nDockSpace           ID=0xA7781082 Window=0x10FEAD0B Pos=80,82 Size=2480,1412 Split=X\n  DockNode          ID=0x00000009 Parent=0xA7781082 SizeRef=83,1409 HiddenTabBar=1 Selected=0x45A0E952\n  DockNode          ID=0x0000000A Parent=0xA7781082 SizeRef=2474,1409 Split=X\n    DockNode        ID=0x00000003 Parent=0x0000000A SizeRef=2067,1409 Split=Y\n      DockNode      ID=0x00000001 Parent=0x00000003 SizeRef=2560,1091 Split=X Selected=0x81A4C6D5\n        DockNode    ID=0x00000007 Parent=0x00000001 SizeRef=1058,1033 CentralNode=1 HiddenTabBar=1 Selected=0x81A4C6D5\n        DockNode    ID=0x00000008 Parent=0x00000001 SizeRef=1006,1033 Split=Y Selected=0xC604BC05\n          DockNode  ID=0x0000000B Parent=0x00000008 SizeRef=882,579 HiddenTabBar=1 Selected=0xC604BC05\n          DockNode  ID=0x0000000C Parent=0x00000008 SizeRef=882,509 HiddenTabBar=1 Selected=0x33841AC5\n      DockNode      ID=0x00000002 Parent=0x00000003 SizeRef=2560,318 Selected=0xE4A75536\n    DockNode        ID=0x00000004 Parent=0x0000000A SizeRef=410,1409 Split=Y Selected=0x63A2E01C\n      DockNode      ID=0x00000005 Parent=0x00000004 SizeRef=623,679 Selected=0xE7FC19E0\n      DockNode      ID=0x00000006 Parent=0x00000004 SizeRef=623,727 HiddenTabBar=1 Selected=0x1667C59A\n\n""}";

	Dictionary<string, object> GetData( BaseWidget w )
	{
		w.OnDataGet();

		var data = new Dictionary<string, object>();
		var properties = w.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance )
			.Where( prop => Attribute.IsDefined( prop, typeof( EditorLayout.DataAttribute ) ) );

		foreach ( var prop in properties )
		{
			var value = prop.GetValue( w );
			var jsonValue = JsonSerializer.Serialize( value );
			data[prop.Name] = jsonValue;
		}

		return data;
	}

	void SetData( BaseWidget w, Dictionary<string, object> data )
	{
		var properties = w.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance )
				.Where( prop => Attribute.IsDefined( prop, typeof( EditorLayout.DataAttribute ) ) );

		foreach ( var prop in properties )
		{
			if ( data.TryGetValue( prop.Name, out var jsonValue ) )
			{
				var value = JsonSerializer.Deserialize( jsonValue.ToString(), prop.PropertyType );
				prop.SetValue( w, value );
			}
		}
	}

	public EditorLayout SaveLayout()
	{
		var widgets = new List<EditorLayout.WidgetInfo>();

		foreach ( var w in dockWidgets )
		{
			var wi = new EditorLayout.WidgetInfo()
			{
				LayoutId = w.LayoutID,
				TypeName = w.GetType().FullName,
				WidgetData = GetData( w )
			};
			widgets.Add( wi );
		}

		return new EditorLayout()
		{
			Widgets = widgets,
			ImGuiConfig = ImGui.SaveIniSettingsToMemory()
		};
	}

	public void LoadLayout( EditorLayout layout )
	{
		foreach ( var w in dockWidgets )
		{
			w.Destroy();
		}
		dockWidgets.Clear();

		foreach ( var w in layout.Widgets )
		{
			var type = Type.GetType( w.TypeName );
			if ( type == null ) continue;

			var inst = Activator.CreateInstance( type ) as BaseWidget;
			if ( inst == null ) continue;

			inst.LayoutID = w.LayoutId;
			SetData( inst, w.WidgetData );
			inst.OnDataSet();
			dockWidgets.Add( inst );
		}

		ImGui.LoadIniSettingsFromMemory( layout.ImGuiConfig );
	}

}

public class EditorLayout
{

	public string Name { get; set; }
	public List<WidgetInfo> Widgets { get; set; }
	public string ImGuiConfig { get; set; }

	public class WidgetInfo
	{
		public string TypeName { get; set; }
		public string LayoutId { get; set; }
		public Dictionary<string, object> WidgetData { get; set; } = new();
	}

	public class DataAttribute : System.Attribute
	{

	}

}
