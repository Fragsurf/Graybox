using System.Collections.Generic;
using System.Linq;

namespace Graybox.DataStructures.Geometric
{
	// Ported from: https://github.com/evanw/csg.js/
	// Copyright (c) 2011 Evan Wallace (http://madebyevan.com/)
	// MIT license
	public class CsgNode
	{
		private List<Polygon> Polygons { get; set; }

		private Plane Plane { get; set; }
		private CsgNode Front { get; set; }
		private CsgNode Back { get; set; }

		public CsgNode( CsgSolid solid ) : this()
		{
			Build( solid.Polygons.Select( x => x.Clone() ).ToList() );
		}

		private CsgNode()
		{
			Polygons = new List<Polygon>();
			Front = null;
			Back = null;
		}

		private List<Polygon> ClipPolygons( IEnumerable<Polygon> polygons )
		{
			if ( Plane == default ) return polygons.ToList();
			List<Polygon> front = new List<Polygon>();
			List<Polygon> back = new List<Polygon>();
			foreach ( Polygon polygon in polygons )
			{
				Polygon f, b, cf, cb;
				polygon.Split( Plane, out b, out f, out cb, out cf );
				if ( f != null ) front.Add( f );
				if ( b != null ) back.Add( b );
				if ( cf != null ) front.Add( cf );
				if ( cb != null ) back.Add( cb );
			}
			if ( Front != null ) front = Front.ClipPolygons( front );
			back = Back != null ? Back.ClipPolygons( back ) : new List<Polygon>();
			return front.Concat( back ).ToList();
		}

		public void ClipTo( CsgNode bsp )
		{
			Polygons = bsp.ClipPolygons( Polygons );
			if ( Front != null ) Front.ClipTo( bsp );
			if ( Back != null ) Back.ClipTo( bsp );
		}

		public void Invert()
		{
			foreach ( Polygon polygon in Polygons )
			{
				polygon.Flip();
			}
			Plane = new Plane( -Plane.Normal, Plane.PointOnPlane );
			if ( Front != null ) Front.Invert();
			if ( Back != null ) Back.Invert();
			CsgNode temp = Front;
			Front = Back;
			Back = temp;
		}

		public List<Polygon> AllPolygons()
		{
			List<Polygon> polygons = [.. Polygons];
			if ( Front != null ) polygons.AddRange( Front.AllPolygons() );
			if ( Back != null ) polygons.AddRange( Back.AllPolygons() );
			return polygons;
		}

		public void Build( List<Polygon> polygons )
		{
			if ( polygons.Count == 0 ) return;
			if ( Plane == default ) Plane = polygons[0].Plane;
			List<Polygon> front = new List<Polygon>();
			List<Polygon> back = new List<Polygon>();
			foreach ( Polygon polygon in polygons )
			{
				Polygon f, b, cf, cb;
				polygon.Split( Plane, out b, out f, out cb, out cf );
				if ( f != null ) front.Add( f );
				if ( b != null ) back.Add( b );
				if ( cf != null ) Polygons.Add( cf );
				if ( cb != null ) Polygons.Add( cb );
			}
			if ( front.Count > 0 )
			{
				if ( Front == null ) Front = new CsgNode();
				Front.Build( front );
			}
			if ( back.Count > 0 )
			{
				if ( Back == null ) Back = new CsgNode();
				Back.Build( back );
			}
		}
	}
}
