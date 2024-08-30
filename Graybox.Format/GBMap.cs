
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using ProtoBuf;
using System.IO.Compression;
using System.Drawing;

namespace Graybox.Format;

public enum GBObjectTypes
{
	Default,
	Solid,
	Entity,
	World,
	Group,
	Light
}

public enum GBLightTypes
{
	Point,
	Directional
}

[ProtoContract]
public class GBMap
{
	[ProtoMember(1)]
	public string Name { get; set; } = string.Empty;
	[ProtoMember(2)]
	public GBLightmapData LightmapData { get; set; } = new();
	[ProtoMember(3)]
	public GBObject World { get; set; } = new();
	[ProtoMember(4)]
	public List<GBAsset> Assets { get; set; } = new();
	[ProtoMember(5)]
	public GBEnvironmentData EnvironmentData { get; set; } = new();

	public static GBMap FromFile(string filePath)
	{
		var compressedData = File.ReadAllBytes(filePath);
		byte[] decompressedBytes;
		using (var inputStream = new MemoryStream(compressedData))
		using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
		using (var outputStream = new MemoryStream())
		{
			gzipStream.CopyTo(outputStream);
			decompressedBytes = outputStream.ToArray();
		}

		GBMap result;
		using (var memoryStream = new MemoryStream(decompressedBytes))
		{
			result = Serializer.Deserialize<GBMap>(memoryStream);
		}

		return result;
	}

	public static void ToFile(GBMap map, string filePath)
	{
		byte[] protoBytes;
		using (var memoryStream = new MemoryStream())
		{
			Serializer.Serialize(memoryStream, map);
			protoBytes = memoryStream.ToArray();
		}

		byte[] compressedBytes;
		using (var outputStream = new MemoryStream())
		{
			using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest, true))
			{
				gzipStream.Write(protoBytes, 0, protoBytes.Length);
			}
			compressedBytes = outputStream.ToArray();
		}

		File.WriteAllBytes(filePath, compressedBytes);
	}

}

[ProtoContract]
[ProtoInclude(1, typeof(GBTextureAsset))]
[ProtoInclude(2, typeof(GBMaterialAsset))]
[JsonConverter(typeof(GBAssetConverter))]
public class GBAsset
{
	[ProtoMember(101)]
	public string RelativePath { get; set; }
}

[ProtoContract]
public class GBTextureAsset : GBAsset
{
	[ProtoMember(201)]
	public int Width { get; set; }
	[ProtoMember(202)]
	public int Height { get; set; }
	[ProtoMember(203)]
	[JsonConverter(typeof(OptimizedByteArrayConverter))]
	public byte[] Data { get; set; }
	[ProtoMember(204)]
	public bool Transparent { get; set; }
}

[ProtoContract]
public class GBMaterialAsset : GBAsset
{
	[ProtoMember(301)]
	public Dictionary<string, string> Properties { get; set; } = new();
}

[ProtoContract]
public class GBLightmapData
{
	[ProtoMember(401)]
	public List<GBLightmap> Lightmaps { get; set; } = new();
}

[ProtoContract]
public class GBEnvironmentData
{
	[ProtoMember(1)]
	public bool FogEnabled { get; set; }
	[ProtoMember(2)]
	public GBVec4 FogColor { get; set; }
	[ProtoMember(3)]
	public float FogDensity { get; set; }
	[ProtoMember(4)]
	public GBVec4 AmbientColor { get; set; }
	[ProtoMember(5)]
	public string Skybox { get; set; }
	[ProtoMember(6)]
	public GBVec4 ClearColor { get; set; }
}

[ProtoContract]
public class GBLightmap
{
	[ProtoMember(1)]
	public int Width { get; set; }
	[ProtoMember(2)]
	public int Height { get; set; }
	[ProtoMember(3)]
	[JsonConverter(typeof(OptimizedFloatArrayConverter))]
	public float[] Data { get; set; }
	[ProtoMember(4)]
	[JsonConverter(typeof(OptimizedFloatArrayConverter))]
	public float[] DirectionalData { get; set; }
	[ProtoMember(5)]
	[JsonConverter(typeof(OptimizedFloatArrayConverter))]
	public float[] ShadowMaskData { get; set; }
	[ProtoMember(6)]
	public GBVec2 DirectionalSize { get; set; }
	[ProtoMember(7)]
	public GBVec2 ShadowMaskSize { get; set; }
}

[ProtoContract]
[ProtoInclude(1, typeof(GBWorld))]
[ProtoInclude(2, typeof(GBGroup))]
[ProtoInclude(3, typeof(GBSolid))]
[ProtoInclude(4, typeof(GBEntity))]
[ProtoInclude(5, typeof(GBLight))]
[JsonConverter(typeof(GBObjectConverter))]
public class GBObject
{
	[ProtoMember(101)]
	public virtual GBObjectTypes Type { get; set; } = GBObjectTypes.Default;
	[ProtoMember(102)]
	public string Name { get; set; } = string.Empty;
	[ProtoMember(103)]
	public long ID { get; set; }
	[ProtoMember(104)]
	public List<GBObject> Children { get; set; } = new();
}

[ProtoContract]
public class GBWorld : GBObject
{
	[ProtoMember(201)]
	public override GBObjectTypes Type { get; set; } = GBObjectTypes.World;
}

[ProtoContract]
public class GBGroup : GBObject
{
	[ProtoMember(301)]
	public override GBObjectTypes Type { get; set; } = GBObjectTypes.Group;
}

[ProtoContract]
public class GBSolid : GBObject
{
	[ProtoMember(401)]
	public override GBObjectTypes Type { get; set; } = GBObjectTypes.Solid;
	[ProtoMember(402)]
	public List<GBFace> Faces { get; set; } = new List<GBFace>();
}

[ProtoContract]
public class GBEntity : GBObject
{
	[ProtoMember(501)]
	public override GBObjectTypes Type { get; set; } = GBObjectTypes.Entity;
	[ProtoMember(502)]
	public string ClassName { get; set; } = string.Empty;
	[ProtoMember(503)]
	public Dictionary<string, string> Properties { get; set; } = new();
}

[ProtoContract]
public class GBLight : GBObject
{
	[ProtoMember(601)]
	public override GBObjectTypes Type { get; set; } = GBObjectTypes.Light;
	[ProtoMember(602)]
	public GBLightInfo LightInfo { get; set; } = new();
}

[ProtoContract]
public class GBTextureReference
{
	[ProtoMember(1)]
	public string AssetPath { get; set; } = string.Empty;
	[ProtoMember(2)]
	public GBVec3 UAxis { get; set; }
	[ProtoMember(3)]
	public GBVec3 VAxis { get; set; }
	/// <summary>
	/// Shift is X and Y, Scale is Z and W
	/// </summary>
	[ProtoMember(4)]
	public GBVec4 ShiftScale { get; set; }
	[ProtoMember(5)]
	public float Rotation { get; set; }
}

[ProtoContract]
public class GBFace
{
	[ProtoMember(1)]
	public long ID { get; set; }
	[ProtoMember(2)]
	public GBVec4 Color { get; set; }
	[ProtoMember(3)]
	public GBVec3 Normal { get; set; } = new GBVec3();
	[ProtoMember(4)]
	public GBTextureReference Texture { get; set; } = new();
	[ProtoMember(5)]
	public List<GBVertex> Vertices { get; set; } = new List<GBVertex>();
	[ProtoMember(6)]
	public int TexelSize { get; set; } = 4;
	[ProtoMember(7)]
	public bool DisableInLightmap { get; set; } = false;
}

[ProtoContract]
public partial class GBVertex
{
	[ProtoMember(1)]
	public GBVec3 Position { get; set; }
	[ProtoMember(2)]
	public GBVec2 UV0 { get; set; }
	[ProtoMember(3)]
	public GBVec2 UV1 { get; set; }
}

[ProtoContract]
[JsonConverter(typeof(GBVec2Converter))]
public partial struct GBVec2
{
	[ProtoMember(1)]
	public float X { get; set; }
	[ProtoMember(2)]
	public float Y { get; set; }

	public GBVec2(float x, float y)
	{
		X = x;
		Y = y;
	}
}

[ProtoContract]
public partial struct GBLightInfo
{
	[ProtoMember(1)]
	public GBVec3 Position { get; set; }
	[ProtoMember(2)]
	public GBVec3 Direction { get; set; }
	[ProtoMember(3)]
	public float Range { get; set; }
	[ProtoMember(4)]
	public float Intensity { get; set; }
	[ProtoMember(5)]
	public GBVec4 Color { get; set; }
	[ProtoMember(6)]
	public GBLightTypes Type { get; set; }

}

[ProtoContract]
[JsonConverter(typeof(GBVec3Converter))]
public partial struct GBVec3
{
	[ProtoMember(1)]
	public float X { get; set; }
	[ProtoMember(2)]
	public float Y { get; set; }
	[ProtoMember(3)]
	public float Z { get; set; }

	public GBVec3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}
}

[ProtoContract]
[JsonConverter(typeof(GBVec4Converter))]
public partial struct GBVec4
{
	[ProtoMember(1)]
	public float X { get; set; }
	[ProtoMember(2)]
	public float Y { get; set; }
	[ProtoMember(3)]
	public float Z { get; set; }
	[ProtoMember(4)]
	public float W { get; set; }

	public GBVec4(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
}

public class GBVec2Converter : JsonConverter<GBVec2>
{
	public override void Write(Utf8JsonWriter writer, GBVec2 value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.X} {value.Y}");
	}

	public override GBVec2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var parts = reader.GetString().Split(' ');
			return new GBVec2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
		}
		throw new JsonException("Unexpected token type");
	}
}

public class GBVec3Converter : JsonConverter<GBVec3>
{
	public override void Write(Utf8JsonWriter writer, GBVec3 value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.X} {value.Y} {value.Z}");
	}

	public override GBVec3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var parts = reader.GetString().Split(' ');
			return new GBVec3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
		}
		throw new JsonException("Unexpected token type");
	}
}

public class GBVec4Converter : JsonConverter<GBVec4>
{
	public override void Write(Utf8JsonWriter writer, GBVec4 value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.X} {value.Y} {value.Z} {value.W}");
	}

	public override GBVec4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var parts = reader.GetString().Split(' ');
			return new GBVec4(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
		}
		throw new JsonException("Unexpected token type");
	}
}

public class GBObjectConverter : JsonConverter<GBObject>
{
	public override GBObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}

		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			if (!doc.RootElement.TryGetProperty("Type", out JsonElement typeElement))
			{
				throw new JsonException();
			}

			GBObject item = (GBObjectTypes)typeElement.GetInt32() switch
			{
				GBObjectTypes.Solid => new GBSolid(),
				GBObjectTypes.Entity => new GBEntity(),
				GBObjectTypes.World => new GBWorld(),
				GBObjectTypes.Group => new GBGroup(),
				GBObjectTypes.Light => new GBLight(),
				_ => new GBObject(),
			};

			return (GBObject)JsonSerializer.Deserialize(doc.RootElement.GetRawText(), item.GetType(), options);
		}
	}

	public override void Write(Utf8JsonWriter writer, GBObject value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, value.GetType(), options);
	}
}

public class OptimizedFloatArrayConverter : JsonConverter<float[]>
{
	private const int Scale = 1000;
	private const float MinValue = -10;
	private const float MaxValue = 10;
	private const int ChunkSize = 65536;

	public override float[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
			throw new JsonException("Expected start of array for float data.");

		List<float[]> chunks = new List<float[]>();
		int[] buffer = ArrayPool<int>.Shared.Rent(ChunkSize);
		long totalCount = 0;
		int bufferIndex = 0;

		try
		{
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			{
				if (reader.TokenType != JsonTokenType.Number)
					throw new JsonException("Expected number in float array.");

				buffer[bufferIndex++] = reader.GetInt32();

				if (bufferIndex == ChunkSize)
				{
					chunks.Add(ProcessChunk(buffer, bufferIndex));
					totalCount += bufferIndex;
					bufferIndex = 0;
				}
			}

			if (bufferIndex > 0)
			{
				chunks.Add(ProcessChunk(buffer, bufferIndex));
				totalCount += bufferIndex;
			}
		}
		finally
		{
			ArrayPool<int>.Shared.Return(buffer);
		}

		return ReconstructResultArray(chunks, totalCount);
	}
	public override void Write(Utf8JsonWriter writer, float[] value, JsonSerializerOptions options)
	{
		if (value == null || value.Length == 0)
		{
			writer.WriteStartArray();
			writer.WriteEndArray();
			return;
		}

		int totalChunks = (value.Length + ChunkSize - 1) / ChunkSize;
		var chunks = new ConcurrentDictionary<int, byte[]>();

		Parallel.For(0, totalChunks, chunkIndex =>
		{
			int start = chunkIndex * ChunkSize;
			int end = Math.Min(start + ChunkSize, value.Length);
			byte[] buffer = ArrayPool<byte>.Shared.Rent((end - start) * 12); // Max 11 chars per number + comma
			int written = 0;

			for (int i = start; i < end; i++)
			{
				if (i > start)
					buffer[written++] = (byte)',';

				int scaledValue = (int)(Math.Clamp(value[i], MinValue, MaxValue) * Scale);
				written += Encoding.UTF8.GetBytes(scaledValue.ToString(), buffer.AsSpan(written));
			}

			chunks[chunkIndex] = buffer.AsSpan(0, written).ToArray();
			ArrayPool<byte>.Shared.Return(buffer);
		});

		writer.WriteStartArray();
		for (int i = 0; i < totalChunks; i++)
		{
			if (chunks.TryGetValue(i, out byte[] chunk))
			{
				writer.WriteRawValue(chunk, skipInputValidation: true);
			}
		}
		writer.WriteEndArray();
	}

	private float[] ProcessChunk(int[] buffer, int count)
	{
		float[] floatChunk = new float[count];
		for (int i = 0; i < count; i++)
		{
			floatChunk[i] = buffer[i] / (float)Scale;
		}
		return floatChunk;
	}

	private float[] ReconstructResultArray(List<float[]> chunks, long totalCount)
	{
		float[] result = new float[totalCount];
		int offset = 0;
		foreach (var chunk in chunks)
		{
			Array.Copy(chunk, 0, result, offset, chunk.Length);
			offset += chunk.Length;
		}

		return result;
	}
}

public class OptimizedByteArrayConverter : JsonConverter<byte[]>
{
	private const int ChunkSize = 65536;

	public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			// Handle base64 encoded string
			return Convert.FromBase64String(reader.GetString());
		}

		if (reader.TokenType != JsonTokenType.StartArray)
			throw new JsonException("Expected start of array or base64 string for byte data.");

		List<byte[]> chunks = new List<byte[]>();
		byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
		long totalCount = 0;
		int bufferIndex = 0;

		try
		{
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			{
				if (reader.TokenType == JsonTokenType.Number)
				{
					buffer[bufferIndex++] = reader.GetByte();
				}
				else if (reader.TokenType == JsonTokenType.String)
				{
					// Handle individual bytes as strings
					if (byte.TryParse(reader.GetString(), out byte byteValue))
					{
						buffer[bufferIndex++] = byteValue;
					}
					else
					{
						throw new JsonException("Invalid byte value in array.");
					}
				}
				else
				{
					throw new JsonException("Expected number or string in byte array.");
				}

				if (bufferIndex == ChunkSize)
				{
					chunks.Add(buffer[..bufferIndex].ToArray());
					totalCount += bufferIndex;
					bufferIndex = 0;
				}
			}

			if (bufferIndex > 0)
			{
				chunks.Add(buffer[..bufferIndex].ToArray());
				totalCount += bufferIndex;
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		return ReconstructResultArray(chunks, totalCount);
	}

	public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
	{
		if (value == null || value.Length == 0)
		{
			writer.WriteStartArray();
			writer.WriteEndArray();
			return;
		}

		// Option to write as base64 string if the array is large
		//if (value.Length > 1024 * 1024) // 1 MB threshold, adjust as needed
		//{
		//	writer.WriteStringValue(Convert.ToBase64String(value));
		//	return;
		//}

		writer.WriteStartArray();
		foreach (byte b in value)
		{
			writer.WriteNumberValue(b);
		}
		writer.WriteEndArray();
	}

	private byte[] ReconstructResultArray(List<byte[]> chunks, long totalCount)
	{
		byte[] result = new byte[totalCount];
		int offset = 0;
		foreach (var chunk in chunks)
		{
			Array.Copy(chunk, 0, result, offset, chunk.Length);
			offset += chunk.Length;
		}
		return result;
	}
}

public class GBAssetConverter : JsonConverter<GBAsset>
{
	public override GBAsset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Expected start of object for GBAsset data.");
		}

		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			if (!doc.RootElement.TryGetProperty("Type", out JsonElement typeElement))
			{
				throw new JsonException("Missing 'Type' property in GBAsset.");
			}

			string assetType = typeElement.GetString();
			GBAsset asset = assetType switch
			{
				"Texture" => JsonSerializer.Deserialize<GBTextureAsset>(doc.RootElement.GetRawText(), options),
				"Material" => JsonSerializer.Deserialize<GBMaterialAsset>(doc.RootElement.GetRawText(), options),
				_ => JsonSerializer.Deserialize<GBAsset>(doc.RootElement.GetRawText(), options),
			};

			return asset;
		}
	}

	public override void Write(Utf8JsonWriter writer, GBAsset value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		// Write common properties
		writer.WriteString("RelativePath", value.RelativePath);

		// Write type-specific properties
		switch (value)
		{
			case GBTextureAsset textureAsset:
				writer.WriteString("Type", "Texture");
				writer.WriteNumber("Width", textureAsset.Width);
				writer.WriteNumber("Height", textureAsset.Height);
				writer.WriteBoolean("Transparent", textureAsset.Transparent);
				writer.WritePropertyName("Data");
				JsonSerializer.Serialize(writer, textureAsset.Data, options);
				break;

			case GBMaterialAsset materialAsset:
				writer.WriteString("Type", "Material");
				writer.WritePropertyName("Properties");
				JsonSerializer.Serialize(writer, materialAsset.Properties, options);
				break;

			default:
				writer.WriteString("Type", "Default");
				break;
		}

		writer.WriteEndObject();
	}
}

