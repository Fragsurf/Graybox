
using Graybox.Editor.Documents;
using ImGuiNET;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Graybox.Editor
{
	internal static class ImObjectEditor
	{

		private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();
		private const float LabelWidth = 150f;
		private static readonly Dictionary<string, DateTime> LastEditTimes = new Dictionary<string, DateTime>();
		private const double DebounceTime = 0.5; // 500ms debounce time

		public static bool EditObject( object obj )
		{
			return EditObjectInternal( ref obj, string.Empty );
		}

		public static bool EditObject<T>( ref T obj )
		{
			return EditObjectInternal( ref obj, string.Empty );
		}

		private static bool EditObjectInternal<T>( ref T obj, string prefix )
		{
			bool anyPropertyChanged = false;
			var type = obj.GetType();
			if ( !PropertyCache.TryGetValue( type, out var properties ) )
			{
				properties = type.GetProperties( BindingFlags.Public | BindingFlags.Instance )
								 .Where( p => p.CanRead && p.CanWrite && p.GetCustomAttribute<HideInEditorAttribute>() == null )
								 .ToArray();
				PropertyCache[type] = properties;
			}

			foreach ( var property in properties )
			{
				var propertyName = string.IsNullOrEmpty( prefix ) ? property.Name : $"{prefix}.{property.Name}";
				var propertyType = property.PropertyType;
				var rangeAttribute = property.GetCustomAttribute<RangeAttribute>();

				ImGui.AlignTextToFramePadding();
				ImGui.Text( propertyName );
				ImGui.SameLine( LabelWidth );
				ImGui.PushItemWidth( ImGui.GetContentRegionAvail().X );

				var value = property.GetValue( obj );
				bool changed = false;

				if ( IsCustomValueType( propertyType ) )
				{
					ImGui.NewLine();
					ImGui.Indent();
					changed = EditCustomStruct( ref value, propertyType, propertyName );
					ImGui.Unindent();
				}
				else
				{
					changed = EditProperty( ref value, property, propertyName, rangeAttribute );
				}

				if ( changed )
				{
					property.SetValue( obj, value );
					anyPropertyChanged = true;
				}

				ImGui.PopItemWidth();
			}

			return anyPropertyChanged;
		}

		private static bool EditCustomStruct( ref object value, Type type, string prefix )
		{
			bool changed = false;
			var properties = type.GetProperties( BindingFlags.Public | BindingFlags.Instance )
								 .Where( p => p.CanRead && p.CanWrite );

			foreach ( var property in properties )
			{
				var propertyName = $"{prefix}.{property.Name}";
				var propertyValue = property.GetValue( value );
				var rangeAttribute = property.GetCustomAttribute<RangeAttribute>();

				ImGui.AlignTextToFramePadding();
				ImGui.Text( property.Name );
				ImGui.SameLine( LabelWidth );
				ImGui.PushItemWidth( ImGui.GetContentRegionAvail().X );

				bool propertyChanged = EditProperty( ref propertyValue, property, propertyName, rangeAttribute );

				if ( propertyChanged )
				{
					property.SetValue( value, propertyValue );
					changed = true;
				}

				ImGui.PopItemWidth();
			}

			return changed;
		}

		private static bool IsCustomValueType( Type type )
		{
			return type.IsValueType && !type.IsPrimitive && !type.IsEnum &&
				   type != typeof( decimal ) && type != typeof( Vector2 ) &&
				   type != typeof( Vector3 ) && type != typeof( Vector4 ) &&
				   type != typeof( Quaternion ) && type != typeof( Color4 );
		}

		private static bool EditProperty( ref object value, PropertyInfo property, string propertyName, RangeAttribute rangeAttribute )
		{
			bool valueChanged = false;

			ImGui.BeginGroup();

			var propertyType = property.PropertyType;
			if ( propertyType == typeof( int ) )
				valueChanged |= EditIntProperty( ref value, propertyName, rangeAttribute );
			else if ( propertyType == typeof( float ) )
				valueChanged |= EditFloatProperty( ref value, propertyName, rangeAttribute );
			else if ( propertyType == typeof( string ) )
			{
				if ( property.GetCustomAttribute<EditAsAssetAttribute>() != null )
					valueChanged |= EditAssetProperty( ref value, propertyName );
				else
					valueChanged |= EditStringProperty( ref value, propertyName );
			}
			else if ( propertyType == typeof( bool ) )
				valueChanged |= EditBoolProperty( ref value, propertyName );
			else if ( propertyType == typeof( Vector2 ) )
				valueChanged |= EditVector2Property( ref value, propertyName );
			else if ( propertyType == typeof( Vector3 ) )
				valueChanged |= EditVector3Property( ref value, propertyName );
			else if ( propertyType == typeof( Vector4 ) )
				valueChanged |= EditVector4Property( ref value, propertyName );
			else if ( propertyType == typeof( Color4 ) )
				valueChanged |= EditColorProperty( ref value, propertyName );
			else if ( propertyType.IsEnum )
				valueChanged |= EditEnumProperty( ref value, propertyName, propertyType );
			else
				ImGui.Text( $"Unsupported type: {propertyType.Name}" );

			ImGui.EndGroup();

			bool openContextMenu = ImGui.BeginPopupContextItem( propertyName );

			if ( openContextMenu )
			{
				if ( ImGui.MenuItem( "Reset To Default" ) )
				{
					value = GetDefaultValue( property.PropertyType );
					valueChanged = true;
				}
				ImGui.EndPopup();
			}

			return valueChanged;
		}

		private static object GetDefaultValue( Type type )
		{
			if ( type.IsValueType )
				return Activator.CreateInstance( type );
			else
				return null;
		}

		private static bool EditEnumProperty( ref object value, string propertyName, Type enumType )
		{
			string[] enumNames = Enum.GetNames( enumType );
			int currentIndex = Array.IndexOf( enumNames, value.ToString() );

			if ( ImGui.BeginCombo( $"###{propertyName}", enumNames[currentIndex] ) )
			{
				bool changed = false;
				for ( int i = 0; i < enumNames.Length; i++ )
				{
					bool isSelected = (i == currentIndex);
					if ( ImGui.Selectable( enumNames[i], isSelected ) )
					{
						value = Enum.Parse( enumType, enumNames[i] );
						changed = true;
					}

					if ( isSelected )
					{
						ImGui.SetItemDefaultFocus();
					}
				}
				ImGui.EndCombo();
				return changed;
			}

			return false;
		}

		private static bool EditIntProperty( ref object value, string propertyName, RangeAttribute rangeAttribute )
		{
			int intValue = (int)value;
			var (minRange, maxRange) = GetRangeValues( rangeAttribute );
			bool changed = ImGui.DragInt( $"###{propertyName}", ref intValue, 1, (int)minRange, (int)maxRange );
			if ( changed ) value = intValue;
			return changed;
		}

		private static bool EditFloatProperty( ref object value, string propertyName, RangeAttribute rangeAttribute )
		{
			float floatValue = (float)value;
			var (minRange, maxRange) = GetRangeValues( rangeAttribute );
			bool changed = ImGui.DragFloat( $"###{propertyName}", ref floatValue, 0.1f, minRange, maxRange );
			if ( changed ) value = floatValue;
			return changed;
		}

		private static bool EditStringProperty( ref object value, string propertyName )
		{
			string stringValue = (string)value ?? string.Empty;
			bool changed = ImGui.InputText( $"###{propertyName}", ref stringValue, 100 );
			if ( changed ) value = stringValue;
			return changed;
		}

		private static bool EditAssetProperty( ref object value, string propertyName )
		{
			string stringValue = (string)value ?? string.Empty;
			var oldValue = $"{stringValue}";
			ImGuiEx.EditAsset( $"###{propertyName}", DocumentManager.CurrentDocument?.AssetSystem ?? new(), ref stringValue );
			var changed = oldValue != stringValue;
			if ( changed ) value = stringValue;
			return changed;
		}

		private static bool EditBoolProperty( ref object value, string propertyName )
		{
			bool boolValue = (bool)value;
			bool changed = ImGui.Checkbox( $"###{propertyName}", ref boolValue );
			if ( changed ) value = boolValue;
			return changed;
		}

		private static bool EditVector2Property( ref object value, string propertyName )
		{
			Vector2 vector3Value = (Vector2)value;
			var svec = new SVector2( vector3Value.X, vector3Value.Y );
			bool changed = ImGui.DragFloat2( $"###{propertyName}", ref svec, 0.1f );
			if ( changed ) value = new Vector2( svec.X, svec.Y );
			return changed;
		}

		private static bool EditVector3Property( ref object value, string propertyName )
		{
			Vector3 vector3Value = (Vector3)value;
			var svec = new SVector3( vector3Value.X, vector3Value.Y, vector3Value.Z );
			bool changed = ImGui.DragFloat3( $"###{propertyName}", ref svec, 0.1f );
			if ( changed ) value = new Vector3( svec.X, svec.Y, svec.Z );
			return changed;
		}

		private static bool EditVector4Property( ref object value, string propertyName )
		{
			Vector4 vector3Value = (Vector4)value;
			var svec = new SVector4( vector3Value.X, vector3Value.Y, vector3Value.Z, vector3Value.W );
			bool changed = ImGui.DragFloat4( $"###{propertyName}", ref svec, 0.1f );
			if ( changed ) value = new Vector4( svec.X, svec.Y, svec.Z, svec.W );
			return changed;
		}

		private static bool EditColorProperty( ref object value, string propertyName )
		{
			Color4 colorValue = (Color4)value;
			var svec = new SVector4( colorValue.R, colorValue.G, colorValue.B, colorValue.A );
			bool changed = ImGui.ColorEdit4( $"###{propertyName}", ref svec );
			if ( changed ) value = new Color4( svec.X, svec.Y, svec.Z, svec.W );
			return changed;
		}

		private static bool IsDebounced( string propertyName )
		{
			if ( !LastEditTimes.TryGetValue( propertyName, out DateTime lastEditTime ) )
			{
				LastEditTimes[propertyName] = DateTime.Now;
				return true;
			}

			if ( (DateTime.Now - lastEditTime).TotalSeconds >= DebounceTime )
			{
				LastEditTimes[propertyName] = DateTime.Now;
				return true;
			}

			return false;
		}

		private static (float minRange, float maxRange) GetRangeValues( RangeAttribute rangeAttribute )
		{
			float minRange = float.MinValue, maxRange = float.MaxValue;
			if ( rangeAttribute != null )
			{
				minRange = Convert.ToSingle( rangeAttribute.Minimum );
				maxRange = Convert.ToSingle( rangeAttribute.Maximum );
			}
			return (minRange, maxRange);
		}
	}
}
