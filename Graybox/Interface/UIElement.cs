

using Facebook.Yoga;
using SkiaSharp;

namespace Graybox.Interface
{

	public class UIElementPaintEvent
	{
		public SKCanvas Canvas;
		public UIElement Element;
		public bool PaintDefault = true;
	}

	public partial class UIElement
	{

		IWindow window;
		public IWindow Window
		{
			get => window ?? FindRoot()?.window;
			set => window = value;
		}

		public Action<UIElementPaintEvent> OnPaint;
		public Action<UIElementPaintEvent> AfterPaint;

		public bool IsRoot { get; set; }
		public string Name { get; set; }
		public virtual CursorTypes Cursor { get; set; }
		public PointerEvents PointerEvents { get; set; } = PointerEvents.All;
		public DisplayModes Display { get; set; }
		public Length Width { get; set; } = Length.Undefined;
		public Length Height { get; set; } = Length.Undefined;
		public Length MinWidth { get; set; } = Length.Undefined;
		public Length MinHeight { get; set; } = Length.Undefined;
		public Length MaxWidth { get; set; } = Length.Undefined;
		public Length MaxHeight { get; set; } = Length.Undefined;
		public Length Left { get; set; } = Length.Undefined;
		public Length Right { get; set; } = Length.Undefined;
		public Length Top { get; set; } = Length.Undefined;
		public Length Bottom { get; set; } = Length.Undefined;
		public FlexDirection Direction { get; set; }
		public WrapModes Wrap { get; set; } = WrapModes.NoWrap;
		public PositionType Position { get; set; } = PositionType.Relative;
		public OverflowTypes Overflow { get; set; } = OverflowTypes.Visible;
		public FlexAlign AlignContent { get; set; } = FlexAlign.Stretch;
		public FlexAlign AlignItems { get; set; } = FlexAlign.Stretch;
		public FlexAlign AlignSelf { get; set; } = FlexAlign.Auto;
		public FlexJustify JustifyContent { get; set; } = FlexJustify.FlexStart;
		public int Grow { get; set; } = 0;
		public int Shrink { get; set; } = 1;
		public Length Basis { get; set; } = Length.Undefined;
		public SKColor BackgroundColor { get; set; } = SKColors.Transparent;
		public SKColor? BackgroundHoverColor { get; set; }
		public float BorderRadius { get; set; } = 0f;
		public bool BoxShadow { get; set; } = false;
		public float BorderWidth
		{
			set
			{
				BorderTopWidth = value;
				BorderBottomWidth = value;
				BorderLeftWidth = value;
				BorderRightWidth = value;
			}
		}

		public Length Margin
		{
			set
			{
				MarginLeft = value;
				MarginRight = value;
				MarginTop = value;
				MarginBottom = value;
			}
		}

		public Length MarginLeft { get; set; } = Length.Undefined;
		public Length MarginRight { get; set; } = Length.Undefined;
		public Length MarginTop { get; set; } = Length.Undefined;
		public Length MarginBottom { get; set; } = Length.Undefined;

		public Length Padding
		{
			set
			{
				PaddingLeft = value;
				PaddingRight = value;
				PaddingTop = value;
				PaddingBottom = value;
			}
		}

		public Length PaddingLeft { get; set; } = Length.Undefined;
		public Length PaddingRight { get; set; } = Length.Undefined;
		public Length PaddingTop { get; set; } = Length.Undefined;
		public Length PaddingBottom { get; set; } = Length.Undefined;

		public float BorderLeftWidth { get; set; }
		public float BorderRightWidth { get; set; }
		public float BorderTopWidth { get; set; }
		public float BorderBottomWidth { get; set; }
		public SKColor BorderColor { get; set; }
		public SKColor? BorderHoverColor { get; set; }

		public IReadOnlyList<UIElement> Children => children;
		public bool HasChildren => children != null && children.Count > 0;

		public UIElement Parent { get; private set; }
		public SKRect BoxRect { get; private set; }
		public SKRect PaddedRect { get; private set; }
		public SKRect MarginRect { get; private set; }

		public string Tooltip { get; set; }

		public Action OnUpdate;

		List<UIElement> children;
		protected YogaNode Node;

		static YogaConfig _cfg;
		static YogaConfig Config
		{
			get
			{
				if ( _cfg == null )
				{
					_cfg = new YogaConfig()
					{
						UseWebDefaults = false,
						PointScaleFactor = 1f,
					};
				}
				return _cfg;
			}
		}

		bool NeedsLayout = false;
		int DrawCount;

		public float CalculateChildrenHeight()
		{
			var minY = float.MaxValue;
			var maxY = float.MinValue;

			foreach ( var child in children )
			{
				if ( child.Display == DisplayModes.None )
					continue;
				if ( child.Position == PositionType.Absolute )
					continue;
				if ( child.MarginRect.Top < minY )
					minY = child.MarginRect.Top;
				if ( child.MarginRect.Bottom > maxY )
					maxY = child.MarginRect.Bottom;
			}
			return maxY - minY;
		}

		public UIElement()
		{
			children = new List<UIElement>();
			Node = new YogaNode( Config );
		}

		public UIElement FindRoot()
		{
			var parent = this;
			while ( parent != null )
			{
				if ( parent.IsRoot )
					return parent;
				parent = parent.Parent;
			}
			return this;
		}

		public void MarkLayoutDirty()
		{
			NeedsLayout = true;
		}

		protected virtual void Update()
		{

		}

		public void HandleUpdate()
		{
			if ( IsHovered && !string.IsNullOrEmpty( Tooltip ) )
			{
				var timeSinceHover = (DateTime.Now - tooltipDecay).Milliseconds;
				if ( timeSinceHover > 350 && !hasTooltip )
				{
					Window?.ShowTooltip( Tooltip );
					hasTooltip = true;
				}
			}
			else
			{
				if ( hasTooltip )
				{
					Window?.HideTooltip();
				}
				hasTooltip = false;
			}

			if ( IsRoot && NeedsLayout )
			{
				CalculateLayout();
			}

			CheckForChanges();
			UpdateScrollVelocity();
			Update();

			OnUpdate?.Invoke();

			foreach ( var child in children )
			{
				child.HandleUpdate();
			}
		}

		public void AddSpace( int space ) => Add( new UIElement() { Margin = space, Shrink = 0 } );
		public void AddGrow() => Add( new UIElement() { Grow = 1, Shrink = 0, PointerEvents = PointerEvents.None } );

		public T Add<T>( T element ) where T : UIElement
		{
			if ( children.Contains( element ) ) return element;

			element.Parent = this;
			children.Add( element );
			Node.AddChild( element.Node );
			
			Refresh();
			FindRoot().MarkLayoutDirty();

			return element;
		}

		public void Remove( UIElement element )
		{
			if ( !children.Contains( element ) ) return;

			children.Remove( element );
			Node.RemoveChild( element.Node );

			Refresh();
			FindRoot().MarkLayoutDirty();
		}

		internal void UpdateYogaNode()
		{
			Node.Display = (YogaDisplay)Display;
			Node.Width = Width;
			Node.Height = Height;
			Node.Wrap = (YogaWrap)Wrap;
			Node.PositionType = (YogaPositionType)Position;
			Node.FlexDirection = (YogaFlexDirection)Direction;
			Node.AlignContent = (YogaAlign)AlignContent;
			Node.AlignItems = (YogaAlign)AlignItems;
			Node.AlignSelf = (YogaAlign)AlignSelf;
			Node.JustifyContent = (YogaJustify)JustifyContent;
			Node.Overflow = (YogaOverflow)Overflow;
			Node.MinWidth = MinWidth;
			Node.MinHeight = MinHeight;
			Node.FlexGrow = Grow;
			Node.FlexShrink = Shrink;
			Node.FlexBasis = Basis;
			Node.MaxWidth = MaxWidth;
			Node.MaxHeight = MaxHeight;
			Node.MarginLeft = MarginLeft;
			Node.MarginRight = MarginRight;
			Node.MarginTop = MarginTop;
			Node.MarginBottom = MarginBottom;
			Node.PaddingLeft = PaddingLeft;
			Node.PaddingRight = PaddingRight;
			Node.PaddingTop = PaddingTop;
			Node.PaddingBottom = PaddingBottom;
			Node.BorderLeftWidth = BorderLeftWidth;
			Node.BorderRightWidth = BorderRightWidth;
			Node.BorderTopWidth = BorderTopWidth;
			Node.BorderBottomWidth = BorderBottomWidth;
			Node.Left = Left;
			Node.Right = Right;
			Node.Top = Top;
			Node.Bottom = Bottom;

			foreach ( var child in children )
			{
				child.UpdateYogaNode();
			}
		}

		public void CalculateLayout()
		{
			NeedsLayout = false;

			UpdateYogaNode();

			Node.CalculateLayout();

			OnLayoutCalculated();

			Window?.Invalidate( this );
		}

		void OnLayoutCalculated()
		{
			if ( Display == DisplayModes.None ) return;

			UpdateRects();

			foreach ( var child in children )
			{
				child.OnLayoutCalculated();
			}
		}

		public void Refresh()
		{
			//FindRoot()?.MarkLayoutDirty();
			Window?.Invalidate( this );
		}

		protected virtual int CalculateLayoutHash()
		{
			var result = HashCode.Combine( Width, Height, MaxWidth, MaxHeight, Left, Right, Top, Bottom );
			result = HashCode.Combine( result, AlignSelf, Grow, Shrink, Basis, MarginLeft, MarginRight, MarginTop );
			result = HashCode.Combine( result, MarginBottom, PaddingLeft, PaddingRight, PaddingTop, PaddingBottom );
			return result;
		}

		public void PaintCanvas( SKCanvas canvas, SKRect? dirtyRect = null )
		{
			if ( Display == DisplayModes.None ) return;

			var marginRect = MarginRect;
			if ( BoxShadow ) marginRect.Inflate( 1, 1 );

			var shouldPaint = (dirtyRect == null) || marginRect.IntersectsWith( dirtyRect.Value );
			var restoreState = -1;

			if ( shouldPaint )
			{
				Paint( canvas );

				var clip = Overflow == OverflowTypes.Hidden;
				var scroll = Overflow == OverflowTypes.Scroll;

				if ( clip || scroll )
				{
					restoreState = canvas.Save();
					if ( BorderRadius > 0 )
					{
						using ( var rect = new SKRoundRect( BoxRect, BorderRadius ) )
						{
							canvas.ClipRoundRect( rect, SKClipOperation.Intersect );
						}
					}
					else
					{
						canvas.ClipRect( BoxRect, SKClipOperation.Intersect );
					}

					if ( scroll )
					{
						canvas.Translate( ScrollPosition );

						if ( dirtyRect.HasValue )
						{
							dirtyRect = new SKRect(
								dirtyRect.Value.Left - ScrollPosition.X,
								dirtyRect.Value.Top - ScrollPosition.Y,
								dirtyRect.Value.Right - ScrollPosition.X,
								dirtyRect.Value.Bottom - ScrollPosition.Y
							);
						}
					}
				}
			}

			foreach ( var child in children )
			{
				child.PaintCanvas( canvas, dirtyRect );
			}

			if ( IsRoot )
			{
				AfterPaint?.Invoke( new UIElementPaintEvent()
				{
					Canvas = canvas,
					Element = this
				} );
			}

			if ( restoreState != -1 )
			{
				canvas.Restore();
			}
		}

		protected virtual void Paint( SKCanvas canvas )
		{
			DrawCount++;

			DrawBoxShadow( canvas );
			DrawBackground( canvas );
			DrawBorders( canvas );

			if ( OnPaint != null )
			{
				var paintEvent = new UIElementPaintEvent()
				{
					Canvas = canvas,
					Element = this
				};

				OnPaint.Invoke( paintEvent );

				if ( !paintEvent.PaintDefault ) return;
			}
		}

		public void Remove()
		{
			ReleaseMouse();
			Parent?.Remove( this );
		}

		public void Clear()
		{
			foreach ( var child in children.ToList() )
			{
				child.Remove();
			}
			children.Clear();
		}

		private void DrawBoxShadow( SKCanvas canvas )
		{
			if ( !BoxShadow ) return;

			var shadowColor = new SKColor( 0, 0, 0, 200 );
			var shadowOffset = new SKPoint( 2, 2 );
			var shadowBlur = 4.0f;

			using ( var paint = new SKPaint() )
			using ( var filter = SKImageFilter.CreateDropShadowOnly( shadowOffset.X, shadowOffset.Y, shadowBlur, shadowBlur, shadowColor ) )
			{
				paint.IsAntialias = true;
				paint.ImageFilter = filter;

				if ( BorderRadius > 0 )
				{
					canvas.DrawRoundRect( BoxRect, new SKSize( BorderRadius, BorderRadius ), paint );
				}
				else
				{
					canvas.DrawRect( BoxRect, paint );
				}
			}
		}

		private void DrawBackground( SKCanvas canvas )
		{
			var backgroundColor = IsHovered && BackgroundHoverColor != null
				? BackgroundHoverColor.Value : BackgroundColor;

			if ( backgroundColor.Alpha == 0 ) return;

			using ( var paint = new SKPaint() )
			{
				paint.Color = backgroundColor;
				paint.IsAntialias = true;

				if ( BorderRadius > 0 )
				{
					canvas.DrawRoundRect( BoxRect, new SKSize( BorderRadius, BorderRadius ), paint );
				}
				else
				{
					canvas.DrawRect( BoxRect, paint );
				}
			}
		}

		private void DrawBorders( SKCanvas canvas )
		{
			var color = IsHovered && BorderHoverColor != null
				? BorderHoverColor.Value : BorderColor;

			if ( BorderLeftWidth <= 0 && BorderRightWidth <= 0 && BorderTopWidth <= 0 && BorderBottomWidth <= 0 )
				return;
			if ( color.Alpha < 0 )
				return;

			var rect = BoxRect;
			rect.Left = ((int)rect.Left) + 0.5f;
			rect.Right = ((int)rect.Right) - 0.5f;
			rect.Top = ((int)rect.Top) + 0.5f;
			rect.Bottom = ((int)rect.Bottom) - 0.5f;

			if ( BorderRadius > 0 )
			{
				using ( var roundedRect = new SKRoundRect( rect, BorderRadius, BorderRadius ) )
				using ( var borderPaint = new SKPaint() )
				{
					borderPaint.IsAntialias = true;
					borderPaint.Style = SKPaintStyle.Stroke;
					borderPaint.Color = color;
					borderPaint.StrokeJoin = SKStrokeJoin.Miter;
					borderPaint.StrokeCap = SKStrokeCap.Square;

					var strokeWidth = BorderLeftWidth;
					strokeWidth = Math.Max( strokeWidth, BorderRightWidth );
					strokeWidth = Math.Max( strokeWidth, BorderTopWidth );
					strokeWidth = Math.Max( strokeWidth, BorderBottomWidth );

					borderPaint.StrokeWidth = strokeWidth;

					canvas.DrawRoundRect( roundedRect, borderPaint );
				}
			}
			else
			{
				using ( var borderPaint = new SKPaint() )
				{
					borderPaint.IsAntialias = true;
					borderPaint.Style = SKPaintStyle.Stroke;
					borderPaint.Color = color;
					borderPaint.StrokeJoin = SKStrokeJoin.Miter;
					borderPaint.StrokeCap = SKStrokeCap.Square;

					var left = rect.Left;
					var top = rect.Top;
					var right = rect.Right;
					var bottom = rect.Bottom;

					if ( BorderTopWidth > 0 )
					{
						borderPaint.StrokeWidth = BorderTopWidth;
						canvas.DrawLine( left, top, right, top, borderPaint );
					}

					if ( BorderRightWidth > 0 )
					{
						borderPaint.StrokeWidth = BorderRightWidth;
						canvas.DrawLine( right, top, right, bottom, borderPaint );
					}

					if ( BorderBottomWidth > 0 )
					{
						borderPaint.StrokeWidth = BorderBottomWidth;
						canvas.DrawLine( left, bottom, right, bottom, borderPaint );
					}

					if ( BorderLeftWidth > 0 )
					{
						borderPaint.StrokeWidth = BorderLeftWidth;
						canvas.DrawLine( left, top, left, bottom, borderPaint );
					}
				}
			}
		}

		void UpdateRects()
		{
			var left = Node.LayoutX;
			var top = Node.LayoutY;
			var width = Node.LayoutWidth;
			var height = Node.LayoutHeight;

			if ( Parent != null )
			{
				left += Parent.BoxRect.Left;
				top += Parent.BoxRect.Top;
			}

			var right = left + width;
			var bottom = top + height;

			BoxRect = new SKRect( left, top, right, bottom );

			var paddedLeft = left + Node.LayoutPaddingLeft;
			var paddedTop = top + Node.LayoutPaddingTop;
			var paddedRight = right - Node.LayoutPaddingRight;
			var paddedBottom = bottom - Node.LayoutPaddingBottom;

			PaddedRect = new SKRect( paddedLeft, paddedTop, paddedRight, paddedBottom );

			var marginLeft = left - Node.LayoutMarginLeft;
			var marginTop = top - Node.LayoutMarginTop;
			var marginRight = right + Node.LayoutMarginRight;
			var marginBottom = bottom + Node.LayoutMarginBottom;

			MarginRect = new SKRect( marginLeft, marginTop, marginRight, marginBottom );
		}

		UIElement MouseCaptureElement;
		public void CaptureMouse()
		{
			var root = FindRoot();
			root.MouseCaptureElement = this;
			HasMouseCapture = true;
		}

		public void ReleaseMouse()
		{
			var root = FindRoot();
			if ( root.MouseCaptureElement == this )
			{
				HasMouseCapture = false;
				root.MouseCaptureElement = null;
			}
		}

		public SKPoint ToScreen( SKPoint point )
		{
			if ( Window == null ) return point;
			var screenPos = Window.ToScreen( point );
			return new SKPoint( screenPos.X, screenPos.Y );
		}

		public SKRect ToScreen( SKRect rect )
		{
			if ( Window == null ) return rect;
			var screenRect = Window.ToScreen( rect );
			return new SKRect( screenRect.Left, screenRect.Top, screenRect.Right, screenRect.Bottom );
		}

		public SKPoint CalculateScrollOffset()
		{
			var scrollOffset = SKPoint.Empty;
			var parent = Parent;

			while ( parent != null )
			{
				scrollOffset += parent.ScrollPosition;
				parent = parent.Parent;
			}

			return scrollOffset;
		}

	}

	public enum LengthTypes
	{
		Point,
		Percent,
		Undefined,
		Auto
	}

	public struct Length
	{

		public LengthTypes Type;
		public float Value;

		public static Length Auto => new Length()
		{
			Type = LengthTypes.Auto,
			Value = 0
		};

		public static Length Undefined => new Length()
		{
			Type = LengthTypes.Undefined,
			Value = float.NaN
		};

		public static implicit operator YogaValue( Length length )
		{
			switch ( length.Type )
			{
				case LengthTypes.Undefined:
					return YogaValue.Undefined();
				case LengthTypes.Percent:
					return YogaValue.Percent( length.Value );
				case LengthTypes.Point:
					return YogaValue.Point( length.Value );
				case LengthTypes.Auto:
				default:
					return YogaValue.Auto();
			}
		}

		public static explicit operator Length( YogaValue yogaValue )
		{
			switch ( yogaValue.Unit )
			{
				case YogaUnit.Percent:
					return new Length { Type = LengthTypes.Percent, Value = yogaValue.Value };
				case YogaUnit.Point:
					return new Length { Type = LengthTypes.Point, Value = yogaValue.Value };
				case YogaUnit.Auto:
					return new Length { Type = LengthTypes.Auto, Value = 0 };
				default:
					return Undefined;
			}
		}

		public static implicit operator Length( int point ) => (Length)YogaValue.Point( point );
		public static implicit operator Length( float point ) => (Length)YogaValue.Point( point );
		public static Length Percent( float value ) => (Length)YogaValue.Percent( value );
		public static Length Point( float value ) => (Length)YogaValue.Point( value );
	}

	public enum PositionType
	{
		Relative = 0,
		Absolute = 1
	}

	public enum FlexDirection
	{
		Column = 0,
		ColumnReverse = 1,
		Row = 2,
		RowReverse = 3
	}

	public enum FlexAlign
	{
		Auto,
		FlexStart,
		Center,
		FlexEnd,
		Stretch,
		Baseline,
		SpaceBetween,
		SpaceAround
	}

	public enum FlexJustify
	{
		FlexStart,
		Center,
		FlexEnd,
		SpaceBetween,
		SpaceAround
	}

	public enum WrapModes
	{
		NoWrap,
		Wrap,
		WrapReverse
	}

	public enum DisplayModes
	{
		Flex,
		None
	}

	public enum PointerEvents
	{
		All,
		None
	}

	public enum CursorTypes
	{
		Default,
		Pointer,
		Text
	}

	public enum OverflowTypes
	{
		Visible,
		Hidden,
		Scroll
	}

	public struct WindowOptions
	{
		public int Width;
		public int Height;
		public string Title;
		public bool HideTitleBar;
		public bool Resizable;
		public bool IsDialog;
		public bool IsPopup;
		public bool IsTooltip;
		public bool TransparentBackground;
		public bool CenterOnScreen;
		public SKPoint? Position;
	}

}
