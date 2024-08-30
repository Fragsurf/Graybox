
using Graybox.DataStructures.MapObjects;

namespace Graybox.Editor.Inspectors;

internal class LightInspector : BaseInspector<Light>
{

	public override void DrawInspector()
	{
		var shitwad = Target as Light;
		if ( shitwad == null ) return;

		if ( ImObjectEditor.EditObject( ref shitwad ) )
		{
			Debug.LogError( "ok" );
			shitwad.IncrementUpdateCounter();
		}
	}

}
