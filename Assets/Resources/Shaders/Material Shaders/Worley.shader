Shader "Custom/WorleySurface"
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

        sampler2D _MainTex;

        struct Input {
            float3 color;
        };

        void vert(inout appdata_full v, out Input o) {
            o.color = v.texcoord;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            o.Albedo = IN.color;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
