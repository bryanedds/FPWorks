#shader vertex
#version 410

layout (location = 0) in vec3 position;
layout (location = 1) in vec2 texCoords;

out vec2 texCoordsOut;

void main()
{
    texCoordsOut = texCoords;
    gl_Position = vec4(position, 1.0);
}

#shader fragment
#version 410
#extension GL_ARB_bindless_texture : require

const float PI = 3.141592654;
const float REFLECTION_LOD_MAX = 7.0;
const float ATTENUATION_CONSTANT = 1.0;
const int LIGHTS_MAX = 32;
const int SHADOWS_MAX = 16;
const float SHADOW_FOV_MAX = 2.1;
const float SHADOW_SEAM_INSET = 0.001;
const vec4 SSVF_DITHERING[4] =
    vec4[4](
        vec4(0.0, 0.5, 0.125, 0.625),
        vec4(0.75, 0.22, 0.875, 0.375),
        vec4(0.1875, 0.6875, 0.0625, 0.5625),
        vec4(0.9375, 0.4375, 0.8125, 0.3125));

uniform vec3 eyeCenter;
uniform mat4 view;
uniform mat4 projection;
uniform float lightCutoffMargin;
uniform vec3 lightAmbientColor;
uniform float lightAmbientBrightness;
uniform float lightShadowExponent;
uniform float lightShadowDensity;
uniform float lightShadowBleedFilter;
uniform float lightShadowVarianceMin;
uniform int ssvfEnabled;
uniform int ssvfSteps;
uniform float ssvfAsymmetry;
uniform float ssvfIntensity;
uniform int ssrEnabled;
uniform float ssrDetail;
uniform int ssrRefinementsMax;
uniform float ssrRayThickness;
uniform float ssrTowardEyeCutoff;
uniform float ssrDepthCutoff;
uniform float ssrDepthCutoffMargin;
uniform float ssrDistanceCutoff;
uniform float ssrDistanceCutoffMargin;
uniform float ssrRoughnessCutoff;
uniform float ssrRoughnessCutoffMargin;
uniform float ssrSlopeCutoff;
uniform float ssrSlopeCutoffMargin;
uniform float ssrEdgeHorizontalMargin;
uniform float ssrEdgeVerticalMargin;
uniform vec3 ssrLightColor;
uniform float ssrLightBrightness;
layout(bindless_sampler) uniform sampler2D positionTexture;
layout(bindless_sampler) uniform sampler2D albedoTexture;
layout(bindless_sampler) uniform sampler2D materialTexture;
layout(bindless_sampler) uniform sampler2D normalPlusTexture;
layout(bindless_sampler) uniform sampler2D brdfTexture;
layout(bindless_sampler) uniform sampler2D irradianceTexture;
layout(bindless_sampler) uniform sampler2D environmentFilterTexture;
layout(bindless_sampler) uniform sampler2D ssaoTexture;
layout(bindless_sampler) uniform sampler2D shadowTextures[SHADOWS_MAX];
uniform vec3 lightOrigins[LIGHTS_MAX];
uniform vec3 lightDirections[LIGHTS_MAX];
uniform vec3 lightColors[LIGHTS_MAX];
uniform float lightBrightnesses[LIGHTS_MAX];
uniform float lightAttenuationLinears[LIGHTS_MAX];
uniform float lightAttenuationQuadratics[LIGHTS_MAX];
uniform float lightCutoffs[LIGHTS_MAX];
uniform int lightDirectionals[LIGHTS_MAX];
uniform float lightConeInners[LIGHTS_MAX];
uniform float lightConeOuters[LIGHTS_MAX];
uniform int lightShadowIndices[LIGHTS_MAX];
uniform int lightsCount;
uniform mat4 shadowMatrices[SHADOWS_MAX];

in vec2 texCoordsOut;

layout (location = 0) out vec4 color;
layout (location = 1) out vec4 fogAccum;
layout (location = 2) out float depth;

float linstep(float low, float high, float v)
{
    return clamp((v - low) / (high - low), 0.0, 1.0);
}

vec3 rotate(vec3 axis, float angle, vec3 v)
{
    return mix(dot(axis, v) * axis, v, cos(angle)) + cross(axis, v) * sin(angle);
}

float fadeShadowScalar(vec2 shadowTexCoords, float shadowScalar)
{
    vec2 normalized = abs(shadowTexCoords * 2.0 - 1.0);
    float fadeScalar =
        max(
            smoothstep(0.85, 1.0, normalized.x),
            smoothstep(0.85, 1.0, normalized.y));
    return 1.0 - (1.0 - shadowScalar) * (1.0 - fadeScalar);
}

float distributionGGX(vec3 normal, vec3 h, float roughness)
{
    float a = roughness * roughness;
    float aPow2 = a * a;
    float nDotH = max(dot(normal, h), 0.0);
    float nDotHPow2 = nDotH * nDotH;
    float nom = aPow2;
    float denom = nDotHPow2 * (aPow2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    return nom / denom;
}

float geometrySchlickGGX(float nDotV, float roughness)
{
    float r = roughness + 1.0;
    float k = r * r / 8.0;
    float nom = nDotV;
    float denom = nDotV * (1.0 - k) + k;
    return nom / denom;
}

float geometrySchlick(vec3 n, vec3 v, vec3 l, float roughness)
{
    float nDotV = max(dot(n, v), 0.0);
    float nDotL = max(dot(n, l), 0.0);
    float ggx2 = geometrySchlickGGX(nDotV, roughness);
    float ggx1 = geometrySchlickGGX(nDotL, roughness);
    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 f0)
{
    return f0 + (1.0 - f0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 fresnelSchlickRoughness(float cosTheta, vec3 f0, float roughness)
{
    return f0 + (max(vec3(1.0 - roughness), f0) - f0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float depthViewToDepthBuffer(float depthView)
{
    // compute near and far planes (these _should_ get baked down to fragment constants)
    float p2z = projection[2].z;
    float p2w = projection[2].w;
    float nearPlane = p2w / (p2z - 1.0);
    float farPlane = p2w / (p2z + 1.0);

    // compute depth
    return (-depthView - nearPlane) / (farPlane - nearPlane);
}

float computeShadowScalar(vec4 position, bool lightDirectional, float lightConeOuter, mat4 shadowMatrix, sampler2D shadowTexture)
{
    vec4 positionShadow = shadowMatrix * position;
    vec3 shadowTexCoordsProj = positionShadow.xyz / positionShadow.w;
    if (shadowTexCoordsProj.x >= -1.0 + SHADOW_SEAM_INSET && shadowTexCoordsProj.x < 1.0 - SHADOW_SEAM_INSET &&
        shadowTexCoordsProj.y >= -1.0 + SHADOW_SEAM_INSET && shadowTexCoordsProj.y < 1.0 - SHADOW_SEAM_INSET &&
        shadowTexCoordsProj.z >= -1.0 + SHADOW_SEAM_INSET && shadowTexCoordsProj.z < 1.0 - SHADOW_SEAM_INSET)
    {
        vec2 shadowTexCoords = shadowTexCoordsProj.xy * 0.5 + 0.5;
        float shadowZ = !lightDirectional ? shadowTexCoordsProj.z * 0.5 + 0.5 : shadowTexCoordsProj.z;
        float shadowZExp = exp(-lightShadowExponent * shadowZ);
        float shadowDepthExp = texture(shadowTexture, shadowTexCoords).y;
        float shadowScalar = clamp(shadowZExp * shadowDepthExp, 0.0, 1.0);
        shadowScalar = pow(shadowScalar, lightShadowDensity);
        shadowScalar = lightConeOuter > SHADOW_FOV_MAX ? fadeShadowScalar(shadowTexCoords, shadowScalar) : shadowScalar;
        return shadowScalar;
    }
    return 1.0;
}

vec3 computeFogAccumDirectional(vec4 position, int lightIndex)
{
    vec3 result = vec3(0.0);
    int shadowIndex = lightShadowIndices[lightIndex];
    if (lightsCount > 0 && lightDirectionals[lightIndex] != 0 && shadowIndex >= 0)
    {
        // compute shadow space
        mat4 shadowMatrix = shadowMatrices[shadowIndex];

        // compute ray info
        vec3 startPosition = eyeCenter;
        vec3 stopPosition = position.xyz;
        vec3 rayVector = stopPosition - startPosition;
        float rayLength = length(rayVector);
        vec3 rayDirection = rayVector / rayLength;

        // compute step info
        float stepLength = rayLength / ssvfSteps;
        vec3 step = rayDirection * stepLength;

        // compute light view term
        float theta = dot(-rayDirection, lightDirections[lightIndex]);

        // compute dithering
        float dithering = SSVF_DITHERING[int(gl_FragCoord.x) % 4][int(gl_FragCoord.y) % 4];

        // march over ray, accumulating fog light value
        vec3 currentPosition = startPosition + step * dithering;
        for (int i = 0; i < ssvfSteps; i++)
        {
            // step through ray, accumulating fog light moment
            vec4 positionShadow = shadowMatrix * vec4(currentPosition, 1.0);
            vec3 shadowTexCoordsProj = positionShadow.xyz / positionShadow.w;
            vec2 shadowTexCoords = vec2(shadowTexCoordsProj.x, shadowTexCoordsProj.y) * 0.5 + 0.5;
            bool shadowTexCoordsInRange = shadowTexCoords.x >= 0.0 && shadowTexCoords.x < 1.0 && shadowTexCoords.y >= 0.0 && shadowTexCoords.y < 1.0;
            float shadowZ = shadowTexCoordsProj.z * 0.5 + 0.5;
            float shadowDepth = shadowTexCoordsInRange ? texture(shadowTextures[shadowIndex], shadowTexCoords).x : 1.0;
            if (shadowZ <= shadowDepth || shadowZ >= 1.0f)
            {
                // mie scaterring approximated with Henyey-Greenstein phase function
                float asymmetrySquared = ssvfAsymmetry * ssvfAsymmetry;
                float fogMoment = (1.0 - asymmetrySquared) / (4.0 * PI * pow(1.0 + asymmetrySquared - 2.0 * ssvfAsymmetry * theta, 1.5));
                result += fogMoment;
            }
            currentPosition += step;
        }
        result = smoothstep(0.0, 1.0, result / ssvfSteps) * lightColors[lightIndex] * lightBrightnesses[lightIndex] * ssvfIntensity;
    }
    return result;
}

void computeSsr(vec4 position, vec3 albedo, float roughness, float metallic, vec3 normal, float slope, out vec3 specularScreen, out float specularScreenWeight)
{
    // compute view values
    vec4 positionView = view * position;
    vec3 positionViewNormal = normalize(positionView.xyz);
    vec3 normalView = mat3(view) * normal;
    vec3 reflectionView = reflect(positionViewNormal, normalView);
    vec4 startView = vec4(positionView.xyz, 1.0);
    vec4 stopView = vec4(positionView.xyz + reflectionView * ssrDistanceCutoff, 1.0);
    float eyeDistanceFromPlane = abs(dot(normalView, positionView.xyz));

    // compute the fragment at which to start marching
    vec2 texSize = textureSize(positionTexture, 0).xy;
    vec4 startFrag4 = projection * startView;
    vec2 startFrag = startFrag4.xy / startFrag4.w;
    startFrag = startFrag * 0.5 + 0.5;
    startFrag *= texSize;

    // compute the fragment at which to end marching as well as total length
    vec4 stopFrag4 = projection * stopView;
    vec2 stopFrag = stopFrag4.xy / stopFrag4.w;
    stopFrag = stopFrag * 0.5 + 0.5;
    stopFrag *= texSize;
    float lengthFrag = length(stopFrag - startFrag);

    // initialize current fragment
    vec2 currentFrag = startFrag;
    vec2 currentTexCoords = currentFrag / texSize;
    vec4 currentPosition = position;
    vec4 currentPositionView = positionView;

    // compute fragment step amount
    float marchHorizontal = stopFrag.x - startFrag.x;
    float marchVertical = stopFrag.y - startFrag.y;
    bool shouldMarchHorizontal = abs(marchHorizontal) >= abs(marchVertical);
    float stepCount = abs(shouldMarchHorizontal ? marchHorizontal : marchVertical) * ssrDetail;
    vec2 stepAmount = vec2(marchHorizontal, marchVertical) / max(stepCount, 0.001);

    // march fragment
    float currentProgressA = 0.0;
    float currentProgressB = 0.0;
    float currentDepthView = 0.0;
    for (int i = 0; i < stepCount && currentTexCoords.x >= 0.0 && currentTexCoords.x < 1.0 && currentTexCoords.y >= 0.0 && currentTexCoords.y < 1.0; ++i)
    {
        // advance frag values
        currentFrag += stepAmount;
        currentTexCoords = currentFrag / texSize;
        currentPosition = texture(positionTexture, currentTexCoords);
        currentPositionView = view * currentPosition;
        currentProgressB = length(currentFrag - startFrag) / lengthFrag;
        currentDepthView = -startView.z * -stopView.z / max(0.00001, mix(-stopView.z, -startView.z, currentProgressB)); // NOTE: uses perspective correct interpolation for depth, but causes precision issues as ssrDistanceCutoff increases.

        // compute depth delta and thickness based on view state
        float depthDelta = currentDepthView - -currentPositionView.z;
        float thickness = max(-currentPositionView.z * ssrRayThickness, ssrRayThickness);
        if (-currentPositionView.z > 3.0 && eyeDistanceFromPlane < 1.0)
            thickness = max(1.0 - eyeDistanceFromPlane + ssrRayThickness, thickness);

        // determine whether we hit geometry within acceptable thickness
        if (currentPosition.w == 1.0 && depthDelta >= 0.0 && depthDelta <= thickness)
        {
            // perform refinements within walk
            currentProgressB = currentProgressA + (currentProgressB - currentProgressA) * 0.5;
            for (int j = 0; j < ssrRefinementsMax; ++j)
            {
                // advance frag values
                currentFrag = mix(startFrag, stopFrag, currentProgressB);
                currentTexCoords = currentFrag / texSize;
                currentPosition = texture(positionTexture, currentTexCoords);
                currentPositionView = view * currentPosition;
                currentDepthView = -startView.z * -stopView.z / max(0.00001, mix(-stopView.z, -startView.z, currentProgressB)); // NOTE: uses perspective correct interpolation for depth, but causes precision issues as ssrDistanceCutoff increases.

                // compute depth delta and thickness based on view state
                float depthDelta = currentDepthView - -currentPositionView.z;
                float thickness = max(-currentPositionView.z * ssrRayThickness, ssrRayThickness);
                if (-currentPositionView.z > 3.0 && eyeDistanceFromPlane < 1.0)
                    thickness = max(1.0 - eyeDistanceFromPlane + ssrRayThickness, thickness);

                // determine whether we hit geometry within acceptable thickness
                if (currentPosition.w == 1.0 && depthDelta >= 0.0 && depthDelta <= thickness)
                {
                    // compute screen-space specular color and weight
                    vec3 f0 = mix(vec3(0.04), albedo, metallic);
                    vec3 v = normalize(-positionView.xyz);
                    vec3 h = normalize(v + normal);
                    vec3 f = fresnelSchlick(max(dot(h, v), 0.0), f0);
                    vec3 specularIntensity = f * (1.0 - roughness);
                    specularScreen = vec3(texture(albedoTexture, currentTexCoords).rgb * ssrLightColor * ssrLightBrightness * specularIntensity);
                    specularScreenWeight =
                        (1.0 - smoothstep(1.0 - ssrRoughnessCutoffMargin, 1.0, roughness / ssrRoughnessCutoff)) * // filter out as fragment reaches max roughness
                        (1.0 - smoothstep(1.0 - ssrDepthCutoffMargin, 1.0, positionView.z / -ssrDepthCutoff)) * // filter out as fragment reaches max depth
                        (1.0 - smoothstep(1.0 - ssrDistanceCutoffMargin, 1.0, length(currentPositionView - positionView) / ssrDistanceCutoff)) * // filter out as reflection point reaches max distance from fragment
                        (1.0 - smoothstep(1.0 - ssrSlopeCutoffMargin, 1.0, slope / ssrSlopeCutoff)) * // filter out as slope nears cutoff
                        smoothstep(0.0, 1.0, eyeDistanceFromPlane) * // filter out as eye nears plane
                        smoothstep(0.0, ssrEdgeHorizontalMargin, min(currentTexCoords.x, 1.0 - currentTexCoords.x)) *
                        smoothstep(0.0, ssrEdgeVerticalMargin, min(currentTexCoords.y, 1.0 - currentTexCoords.y));
                    specularScreenWeight = clamp(specularScreenWeight, 0.0, 1.0);
                    break;
                }

                // continue in the same direction
                float temp = currentProgressB;
                currentProgressB = currentProgressB + (currentProgressB - currentProgressA) * 0.5;
                currentProgressA = temp;
            }

            // fin
            break;
        }
    }

    // otherwise loop
    currentProgressA = currentProgressB;
}

void main()
{
    // ensure position was written
    vec4 position = texture(positionTexture, texCoordsOut);
    if (position.w == 1.0)
    {
        // retrieve remaining data from geometry buffers
        vec3 albedo = texture(albedoTexture, texCoordsOut).rgb;
        vec4 material = texture(materialTexture, texCoordsOut);
        vec3 normal = texture(normalPlusTexture, texCoordsOut).xyz;

        // retrieve data from intermediate buffers
        vec3 irradiance = texture(irradianceTexture, texCoordsOut).rgb;
        vec3 environmentFilter = texture(environmentFilterTexture, texCoordsOut).rgb;
        float ssao = texture(ssaoTexture, texCoordsOut).r;

        // compute materials
        float roughness = material.r;
        float metallic = material.g;
        float ambientOcclusion = material.b * ssao;
        vec3 emission = vec3(material.a);

        // compute light accumulation
        vec3 v = normalize(eyeCenter - position.xyz);
        vec3 f0 = mix(vec3(0.04), albedo, metallic); // if dia-electric (plastic) use f0 of 0.04f and if metal, use the albedo color as f0.
        vec3 lightAccum = vec3(0.0);
        for (int i = 0; i < lightsCount; ++i)
        {
            // per-light radiance
            bool lightDirectional = lightDirectionals[i] == 1;
            vec3 l, h, radiance;
            if (!lightDirectional)
            {
                vec3 d = lightOrigins[i] - position.xyz;
                l = normalize(d);
                h = normalize(v + l);
                float distanceSquared = dot(d, d);
                float distance = sqrt(distanceSquared);
                float cutoff = lightCutoffs[i];
                float cutoffScalar = 1.0 - smoothstep(cutoff * (1.0 - lightCutoffMargin), cutoff, distance);
                float attenuation = 1.0 / (ATTENUATION_CONSTANT + lightAttenuationLinears[i] * distance + lightAttenuationQuadratics[i] * distanceSquared);
                float angle = acos(dot(l, -lightDirections[i]));
                float halfConeInner = lightConeInners[i] * 0.5;
                float halfConeOuter = lightConeOuters[i] * 0.5;
                float halfConeDelta = halfConeOuter - halfConeInner;
                float halfConeBetween = angle - halfConeInner;
                float halfConeScalar = clamp(1.0 - halfConeBetween / halfConeDelta, 0.0, 1.0);
                float intensity = attenuation * halfConeScalar;
                radiance = lightColors[i] * lightBrightnesses[i] * intensity * cutoffScalar;
            }
            else
            {
                l = -lightDirections[i];
                h = normalize(v + l);
                radiance = lightColors[i] * lightBrightnesses[i];
            }

            // shadow scalar
            int shadowIndex = lightShadowIndices[i];
            float shadowScalar =
                shadowIndex >= 0 ?
                computeShadowScalar(position, lightDirectional, lightConeOuters[i], shadowMatrices[shadowIndex], shadowTextures[shadowIndex]) :
                1.0;

            // cook-torrance brdf
            float ndf = distributionGGX(normal, h, roughness);
            float g = geometrySchlick(normal, v, l, roughness);
            vec3 f = fresnelSchlick(max(dot(h, v), 0.0), f0);

            // compute specularity
            vec3 numerator = ndf * g * f;
            float denominator = 4.0 * max(dot(normal, v), 0.0) * max(dot(normal, l), 0.0) + 0.0001; // add epsilon to prevent division by zero
            vec3 specular = numerator / denominator;

            // compute diffusion
            vec3 kS = f;
            vec3 kD = vec3(1.0) - kS;
            kD *= 1.0 - metallic;

            // compute light scalar
            float nDotL = max(dot(normal, l), 0.0);

            // add to outgoing lightAccum
            lightAccum += (kD * albedo / PI + specular) * radiance * nDotL * shadowScalar;
        }

        // compute light ambient terms
        // NOTE: lightAmbientSpecular gets an additional ao multiply for some specular occlusion.
        // TODO: use a better means of computing specular occlusion as this one isn't very effective.
        vec3 lightAmbientDiffuse = lightAmbientColor * lightAmbientBrightness * ambientOcclusion;
        vec3 lightAmbientSpecular = lightAmbientDiffuse * ambientOcclusion;

        // compute diffuse term
        vec3 f = fresnelSchlickRoughness(max(dot(normal, v), 0.0), f0, roughness);
        vec3 kS = f;
        vec3 kD = 1.0 - kS;
        kD *= 1.0 - metallic;
        vec3 diffuse = kD * irradiance * albedo * lightAmbientDiffuse;

        // compute specular term and weight from screen-space
        vec3 forward = vec3(view[0][2], view[1][2], view[2][2]);
        float towardEye = dot(forward, normal);
        float slope = 1.0 - abs(dot(normal, vec3(0.0, 1.0, 0.0)));
        vec4 positionView = view * position;
        vec3 specularScreen = vec3(0.0);
        float specularScreenWeight = 0.0;
        if (ssrEnabled == 1 && towardEye <= ssrTowardEyeCutoff && -positionView.z <= ssrDepthCutoff && roughness <= ssrRoughnessCutoff && slope <= ssrSlopeCutoff)
        {
            vec2 texSize = textureSize(positionTexture, 0).xy;
            float texelHeight = 1.0 / texSize.y;
            vec2 texCoordsBelow = texCoordsOut + vec2(0.0, -texelHeight); // using tex coord below current pixel reduces 'cracks' on floor reflections
            texCoordsBelow.y = max(0.0, texCoordsBelow.y);
            vec4 positionBelow = texture(positionTexture, texCoordsBelow);
            computeSsr(positionBelow, albedo, roughness, metallic, normal, slope, specularScreen, specularScreenWeight);
        }

        // compute specular term
        vec2 environmentBrdf = texture(brdfTexture, vec2(max(dot(normal, v), 0.0), roughness)).rg;
        vec3 specularEnvironmentSubterm = f * environmentBrdf.x + environmentBrdf.y;
        vec3 specularEnvironment = environmentFilter * specularEnvironmentSubterm * lightAmbientSpecular;
        vec3 specular = (1.0 - specularScreenWeight) * specularEnvironment + specularScreenWeight * specularScreen;

        // write lighting values
        color = vec4(lightAccum + diffuse + emission * albedo + specular, 1.0);
        fogAccum = ssvfEnabled == 1 ? vec4(computeFogAccumDirectional(position, 0), 1.0) : vec4(0.0);
        depth = depthViewToDepthBuffer(positionView.z);
    }
}
