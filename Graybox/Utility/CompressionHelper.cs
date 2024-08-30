using System.Text.Json;
using System.IO.Compression;
using ProtoBuf;

namespace Graybox.Utility;

public class CompressionHelper
{

	public static byte[] CompressAndSerialize<T>( T obj )
	{
		var profiler = new Profiler();
		var toJson = profiler.Begin( "Convert to JSON" );
		var jsonBytes = JsonSerializer.SerializeToUtf8Bytes( obj );
		toJson.Dispose();

		var compress = profiler.Begin( "Compress" );
		using ( var memoryStream = new MemoryStream() )
		{
			using ( var gzipStream = new GZipStream( memoryStream, CompressionLevel.Fastest, true ) )
			{
				gzipStream.Write( jsonBytes, 0, jsonBytes.Length );
			}
			compress.Dispose();
			Debug.Profile( profiler );
			return memoryStream.ToArray();
		}
	}

	public static T DecompressAndDeserialize<T>( byte[] compressedData )
	{
		using ( var memoryStream = new MemoryStream( compressedData ) )
		using ( var gzipStream = new GZipStream( memoryStream, CompressionMode.Decompress ) )
		using ( var reader = new StreamReader( gzipStream ) )
		{
			var profiler = new Profiler();
			var decompress = profiler.Begin( "Decompress" );
			string json = reader.ReadToEnd();
			decompress.Dispose();
			var deserialize = profiler.Begin( "Deserialize" );
			var result = JsonSerializer.Deserialize<T>( json );
			deserialize.Dispose();
			Debug.Profile( profiler );
			return result;
		}
	}

}

public static class ProtobufCompressionUtils
{
	public static byte[] CompressAndSerialize<T>( T obj )
	{
		var profiler = new Profiler();
		var serialize = profiler.Begin( "Serialize to Protobuf" );
		byte[] protoBytes;
		using ( var memoryStream = new MemoryStream() )
		{
			Serializer.Serialize( memoryStream, obj );
			protoBytes = memoryStream.ToArray();
		}
		serialize.Dispose();

		var compress = profiler.Begin( "Compress" );
		byte[] compressedBytes;
		using ( var outputStream = new MemoryStream() )
		{
			using ( var gzipStream = new GZipStream( outputStream, CompressionLevel.Fastest, true ) )
			{
				gzipStream.Write( protoBytes, 0, protoBytes.Length );
			}
			compressedBytes = outputStream.ToArray();
		}
		compress.Dispose();

		Debug.Profile( profiler );
		return compressedBytes;
	}

	public static T DecompressAndDeserialize<T>( byte[] compressedData )
	{
		var profiler = new Profiler();
		var decompress = profiler.Begin( "Decompress" );
		byte[] decompressedBytes;
		using ( var inputStream = new MemoryStream( compressedData ) )
		using ( var gzipStream = new GZipStream( inputStream, CompressionMode.Decompress ) )
		using ( var outputStream = new MemoryStream() )
		{
			gzipStream.CopyTo( outputStream );
			decompressedBytes = outputStream.ToArray();
		}
		decompress.Dispose();

		var deserialize = profiler.Begin( "Deserialize from Protobuf" );
		T result;
		using ( var memoryStream = new MemoryStream( decompressedBytes ) )
		{
			result = Serializer.Deserialize<T>( memoryStream );
		}
		deserialize.Dispose();

		Debug.Profile( profiler );
		return result;
	}
}
