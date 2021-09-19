Shader "Unlit/StarrySky"
{
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                uint index : SV_InstanceID;
            };

            StructuredBuffer<float4x4> transformations;
            StructuredBuffer<float3> colors;
            StructuredBuffer<float> scales;
            float4 cameraPos;

            v2f vert (appdata v, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 objPos = mul(transformations[instanceID], v.vertex);
                o.worldPos = (objPos + cameraPos).xyz;
                o.vertex = mul(UNITY_MATRIX_VP, objPos + cameraPos);
                o.index = instanceID;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                float3 centerOfStar = (cameraPos + mul(transformations[i.index], float4(0,0,0,1))).xyz;
                float dist = length(i.worldPos - centerOfStar) / scales[i.index];
                return float4(lerp(float3(1, 1, 1), colors[i.index], dist), 1 - dist);
            }
            ENDCG
        }
    }
}
