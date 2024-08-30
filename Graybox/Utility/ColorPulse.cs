
using System.Drawing;

namespace Graybox;

public struct ColorPulse
{

	public Color BaseColor;
	private const double pulseFrequency = 1.5;
	private const double pulseRange = 35.0;
	private const int baseAlpha = 25;

	public ColorPulse( Color baseColor )
	{
		BaseColor = baseColor;
	}

	public static implicit operator Color( ColorPulse colorPulse )
	{
		var timeSinceChange = DateTime.Now - new DateTime( 1970, 1, 1 );
		var pulse = Math.Sin( timeSinceChange.TotalSeconds * 2.0 * Math.PI / pulseFrequency ) * 0.5 + 0.5;
		var alpha = (int)(baseAlpha + pulseRange * pulse);
		return Color.FromArgb( alpha, colorPulse.BaseColor );
	}

}
