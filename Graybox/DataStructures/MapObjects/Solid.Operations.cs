
namespace Graybox.DataStructures.MapObjects;

public partial class Solid
{
	public (Solid, Face) Extrude( Face faceToExtrude, float distance, Vector3 eulerRotation, IDGenerator generator, Vector3? pivot = null )
	{
		Solid newSolid = new Solid( generator.GetNextObjectID() )
		{
			Colour = ColorUtility.GetRandomBrushColour(),
			// Copy other relevant properties
		};

		// Calculate face center
		Vector3 faceCenter = faceToExtrude.CalculateCenter();
		pivot ??= faceCenter;

		// Get original vertices
		var baseVertices = faceToExtrude.Vertices.Select( v => v.Position ).ToList();

		// Extrude and rotate vertices
		var extrudedVertices = ExtrudeAndRotateFace( baseVertices.ToArray(), faceToExtrude.Plane.Normal, pivot.Value, eulerRotation, distance );

		// Add original face (flipped)
		Face originalFace = new Face( generator.GetNextFaceID() )
		{
			Parent = newSolid,
			Plane = new Plane( -faceToExtrude.Plane.Normal, faceToExtrude.Plane.DistanceFromOrigin ),
			TextureRef = faceToExtrude.TextureRef.Clone(),
			Colour = ColorUtility.GetRandomBrushColour()
		};
		for ( int i = baseVertices.Count - 1; i >= 0; i-- )
		{
			originalFace.Vertices.Add( new Vertex( baseVertices[i], originalFace ) );
		}
		newSolid.Faces.Add( originalFace );

		// Add extruded face
		Face extrudedFace = new Face( generator.GetNextFaceID() )
		{
			Parent = newSolid,
			Plane = new Plane( extrudedVertices[0], extrudedVertices[1], extrudedVertices[2] ),
			TextureRef = faceToExtrude.TextureRef.Clone(),
		};

		foreach ( var vertex in extrudedVertices )
		{
			extrudedFace.Vertices.Add( new Vertex( vertex, extrudedFace ) );
		}
		newSolid.Faces.Add( extrudedFace );

		// Add connecting faces
		for ( int i = 0; i < baseVertices.Count; i++ )
		{
			int nextIndex = (i + 1) % baseVertices.Count;

			Face connectingFace = new Face( generator.GetNextFaceID() )
			{
				Parent = newSolid,
				TextureRef = faceToExtrude.TextureRef.Clone(),
				Colour = ColorUtility.GetRandomBrushColour()
			};

			connectingFace.Vertices.Add( new Vertex( baseVertices[i], connectingFace ) );
			connectingFace.Vertices.Add( new Vertex( baseVertices[nextIndex], connectingFace ) );
			connectingFace.Vertices.Add( new Vertex( extrudedVertices[nextIndex], connectingFace ) );
			connectingFace.Vertices.Add( new Vertex( extrudedVertices[i], connectingFace ) );

			connectingFace.Plane = new Plane( connectingFace.Vertices[0].Position, connectingFace.Vertices[1].Position, connectingFace.Vertices[2].Position );
			newSolid.Faces.Add( connectingFace );
		}

		foreach ( var face in newSolid.Faces )
		{
			if ( face != extrudedFace && face != originalFace )
			{
				face.Colour = ColorUtility.GetRandomBrushColour();
				face.TextureRef = GuessBestTexture( face ) ?? face.TextureRef;
				face.AlignTextureToWorld();
				face.UpdateBoundingBox();
				face.CalculateTextureCoordinates( true );
			}
		}

		newSolid.Refresh();

		return (newSolid, extrudedFace);
	}

	private Vector3[] ExtrudeAndRotateFace( Vector3[] originalVertices, Vector3 faceNormal, Vector3 pivot, Vector3 eulerRotation, float distance )
	{
		Vector3 eulerRotationInRadians = eulerRotation * (float)(Math.PI / 180f);
		Quaternion rotation = Quaternion.FromEulerAngles( eulerRotationInRadians );

		Vector3[] newVertices = new Vector3[originalVertices.Length];

		for ( int i = 0; i < originalVertices.Length; i++ )
		{
			Vector3 extrudedVertex = originalVertices[i] + (faceNormal.Normalized() * distance);
			Vector3 directionFromCenter = extrudedVertex - pivot;
			directionFromCenter = rotation * directionFromCenter;
			extrudedVertex = pivot + directionFromCenter;
			newVertices[i] = extrudedVertex;
		}

		return newVertices;
	}

}
