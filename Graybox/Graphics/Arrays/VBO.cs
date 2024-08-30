
using System.Runtime.InteropServices;

namespace Graybox.Graphics;

public class Subset
{
	public int GroupID { get; set; }
	public Type Type { get; set; }
	public object Instance { get; set; }
	public int Start { get; set; }
	public int Count { get; set; }

	public Subset( int groupId, Type type, object instance, int start, int count )
	{
		GroupID = groupId;
		Type = type;
		Instance = instance;
		Start = start;
		Count = count;
	}
}

public abstract class VBO<TIn, TOut> : IDisposable
	where TOut : struct
{
	private ArraySpecification Specification { get; set; }
	private readonly int _size;

	private readonly List<TOut> _data;
	private readonly Dictionary<int, List<uint>> _indices;
	private readonly Dictionary<int, int> _subsetState;
	private readonly Dictionary<int, List<Subset>> _subsets;
	private readonly Dictionary<object, int> _offsets;

	private int _array = -1;
	private int _elementArray = -1;

	protected VBO( IEnumerable<TIn> data )
	{
		Specification = new ArraySpecification( typeof( TOut ) );
		_size = Marshal.SizeOf( typeof( TOut ) );

		_data = new List<TOut>();
		_indices = new Dictionary<int, List<uint>>();
		_subsetState = new Dictionary<int, int>();
		_subsets = new Dictionary<int, List<Subset>>();
		_offsets = new Dictionary<object, int>();

		Update( data );
	}

	public void Update( IEnumerable<TIn> data )
	{
		Clear();
		CreateArray( data );
		Commit();
	}

	protected void Begin()
	{
		if ( begun ) return;
		begun = true;
		GL.BindBuffer( BufferTarget.ArrayBuffer, _array );
		//CheckGLError( "BindBuffer ArrayBuffer" );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, _elementArray );
		//CheckGLError( "BindBuffer ElementArrayBuffer" );

		int stride = Specification.Stride;
		for ( int j = 0; j < Specification.Indices.Count; j++ )
		{
			ArrayIndex ai = Specification.Indices[j];
			GL.EnableVertexAttribArray( j );
			//CheckGLError( "EnableVertexAttribArray" );
			GL.VertexAttribPointer( j, ai.Length, ai.Type, false, stride, ai.Offset );
			//CheckGLError( "VertexAttribPointer" );
		}
	}

	bool begun = false;
	protected void End()
	{
		if ( !begun ) return;
		begun = false;
		GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );
	}

	protected void Render( PrimitiveType mode, Subset subset )
	{
		if ( !begun )
		{
			throw new System.Exception( "VBO not begun" );
		}
		GL.DrawElements( mode, subset.Count, DrawElementsType.UnsignedInt, subset.Start * sizeof( uint ) );
		//CheckGLError( "DrawElements" );
	}

	[System.Diagnostics.Conditional( "DEBUG" )]
	private void CheckGLError( string location )
	{
		ErrorCode error = GL.GetError();
		if ( error != ErrorCode.NoError )
		{
			System.Diagnostics.Debug.WriteLine( $"OpenGL error {typeof( TOut )} at {location}: {error}" );
		}
	}

	public void Dispose()
	{
		if ( _array >= 0 )
		{
			GL.DeleteBuffers( 1, ref _array );
			_array = -1;
		}
		if ( _elementArray >= 0 )
		{
			GL.DeleteBuffers( 1, ref _elementArray );
			_elementArray = -1;
		}
	}

	private void Clear()
	{
		_data.Clear();
		_indices.Clear();
		_subsetState.Clear();
		_subsets.Clear();
		_offsets.Clear();
	}

	protected void Update( int index, IEnumerable<TOut> data )
	{
		TOut[] d = data.ToArray();
		GL.BindBuffer( BufferTarget.ArrayBuffer, _array );
		GL.BufferSubData( BufferTarget.ArrayBuffer, new IntPtr( index * _size ), new IntPtr( d.Length * _size ), d );
		GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
	}

	private void Commit()
	{
		TOut[] data = _data.ToArray();
		uint[] elementData = new uint[_indices.Sum( x => x.Value.Count )];
		int idx = 0;
		foreach ( KeyValuePair<int, List<uint>> list in _indices )
		{
			list.Value.CopyTo( 0, elementData, idx, list.Value.Count );
			if ( idx > 0 && _subsets.ContainsKey( list.Key ) )
			{
				foreach ( Subset subset in _subsets[list.Key] )
				{
					subset.Start += idx;
				}
			}
			idx += list.Value.Count;
		}

		// Array buffer
		if ( _array < 0 ) GL.GenBuffers( 1, out _array );
		GL.BindBuffer( BufferTarget.ArrayBuffer, _array );
		GL.BufferData( BufferTarget.ArrayBuffer, new IntPtr( data.Length * _size ), data, BufferUsageHint.StreamDraw );
		CheckGLError( "BufferData ArrayBuffer" );
		GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );

		// Element buffer
		if ( _elementArray < 0 ) GL.GenBuffers( 1, out _elementArray );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, _elementArray );
		GL.BufferData( BufferTarget.ElementArrayBuffer, new IntPtr( elementData.Length * sizeof( uint ) ), elementData, BufferUsageHint.StreamDraw );
		CheckGLError( "BufferData ElementArrayBuffer" );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, 0 );

		_data.Clear();
		_indices.Clear();
		_subsetState.Clear();
	}

	protected abstract void CreateArray( IEnumerable<TIn> data );

	protected void StartSubset( int groupId )
	{
		if ( _subsetState.ContainsKey( groupId ) ) throw new Exception( "Cannot start two subsets for the same group." );

		if ( !_indices.ContainsKey( groupId ) ) _indices.Add( groupId, new List<uint>() );
		List<uint> list = _indices[groupId];

		_subsetState.Add( groupId, list.Count );
	}

	protected void PushSubset<T>( int groupId, T context )
	{
		if ( !_subsetState.ContainsKey( groupId ) ) throw new Exception( "No subset exists for this group." );

		int ss = _subsetState[groupId];
		_subsetState.Remove( groupId );

		if ( !_indices.ContainsKey( groupId ) ) _indices.Add( groupId, new List<uint>() );
		List<uint> list = _indices[groupId];

		int num = list.Count - ss;
		if ( num == 0 ) return;

		if ( !_subsets.ContainsKey( groupId ) ) _subsets.Add( groupId, new List<Subset>() );
		_subsets[groupId].Add( new Subset( groupId, typeof( T ), context, ss, num ) );
	}

	protected IEnumerable<Subset> GetSubsets( int groupId )
	{
		if ( !_subsets.ContainsKey( groupId ) )
			return new List<Subset>();

		return _subsets[groupId];
	}

	protected IEnumerable<Subset> GetSubsets<T>( int groupId )
	{
		if ( !_subsets.ContainsKey( groupId ) )
			yield break;

		Type t = typeof( T );

		foreach ( var subset in _subsets[groupId] )
		{
			if ( subset.Type == t )
				yield return subset;
		}
	}

	protected void PushIndex( int groupId, uint start, IEnumerable<uint> indices )
	{
		if ( !_indices.ContainsKey( groupId ) ) _indices.Add( groupId, new List<uint>() );
		List<uint> list = _indices[groupId];
		list.AddRange( indices.Select( x => x + start ) );
	}

	protected void PushOffset<T>( T obj )
	{
		_offsets.Add( obj, _data.Count );
	}

	protected int GetOffset<T>( T obj )
	{
		if ( !_offsets.ContainsKey( obj ) ) return -1;
		return _offsets[obj];
	}

	protected uint PushData( IEnumerable<TOut> data )
	{
		int index = _data.Count;
		_data.AddRange( data );
		return (uint)index;
	}

	protected static IEnumerable<uint> Triangulate( int num )
	{
		for ( uint i = 1; i < num - 1; i++ )
		{
			yield return 0;
			yield return i;
			yield return i + 1;
		}
	}

	protected static IEnumerable<uint> Linearise( int num )
	{
		for ( uint i = 0; i < num; i++ )
		{
			uint ni = (uint)((i + 1) % num);
			yield return i;
			yield return ni;
		}
	}
}
