
namespace Graybox.Scenes.Shaders
{
	internal class ShaderSource
	{

		internal const string VertexTexelDisplay = @"
#version 330

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 3) in vec2 inTexCoord;

uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _SunMatrix;
uniform mat4 _ModelMatrix;

out vec3 fragPosition;
out vec3 fragNormal;

void main()
{
	fragPosition = (_ModelMatrix * vec4(inPosition, 1.0)).xyz;
    fragNormal = mat3(transpose(inverse(_ModelMatrix))) * inNormal;
    vec4 worldPosition = _ModelMatrix * vec4(inPosition, 1.0);
    gl_Position = _CameraProjection * _CameraView * worldPosition;
}
";

		internal static string FragTexelDisplay => @"
#version 330 core

in vec3 fragPosition;
in vec3 fragNormal;

uniform vec3 _Normal;
uniform float _TexelSize;
uniform bool _DisabledInLightmap;

out vec4 outColor;

void main() {
	vec3 lightDir = vec3( .25, .45, .85 );
    float bias = 0.001; // Small bias to stabilize rounding
    int x = int(floor((fragPosition.x + bias) / _TexelSize));
    int y = int(floor((fragPosition.y + bias) / _TexelSize));
    int z = int(floor((fragPosition.z + bias) / _TexelSize));

    float lightIntensity = max(dot(_Normal, lightDir), 0.7);

    vec3 lightBlue = vec3(0.6, 0.8, 1.0);
    vec3 lightCoral = vec3(1.0, 0.8, 0.6);

    // Apply diffuse factor to colors
    vec3 color1 = lightBlue * lightIntensity;
    vec3 color2 = lightCoral * lightIntensity;

	vec3 finalColor;
    if ((x + y + z) % 2 == 0 )
        finalColor = color1;
    else
        finalColor = color2;

	if ( _DisabledInLightmap )
	{
		finalColor = vec3( 0.8, 0.2, 0.2 );
	}

	outColor = vec4(finalColor, 0.55);
}";

		internal static string VertexSimpleLit => @"
#version 330

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec3 inTangent;
layout(location = 3) in vec2 inTexCoord;
layout(location = 4) in vec2 inTexCoordLM;
layout(location = 5) in vec2 inVertexColor;
layout(location = 6) in float inIsSelected;

uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _SunMatrix;
uniform mat4 _ModelMatrix;

out vec3 fragPosition;
out vec3 fragNormal;
out vec3 fragTangent;
out vec2 fragTexCoord;
out vec2 fragTexCoordLM;
out vec4 fragPosLightSpace;
out float fragIsSelected;

void main()
{
	fragIsSelected = inIsSelected;
    fragTexCoord = inTexCoord;
	fragTexCoordLM = inTexCoordLM;
	fragPosition = (_ModelMatrix * vec4(inPosition, 1.0)).xyz;
    fragNormal = mat3(transpose(inverse(_ModelMatrix))) * inNormal;
	fragTangent = mat3(transpose(inverse(_ModelMatrix))) * inTangent;
    vec4 worldPosition = _ModelMatrix * vec4(inPosition, 1.0);
    gl_Position = _CameraProjection * _CameraView * worldPosition;

    // Assuming you have a light space matrix for shadow mapping
    fragPosLightSpace = _SunMatrix * worldPosition;
}
";

		internal static string FragmentSimpleLit => @"
#version 330 core

in vec3 fragPosition;
in vec3 fragNormal;
in vec3 fragTangent;
in vec2 fragTexCoord;
in vec2 fragTexCoordLM;
in vec4 fragPosLightSpace;
in float fragIsSelected;

uniform sampler2D _MainTexture;
uniform sampler2D _NormalMap;
uniform sampler2D _Lightmap0;
uniform float _LightmapIndex;
uniform bool _HasNormalMap;
uniform float _GridSize;
uniform sampler2D _SunShadowMap;
uniform vec3 _SunDirection;
uniform vec3 _SunColor;
uniform bool _SunEnabled;
uniform vec3 _AmbientColor;
uniform vec3 _CameraPosition;
uniform float _Shininess;
uniform bool _FogEnabled;
uniform vec3 _FogColor;
uniform float _FogDensity;

out vec4 outColor;

float ShadowCalculation(vec4 fragPosLightSpace)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    if(projCoords.z > 1.0)
        return 0.0;

    float closestDepth = texture(_SunShadowMap, projCoords.xy).r; 
    float currentDepth = projCoords.z;
    float bias = max(0.001 * (1.0 - dot(fragNormal.xyz, _SunDirection)), 0.0001);

    float shadow = 0.0;
    int samples = 1; 
    float offset = 1.0 / textureSize(_SunShadowMap, 0).x;
    float weightSum = 0.0;
    
    for(int x = -samples; x <= samples; ++x)
    {
        for(int y = -samples; y <= samples; ++y)
        {
            vec2 offsetVec = vec2(x, y) * offset;
            float pcfDepth = texture(_SunShadowMap, projCoords.xy + offsetVec).r; 
            float weight = 1.0 / (1.0 + length(offsetVec)); // Distance-based weight
            shadow += (currentDepth - bias > pcfDepth ? 0.0 : 1.0) * weight;
            weightSum += weight;
        }    
    }
    shadow /= weightSum;

    return shadow;
}

vec3 CalculateGrid(vec3 worldPosition, vec3 worldNormal, vec3 color)
{	
    float luminance = dot(color, vec3(0.299, 0.587, 0.114));
    vec3 gridColor = luminance > 0.5 ? vec3(0.1, 0.1, 0.1) : vec3(0.9, 0.9, 0.9);
    float lineWidth = (_GridSize < 8.0) ? 0.35 :
                      (_GridSize < 32.0) ? 0.65 :
                      (_GridSize > 128.0) ? 2.5 : 1.5;

    // Align the grid relative to the camera position
    float offset = mod(_CameraPosition.x + _CameraPosition.y + _CameraPosition.z, _GridSize);

    float xDist = abs(mod(worldPosition.x + offset, _GridSize) - offset);
    float yDist = abs(mod(worldPosition.y + offset, _GridSize) - offset);
    float zDist = abs(mod(worldPosition.z + offset, _GridSize) - offset);

    float xGrid = smoothstep(0.0, lineWidth, xDist);
    float yGrid = smoothstep(0.0, lineWidth, yDist);
    float zGrid = smoothstep(0.0, lineWidth, zDist);

    vec3 finalColor = color;
    if (abs(worldNormal.x) < 0.9999) finalColor = mix(finalColor, gridColor, 1.0 - xGrid);
    if (abs(worldNormal.y) < 0.9999) finalColor = mix(finalColor, gridColor, 1.0 - yGrid);
    if (abs(worldNormal.z) < 0.9999) finalColor = mix(finalColor, gridColor, 1.0 - zGrid);

    // Calculate distance from the camera
    float distance = length(worldPosition - _CameraPosition);

    // Fade out the grid lines starting at 1024 units
    float fadeStart = 1024.0;
    float fadeEnd = 2048.0;
    float alpha = clamp((fadeEnd - distance) / (fadeEnd - fadeStart), 0.0, 1.0);

    // Apply the alpha to the final grid color
    return mix(color, finalColor, alpha);
}

vec3 sRGBToLinear(vec3 color) {
    return pow(color, vec3(2.2));  // Simple gamma correction from sRGB to linear
}

vec3 linearToSRGB(vec3 color) {
    return pow(color, vec3(1.0/2.2));  // Convert from linear to sRGB
}

vec4 cubic(float v){
    vec4 n = vec4(1.0, 2.0, 3.0, 4.0) - v;
    vec4 s = n * n * n;
    float x = s.x;
    float y = s.y - 4.0 * s.x;
    float z = s.z - 4.0 * s.y + 5.75 * s.x;
    float w = 5.75 - x - y - z;
    return vec4(x, y, z, w) * (1.0/5.75);
}

vec4 textureBicubic(sampler2D sampler, vec2 texCoords){

   vec2 texSize = textureSize(sampler, 0);
   vec2 invTexSize = 1.0 / texSize;
   
   texCoords = texCoords * texSize - 0.5;

   
    vec2 fxy = fract(texCoords);
    texCoords -= fxy;

    vec4 xcubic = cubic(fxy.x);
    vec4 ycubic = cubic(fxy.y);

    vec4 c = texCoords.xxyy + vec2 (-0.5, +1.5).xyxy;
    
    vec4 s = vec4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
    vec4 offset = c + vec4 (xcubic.yw, ycubic.yw) / s;
    
    offset *= invTexSize.xxyy;
    
    vec4 sample0 = texture(sampler, offset.xz);
    vec4 sample1 = texture(sampler, offset.yz);
    vec4 sample2 = texture(sampler, offset.xw);
    vec4 sample3 = texture(sampler, offset.yw);

    float sx = s.x / (s.x + s.y);
    float sy = s.z / (s.z + s.w);

    return mix(
       mix(sample3, sample2, sx), mix(sample1, sample0, sx)
    , sy);
}

void main()
{
	vec4 texColor = texture(_MainTexture, fragTexCoord);
	vec3 lightDir = normalize(-_SunDirection);
	vec3 viewDir = normalize(_CameraPosition - fragPosition);
	vec3 normal = normalize(fragNormal);
    vec3 tangent = normalize(fragTangent);
    vec3 bitangent = normalize(cross(normal, tangent));

    // TBN matrix construction
    mat3 TBN = mat3(tangent, bitangent, normal);

	if ( _HasNormalMap )
	{
        vec3 normalMap = texture(_NormalMap, fragTexCoord).rgb;
        normalMap = normalMap * 2.0 - 1.0;
        normal = normalize(TBN * normalMap);
	}

	vec3 finalColor;
	if (_LightmapIndex >= 0.0 && fragTexCoordLM.x != 0 && fragTexCoordLM.y != 0)
    {
        vec3 lightmapColor = texture(_Lightmap0, fragTexCoordLM).rgb;
		finalColor = lightmapColor * texColor.rgb;
    }
	else
	{
		// Ambient component
		vec3 ambient = _AmbientColor * texColor.rgb;

		// Diffuse component
		float diff = max(dot(normal, lightDir), 0.5);
		vec3 diffuse = diff * _SunColor;

		// Specular component
		vec3 halfVector = normalize(lightDir + viewDir);
		float spec = pow(max(dot(normal, halfVector), 0.0), _Shininess);
		vec3 specular = spec * _SunColor * .15; 

		// Shadow component
		float shadow = 1.0;
		if(_SunEnabled)
		{
			shadow = ShadowCalculation(fragPosLightSpace);
		}
		vec3 lighting = ambient + (diffuse + specular) * shadow;

		// Calculate final color
		finalColor = lighting * texColor.rgb;
	}

    // Apply grid if enabled
    if ( _GridSize > 0 )
    {
        finalColor = CalculateGrid(fragPosition.xyz, normal, finalColor);
    }

	if ( _FogEnabled )
	{
		float distance = length(fragPosition - _CameraPosition) * .0254;
		float fogFactor = exp(-pow(_FogDensity * distance, 2.0));
		fogFactor = clamp(fogFactor, 0.0, 1.0);
		finalColor = mix(_FogColor, finalColor, fogFactor);
	}

	if ( fragIsSelected > 0.9 ) 
    {
		//vec3 highlightColor = vec3(1.0, 1.0, 0.0); // Yellow tint
        //finalColor = mix(finalColor, highlightColor, 0.5);
    }

	//finalColor = finalColor * .0001;
	//finalColor = finalColor + fragNormal;

    outColor = vec4(finalColor, texColor.a);
	//outColor = vec4(linearToSRGB(finalColor), texColor.a);
}
";

		internal const string VertVertexColorShader = @"#version 330
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _ModelMatrix;

in vec3 inPosition;
in vec3 inNormal;
in vec2 inTexCoords;
in vec2 inTexCoordsLM;
in vec4 inVertexColor;

out vec3 fragColor;

void main()
{
    fragColor = inVertexColor.xyz;
    gl_Position = _CameraProjection * _CameraView * _ModelMatrix * vec4(inPosition, 1.0);
}";

		internal const string FragVertexColorShader = @"#version 330
in vec3 fragColor;

out vec4 outColor;

void main()
{
    outColor = vec4(fragColor, 1.0);
}";


		internal static string VertDepth => @"
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;

in vec3 inPosition;
in vec3 inNormal;

void main()
{
    gl_Position = _CameraProjection * _CameraView * vec4(inPosition, 1.0);
}
";

		internal static string FragDepth => @"
#version 330 core

#define BIAS 0.005

void main()
{
	//gl_FragDepth = gl_FragCoord.z;
	//gl_FragDepth += gl_FrontFacing ? 0.0 : BIAS;
}
";

		internal const string VertNormalDebug = @"#version 330
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _ModelMatrix;

in vec3 inPosition;
in vec3 inNormal;

out vec3 fragNormal;

void main()
{
    fragNormal = mat3(transpose(inverse(_ModelMatrix))) * inNormal;
    gl_Position = _CameraProjection * _CameraView * _ModelMatrix * vec4(inPosition, 1.0);
}";

		internal const string FragNormalDebug = @"#version 330
in vec3 fragNormal;

out vec4 outColor;

void main()
{
    vec3 normal = normalize(fragNormal);
    outColor = vec4(normal * 0.5 + 0.5, 1.0); // Map normals from [-1, 1] to [0, 1]
}";

		internal const string VertWackyAnimated = @"#version 330
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 model;

in vec3 inPosition;
in vec3 inNormal;

out vec3 fragPosition;
out vec3 fragNormal;

void main()
{
    fragPosition = (model * vec4(inPosition, 1.0)).xyz;
    fragNormal = mat3(transpose(inverse(model))) * inNormal;
    gl_Position = _CameraProjection * _CameraView * model * vec4(inPosition, 1.0);
}";

		internal const string FragWackyAnimated = @"#version 330
in vec3 fragPosition;
in vec3 fragNormal;

uniform float time;

out vec4 outColor;

void main()
{
    vec3 normal = normalize(fragNormal);
    float wave = sin(time + length(fragPosition)) * 0.5 + 0.5;
    vec3 color = vec3(wave) * normal * 0.5 + 0.5; // Create a wacky animated color effect
    outColor = vec4(color, 1.0);
}";

		internal static string VertexGrid => @"
#version 330 core
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _ModelMatrix;

in vec3 inPosition;

out vec2 vUV;
out vec3 vCameraPosition;

void main()
{
    vec4 worldPosition = _ModelMatrix * vec4(inPosition, 1.0);
    gl_Position = _CameraProjection * _CameraView * worldPosition;
    vUV = inPosition.xy;  // Assuming the UVs map directly to xy world space coordinates for simplicity.
}
";

		internal static string FragmentGrid => @"
#version 330 core
in vec2 vUV;

uniform vec3 _CameraPosition;
uniform float _GridSize;

out vec4 fragColor;

float processGrid(vec2 uv, float lineWidth) {
    // Calculate UV derivatives
    vec2 uvDeriv = vec2(length(vec2(dFdx(uv).x, dFdy(uv).x)), length(vec2(dFdx(uv).y, dFdy(uv).y)));

    // Determine line width settings
    bool invertLineX = lineWidth > 0.5;
    bool invertLineY = lineWidth > 0.5;
    float targetWidthX = invertLineX ? 1.0 - lineWidth : lineWidth;
    float targetWidthY = invertLineY ? 1.0 - lineWidth : lineWidth;
    float drawWidthX = clamp(targetWidthX, uvDeriv.x, 0.5);
    float drawWidthY = clamp(targetWidthY, uvDeriv.y, 0.5);

    // Anti-aliasing line width
    float lineAAX = uvDeriv.x * 1.5;
    float lineAAY = uvDeriv.y * 1.5;

    // Calculate grid UV coordinates
    vec2 gridUV = abs(fract(uv) * 2.0 - 1.0);
    gridUV.x = invertLineX ? gridUV.x : 1.0 - gridUV.x;
    gridUV.y = invertLineY ? gridUV.y : 1.0 - gridUV.y;

    // Apply smoothstep for anti-aliasing
    float gridX = smoothstep(drawWidthX + lineAAX, drawWidthX - lineAAX, gridUV.x);
    float gridY = smoothstep(drawWidthY + lineAAY, drawWidthY - lineAAY, gridUV.y);
    gridX *= clamp(targetWidthX / drawWidthX, 0, 1);
    gridY *= clamp(targetWidthY / drawWidthY, 0, 1);

    // Adjust grid intensity based on UV derivatives
    gridX = mix(gridX, targetWidthX, clamp(uvDeriv.x * 2.0 - 1.0, 0, 1));
    gridY = mix(gridY, targetWidthY, clamp(uvDeriv.y * 2.0 - 1.0, 0, 1));
    gridX = invertLineX ? 1.0 - gridX : gridX;
    gridY = invertLineY ? 1.0 - gridY : gridY;

    // Combine the grid values for both axes
    float grid = mix(gridX, 1.0, gridY);
    return grid;
}


const float N = 10.0; // Grid ratio, controlling the density
float gridTextureGradBox(in vec2 p, in vec2 ddx, in vec2 ddy) {
	vec2 w = max(abs(ddx), abs(ddy)) + 0.001;
	// analytic (box) filtering
    vec2 a = p + 0.5*w;                        
    vec2 b = p - 0.5*w;           
    vec2 i = (floor(a)+min(fract(a)*N,1.0)-
              floor(b)-min(fract(b)*N,1.0))/(N*w);
    //pattern
    return (1.0-i.x)*(1.0-i.y);
}

void main()
{
    vec2 uv = vUV * _GridSize;
    vec2 ddx_uv = dFdx(uv);
    vec2 ddy_uv = dFdy(uv);
	//float grid = 1.0 - gridTextureGradBox(uv, ddx_uv, ddy_uv);
    float grid = processGrid(uv, .04);

    vec4 worldPos = vec4(vUV, 0.0, 1.0); // Assuming z = 0 and w = 1 for plane coordinates
    float dist = length(_CameraPosition - worldPos.xyz);

    float fadeStart = 2048.0;
    float fadeEnd = 4096.0; // Adjust as needed
    float alpha = clamp((fadeEnd - dist) / (fadeEnd - fadeStart), 0.0, 1.0);
    alpha *= grid;

	vec3 color = vec3(1,1,1);
    fragColor = vec4(color, alpha * 0.77); 
}
";

		internal const string VertGridLinesShader = @"#version 330
uniform mat4 _CameraProjection;
uniform mat4 _CameraView;
uniform mat4 _ModelMatrix;
uniform vec2 _ScreenSize; // Screen width and height in pixels
uniform float _ZoomLevel; // Zoom level factor

in vec3 inPosition;
in vec3 inVertexColor;

out vec3 fragColor;

void main()
{
    vec4 worldPosition = _ModelMatrix * vec4(inPosition, 1.0);
    vec4 viewPosition = _CameraView * worldPosition;
    vec4 clipPosition = _CameraProjection * viewPosition;

    // Convert to window coordinates
    vec2 windowPos = clipPosition.xy / clipPosition.w;
    windowPos = (windowPos + 1.0) * 0.5 * _ScreenSize;

    // Snap to the nearest pixel
    windowPos = round(windowPos);

    // Convert back to normalized device coordinates
    vec2 ndcPos = (windowPos / _ScreenSize) * 2.0 - 1.0;

    // Ensure the z and w components are preserved correctly
    gl_Position = vec4(ndcPos, clipPosition.z, clipPosition.w);

    fragColor = inVertexColor;
}";

		internal const string FragGridLinesShader = @"#version 330
in vec3 fragColor;

out vec4 outColor;

void main()
{
    outColor = vec4(fragColor.rgb, 1.0);
}";

	}
}
