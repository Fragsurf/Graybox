
using Graybox.DataStructures.Transformations;

namespace Graybox;

public class Box
{

	public readonly static Box Empty = new Box( Vector3.Zero, Vector3.Zero );

	public Vector3 Start { get; private set; }
	public Vector3 End { get; private set; }
	public Vector3 Center { get; private set; }

	/// <summary>
	/// The X value difference of this box
	/// </summary>
	public float Width => End.X - Start.X;

	/// <summary>
	/// The Y value difference of this box
	/// </summary>
	public float Length => End.Y - Start.Y;

	/// <summary>
	/// The Z value difference of this box
	/// </summary>
	public float Height => End.Z - Start.Z;

	public Vector3 Dimensions => new Vector3( Width, Length, Height );

	public Box( Vector3 start, Vector3 end )
	{
		Start = start;
		End = end;
		Center = (Start + End) / 2;
	}

	public Box( IEnumerable<Vector3> coordinates )
	{
		if ( !coordinates.Any() )
		{
			throw new Exception( "Cannot create a bounding box out of zero coordinates." );
		}
		Vector3 min = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
		Vector3 max = new Vector3( float.MinValue, float.MinValue, float.MinValue );
		foreach ( Vector3 vertex in coordinates )
		{
			min.X = Math.Min( vertex.X, min.X );
			min.Y = Math.Min( vertex.Y, min.Y );
			min.Z = Math.Min( vertex.Z, min.Z );
			max.X = Math.Max( vertex.X, max.X );
			max.Y = Math.Max( vertex.Y, max.Y );
			max.Z = Math.Max( vertex.Z, max.Z );
		}
		Start = min;
		End = max;
		Center = (Start + End) / 2;
	}

	public Box( IEnumerable<Box> boxes )
	{
		Vector3 min = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
		Vector3 max = new Vector3( float.MinValue, float.MinValue, float.MinValue );
		foreach ( Box box in boxes )
		{
			min.X = Math.Min( box.Start.X, min.X );
			min.Y = Math.Min( box.Start.Y, min.Y );
			min.Z = Math.Min( box.Start.Z, min.Z );
			max.X = Math.Max( box.End.X, max.X );
			max.Y = Math.Max( box.End.Y, max.Y );
			max.Z = Math.Max( box.End.Z, max.Z );
		}
		Start = min;
		End = max;
		Center = (Start + End) / 2;
	}

	public bool IsEmpty()
	{
		return Width == 0 && Height == 0 && Length == 0;
	}

	public IEnumerable<Vector3> GetBoxPoints()
	{
		yield return new Vector3( Start.X, End.Y, End.Z );
		yield return End;
		yield return new Vector3( Start.X, Start.Y, End.Z );
		yield return new Vector3( End.X, Start.Y, End.Z );

		yield return new Vector3( Start.X, End.Y, Start.Z );
		yield return new Vector3( End.X, End.Y, Start.Z );
		yield return Start;
		yield return new Vector3( End.X, Start.Y, Start.Z );
	}

	public Plane[] GetBoxPlanes()
	{
		Plane[] planes = new Plane[6];
		Vector3[][] faces = GetBoxFaces();
		for ( int i = 0; i < 6; i++ )
		{
			planes[i] = new Plane( faces[i][0], faces[i][1], faces[i][2] );
		}
		return planes;
	}

	public Vector3[][] GetBoxFaces()
	{
		Vector3 topLeftBack = new Vector3( Start.X, End.Y, End.Z );
		Vector3 topRightBack = End;
		Vector3 topLeftFront = new Vector3( Start.X, Start.Y, End.Z );
		Vector3 topRightFront = new Vector3( End.X, Start.Y, End.Z );

		Vector3 bottomLeftBack = new Vector3( Start.X, End.Y, Start.Z );
		Vector3 bottomRightBack = new Vector3( End.X, End.Y, Start.Z );
		Vector3 bottomLeftFront = Start;
		Vector3 bottomRightFront = new Vector3( End.X, Start.Y, Start.Z );
		return
				   [
					   [topLeftFront, topRightFront, bottomRightFront, bottomLeftFront],
					   [topRightBack, topLeftBack, bottomLeftBack, bottomRightBack],
					   [topLeftBack, topLeftFront, bottomLeftFront, bottomLeftBack],
					   [topRightFront, topRightBack, bottomRightBack, bottomRightFront],
					   [topLeftBack, topRightBack, topRightFront, topLeftFront],
					   [bottomLeftFront, bottomRightFront, bottomRightBack, bottomLeftBack]
				   ];
	}

	public IEnumerable<Line> GetBoxLines()
	{
		Vector3 topLeftBack = new Vector3( Start.X, End.Y, End.Z );
		Vector3 topRightBack = End;
		Vector3 topLeftFront = new Vector3( Start.X, Start.Y, End.Z );
		Vector3 topRightFront = new Vector3( End.X, Start.Y, End.Z );

		Vector3 bottomLeftBack = new Vector3( Start.X, End.Y, Start.Z );
		Vector3 bottomRightBack = new Vector3( End.X, End.Y, Start.Z );
		Vector3 bottomLeftFront = Start;
		Vector3 bottomRightFront = new Vector3( End.X, Start.Y, Start.Z );

		yield return new Line( topLeftBack, topRightBack );
		yield return new Line( topLeftFront, topRightFront );
		yield return new Line( topLeftBack, topLeftFront );
		yield return new Line( topRightBack, topRightFront );

		yield return new Line( topLeftBack, bottomLeftBack );
		yield return new Line( topLeftFront, bottomLeftFront );
		yield return new Line( topRightBack, bottomRightBack );
		yield return new Line( topRightFront, bottomRightFront );

		yield return new Line( bottomLeftBack, bottomRightBack );
		yield return new Line( bottomLeftFront, bottomRightFront );
		yield return new Line( bottomLeftBack, bottomLeftFront );
		yield return new Line( bottomRightBack, bottomRightFront );
	}

	/// <summary>
	/// Returns true if this box overlaps the given box in any way
	/// </summary>
	public bool IntersectsWith( Box that )
	{
		if ( Start.X >= that.End.X ) return false;
		if ( that.Start.X >= End.X ) return false;

		if ( Start.Y >= that.End.Y ) return false;
		if ( that.Start.Y >= End.Y ) return false;

		if ( Start.Z >= that.End.Z ) return false;
		if ( that.Start.Z >= End.Z ) return false;

		return true;
	}

	/// <summary>
	/// Returns true if this box is completely inside the given box
	/// </summary>
	public bool ContainedWithin( Box that )
	{
		if ( Start.X < that.Start.X ) return false;
		if ( Start.Y < that.Start.Y ) return false;
		if ( Start.Z < that.Start.Z ) return false;

		if ( End.X > that.End.X ) return false;
		if ( End.Y > that.End.Y ) return false;
		if ( End.Z > that.End.Z ) return false;

		return true;
	}

	/* http://www.gamedev.net/community/forums/topic.asp?topic_id=338987 */
	/// <summary>
	/// Returns true if this box intersects the given line
	/// </summary>
	public bool IntersectsWith( Line that )
	{
		Vector3 start = that.Start;
		Vector3 finish = that.End;

		if ( start.X < Start.X && finish.X < Start.X ) return false;
		if ( start.X > End.X && finish.X > End.X ) return false;

		if ( start.Y < Start.Y && finish.Y < Start.Y ) return false;
		if ( start.Y > End.Y && finish.Y > End.Y ) return false;

		if ( start.Z < Start.Z && finish.Z < Start.Z ) return false;
		if ( start.Z > End.Z && finish.Z > End.Z ) return false;

		Vector3 d = (finish - start) / 2;
		Vector3 e = (End - Start) / 2;
		Vector3 c = start + d - ((Start + End) / 2);
		Vector3 ad = d.Absolute();

		if ( Math.Abs( c.X ) > e.X + ad.X ) return false;
		if ( Math.Abs( c.Y ) > e.Y + ad.Y ) return false;
		if ( Math.Abs( c.Z ) > e.Z + ad.Z ) return false;

		Vector3 dca = d.Cross( c ).Absolute();

		if ( dca.X > e.Y * ad.Z + e.Z * ad.Y ) return false;
		if ( dca.Y > e.Z * ad.X + e.X * ad.Z ) return false;
		if ( dca.Z > e.X * ad.Y + e.Y * ad.X ) return false;

		return true;
	}

	/// <summary>
	/// Returns true if the given coordinate is inside this box.
	/// </summary>
	/// <param name="c"></param>
	/// <returns></returns>
	public bool CoordinateIsInside( Vector3 c )
	{
		return c.X >= Start.X && c.Y >= Start.Y && c.Z >= Start.Z
			   && c.X <= End.X && c.Y <= End.Y && c.Z <= End.Z;
	}

	public Box Transform( IUnitTransformation transform )
	{
		return new Box( GetBoxPoints().Select( transform.Transform ) );
	}

	public Box Clone()
	{
		return new Box( Start, End );
	}

	public Box EnsurePositive()
	{
		Vector3 newStart = new Vector3(
			Math.Min( Start.X, End.X ),
			Math.Min( Start.Y, End.Y ),
			Math.Min( Start.Z, End.Z )
		);

		Vector3 newEnd = new Vector3(
			Math.Max( Start.X, End.X ),
			Math.Max( Start.Y, End.Y ),
			Math.Max( Start.Z, End.Z )
		);

		return new Box( newStart, newEnd );
	}

}
