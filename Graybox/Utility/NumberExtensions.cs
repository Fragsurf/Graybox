using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graybox;

public static class NumberExtensions
{

	public static float ToNearestPowerOfTwo( this float n )
	{
		return ((int)MathF.Round ( n ) ).ToNearestPowerOfTwo();
	}

	public static int ToNearestPowerOfTwo( this int n )
	{
		if ( n < 0 )
			return 0;

		n--;
		n |= n >> 1;
		n |= n >> 2;
		n |= n >> 4;
		n |= n >> 8;
		n |= n >> 16;
		n++;

		return n;
	}

	public static bool IsNearlyZero( this float f, float tolerance = 0.0001f )
	{
		return Math.Abs( f ) <= tolerance;
	}

}
