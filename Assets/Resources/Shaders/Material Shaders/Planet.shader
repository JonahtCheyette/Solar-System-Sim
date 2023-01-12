Shader "Custom/PlanetSurfaceShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static float epsilon = 1E-4;
        static const float PI = 3.14159265f;

        struct Input {
            float3 pos;
            float3 normal;
        };

        struct LayerInfo {
            float baseRadius;
            float blendAmount;
            float3 albedo;
            int useNormalColoration;
            float minAlignment;
            float normalBlendAmount;
            float3 normalAlbedo;
        };

        float inverseLerp(float min, float max, float val) {
            //the saturate function clamps its value between 0 and 1
            return saturate((val - min) / (max - min));
        }
        #ifdef SHADER_API_D3D11
        StructuredBuffer<LayerInfo> layerInfo;
        #endif
        uint numLayers;

        float4 poleColor; // w is pole blend
        float minPoleAlignment;

        float maxRadius;
        float minRadius;
        float oceanRadius;

        void vert(inout appdata_full v, out Input o) {
            o.pos = v.vertex.xyz;
            o.normal = v.normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float radius = length(IN.pos);
            float3 pointDir = IN.pos / radius;

            float radPercent = inverseLerp(minRadius, oceanRadius, radius) + inverseLerp(oceanRadius, maxRadius, radius);
            float alignment = 1 - 2 * acos(dot(IN.normal, pointDir)) / PI; // could speed up by calculating this on startup and storing in UVs, I think. Couldn't do it on RadPercent because that could cause notable inaccuracies at ocean level

            float drawStrength = 0;
            float3 color = float3(alignment, alignment, alignment);
            o.Albedo = color;

            #ifdef SHADER_API_D3D11
            for (uint i = 0; i < numLayers; i++) {
                //the epsilon is to prevent divide by 0 errors from occurring in the inverseLerp function when the blendAmount is 0
                drawStrength = inverseLerp(-layerInfo[i].blendAmount / 2 - epsilon, layerInfo[i].blendAmount / 2, radPercent - layerInfo[i].baseRadius - 1);
                color = layerInfo[i].albedo;
                float normalStrength = 1 - inverseLerp(-layerInfo[i].normalBlendAmount / 2 - epsilon, layerInfo[i].normalBlendAmount / 2, alignment - layerInfo[i].minAlignment);
                normalStrength *= layerInfo[i].useNormalColoration;
                color = normalStrength * layerInfo[i].normalAlbedo + (1 - normalStrength) * color;
                #endif
                o.Albedo = o.Albedo * (1 - drawStrength) + color * drawStrength;
            #ifdef SHADER_API_D3D11
            }
            #endif

            float poleAlignment = dot(float3(0, 1, 0), pointDir);
            poleAlignment = max(poleAlignment, -poleAlignment);
            float poleStrength = inverseLerp(-poleColor.w / 2 - epsilon, poleColor.w / 2, poleAlignment - minPoleAlignment);
            poleStrength *= max(0, ceil(radPercent - 1));
            o.Albedo = o.Albedo * (1 - poleStrength) + poleColor.xyz * poleStrength;

            o.Albedo *= min(1, numLayers);

            o.Metallic = 0.0;
            o.Smoothness = 0.5;
            o.Alpha = 1;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
