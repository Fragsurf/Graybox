
using Graybox.Interface;
using SkiaSharp;
using System.Collections.Generic;

namespace Graybox.Editor.UI.TestElements
{
	internal class FormRow : UIElement
	{

		public string Label
		{
			get => labelElement.Text;
			set => labelElement.Text = value;
		}

		TextElement labelElement;
		UIElement controlHolder;
		UIElement controlElement;
		float controlWidth = 200;

		public FormRow()
		{
			Grow = 0;
			Shrink = 0;
			Width = Length.Percent( 100 );
			AlignItems = FlexAlign.Center;
			JustifyContent = FlexJustify.Center;
			BorderTopWidth = 1;
			BorderColor = Graybox.Interface.Theme.PopupBorderColor.Darken( .15f );

			Direction = FlexDirection.Row;
			labelElement = new TextElement();
			labelElement.Grow = 1;
			labelElement.Shrink = 0;
			Add( labelElement );

			controlHolder = new UIElement();
			controlHolder.MinWidth = 200;
			controlHolder.MaxWidth = 200;
			controlHolder.Grow = 1;
			controlHolder.Shrink = 0;
			controlHolder.Direction = FlexDirection.RowReverse;
			Add( controlHolder );
		}

		public FormRow( string label, UIElement control, float controlWidth = 200 ) : this()
		{
			this.controlWidth = controlWidth;
			Label = label;
			SetControl( control );
		}

		public void SetControl( UIElement control )
		{
			controlHolder.MinWidth = controlWidth;
			controlHolder.MaxWidth = controlWidth;

			if ( controlElement == control ) return;

			controlElement?.Remove();
			controlElement = control;

			if ( controlElement == null ) return;
			controlElement.Grow = 1;
			controlElement.Shrink = 0;
			controlHolder.Add( controlElement );
		}

	}
	internal class FormPageTest : UIElement
	{

		public FormPageTest()
		{
			Width = Length.Percent( 100 );
			Height = Length.Percent( 100 );

			Add( new TextElement()
			{
				Text = "Form elements",
				Margin = 10,
				FontSize = 20,
			} );

			Add( new FormRow( "Text Input", new TextEntryElement()
			{
				Text = "This is a text entry",
				BorderRadius = 4,
				Padding = 4,
				Height = 24,
			} )
			{ Padding = 4 } );

			Add( new FormRow( "Integer Input", new TextEntryElement()
			{
				Text = "22",
				Mode = TextEntryModes.Integer,
				BorderRadius = 4,
				Padding = 4,
				Height = 24,
				Centered = true,
				ShowArrows = true
			} )
			{ Padding = 4 } );

			Add( new FormRow( "Vector Input", new VectorEntryElement()
			{
				BorderRadius = 4,
				Padding = 4,
				Height = 24,
			} )
			{ Padding = 4 } );

			Add( new FormRow( "Float Input", new TextEntryElement()
			{
				Text = "22.32",
				Mode = TextEntryModes.Float,
				Padding = 4,
				Height = 24,
				Centered = true,
				ShowArrows = true
			} )
			{ Padding = 4 } );

			var combo = new ComboBoxElement()
			{
				Text = "Combo Option 1",
				Editable = false,
				Selectable = false,
				Cursor = CursorTypes.Pointer,
				BorderRadius = 4,
				Padding = 4,
				Options = new List<string>()
				{
					"Taco Bell",
					"Panda Express",
					"Panera Bread",
					"Paul Bunyans Burgers"
				},
			};

			combo.SetValue( combo.Options[3], false );

			Add( new FormRow( "Combo Box", combo ) { Padding = 4 } );
			Add( new FormRow( "EMPTY", null ) { Padding = 4 } );
			Add( new FormRow( "Checkbox", new CheckboxElement() ) { Padding = 4 } );
			Add( new FormRow( "Button", new ButtonElement( "Button 1" ) ) { Padding = 4 } );
			Add( new FormRow( "Dim Button", new ButtonElement.Dim( "Button 2" ) ) { Padding = 4 } );
			Add( new FormRow( "EMPTY", null ) { Padding = 4 } );
			Add( new FormRow( "Slider", new SliderElement() ) { Padding = 4 } );
		}

	}
}
