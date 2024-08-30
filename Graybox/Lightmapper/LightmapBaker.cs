
using Graybox.DataStructures.MapObjects;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
//using Aardvark.OpenImageDenoise;
//using Aardvark.Base;

namespace Graybox.Lightmapper;

public class LightmapBaker
{

	public enum BakeStatus
	{
		None,
		Baking,
		Finished,
		Cancelled
	}

	struct LightmapJob
	{
		public int Index;
		public Rect PackedRect;
		public LightmapFace LightmapFace;
		public LightInfo[] Lights;
	}

	public float Progress { get; private set; }

	float[] _lightmapData;
	//float[] _directionalData;
	//float[] _shadowMaskData;
	const int MAX_LIGHT_COUNT = 4;
	LightmapConfig _config;
	Profiler _profile;

	public BakeStatus Status { get; private set; }

	CancellationTokenSource CancelToken;
	CancellationToken Token;

	public void Cancel()
	{
		CancelToken?.Cancel();
		CancelToken = null;
	}

	public async Task<LightmapResult> BakeAsync( LightmapConfig config )
	{
		CancelToken = new();
		Token = CancelToken.Token;

		LightmapResult result = null;
		try
		{
			result = await BakeInternalAsync( config );
		}
		catch ( Exception e )
		{
			result ??= new();
			result.Success = false;
			result.ErrorMessage = "Lightmap Bake Exception:" + e;
		}
		finally
		{
			CancelToken?.Dispose();
			CancelToken = null;
			Token = default;
			Progress = 0f;
			Status = BakeStatus.None;
			GC.Collect();
			GC.Collect();
		}

		return result;
	}

	async Task<LightmapResult> BakeInternalAsync( LightmapConfig config )
	{
		var result = new LightmapResult();
		result.Scene = config.Scene;
		result.Lightmaps = new();

		if ( Status == BakeStatus.Baking )
		{
			result.Success = false;
			result.ErrorMessage = "This baker is already baking";
			return result;
		}

		Status = BakeStatus.Baking;

		if ( config.AmbientColor.R == 0 && config.AmbientColor.G == 0 && config.AmbientColor.B == 0 )
		{
			config.AmbientColor = new( .0001f, .0001f, .0001f, 1f );
		}

		Progress = 0;

		_profile = new();
		{
			using var _ = _profile.Begin( "Bake Lights" );
			Debug.LogExciting( "Baking lightmaps..." );

			_config = config;

			var allRects = new List<Rect>();
			var allFaces = new List<LightmapFace>();
			var allJobs = new List<LightmapJob>();
			var allSolids = _config.Solids.ToArray();
			var allLights = _config.Lights.ToArray();
			List<Rect> packedRects = null;

			using ( var __ = _profile.Begin( "Pack UVs" ) )
			{
				foreach ( var solid in allSolids )
				{
					foreach ( var face in solid.Faces )
					{
						foreach ( var v in face.Vertices )
						{
							v.LightmapU = -1000;
							v.LightmapV = -1000;
						}

						if ( face.TextureRef?.Texture == null || face.TextureRef.Texture.GraphicsID == 0 ) continue;
						if ( face.DisableInLightmap ) continue;

						var lmFace = new LightmapFace()
						{
							Face = face,
							Solid = solid,
						};
						var scaledBounds = CalculateUvBounds( face );
						scaledBounds = new( 0, 0, scaledBounds.Width, scaledBounds.Height );
						allRects.Add( scaledBounds );
						allFaces.Add( lmFace );
					}
				}

				try
				{
					var packResult = RectPacker.Pack( new( 0, 0, _config.Width, _config.Height ), allRects, 4f );
					packedRects = packResult.packedRects;
					var maxSize = packResult.maxSize;
					_config.Width = (int)maxSize.X.ToNearestPowerOfTwo();
					_config.Height = (int)maxSize.Y.ToNearestPowerOfTwo();
					_lightmapData = new float[config.Width * config.Height * 3];
					//_directionalData = new float[config.Width * config.Height * 3];
					//_shadowMaskData = new float[config.Width * config.Height * MAX_LIGHT_COUNT];
				}
				catch ( Exception e )
				{
					Progress = -1.0f;
					result.ErrorMessage = "Lightmap Pack Exception:" + e;
					return result;
				}

				if ( packedRects == null )
				{
					Progress = -1.0f;
					result.ErrorMessage = "Failed to pack lightmap rects:";
					return result;
				}

				for ( int i = 0; i < packedRects.Count; i++ )
				{
					allFaces[i].UVBounds = packedRects[i];

					var job = new LightmapJob()
					{
						Index = i,
						PackedRect = packedRects[i],
						LightmapFace = allFaces[i],
						Lights = allLights
					};

					allJobs.Add( job );
				}
			}

			Token.ThrowIfCancellationRequested();

			await ProcessArrayAsync( allJobs );

			Token.ThrowIfCancellationRequested();

			using ( var ___ = _profile.Begin( "Dilate Edges" ) )
			{
				DilateEdges( _lightmapData, _config.Width, _config.Height );
			}

			//using ( var ____ = Profile.Begin( "Denoiser" ) )
			//{
			//	Denoise( _lightmapData, _config.Width, _config.Height );
			//}
		}

		Progress = 1.0f;
		Status = BakeStatus.Finished;

		int directionalSize = 1024;
		int shadowmaskSize = 256;

		var downscaledDirectionalData = new float[0];
		var downscaledShadowMaskData = new float[0];
		//var downscaledDirectionalData = DownscaleDirectionalData( _directionalData, config.Width, config.Height, directionalSize, directionalSize );
		//var downscaledShadowMaskData = DownscaleShadowMaskData( _shadowMaskData, config.Width, config.Height, shadowmaskSize, shadowmaskSize );

		var lightmap1 = new Lightmap()
		{
			Width = _config.Width,
			Height = _config.Height,
			ImageData = _lightmapData,
			ShadowMaskSize = new( shadowmaskSize ),
			DirectionalSize = new( directionalSize ),
			DirectionalData = downscaledDirectionalData,
			ShadowMaskData = downscaledShadowMaskData
		};
		result.Success = true;
		result.ErrorMessage = string.Empty;
		result.Lightmaps.Add( lightmap1 );

		//Debug.Profile( _profile );

		return result;
	}

	async Task ProcessArrayAsync( List<LightmapJob> jobs )
	{
		using var _ = _profile.Begin( "Calculate Samples" );

		int completedCount = 0;
		int totalJobs = jobs.Count;
		var tasks = new List<Task>();

		foreach ( var job in jobs )
		{
			var task = Task.Run( () =>
			{
				Token.ThrowIfCancellationRequested();
				ProcessItem( job );
			}, Token ).ContinueWith( x =>
			{
				Token.ThrowIfCancellationRequested();

				Interlocked.Increment( ref completedCount );
				Progress = (float)completedCount / totalJobs * .9f;
			}, Token );

			tasks.Add( task );
		}

		await Task.WhenAll( tasks );
	}

	void ProcessItem( LightmapJob job )
	{
		var lmFace = job.LightmapFace;
		var direction = lmFace.Face.Plane.GetClosestAxisToNormal();
		var tempV = direction == Vector3.UnitZ ? Vector3.UnitY : Vector3.UnitZ;
		var uAxis = Vector3.Cross( lmFace.Face.Plane.Normal, tempV ).Normalized();
		var vAxis = Vector3.Cross( uAxis, lmFace.Face.Plane.Normal ).Normalized();
		var minU = float.MaxValue;
		var minV = float.MaxValue;
		var maxU = float.MinValue;
		var maxV = float.MinValue;

		foreach ( var vertex in lmFace.Face.Vertices )
		{
			var u = Vector3.Dot( vertex.Position, uAxis );
			var v = Vector3.Dot( vertex.Position, vAxis );
			minU = Math.Min( minU, u );
			minV = Math.Min( minV, v );
			maxU = Math.Max( maxU, u );
			maxV = Math.Max( maxV, v );
		}

		var width = maxU - minU;
		var height = maxV - minV;
		var uvScale = new Vector2( lmFace.UVBounds.Width / _config.Width, lmFace.UVBounds.Height / _config.Height );
		var uvOffset = new Vector2( lmFace.UVBounds.X / _config.Width, lmFace.UVBounds.Y / _config.Height );
		var isSmallSurface = width * height < (32 * 32);

		foreach ( var vert in lmFace.Face.Vertices )
		{
			var u = Vector3.Dot( vert.Position, uAxis );
			var v = Vector3.Dot( vert.Position, vAxis );
			var x = (u - minU) / width * uvScale.X + uvOffset.X;
			var y = (v - minV) / height * uvScale.Y + uvOffset.Y;

			vert.LightmapU = x;
			vert.LightmapV = y;
		}

		foreach ( var sample in lmFace.GenerateSamples() )
		{
			Token.ThrowIfCancellationRequested();

			//if ( sample.Normal.Z > 0 && sample.Normal.Z < 1 )
			//{
			//	_config.Scene.Gizmos.Line( sample.Origin, sample.Origin + sample.Normal * 32f, new Vector3( 1.0f, 0, 0 ), 1.0f, 128 );
			//}

			var (sampleColor, dominantLightDir, shadowMask) = CalculateSample( sample, job.Lights, isSmallSurface );
			var startX = (int)sample.TexCoords.X;
			var startY = (int)sample.TexCoords.Y;

			int index = (startY * _config.Width + startX) * 3;
			if ( startX >= _config.Width || startY >= _config.Height || index < 0 || index >= _lightmapData.Length - 3 )
				continue;

			// Store color data
			_lightmapData[index] = sampleColor.X;
			_lightmapData[index + 1] = sampleColor.Y;
			_lightmapData[index + 2] = sampleColor.Z;

			// Store directional data
			//_directionalData[index] = dominantLightDir.X;
			//_directionalData[index + 1] = dominantLightDir.Y;
			//_directionalData[index + 2] = dominantLightDir.Z;

			// Store shadowmask data
			//int shadowIndex = (startY * _config.Width + startX) * MAX_LIGHT_COUNT;
			//for ( int i = 0; i < MAX_LIGHT_COUNT; i++ )
			//{
			//	_shadowMaskData[shadowIndex + i] = i < shadowMask.Length ? shadowMask[i] : 1f;
			//}
		}

		Token.ThrowIfCancellationRequested();

		if ( _config.BlurStrength > 0f )
		{
			ApplyBlurWithinRect( _config.BlurStrength, lmFace.UVBounds );
		}
	}

	private Rect CalculateUvBounds( Face face )
	{
		var direction = face.Plane.GetClosestAxisToNormal();
		var tempV = direction == Vector3.UnitZ ? Vector3.UnitY : Vector3.UnitZ;
		var uAxis = Vector3.Cross( face.Plane.Normal, tempV ).Normalized();
		var vAxis = Vector3.Cross( uAxis, face.Plane.Normal ).Normalized();

		var minU = float.MaxValue;
		var minV = float.MaxValue;
		float maxU = float.MinValue, maxV = float.MinValue;

		foreach ( var vertex in face.Vertices )
		{
			var u = Vector3.Dot( vertex.Position, uAxis );
			var v = Vector3.Dot( vertex.Position, vAxis );
			minU = Math.Min( minU, u );
			minV = Math.Min( minV, v );
			maxU = Math.Max( maxU, u );
			maxV = Math.Max( maxV, v );
		}

		minU = MathF.Floor( minU );
		minV = MathF.Floor( minV );
		maxU = MathF.Ceiling( maxU );
		maxV = MathF.Ceiling( maxV );

		var width = maxU - minU;
		var height = maxV - minV;
		width /= face.TexelSize;
		height /= face.TexelSize;

		float aspectRatio = width / height;
		float minWidth = 4, minHeight = 4;
		float maxWidth = _config.Width, maxHeight = _config.Height;

		if ( width < minWidth || height < minHeight )
		{
			if ( width < minWidth )
			{
				width = minWidth;
				height = width / aspectRatio;
			}
			if ( height < minHeight )
			{
				height = minHeight;
				width = height * aspectRatio;
			}
		}

		if ( width > maxWidth || height > maxHeight )
		{
			if ( width > maxWidth )
			{
				width = maxWidth;
				height = width / aspectRatio;
			}
			if ( height > maxHeight )
			{
				height = maxHeight;
				width = height * aspectRatio;
			}
		}

		return new Rect( minU, minV, width, height );
	}

	(Vector3 color, Vector3 dominantLightDir, float[] shadowMask) CalculateSample( LightmapSample sample, IEnumerable<LightInfo> lights, bool isSmallSurface = false )
	{
		var ambientColor = new Vector3( _config.AmbientColor.R, _config.AmbientColor.G, _config.AmbientColor.B );
		var origin = isSmallSurface ? sample.FaceCenter + sample.Normal * .1f : sample.Origin + sample.Normal * .1f;
		var totalIllumination = ambientColor;
		var dominantLightDir = Vector3.Zero;
		float maxIntensity = 0f;

		Vector3 totalLightContribution = Vector3.Zero;

		foreach ( var light in lights )
		{
			var lightForward = light.Direction.EulerToForward();
			var lightColor = new Vector3( light.Color.R, light.Color.G, light.Color.B );
			var directionToLight = light.Type == LightTypes.Directional
				? -lightForward.Normalized()
				: (light.Position - origin).Normalized();

			float diffuse = Math.Max( 0, Vector3.Dot( sample.Normal.Normalized(), directionToLight.Normalized() ) );
			if ( diffuse <= 0 ) continue;

			Vector3 lightContribution = Vector3.Zero;
			bool isShadowed = false;

			switch ( light.Type )
			{
				case LightTypes.Directional:
					var sunPos = origin + directionToLight * 90000;
					var tr = _config.Scene.Physics.Trace<Solid>( sunPos, -directionToLight, 50000, false );
					isShadowed = tr.Hit && Vector3.Distance( tr.Position, origin ) >= 5;
					if ( !isShadowed )
					{
						lightContribution = lightColor * diffuse;
					}
					break;
				case LightTypes.Point:
					var distanceInches = Vector3.Distance( light.Position, origin );

					// Convert distances to meters
					float distanceMeters = distanceInches * 0.0254f;
					float rangeMeters = light.Range * 0.0254f;

					// Calculate attenuation
					float attenuationFactor = 1.0f;
					float attenuation = 1.0f / (attenuationFactor + 0.09f * distanceMeters + 0.032f * (distanceMeters * distanceMeters));

					// Apply smooth falloff based on light range
					float rangeFalloff = 1.0f - MathHelper.Clamp( distanceMeters / rangeMeters, 0.0f, 1.0f );
					rangeFalloff = rangeFalloff * rangeFalloff * rangeFalloff; // Smooth cubic falloff

					// Combine attenuation with range falloff
					float finalAttenuation = attenuation * rangeFalloff;
					var trace = _config.Scene.Physics.Trace<Solid>( light.Position, (origin - light.Position).Normalized(), 50000, false );
					isShadowed = trace.Hit && Vector3.Distance( light.Position, trace.Position ) <= distanceInches - 5;
					if ( !isShadowed )
					{
						lightContribution = lightColor * light.Intensity * diffuse * finalAttenuation;
					}
					break;
			}

			totalLightContribution += lightContribution;

			// Determine dominant light direction
			float intensity = lightContribution.Length;
			if ( intensity > maxIntensity )
			{
				maxIntensity = intensity;
				dominantLightDir = directionToLight;
			}
		}

		// Add total light contribution using a more physically-based approach
		totalIllumination += totalLightContribution;

		// Apply energy conservation
		//float maxComponent = Math.Max( totalIllumination.X, Math.Max( totalIllumination.Y, totalIllumination.Z ) );
		//if ( maxComponent > 1.0f )
		//{
		//	totalIllumination /= maxComponent;
		//}

		return (totalIllumination, dominantLightDir, null);
	}

	private float[,] CreateGaussianKernel( int size, float sigma )
	{
		float[,] kernel = new float[size, size];
		float sum = 0;
		int radius = size / 2;
		float sigma2 = 2 * sigma * sigma;
		float normalizationFactor = 1 / (float)(Math.PI * sigma2);

		for ( int y = -radius; y <= radius; y++ )
		{
			for ( int x = -radius; x <= radius; x++ )
			{
				float value = normalizationFactor * MathF.Exp( -(x * x + y * y) / sigma2 );
				kernel[y + radius, x + radius] = value;
				sum += value;
			}
		}

		// Normalize the kernel so that the sum of all elements equals 1
		for ( int y = 0; y < size; y++ )
		{
			for ( int x = 0; x < size; x++ )
			{
				kernel[y, x] /= sum;
			}
		}

		return kernel;
	}

	private static void BilateralFilterBlur( ref float[] image, int width, int height, Rect rectangle, int blurSize, double sigmaColor, double sigmaSpace )
	{
		// Create a copy of the image to store the blurred values
		float[] blurredImage = new float[image.Length];
		Array.Copy( image, blurredImage, image.Length );

		// Precalculate exponent factor
		double colorFactor = -0.5 / (sigmaColor * sigmaColor);
		double spaceFactor = -0.5 / (sigmaSpace * sigmaSpace);

		// Process each pixel within the rectangle
		for ( int xx = (int)rectangle.X; xx < rectangle.X + rectangle.Width; xx++ )
		{
			for ( int yy = (int)rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++ )
			{
				double sumR = 0, sumG = 0, sumB = 0;
				double normFactor = 0;

				// The central pixel position
				int centerIndex = (yy * width + xx) * 3;
				float centerR = image[centerIndex + 2];
				float centerG = image[centerIndex + 1];
				float centerB = image[centerIndex];

				// Compute the sum of RGB values within the blur size with weights
				for ( int x = Math.Max( (int)rectangle.X, xx - blurSize ); x <= Math.Min( xx + blurSize, rectangle.X + rectangle.Width - 1 ); x++ )
				{
					for ( int y = Math.Max( (int)rectangle.Y, yy - blurSize ); y <= Math.Min( yy + blurSize, rectangle.Y + rectangle.Height - 1 ); y++ )
					{
						int index = (y * width + x) * 3;

						float R = image[index + 2];
						float G = image[index + 1];
						float B = image[index];

						double dR = centerR - R;
						double dG = centerG - G;
						double dB = centerB - B;

						double colorDistance = dR * dR + dG * dG + dB * dB;
						double spatialDistance = (xx - x) * (xx - x) + (yy - y) * (yy - y);

						double weight = Math.Exp( colorDistance * colorFactor + spatialDistance * spaceFactor );

						sumR += weight * R;
						sumG += weight * G;
						sumB += weight * B;
						normFactor += weight;
					}
				}

				// Calculate the average RGB values weighted
				int targetIndex = centerIndex;
				blurredImage[targetIndex + 2] = (float)(sumR / normFactor);
				blurredImage[targetIndex + 1] = (float)(sumG / normFactor);
				blurredImage[targetIndex] = (float)(sumB / normFactor);
			}
		}

		// Copy the blurred image back to the original image reference
		Array.Copy( blurredImage, image, image.Length );
	}

	private static void BoxBlur( ref float[] image, int width, int height, Rect rectangle, int blurSize )
	{
		// Create a copy of the image to store the blurred values
		float[] blurredImage = new float[image.Length];
		Array.Copy( image, blurredImage, image.Length );

		// Process each pixel within the rectangle
		for ( int xx = (int)rectangle.X; xx < rectangle.X + rectangle.Width; xx++ )
		{
			for ( int yy = (int)rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++ )
			{
				double sumR = 0, sumG = 0, sumB = 0;
				int count = 0;

				// Compute the sum of RGB values within the blur size
				for ( int x = Math.Max( (int)rectangle.X, xx - blurSize ); x <= Math.Min( xx + blurSize, rectangle.X + rectangle.Width - 1 ); x++ )
				{
					for ( int y = Math.Max( (int)rectangle.Y, yy - blurSize ); y <= Math.Min( yy + blurSize, rectangle.Y + rectangle.Height - 1 ); y++ )
					{
						int index = (y * width + x) * 3;
						if ( image[index] == 0 ) continue;
						sumR += image[index + 2];
						sumG += image[index + 1];
						sumB += image[index];
						count++;
					}
				}

				// Calculate the average RGB values
				int centerIndex = (yy * width + xx) * 3;
				blurredImage[centerIndex + 2] = (float)(sumR / count);
				blurredImage[centerIndex + 1] = (float)(sumG / count);
				blurredImage[centerIndex] = (float)(sumB / count);
			}
		}

		// Copy the blurred image back to the original image reference
		Array.Copy( blurredImage, image, image.Length );
	}

	private static void Blur( ref float[] image, int width, int height, Rect rectangle, int blurSize )
	{
		// Create a copy of the image to store the blurred values
		float[] blurredImage = new float[image.Length];

		// Process each pixel within the rectangle
		for ( int xx = (int)rectangle.X; xx < rectangle.X + rectangle.Width; xx++ )
		{
			for ( int yy = (int)rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++ )
			{
				float sumR = 0, sumG = 0, sumB = 0;
				int blurPixelCount = 0;

				// Compute the sum of RGB values within the blur size
				for ( int x = xx; x < xx + blurSize && x < width && x < rectangle.Right; x++ )
				{
					for ( int y = yy; y < yy + blurSize && y < height && y < rectangle.Bottom; y++ )
					{
						int index = (y * width + x) * 3;
						if ( image[index] == 0 ) continue;
						sumB += image[index];
						sumG += image[index + 1];
						sumR += image[index + 2];
						blurPixelCount++;
					}
				}

				// Calculate the average RGB values
				float avgR = sumR / blurPixelCount;
				float avgG = sumG / blurPixelCount;
				float avgB = sumB / blurPixelCount;

				// Set each pixel in the blur area to the average color
				for ( int x = xx; x < xx + blurSize && x < width && x < rectangle.Right; x++ )
				{
					for ( int y = yy; y < yy + blurSize && y < height && y < rectangle.Bottom; y++ )
					{
						int index = (y * width + x) * 3;
						blurredImage[index] = avgB;
						blurredImage[index + 1] = avgG;
						blurredImage[index + 2] = avgR;
					}
				}
			}
		}

		// Copy the blurred image back to the original image reference
		Array.Copy( blurredImage, image, image.Length );
	}


	float[,] kernel;
	int kernelHash;
	void ApplyBlurWithinRect( float blurRadius, Rect rect )
	{
		int kernelSize = (int)Math.Ceiling( blurRadius * 3 ) * 2 + 1;
		var khash = System.HashCode.Combine( blurRadius, kernelSize );
		if ( khash != kernelHash || kernel == null )
		{
			kernel = CreateGaussianKernel( kernelSize, blurRadius );
			kernelHash = khash;
		}

		int width = _config.Width;
		int height = _config.Height;
		int channels = 3; // Assuming RGB

		var blurredData = new float[(int)(rect.Width * rect.Height * channels)];

		int rectWidth = (int)rect.Right - (int)rect.Left;
		int rectHeight = (int)rect.Bottom - (int)rect.Top;

		Parallel.For( 0, rectHeight, dy =>
		{
			Token.ThrowIfCancellationRequested();

			int y = (int)rect.Top + dy;
			for ( int dx = 0; dx < rectWidth; dx++ )
			{
				int x = (int)rect.Left + dx;
				float[] newColor = new float[channels];
				float normalizationFactor = 0;

				for ( int ky = 0; ky < kernelSize; ky++ )
				{
					int posY = Math.Min( height - 1, Math.Max( 0, y + ky - kernelSize / 2 ) );

					for ( int kx = 0; kx < kernelSize; kx++ )
					{
						int posX = Math.Min( width - 1, Math.Max( 0, x + kx - kernelSize / 2 ) );
						float kernelValue = kernel[ky, kx];

						// Checking pixel initialization can be optimized or removed
						if ( posY >= rect.Top && posY < rect.Bottom && posX >= rect.Left && posX < rect.Right )
						{
							normalizationFactor += kernelValue;

							for ( int c = 0; c < channels; c++ )
							{
								newColor[c] += _lightmapData[(posY * width + posX) * channels + c] * kernelValue;
							}
						}
					}
				}

				// Normalize and store the blurred values
				if ( normalizationFactor > 0 )
				{
					for ( int c = 0; c < channels; c++ )
					{
						blurredData[(dy * rectWidth + dx) * channels + c] = newColor[c] / normalizationFactor;
					}
				}
				else
				{
					for ( int c = 0; c < channels; c++ )
					{
						blurredData[(dy * rectWidth + dx) * channels + c] = _lightmapData[(y * width + x) * channels + c];
					}
				}
			}
		} );

		// Copy blurred data back to the lightmap data
		for ( int dy = 0; dy < rectHeight; dy++ )
		{
			int y = (int)rect.Top + dy;
			for ( int dx = 0; dx < rectWidth; dx++ )
			{
				Token.ThrowIfCancellationRequested();
				int x = (int)rect.Left + dx;
				for ( int c = 0; c < channels; c++ )
				{
					_lightmapData[(y * width + x) * channels + c] = blurredData[(dy * rectWidth + dx) * channels + c];
				}
			}
		}
	}

	void Denoise( float[] lightmap, int width, int height )
	{
		//Aardvark.Base.Aardvark.Init();

		//using var device = new Device();

		//var pixImg = PixImage<float>.Create( lightmap, Col.Format.RGB, width, height ).AsPixImage<float>();

		//device.DenoiseLightmap( pixImg, pixImg );
	}

	void DilateEdges( float[] lightmap, int width, int height )
	{
		// Use a temporary buffer to avoid overwriting the lightmap as we're modifying it
		float[] tempLightmap = (float[])lightmap.Clone();

		int channels = 3; // RGB channels

		for ( int y = 0; y < height; ++y )
		{
			for ( int x = 0; x < width; ++x )
			{
				int index = (y * width + x) * channels;
				// Assume the pixel is uninitialized if all color channels are zero (black)
				if ( lightmap[index] != 0 || lightmap[index + 1] != 0 || lightmap[index + 2] != 0 ) continue;

				float accumulatedR = 0;
				float accumulatedG = 0;
				float accumulatedB = 0;
				int validPixelCount = 0;

				for ( int offsetY = -1; offsetY <= 1; ++offsetY )
				{
					int lookY = y + offsetY;
					// Skip if the row is outside the image
					if ( lookY < 0 || lookY >= height ) continue;

					for ( int offsetX = -1; offsetX <= 1; ++offsetX )
					{
						int lookX = x + offsetX;
						// Skip if the column is outside the image
						if ( lookX < 0 || lookX >= width ) continue;

						int lookIndex = (lookY * width + lookX) * channels;
						// Only consider neighboring pixels that are part of the lightmap (not black)
						if ( lightmap[lookIndex] != 0 || lightmap[lookIndex + 1] != 0 || lightmap[lookIndex + 2] != 0 )
						{
							accumulatedR += lightmap[lookIndex];
							accumulatedG += lightmap[lookIndex + 1];
							accumulatedB += lightmap[lookIndex + 2];
							validPixelCount++;
						}
					}
				}

				// If there are any valid neighboring pixels, compute the average color
				if ( validPixelCount > 0 )
				{
					tempLightmap[index] = accumulatedR / validPixelCount;
					tempLightmap[index + 1] = accumulatedG / validPixelCount;
					tempLightmap[index + 2] = accumulatedB / validPixelCount;
				}
			}
		}

		// Copy the manipulated data back to the original lightmap
		Array.Copy( tempLightmap, lightmap, lightmap.Length );
	}

	public void SaveToFile( string path )
	{
		using var bmp = CreateLightmapBitmap();
		bmp.Save( path );
	}

	public Bitmap CreateLightmapBitmap()
	{
		Bitmap bitmap = new Bitmap( _config.Width, _config.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

		BitmapData bmpData = bitmap.LockBits( new Rectangle( 0, 0, bitmap.Width, bitmap.Height ), ImageLockMode.WriteOnly, bitmap.PixelFormat );
		IntPtr ptr = bmpData.Scan0;
		int bytes = Math.Abs( bmpData.Stride ) * bitmap.Height;
		System.Runtime.InteropServices.Marshal.Copy( _lightmapData, 0, ptr, bytes );
		bitmap.UnlockBits( bmpData );

		return bitmap;
	}

	private float[] DownscaleDirectionalData( float[] originalData, int originalWidth, int originalHeight, int newWidth, int newHeight )
	{
		float[] downscaledData = new float[newWidth * newHeight * 3]; // 3 floats per pixel for direction
		for ( int y = 0; y < newHeight; y++ )
		{
			for ( int x = 0; x < newWidth; x++ )
			{
				int originalX = x * originalWidth / newWidth;
				int originalY = y * originalHeight / newHeight;
				int originalIndex = (originalY * originalWidth + originalX) * 3;
				int newIndex = (y * newWidth + x) * 3;

				// Simply copy the direction data
				for ( int i = 0; i < 3; i++ )
				{
					downscaledData[newIndex + i] = originalData[originalIndex + i];
				}
			}
		}
		return downscaledData;
	}

	private float[] DownscaleShadowMaskData( float[] originalData, int originalWidth, int originalHeight, int newWidth, int newHeight )
	{
		float[] downscaledData = new float[newWidth * newHeight * MAX_LIGHT_COUNT];
		for ( int y = 0; y < newHeight; y++ )
		{
			for ( int x = 0; x < newWidth; x++ )
			{
				int originalX = x * originalWidth / newWidth;
				int originalY = y * originalHeight / newHeight;
				int originalIndex = (originalY * originalWidth + originalX) * MAX_LIGHT_COUNT;
				int newIndex = (y * newWidth + x) * MAX_LIGHT_COUNT;

				for ( int i = 0; i < MAX_LIGHT_COUNT; i++ )
				{
					downscaledData[newIndex + i] = originalData[originalIndex + i];
				}
			}
		}
		return downscaledData;
	}

}
