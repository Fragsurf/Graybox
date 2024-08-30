
namespace Graybox.Editor;

public enum EditorEvents
{
	// Settings messages
	SettingsChanged,
	OpenSettings,

	// Layout messages
	CreateNewLayoutWindow,
	OpenLayoutSettings,
	ViewportCreated,

	// Document messages
	DocumentOpened,
	DocumentSaved,
	DocumentDeactivated,
	DocumentActivated,
	DocumentClosed,
	DocumentAllClosed,

	// Document manager messages

	// Editing messages
	TextureSelected,
	ToolSelected,
	ContextualHelpChanged,
	ViewportRightClick,
	WorldspawnProperties,
	ResetSelectedBrushType,
	IgnoreGroupingChanged,
	SelectMatchingTextures,

	VisgroupToggled,
	VisgroupsChanged,
	VisgroupVisibilityChanged,
	VisgroupShowEditor,
	VisgroupSelect,
	VisgroupShowAll,

	// Action messages

	DocumentTreeStructureChanged,
	DocumentTreeSelectedObjectsChanged,
	DocumentTreeObjectsChanged,
	DocumentTreeSelectedFacesChanged,
	DocumentTreeFacesChanged,

	EntityDataChanged,

	SelectionTypeChanged,
	SelectionChanged,

	HistoryChanged,
	ClipboardChanged,

	CompileStarted,
	CompileFinished,
	CompileFailed,

	// Status bar messages
	SelectionBoxChanged,
	DocumentGridSpacingChanged,

	// Message logging
	OutputMessage,

	// Editor messages
	LoadFile,
	UpdateToolstrip,
	CheckForUpdates,
	OpenWebsite,
	About,
	Exit,

	// Single instance

}
