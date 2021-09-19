Shader "Unlit/StarrySky"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

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
            };

            StructuredBuffer<float4x4> transformations;
            float4 center;

            v2f vert (appdata v, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 objPos = mul(transformations[instanceID], v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, objPos + center);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return float4(1,1,1,1);
            }
            ENDCG
        }
    }
}
