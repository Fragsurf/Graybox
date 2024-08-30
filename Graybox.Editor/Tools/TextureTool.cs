
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Actions.MapObjects.Operations;
using Graybox.Editor.Documents;
using System.Drawing;
using Graybox.Graphics.Immediate;
using Graybox.Editor.Actions;
using Graybox.Scenes;
using ImGuiNET;
using System.Text.Json;

namespace Graybox.Editor.Tools;

public class TextureTool : BaseTool
{

	struct TextureProperties
	{
		public Vector2 Shift;
		public Vector2 Scale;
		public float Rotation;
		public bool AlignedToWorld;

		public int CalculateHash()
		{
			return HashCode.Combine( Shift, Scale, Rotation );
		}
	}

	public override string Name => "Texture Tool";
	public override string EditorIcon => "assets/icons/tool_texture.png";

	Face HoveredFace;
	string[] TexturePalette = new string[5];
	int TexturePaletteIndex = 0;
	int FitTileX = 1;
	int FitTileY = 1;
	bool TreatAsOne = true;
	TextureProperties LastTextureProperties;

	TextureProperties GetTextureProperties()
	{
		var selection = Document.Selection.GetSelectedFaces();
		var count = selection.Count();

		if ( count == 0 ) return default;

		var item = selection.First();
		var result = new TextureProperties()
		{
			Shift = new Vector2( item.TextureRef.XShift, item.TextureRef.YShift ),
			Scale = new Vector2( item.TextureRef.XScale, item.TextureRef.YScale ),
			Rotation = item.TextureRef.Rotation,
			AlignedToWorld = item.IsTextureAlignedToWorld()
		};

		foreach ( var face in selection )
		{
			if ( face.TextureRef.XShift != result.Shift.X ) result.Shift.X = float.NaN;
			if ( face.TextureRef.YShift != result.Shift.Y ) result.Shift.Y = float.NaN;
			if ( face.TextureRef.XScale != result.Scale.X ) result.Scale.X = float.NaN;
			if ( face.TextureRef.YScale != result.Scale.Y ) result.Scale.Y = float.NaN;
			if ( face.TextureRef.Rotation != result.Rotation ) result.Rotation = float.NaN;
			if ( face.IsTextureAlignedToWorld() != result.AlignedToWorld ) result.AlignedToWorld = false;
		}

		return result;
	}

	void ApplyTextureProperties( TextureProperties properties )
	{
		var selection = Document?.Selection?.GetSelectedFaces();

		if ( selection == null || !selection.Any() )
		{
			return;
		}

		Action<Document, Face> action = ( d, f ) =>
		{
			if ( !properties.Scale.X.IsNaN() ) f.TextureRef.XScale = properties.Scale.X;
			if ( !properties.Scale.Y.IsNaN() ) f.TextureRef.YScale = properties.Scale.Y;
			if ( !properties.Shift.X.IsNaN() ) f.TextureRef.XShift = properties.Shift.X;
			if ( !properties.Shift.Y.IsNaN() ) f.TextureRef.YShift = properties.Shift.Y;
			if ( !properties.Rotation.IsNaN() ) f.SetTextureRotation( properties.Rotation );
			f.CalculateTextureCoordinates( true );
		};

		Document.PerformAction( "Apply Texture Properties", new EditFace( selection, action ) );
	}

	public override void UpdateWidget()
	{
		base.UpdateWidget();

		ImGuiEx.Header( "Texture Tool" );

		// Selected Texture section
		ImGuiEx.EditAssetGrid( "Texture", DocumentManager.CurrentDocument.AssetSystem, TexturePalette, ref TexturePaletteIndex, 75f );

		if ( ImGui.Button( "Apply" ) )
		{
			var selection = DocumentManager.CurrentDocument?.Selection?.GetSelectedFaces();
			if ( selection?.Any() ?? false )
			{
				var tex = GetActiveTexture();
				if ( tex != null )
				{
					ApplyTexture( selection, tex );
				}
			}
		}

		var props = GetTextureProperties();
		var propHash = props.CalculateHash();

		if ( ImGui.IsAnyItemActive() )
		{
			props = LastTextureProperties;
		}

		// Face Properties section
		ImGuiEx.Header( "Texture Coordinates", true );

		if ( ImGui.SmallButton( "Scale Down" ) )
		{
			if ( props.Scale.X.IsNaN() ) props.Scale.X = 1.0f;
			if ( props.Scale.Y.IsNaN() ) props.Scale.Y = 1.0f;

			props.Scale /= 2;
		}

		ImGui.SameLine();
		if ( ImGui.SmallButton( "Scale Up" ) )
		{
			if ( props.Scale.X.IsNaN() ) props.Scale.X = 1.0f;
			if ( props.Scale.Y.IsNaN() ) props.Scale.Y = 1.0f;

			props.Scale *= 2;
		}

		ImGuiEx.EditVector2( "Scale", ref props.Scale, 0.1f, 0.1f, 10.0f );
		ImGuiEx.EditVector2( "Shift", ref props.Shift, 0.1f, -1024, 1024 );
		ImGuiEx.EditFloat( "Angle", ref props.Rotation, 1f, -180.0f, 180.0f );

		LastTextureProperties = props;

		var newPropHash = props.CalculateHash();
		if ( newPropHash != propHash && !ImGui.IsAnyItemActive() )
		{
			ApplyTextureProperties( props );
		}

		ImGui.Spacing();
		ImGui.Columns( 2, "texcolumns", false );
		ImGui.SetColumnWidth( 0, 110 );
		ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new SVector2( 0, 0 ) );
		{
			var buttonSize = new SVector2( 32, 32 );
			var buttonSpacing = 5.0f;
			var availWidth = ImGui.GetContentRegionAvail().X;
			var xPos = ImGui.GetCursorPosX();

			// Calculate position for Top button
			ImGui.SetCursorPosX( xPos + (availWidth - buttonSize.X) * 0.5f );

			if ( ImGui.Button( "T", buttonSize ) )
			{
				TextureJustified( JustifyMode.Top, TreatAsOne, 1, 1 );
			}

			float totalButtonWidth = buttonSize.X * 3 + buttonSpacing * 2;
			ImGui.SetCursorPosX( xPos + (availWidth - totalButtonWidth) * 0.5f );

			if ( ImGui.Button( "L", buttonSize ) )
			{
				TextureJustified( JustifyMode.Left, TreatAsOne, FitTileX, FitTileY );
			}
			ImGui.SameLine( 0, buttonSpacing );
			if ( ImGui.Button( "C", buttonSize ) )
			{
				TextureJustified( JustifyMode.Center, TreatAsOne, FitTileX, FitTileY );
			}
			ImGui.SameLine( 0, buttonSpacing );
			if ( ImGui.Button( "R", buttonSize ) )
			{
				TextureJustified( JustifyMode.Right, TreatAsOne, FitTileX, FitTileY );
			}

			// Calculate position for Bottom button
			ImGui.SetCursorPosX( xPos + (availWidth - buttonSize.X) * 0.5f );
			if ( ImGui.Button( "B", buttonSize ) )
			{
				TextureJustified( JustifyMode.Bottom, TreatAsOne, FitTileX, FitTileY );
			}
		}
		ImGui.PopStyleVar( 1 );

		ImGui.NextColumn();
		{
			var fitTile = new Vector2( FitTileX, FitTileY );
			if ( ImGui.Button( "Fit" ) )
			{
				TextureJustified( JustifyMode.Fit, TreatAsOne, FitTileX, FitTileY );
			}
			ImGui.SameLine();
			ImGuiEx.EditVector2Int( "", ref fitTile, 0.1f, 1, 10 );
			FitTileX = Math.Max( (int)fitTile.X, 1 );
			FitTileY = Math.Max( (int)fitTile.Y, 1 );

			var alignedToWorld = props.AlignedToWorld;
			if ( ImGui.Checkbox( "Align to World", ref alignedToWorld ) )
			{
				TextureAligned( alignedToWorld ? AlignMode.World : AlignMode.Face );
			}
			ImGui.Checkbox( "Treat as one", ref TreatAsOne );
		}

		ImGui.Columns( 1 );

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.TextDisabled( "Hotkeys:" );
		ImGui.TextDisabled( "Alt+Click: Apply to hovered face" );
		ImGui.TextDisabled( "Alt+1-5: Apply to selected faces" );
	}

	public void ApplySelection()
	{
		var tex = Document.SelectedTexture;
		if ( tex == null ) return;

		var ti = tex;

		Action<Document, Face> action = ( document, face ) =>
		{
			face.TextureRef.AssetPath = tex.Name;
			face.TextureRef.Texture = ti;
			face.CalculateTextureCoordinates( false );
		};

		Document.PerformAction( "Apply texture", new EditFace( Document.Selection.GetSelectedFaces(), action ) );
	}

	private void TextureJustified( JustifyMode justifymode, bool treatasone, int tileX, int tileY )
	{
		var faces = Document.Selection.GetSelectedFaces();
		if ( faces.Count() == 0 ) return;

		var boxAlignMode = (justifymode == JustifyMode.Fit)
							   ? Face.BoxAlignMode.Center // Don't care about the align mode when centering
							   : (Face.BoxAlignMode)Enum.Parse( typeof( Face.BoxAlignMode ), justifymode.ToString() );

		PointCloud cloud = null;
		Action<Document, Face> action;

		if ( treatasone )
		{
			// If we treat as one, it means we want to align to one great big cloud
			cloud = new PointCloud( faces.SelectMany( x => x.Vertices ).Select( x => x.Position ) );
		}

		if ( justifymode == JustifyMode.Fit )
		{
			action = ( d, x ) => x.FitTextureToPointCloud( cloud ?? new PointCloud( x.Vertices.Select( y => y.Position ) ), tileX, tileY );
		}
		else
		{
			action = ( d, x ) => x.AlignTextureWithPointCloud( cloud ?? new PointCloud( x.Vertices.Select( y => y.Position ) ), boxAlignMode );
		}

		//var alignmentParams = new TextureAlignmentTool.TextureAlignmentConfig()
		//{
		//	Faces = faces.ToList(),
		//	JustifyMode = justifymode,
		//	AlignMode = AlignMode.Face,
		//	TreatAsOne = TreatAsOne
		//};
		//TextureAlignmentTool.AlignTexture( alignmentParams );

		var solids = faces.Select( x => x.Parent ).Distinct();
		foreach ( var item in solids )
		{
			item.Refresh();
		}
		Document.PerformAction( "Align texture", new EditFace( faces, action ) );
	}

	private void TextureAligned( AlignMode align )
	{
		Action<Document, Face> action = ( document, face ) =>
		{
			if ( align == AlignMode.Face ) face.AlignTextureToFace();
			else if ( align == AlignMode.World ) face.AlignTextureToWorld();
			face.CalculateTextureCoordinates( false );
		};

		Document.PerformAction( "Align texture", new EditFace( Document.Selection.GetSelectedFaces(), action ) );
	}

	public override void MouseLeave( Scene scene, ref InputEvent e )
	{
		HoveredFace = null;
	}

	public override void MouseMove( Scene scene, ref InputEvent e )
	{
		var newFace = Document?.Trace?.FirstFace( scene, e.LocalMousePosition, true );

		if ( Input.IsDown( MouseButton.Left ) && newFace != null && e.Alt )
		{
			if ( GetActiveTexture() is TextureAsset tex )
			{
				ApplyTexture( new List<Face> { HoveredFace }, tex );
			}
		}

		if ( newFace == HoveredFace ) return;

		HoveredFace = newFace;
	}

	void ApplyTexture( Face face, TextureAsset asset ) => ApplyTexture( new Face[] { face }, asset );
	void ApplyTexture( IEnumerable<Face> faces, TextureAsset asset )
	{
		if ( (!faces?.Any() ?? false) || asset == null ) return;

		var applyTo = faces.Where( x => x.TextureRef.Texture != asset ).ToList();
		if ( !applyTo.Any() ) return;

		var ac = new ActionCollection();
		ac.Add( new EditFace( applyTo, ( document, face ) =>
		{
			face.TextureRef.AssetPath = asset.RelativePath;
			face.TextureRef.Texture = asset;
			//face.CalculateTextureCoordinates( true );
			//if ( behaviour == SelectBehaviour.ApplyWithValues && firstSelected != null )
			//{
			//	// Calculates the texture coordinates
			//	face.AlignTextureWithFace( firstSelected );
			//}
			//else if ( behaviour == SelectBehaviour.ApplyWithValues )
			//{
			//	face.Texture.XScale = _form.CurrentProperties.XScale;
			//	face.Texture.YScale = _form.CurrentProperties.YScale;
			//	face.Texture.XShift = _form.CurrentProperties.XShift;
			//	face.Texture.YShift = _form.CurrentProperties.YShift;
			//	face.SetTextureRotation( _form.CurrentProperties.Rotation );
			//}
			//else
			//{

			//}
		} ) );

		Document.PerformAction( "Apply Texture", ac );
	}

	public override void MouseDoubleClick( Scene scene, ref InputEvent e )
	{
		if ( HoveredFace?.Parent == null ) return;
		if ( e.Button != MouseButton.Left ) return;

		if ( e.Control )
		{
			var parentFaces = HoveredFace.Parent.Faces;
			Document.Selection.Select( parentFaces );
		}
		else
		{
			Document.Selection.Clear();
			Document.Selection.Select( HoveredFace.Parent.Faces );
		}

		e.Handled = true;
	}

	TextureAsset GetActiveTexture()
	{
		if ( TexturePaletteIndex < 0 || TexturePaletteIndex >= TexturePalette.Length ) return null;
		var path = TexturePalette[TexturePaletteIndex];
		return DocumentManager.CurrentDocument?.AssetSystem?.FindAsset<TextureAsset>( path );
	}

	TextureAsset GetTextureAtIndex( int index )
	{
		if ( index < 0 || index >= TexturePalette.Length ) return null;
		var path = TexturePalette[index];
		return DocumentManager.CurrentDocument?.AssetSystem?.FindAsset<TextureAsset>( path );
	}

	public override void MouseDown( Scene scene, ref InputEvent e )
	{
		if ( e.Button == MouseButton.Left && e.Alt )
		{
			if ( HoveredFace != null && GetActiveTexture() is TextureAsset tex )
			{
				ApplyTexture( new List<Face> { HoveredFace }, tex );
			}
			e.Handled = true;
			return;
		}

		if ( e.Button == MouseButton.Left )
		{
			if ( HoveredFace == null )
			{
				Document.Selection.Clear();
				return;
			}

			if ( e.Modifiers == OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Control )
			{
				if ( HoveredFace.IsSelected )
				{
					Document.Selection.Deselect( HoveredFace );
				}
				else
				{
					Document.Selection.Select( HoveredFace );
				}
			}
			else
			{
				Document.Selection.Clear();
				Document.Selection.Select( HoveredFace );
			}
		}
	}

	public override void KeyDown( Scene scene, ref InputEvent e )
	{
		if ( e.Key == Key.Escape )
		{
			Document.Selection.Clear();
			e.Handled = true;
			return;
		}

		if ( e.Alt && e.Key > Key.D0 && e.Key <= Key.D9 )
		{
			var selectedFaces = Document?.Selection?.GetSelectedFaces();
			if ( selectedFaces.Any() )
			{
				var index = e.Key - Key.D1;
				if ( index >= 0 && index < TexturePalette.Length )
				{
					var tex = GetTextureAtIndex( index );
					if ( tex != null )
					{
						ApplyTexture( selectedFaces, tex );
					}
				}
			}
		}
	}

	public override void Render( Scene scene )
	{
		if ( Document == null ) return;

		if ( HoveredFace != null )
		{
			var face = HoveredFace.Clone();
			foreach ( var v in face.Vertices )
			{
				v.Position += face.Plane.Normal;
			}

			var enumerable = new List<Face>() { face };

			if ( !face.IsSelected )
			{
				GL.Color4( Color.Yellow );
				MapObjectRenderer.DrawWireframe( enumerable, true, false );
			}
			else
			{
				GL.Color4( Color.Yellow );
				MapObjectRenderer.DrawWireframe( enumerable, true, false );
			}
		}

		var selectedFaces = Document.Selection.GetSelectedFaces();
		GL.Color4( Color.Yellow );
		MapObjectRenderer.DrawWireframe( selectedFaces, true, false );
		MapObjectRenderer.DrawFilledNoFucks( selectedFaces, Color.FromArgb( 50, Color.Yellow ), false, false, 1f );

		GL.Begin( PrimitiveType.Lines );
		foreach ( Face ff in selectedFaces )
		{
			var lineStart = ff.BoundingBox.Center + ff.Plane.Normal * 0.5f;
			var uEnd = lineStart + ff.TextureRef.UAxis * 16;
			var vEnd = lineStart + ff.TextureRef.VAxis * 16;

			GL.Color3( Color.Yellow );
			GL.Vertex3( lineStart.X, lineStart.Y, lineStart.Z );
			GL.Vertex3( uEnd.X, uEnd.Y, uEnd.Z );

			GL.Color3( Color.FromArgb( 0, 255, 0 ) );
			GL.Vertex3( lineStart.X, lineStart.Y, lineStart.Z );
			GL.Vertex3( vEnd.X, vEnd.Y, vEnd.Z );
		}
		GL.End();
	}

	internal override void LoadWidgetData( string data )
	{
		base.LoadWidgetData( data );

		try
		{
			TexturePalette = JsonSerializer.Deserialize<string[]>( data );
		}
		catch
		{
			TexturePalette = new string[5];
		}
	}

	internal override string SaveWidgetData()
	{
		return JsonSerializer.Serialize( TexturePalette );
	}

	public enum SelectBehaviour
	{
		LiftSelect,
		Lift,
		Select,
		Apply,
		ApplyWithValues,
		AlignToView
	}

	public enum JustifyMode
	{
		Fit,
		Left,
		Right,
		Center,
		Top,
		Bottom
	}

	public enum AlignMode
	{
		Face,
		World
	}

	public class TextureAlignmentTool
	{

		public struct TextureAlignmentConfig
		{
			public List<Face> Faces;
			public JustifyMode JustifyMode;
			public AlignMode AlignMode;
			public bool TreatAsOne;
		}

		public static void AlignTexture( TextureAlignmentConfig config )
		{
			if ( config.Faces == null || config.Faces.Count == 0 )
				return;

			Vector3 avgNormal;
			Vector3 avgCenter;
			CalculateAverages( config.Faces, out avgNormal, out avgCenter );

			Vector3 uAxis, vAxis;
			CalculateAlignmentAxes( config.AlignMode, avgNormal, out uAxis, out vAxis );

			var projectedPoints = ProjectPoints( config.Faces, avgCenter, avgNormal );

			var (minU, maxU, minV, maxV) = CalculateExtents( projectedPoints, uAxis, vAxis );

			if ( config.TreatAsOne )
			{
				AlignTextureAsOne( config, avgCenter, uAxis, vAxis, minU, maxU, minV, maxV );
			}
			else
			{
				foreach ( var face in config.Faces )
				{
					AlignFaceTexture( face, config.JustifyMode, avgCenter, uAxis, vAxis, minU, maxU, minV, maxV );
				}
			}
		}

		private static void CalculateAverages( List<Face> faces, out Vector3 avgNormal, out Vector3 avgCenter )
		{
			avgNormal = Vector3.Zero;
			avgCenter = Vector3.Zero;

			foreach ( var face in faces )
			{
				avgNormal += face.Plane.Normal;
				avgCenter += face.CalculateCenter();
			}

			avgNormal = (avgNormal / faces.Count).Normalized();
			avgCenter /= faces.Count;
		}

		private static void CalculateAlignmentAxes( AlignMode alignMode, Vector3 normal, out Vector3 uAxis, out Vector3 vAxis )
		{
			if ( alignMode == AlignMode.World )
			{
				uAxis = Vector3.UnitX;
				vAxis = -Vector3.UnitY;
			}
			else // AlignMode.Face
			{
				// First, try to use UnitZ as the up vector
				var up = Vector3.UnitZ;
				uAxis = Vector3.Cross( normal, up ).Normalized();

				// If uAxis is too small, it means normal is too close to up
				// In this case, use UnitX as the reference vector instead
				if ( uAxis.Length < 0.001f )
				{
					var reference = Vector3.UnitX;
					uAxis = Vector3.Cross( normal, reference ).Normalized();
				}

				// Now we can safely calculate vAxis
				vAxis = Vector3.Cross( uAxis, normal ).Normalized();
			}
		}

		private static List<Vector3> ProjectPoints( List<Face> faces, Vector3 center, Vector3 normal )
		{
			var projectedPoints = new List<Vector3>();
			foreach ( var face in faces )
			{
				foreach ( var vertex in face.Vertices )
				{
					var relativePosition = vertex.Position - center;
					var projectedPosition = relativePosition - Vector3.Dot( relativePosition, normal ) * normal;
					projectedPoints.Add( projectedPosition );
				}
			}
			return projectedPoints;
		}

		private static (float minU, float maxU, float minV, float maxV) CalculateExtents( List<Vector3> points, Vector3 uAxis, Vector3 vAxis )
		{
			var uValues = points.Select( p => Vector3.Dot( p, uAxis ) );
			var vValues = points.Select( p => Vector3.Dot( p, vAxis ) );

			return (uValues.Min(), uValues.Max(), vValues.Min(), vValues.Max());
		}

		private static void AlignFaceTexture( Face face, JustifyMode justifyMode, Vector3 center, Vector3 uAxis, Vector3 vAxis, float minU, float maxU, float minV, float maxV )
		{
			if ( face.TextureRef.Texture == null ) return;

			float uShift = 0, vShift = 0;
			float textureWidth = face.TextureRef.Texture.Width;
			float textureHeight = face.TextureRef.Texture.Height;

			switch ( justifyMode )
			{
				case JustifyMode.Left:
					uShift = 0;
					break;
				case JustifyMode.Right:
					uShift = textureWidth - (maxU - minU);
					break;
				case JustifyMode.Top:
					vShift = 0;
					break;
				case JustifyMode.Bottom:
					vShift = textureHeight - (maxV - minV);
					break;
				case JustifyMode.Center:
					uShift = (textureWidth - (maxU - minU)) / 2;
					vShift = (textureHeight - (maxV - minV)) / 2;
					break;
				case JustifyMode.Fit:
					float scaleU = textureWidth / (maxU - minU);
					float scaleV = textureHeight / (maxV - minV);
					float scale = Math.Min( scaleU, scaleV );
					face.TextureRef.XScale = 1 / scale;
					face.TextureRef.YScale = 1 / scale;
					uShift = 0;
					vShift = 0;
					break;
			}

			// Calculate face-specific alignment axes
			Vector3 faceNormal = face.Plane.Normal;
			Vector3 faceUAxis, faceVAxis;
			CalculateAlignmentAxes( AlignMode.Face, faceNormal, out faceUAxis, out faceVAxis );

			face.TextureRef.UAxis = faceUAxis;
			face.TextureRef.VAxis = faceVAxis;
			face.TextureRef.XShift = uShift;
			face.TextureRef.YShift = vShift;

			CalculateFaceTextureCoordinates( face, center, faceUAxis, faceVAxis );
		}

		private static void AlignTextureAsOne( TextureAlignmentConfig config, Vector3 center, Vector3 uAxis, Vector3 vAxis, float minU, float maxU, float minV, float maxV )
		{
			if ( config.Faces.Count == 0 || config.Faces[0].TextureRef.Texture == null ) return;

			var firstFace = config.Faces[0];
			float textureWidth = firstFace.TextureRef.Texture.Width;
			float textureHeight = firstFace.TextureRef.Texture.Height;

			// Calculate the texture properties for the first face
			CalculateTexturePropertiesForFace( firstFace, config.JustifyMode, center, uAxis, vAxis, minU, maxU, minV, maxV, textureWidth, textureHeight );

			// Use the first face's texture properties as a reference
			Vector3 refUAxis = firstFace.TextureRef.UAxis;
			Vector3 refVAxis = firstFace.TextureRef.VAxis;
			float refXShift = firstFace.TextureRef.XShift;
			float refYShift = firstFace.TextureRef.YShift;
			float refXScale = firstFace.TextureRef.XScale;
			float refYScale = firstFace.TextureRef.YScale;

			// Apply the reference texture properties to all faces
			foreach ( var face in config.Faces )
			{
				face.TextureRef.UAxis = refUAxis;
				face.TextureRef.VAxis = refVAxis;
				face.TextureRef.XShift = refXShift;
				face.TextureRef.YShift = refYShift;
				face.TextureRef.XScale = refXScale;
				face.TextureRef.YScale = refYScale;

				// Calculate texture coordinates based on the reference face
				CalculateFaceTextureCoordinates( face, center, refUAxis, refVAxis );
			}
		}

		private static void CalculateTexturePropertiesForFace( Face face, JustifyMode justifyMode, Vector3 center, Vector3 uAxis, Vector3 vAxis, float minU, float maxU, float minV, float maxV, float textureWidth, float textureHeight )
		{
			float uShift = 0, vShift = 0;
			float xScale = face.TextureRef.XScale;
			float yScale = face.TextureRef.YScale;

			switch ( justifyMode )
			{
				case JustifyMode.Left:
					uShift = 0;
					break;
				case JustifyMode.Right:
					uShift = textureWidth - (maxU - minU);
					break;
				case JustifyMode.Top:
					vShift = 0;
					break;
				case JustifyMode.Bottom:
					vShift = textureHeight - (maxV - minV);
					break;
				case JustifyMode.Center:
					uShift = (textureWidth - (maxU - minU)) / 2;
					vShift = (textureHeight - (maxV - minV)) / 2;
					break;
				case JustifyMode.Fit:
					float scaleU = textureWidth / (maxU - minU);
					float scaleV = textureHeight / (maxV - minV);
					float scale = Math.Min( scaleU, scaleV );
					xScale = 1 / scale;
					yScale = 1 / scale;
					uShift = 0;
					vShift = 0;
					break;
			}

			face.TextureRef.UAxis = uAxis;
			face.TextureRef.VAxis = vAxis;
			face.TextureRef.XShift = uShift;
			face.TextureRef.YShift = vShift;
			face.TextureRef.XScale = xScale;
			face.TextureRef.YScale = yScale;
		}

		private static void CalculateFaceTextureCoordinates( Face face, Vector3 center, Vector3 uAxis, Vector3 vAxis )
		{
			var udiv = face.TextureRef.Texture.Width * face.TextureRef.XScale;
			var uadd = face.TextureRef.XShift / face.TextureRef.Texture.Width;
			var vdiv = face.TextureRef.Texture.Height * face.TextureRef.YScale;
			var vadd = face.TextureRef.YShift / face.TextureRef.Texture.Height;

			foreach ( var vertex in face.Vertices )
			{
				var relativePosition = vertex.Position - center;
				vertex.TextureU = (Vector3.Dot( relativePosition, uAxis ) / udiv) + uadd;
				vertex.TextureV = (Vector3.Dot( relativePosition, vAxis ) / vdiv) + vadd;
			}
		}
	}
}
