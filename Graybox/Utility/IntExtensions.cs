using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graybox;

public static class IntExtensions
{

	public static int NearestPowerOfTwo( this int n )
	{
		if ( n <= 0 ) return 1;
		if ( (n & (n - 1)) == 0 ) return n; 

		int power = 1;
		while ( power < n )
		{
			power *= 2;
		}

		if ( power - n > n - (power / 2) )
		{
			return power / 2;
		}

		return power;
	}

}
