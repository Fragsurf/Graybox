using Graybox.DataStructures.Geometric;
using Graybox.Utility;
using System.Collections.Generic;
using System.Drawing;

namespace Graybox.DataStructures.GameData
{
	public class Behaviour
	{
		public string Name { get; set; }
		public List<string> Values { get; set; }

		public Behaviour( string name, params string[] values )
		{
			Name = name;
			Values = new List<string>( values );
		}

		public OpenTK.Mathematics.Vector3 GetCoordinate( int index )
		{
			int first = index * 3;
			return Values.Count < first + 3 ?
				default : ParseUtility.ParseVector3( Values[first], Values[first + 1], Values[first + 2] );
		}

		public Color GetColour( int index )
		{
			OpenTK.Mathematics.Vector3 coord = GetCoordinate( index );
			return coord == default ? Color.White : Color.FromArgb( (int)coord.X, (int)coord.Y, (int)coord.Z );
		}
	}
}
