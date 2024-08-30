
using System.Runtime.InteropServices;

namespace Graybox.Graphics;

internal class VBO2Subset<TItem, TVertex>
{

	public int Index { get; set; }
	public bool IsDirty { get; private set; }

	public List<TVertex> Data = new();
	private LinkedList<Segment> segments = new();
	private Dictionary<TItem, LinkedListNode<Segment>> itemToNodeMap = new();

	public void Add( TItem item, TVertex[] vertices )
	{
		if ( itemToNodeMap.ContainsKey( item ) )
			return;

		var segment = new Segment { Vertices = vertices, Item = item };
		var node = segments.AddLast( segment );
		itemToNodeMap[item] = node;
		IsDirty = true;
	}

	public bool Remove( TItem item )
	{
		if ( !itemToNodeMap.TryGetValue( item, out var node ) )
			return false;

		segments.Remove( node );
		itemToNodeMap.Remove( item );
		IsDirty = true;
		return true;
	}

	public void Rebuild()
	{
		IsDirty = false;
		Data.Clear();
		foreach ( var segment in segments )
		{
			Data.AddRange( segment.Vertices );
		}
	}

	class Segment
	{
		public TVertex[] Vertices;
		public TItem Item;
	}
}

internal class VBO2SubsetPart<TItem, TVertex>
{
	public TVertex[] Data;
	public object Subset;
}

internal class VBO2<TItem, TVertex> : IDisposable where TVertex : struct
{

	private int _vboId;
	private int _bufferSize;
	private bool _isDirty = true;
	private TVertex[] _data;

	protected virtual PrimitiveType Mode => PrimitiveType.Triangles;
	protected virtual int Passes { get; set; } = 1;
	protected int Pass { get; set; } = 0;

	private readonly ArraySpecification _spec = new( typeof( TVertex ) );
	private readonly int _dataSize = Marshal.SizeOf( typeof( TVertex ) );
	private readonly Dictionary<object, VBO2Subset<TItem, TVertex>> _subsets = new();

	public VBO2()
	{
		GL.GenBuffers( 1, out _vboId );
		_bufferSize = 0;
	}

	public void Add( IEnumerable<TItem> items )
	{
		foreach ( var item in items )
		{
			Add( item );
		}
	}

	public void Remove( IEnumerable<TItem> items )
	{
		foreach ( var item in items )
		{
			Remove( item );
		}
	}

	static object nullSubset = new();
	public void Add( TItem item )
	{
		var subsets = Convert( item );

		foreach ( var s in subsets )
		{
			var subset = s.Subset ?? nullSubset;
			var data = s.Data;
			if ( !_subsets.TryGetValue( subset, out var subsetData ) )
			{
				subsetData = new VBO2Subset<TItem, TVertex>();
				_subsets[subset] = subsetData;
			}
			subsetData.Add( item, data );
		}

		_isDirty = true;
	}

	public void Update( TItem item )
	{
		Remove( item );
		Add( item );
		_isDirty = true;
	}

	public void Update( IEnumerable<TItem> items )
	{
		foreach ( var item in items )
		{
			Update( item );
		}
		_isDirty = true;
	}

	public void Remove( TItem item )
	{
		foreach ( var kvp in _subsets )
		{
			if ( kvp.Value.Remove( item ) )
			{
				_isDirty = true;
			}
		}
	}

	public void Clear()
	{
		_subsets.Clear();
		_isDirty = true;
	}

	public virtual void Dispose()
	{
		if ( _vboId != 0 )
		{
			GL.DeleteBuffer( _vboId );
			_vboId = 0;
		}
	}

	protected virtual void PreRender() { }
	protected virtual void PostRender() { }
	protected virtual void PreRenderSubset( object subset ) { }
	protected virtual void PostRenderSubset( object subset ) { }

	protected virtual IEnumerable<VBO2SubsetPart<TItem, TVertex>> Convert( TItem item )
	{
		yield return new VBO2SubsetPart<TItem, TVertex>();
	}

	void RebuildData()
	{
		int totalLength = 0;
		foreach ( var ss in _subsets.Values )
		{
			if ( ss.IsDirty )
			{
				ss.Rebuild();
			}
			totalLength += ss.Data.Count;
		}

		_data = new TVertex[totalLength];

		int index = 0;
		foreach ( var ss in _subsets.Values )
		{
			ss.Index = index;

			for ( int i = ss.Index; i < ss.Index + ss.Data.Count; i++ )
			{
				_data[i] = ss.Data[i - ss.Index];
			}

			index += ss.Data.Count;
		}
	}

	public virtual void DrawArrays()
	{
		GL.BindBuffer( BufferTarget.ArrayBuffer, _vboId );

		EnsureSubsets();

		int stride = _spec.Stride;
		for ( int j = 0; j < _spec.Indices.Count; j++ )
		{
			var ai = _spec.Indices[j];
			GL.EnableVertexAttribArray( j );
			GL.VertexAttribPointer( j, ai.Length, ai.Type, false, stride, ai.Offset );
		}

		foreach ( var kvp in _subsets )
		{
			GL.DrawArrays( this.Mode, kvp.Value.Index, kvp.Value.Data.Count );
		}

		GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
	}

	public virtual void Render()
	{
		GL.BindBuffer( BufferTarget.ArrayBuffer, _vboId );

		EnsureSubsets();

		int stride = _spec.Stride;
		for ( int j = 0; j < _spec.Indices.Count; j++ )
		{
			var ai = _spec.Indices[j];
			GL.EnableVertexAttribArray( j );
			GL.VertexAttribPointer( j, ai.Length, ai.Type, false, stride, ai.Offset );
		}

		PreRender();

		for ( int i = 0; i < Passes; i++ )
		{
			Pass = i;
			int drawcalls = 0;
			foreach ( var kvp in _subsets )
			{
				drawcalls++;
				PreRenderSubset( kvp.Key );
				GL.DrawArrays( Mode, kvp.Value.Index, kvp.Value.Data.Count );
				PostRenderSubset( kvp.Key );
			}
		}

		PostRender();

		GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
	}

	void EnsureSubsets()
	{
		if ( !_isDirty )
			return;

		RebuildData();

		if ( _data.Length * _dataSize > _bufferSize )
		{
			GL.BufferData( BufferTarget.ArrayBuffer, (IntPtr)(_data.Length * _dataSize), _data, BufferUsageHint.DynamicDraw );
			_bufferSize = _data.Length * _dataSize;
		}
		else
		{
			GL.BufferSubData( BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(_data.Length * _dataSize), _data );
		}

		_isDirty = false;
	}

}
