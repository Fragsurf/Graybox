
using Graybox.DataStructures.Meta;
using Graybox.DataStructures.Transformations;

namespace Graybox.DataStructures.MapObjects;

public abstract class MapObject 
{
	[HideInEditor]
	public long ID { get; set; }
	public string Name { get; set; }
	[HideInEditor]
	public virtual string ClassName { get; set; }
	[HideInEditor]
	public MapObject Parent { get; private set; }
	[HideInEditor]
	public virtual Color4 Colour { get; set; }
	[HideInEditor]
	public bool IsSelected { get; set; }
	[HideInEditor]
	public bool IsCodeHidden { get; set; }
	[HideInEditor]
	public bool IsRenderHidden2D { get; set; }
	[HideInEditor]
	public bool IsRenderHidden3D { get; set; }
	[HideInEditor]
	public bool IsVisgroupHidden { get; set; }
	[HideInEditor]
	public Box BoundingBox { get; set; }
	[HideInEditor]
	public MetaData MetaData { get; private set; }

	private List<MapObject> children = new();
	[HideInEditor]
	public IReadOnlyList<MapObject> Children => children;

	public int UpdateCounter { get; private set; }

	protected MapObject( long id )
	{
		ID = id;
		MetaData = new MetaData();
	}

	public IEnumerable<MapObject> GetChildren() => children;

	public IEnumerable<T> GetAllDescendants<T>() where T : MapObject
	{
		foreach ( var child in GetChildren() )
		{
			if ( child is T t ) yield return t;
			foreach ( var descendant in child.GetAllDescendants<T>() )
			{
				yield return descendant;
			}
		}
	}

	[HideInEditor]
	public int ChildCount => children.Count;
	[HideInEditor]
	public bool HasChildren => children.Count > 0;

	/// <summary>
	/// Creates an exact copy of this object with a new ID.
	/// </summary>
	public abstract MapObject Copy( IDGenerator generator );

	/// <summary>
	/// Copies all the values of the provided object into this one, copying children and faces.
	/// </summary>
	public abstract void Paste( MapObject o, IDGenerator generator );

	/// <summary>
	/// Creates an exact clone of this object with the same ID.
	/// </summary>
	public abstract MapObject Clone();

	/// <summary>
	/// Copies all the values of the provided object into this one, cloning children and faces.
	/// </summary>
	public abstract void Unclone( MapObject o );

	protected void CopyBase( MapObject o, IDGenerator generator, bool performClone = false )
	{
		if ( performClone && o.ID != ID )
		{
			MapObject parent = o.Parent;

			bool setPar = o.Parent != null && o.Parent.children.Any( x => x.ID == o.ID );

			if ( setPar ) 
				o.SetParent( null );
			o.ID = ID;

			if ( setPar ) 
				o.SetParent( parent );
		}
		o.ClassName = ClassName;
		o.Parent = Parent;
		o.Colour = Colour;
		o.IsSelected = IsSelected;
		o.IsCodeHidden = IsCodeHidden;
		o.IsRenderHidden2D = IsRenderHidden2D;
		o.IsRenderHidden3D = IsRenderHidden3D;
		o.IsVisgroupHidden = IsVisgroupHidden;
		o.BoundingBox = BoundingBox.Clone();
		o.MetaData = MetaData.Clone();

		var children = GetChildren().Select( x => performClone ? x.Clone() : x.Copy( generator ) );
		foreach ( MapObject c in children )
		{
			c.SetParent( o );
		}
	}

	protected void PasteBase( MapObject o, IDGenerator generator, bool performUnclone = false )
	{
		this.children.Clear();

		if ( performUnclone && o.ID != ID )
		{
			MapObject parent = Parent;
			bool setPar = Parent != null && Parent.children.Any( x => x.ID == ID );

			if ( setPar ) 
				SetParent( null );

			ID = o.ID;

			if ( setPar ) 
				SetParent( parent );
		}
		ClassName = o.ClassName;
		Parent = o.Parent;
		Colour = o.Colour;
		IsSelected = o.IsSelected;
		IsCodeHidden = o.IsCodeHidden;
		IsRenderHidden2D = o.IsRenderHidden2D;
		IsRenderHidden3D = o.IsRenderHidden3D;
		IsVisgroupHidden = o.IsVisgroupHidden;
		BoundingBox = o.BoundingBox.Clone();
		MetaData = o.MetaData.Clone();

		var children = o.GetChildren().Select( x => performUnclone ? x.Clone() : x.Copy( generator ) );
		foreach ( MapObject c in children )
		{
			c.SetParent( this );
		}
	}

	public void SetParent( MapObject parent, bool updateBoundingBox = true )
	{
		if ( Parent != null )
		{
			Parent.children.Remove( this );
			if ( updateBoundingBox ) Parent.UpdateBoundingBox();
		}

		Parent = parent;

		if ( Parent != null )
		{
			Parent.children.Add( this );
			if ( updateBoundingBox ) UpdateBoundingBox();
		}
	}

	public bool RemoveDescendant( MapObject remove )
	{
		if ( remove == null ) return false;
		if ( children.RemoveAll( x => x.ID == remove.ID ) > 0 ) return true;
		foreach ( MapObject child in GetChildren() )
		{
			if ( child.RemoveDescendant( remove ) ) return true;
		}
		return false;
	}

	public virtual void UpdateBoundingBox( bool cascadeToParent = true )
	{
		if ( cascadeToParent && Parent != null )
		{
			Parent.UpdateBoundingBox();
		}
	}

	public virtual void Transform( IUnitTransformation transform, TransformFlags flags )
	{
		foreach ( MapObject mo in GetChildren() )
		{
			mo.Transform( transform, flags );
		}
		UpdateBoundingBox();
		IncrementUpdateCounter();
	}

	public virtual Vector3 GetIntersectionPoint( Line line )
	{
		return default;
	}

	public virtual EntityData GetEntityData()
	{
		return null;
	}

	public virtual Box GetIntersectionBoundingBox()
	{
		return BoundingBox;
	}

	/// <summary>
	/// Searches upwards for the last parent that is not null and is not
	/// an instance of <code>Chisel.DataStructures.MapObjects.World</code>.
	/// </summary>
	/// <param name="o">The starting node</param>
	/// <returns>The highest parent that is not a worldspawn instance</returns>
	public static MapObject GetTopmostNonRootParent( MapObject o )
	{
		while ( o.Parent != null && !(o.Parent is World) )
		{
			o = o.Parent;
		}
		return o;
	}

	/// <summary>
	/// Searches upwards for the root node as an instance of
	/// <code>Chisel.DataStructures.MapObjects.World</code>.
	/// </summary>
	/// <param name="o">The starting node</param>
	/// <returns>The root world node, or null if it doesn't exist.</returns>
	public static World GetRoot( MapObject o )
	{
		while ( o != null && !(o is World) )
		{
			o = o.Parent;
		}
		return o as World;
	}

	/// <summary>
	/// Get all the nodes starting from this node that intersect with a box.
	/// </summary>
	/// <param name="box">The intersection box</param>
	/// <param name="allowCodeHidden">Set to true to include nodes that have been hidden by code</param>
	/// <param name="allowVisgroupHidden">Set to true to include nodes that have been hidden by the user</param>
	/// <returns>A list of all the descendants that intersect with the box.</returns>
	public IEnumerable<MapObject> GetAllNodesIntersectingWith( Box box, bool allowCodeHidden = false, bool allowVisgroupHidden = false )
	{
		List<MapObject> list = new List<MapObject>();
		if ( (!allowCodeHidden && IsCodeHidden) || (!allowVisgroupHidden && IsVisgroupHidden) ) return list;
		if ( !(this is World) )
		{
			if ( BoundingBox == null || !BoundingBox.IntersectsWith( box ) ) return list;
			if ( this is Solid || this is Entity ) list.Add( this );
		}
		list.AddRange( GetChildren().SelectMany( x => x.GetAllNodesIntersectingWith( box, allowCodeHidden, allowVisgroupHidden ) ) );
		return list;
	}

	/// <summary>
	/// Get all the nodes starting from this node with the centers contained within a box.
	/// </summary>
	/// <param name="box">The containing box</param>
	/// <param name="allowCodeHidden">Set to true to include nodes that have been hidden by code</param>
	/// <param name="allowVisgroupHidden">Set to true to include nodes that have been hidden by the user</param>
	/// <returns>A list of all the descendants that have centers inside the box.</returns>
	public IEnumerable<MapObject> GetAllNodesWithCentersContainedWithin( Box box, bool allowCodeHidden = false, bool allowVisgroupHidden = false )
	{
		List<MapObject> list = new List<MapObject>();
		if ( (!allowCodeHidden && IsCodeHidden) || (!allowVisgroupHidden && IsVisgroupHidden) ) return list;
		if ( !(this is World) )
		{
			if ( BoundingBox == null || !box.CoordinateIsInside( BoundingBox.Center ) ) return list;
			if ( (this is Solid || this is Entity) && !children.Any() ) list.Add( this );
		}
		list.AddRange( GetChildren().SelectMany( x => x.GetAllNodesWithCentersContainedWithin( box, allowCodeHidden, allowVisgroupHidden ) ) );
		return list;
	}

	/// <summary>
	/// Get all the nodes starting from this node that intersect with a line.
	/// </summary>
	/// <param name="line">The intersection line</param>
	/// <param name="allowCodeHidden">Set to true to include nodes that have been hidden by code</param>
	/// <param name="allowVisgroupHidden">Set to true to include nodes that have been hidden by the user</param>
	/// <returns>A list of all the descendants that intersect with the line.</returns>
	public IEnumerable<MapObject> GetAllNodesIntersectingWith( Line line, bool allowCodeHidden = false, bool allowVisgroupHidden = false )
	{
		List<MapObject> list = new List<MapObject>();
		if ( (!allowCodeHidden && IsCodeHidden) || (!allowVisgroupHidden && IsVisgroupHidden) ) return list;
		if ( !(this is World) )
		{
			Box bbox = GetIntersectionBoundingBox();
			if ( bbox == null || !bbox.IntersectsWith( line ) ) return list;
			if ( this is Solid || this is Entity ) list.Add( this );
		}
		list.AddRange( GetChildren().SelectMany( x => x.GetAllNodesIntersectingWith( line, allowCodeHidden, allowVisgroupHidden ) ) );
		return list;
	}

	/// <summary>
	/// Get all the nodes starting from this node that are entirely contained within a box.
	/// </summary>
	/// <param name="box">The containing box</param>
	/// <param name="allowCodeHidden">Set to true to include nodes that have been hidden by code</param>
	/// <param name="allowVisgroupHidden">Set to true to include nodes that have been hidden by the user</param>
	/// <returns>A list of all the descendants that are contained within the box.</returns>
	public IEnumerable<MapObject> GetAllNodesContainedWithin( Box box, bool allowCodeHidden = false, bool allowVisgroupHidden = false )
	{
		List<MapObject> list = new List<MapObject>();
		if ( !(this is World) && (allowCodeHidden || !IsCodeHidden) && (allowVisgroupHidden || !IsVisgroupHidden) )
		{
			if ( BoundingBox == null || !BoundingBox.ContainedWithin( box ) ) return list;
			if ( this is Solid || this is Entity ) list.Add( this );
		}
		list.AddRange( GetChildren().SelectMany( x => x.GetAllNodesContainedWithin( box, allowCodeHidden, allowVisgroupHidden ) ) );
		return list;
	}

	/// <summary>
	/// Get all the solid nodes starting from this node where the
	/// edges of the solid intersect with the provided box.
	/// </summary>
	/// <param name="box">The intersection box</param>
	/// <param name="includeOrigin">Set to true to test against the object origins as well</param>
	/// <param name="forceOrigin">Set to true to only test against the object origins and ignore other tests</param>
	/// <param name="allowCodeHidden">Set to true to include nodes that have been hidden by code</param>
	/// <param name="allowVisgroupHidden">Set to true to include nodes that have been hidden by the user</param>
	/// <returns>A list of all the solid descendants where the edges of the solid intersect with the box.</returns>
	public IEnumerable<MapObject> GetAllNodesIntersecting2DLineTest( Box box, bool includeOrigin = false, bool forceOrigin = false, bool allowCodeHidden = false, bool allowVisgroupHidden = false )
	{
		List<MapObject> list = new List<MapObject>();
		if ( !(this is World) && (allowCodeHidden || !IsCodeHidden) && (allowVisgroupHidden || !IsVisgroupHidden) )
		{
			if ( BoundingBox == null || !BoundingBox.IntersectsWith( box ) ) return list;
			// Solids: Match face edges against box
			if ( !forceOrigin && this is Solid && ((Solid)this).Faces.Any( f => f.IntersectsWithLine( box ) ) )
			{
				list.Add( this );
			}
			// Point entities: Match bounding box edges against box
			else if ( !forceOrigin && this is Entity && !children.Any() && BoundingBox.GetBoxLines().Any( box.IntersectsWith ) )
			{
				list.Add( this );
			}
			// Origins: Match bounding box center against box (for solids and point entities)
			else if ( (includeOrigin || forceOrigin) && !children.Any() && (this is Solid || this is Entity) && box.CoordinateIsInside( BoundingBox.Center ) )
			{
				list.Add( this );
			}
		}
		list.AddRange( GetChildren().SelectMany( x => x.GetAllNodesIntersecting2DLineTest( box, includeOrigin, forceOrigin, allowCodeHidden, allowVisgroupHidden ) ) );
		return list;
	}

	/// <summary>
	/// Get all the parents of this node that match a predicate. The first item in the list will be the closest parent.
	/// </summary>
	/// <param name="matcher">The predicate to match</param>
	/// <param name="includeWorld">True to include the World element at the bottom of the list</param>
	/// <returns>The list of parents</returns>
	public IEnumerable<MapObject> FindParents( Predicate<MapObject> matcher, bool includeWorld = false )
	{
		MapObject o = this;
		while ( o.Parent != null )
		{
			if ( o.Parent is World && !includeWorld ) break;
			if ( matcher( o.Parent ) ) yield return o.Parent;
			o = o.Parent;
		}
	}

	/// <summary>
	/// Find the first parent of this object that matches a predicate.
	/// </summary>
	/// <param name="matcher">The predicate to match</param>
	/// <returns>The matching parent if it was found, null otherwise</returns>
	public MapObject FindClosestParent( Predicate<MapObject> matcher )
	{
		return FindParents( matcher ).FirstOrDefault();
	}

	/// <summary>
	/// Find the last parent of this object that matches a predicate.
	/// </summary>
	/// <param name="matcher">The predicate to match</param>
	/// <returns>The matching parent if it was found, null otherwise</returns>
	public MapObject FindTopmostParent( Predicate<MapObject> matcher )
	{
		return FindParents( matcher ).LastOrDefault();
	}

	/// <summary>
	/// Get the single object in the object tree with the given ID.
	/// </summary>
	/// <param name="id">The ID of the object to locate</param>
	/// <returns>The object with the matching ID or null if it wasn't found</returns>
	public MapObject FindByID( long id )
	{
		if ( ID == id ) 
			return this;

		var result = children.FirstOrDefault( x => x.ID == id );
		if ( result != null ) return result;

		foreach ( var mo in children )
		{
			MapObject by = mo.FindByID( id );
			if ( by != null ) return by;
		}

		return null;
	}

	/// <summary>
	/// Flattens the tree underneath this node.
	/// </summary>
	/// <returns>A list containing all the descendants of this node (including this node)</returns>
	public List<MapObject> FindAll()
	{
		return Find( x => true );
	}

	/// <summary>
	/// Flattens the tree and selects the nodes that match the test.
	/// </summary>
	/// <param name="matcher">The prediacate to match</param>
	/// <param name="forceMatchIfParentMatches">If true and a parent matches the predicate, all children will be added regardless of match status.</param>
	/// <returns>A list of all the descendants that match the test (including this node)</returns>
	public List<MapObject> Find( Predicate<MapObject> matcher, bool forceMatchIfParentMatches = false )
	{
		List<MapObject> list = new List<MapObject>();
		FindRecursive( list, matcher, forceMatchIfParentMatches );
		return list;
	}

	/// <summary>
	/// Recursively collect children matching a predicate.
	/// </summary>
	/// <param name="items">The list to populate</param>
	/// <param name="matcher">The prediacate to match</param>
	/// <param name="forceMatchIfParentMatches">If true and a parent matches the predicate, all children will be added regardless of match status.</param>
	private void FindRecursive( ICollection<MapObject> items, Predicate<MapObject> matcher, bool forceMatchIfParentMatches = false )
	{
		bool thisMatch = matcher( this );
		if ( thisMatch )
		{
			items.Add( this );
			if ( forceMatchIfParentMatches ) matcher = x => true;
		}
		foreach ( MapObject mo in GetChildren() )
		{
			mo.FindRecursive( items, matcher, forceMatchIfParentMatches );
		}
	}

	/// <summary>
	/// Recursively modify children matching a predicate.
	/// </summary>
	/// <param name="action">The action to perform on matching children</param>
	/// <param name="matcher">The prediacate to match</param>
	/// <param name="forceMatchIfParentMatches">If true and a parent matches the predicate, all children will be modified regardless of match status.</param>
	public void ForEach( Predicate<MapObject> matcher, Action<MapObject> action, bool forceMatchIfParentMatches = false )
	{
		bool thisMatch = matcher( this );
		if ( thisMatch )
		{
			action( this );
			if ( forceMatchIfParentMatches ) matcher = x => true;
		}
		foreach ( MapObject mo in GetChildren() )
		{
			mo.ForEach( matcher, action, forceMatchIfParentMatches );
		}
	}

	public IEnumerable<Face> GetBoxFaces()
	{
		List<Face> faces = new List<Face>();
		if ( Children.Any() ) return faces;

		var box = BoundingBox.GetBoxFaces();
		var dummySolid = new Solid( -1 )
		{
			IsCodeHidden = IsCodeHidden,
			IsRenderHidden2D = IsRenderHidden2D,
			IsSelected = IsSelected,
			IsRenderHidden3D = IsRenderHidden3D,
			IsVisgroupHidden = IsVisgroupHidden
		};
		foreach ( var ca in box )
		{
			var face = new Face( 0 )
			{
				Plane = new Plane( ca[0], ca[1], ca[2] ),
				Colour = Colour,
				IsSelected = IsSelected,
				Parent = dummySolid
			};
			face.Vertices.AddRange( ca.Select( x => new Vertex( x, face ) ) );
			face.UpdateBoundingBox();
			faces.Add( face );
		}
		return faces;
	}

	public void IncrementUpdateCounter()
	{
		UpdateCounter++;
	}

	public IEnumerable<Face> CollectFaces()
	{
		if ( this is Solid s )
			return s.Faces;

		if ( this is Group g )
			return g.GetAllDescendants<Solid>().SelectMany( x => x.Faces );

		return GetBoxFaces() ?? Enumerable.Empty<Face>();
	}

}
