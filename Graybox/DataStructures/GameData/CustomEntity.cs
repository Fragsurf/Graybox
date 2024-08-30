using System.Collections.Generic;

namespace Graybox.DataStructures.GameData
{
	public class CustomEntity
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public string Sprite { get; set; }
		public bool UseModelRendering { get; set; }
		public List<CustomEntityProperty> Properties { get; set; }
	}

	public class CustomEntityProperty
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string AssetType { get; set; }
		public string SmartEditName { get; set; }
		public string HelpText { get; set; }
		public string DefaultValue { get; set; }
		public List<string> EnumValues { get; set; }
	}
}
