Shader "Unlit/StarrySky"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                //UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in fragment Shader.
            };

            StructuredBuffer<float4x4> transformations;
            float4 center;

            v2f vert (appdata v, uint instanceID: SV_InstanceID) {
                // Allow instancing.
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 pos = mul(transformations[instanceID], v.vertex);
                pos += center;
                o.vertex = UnityObjectToClipPos(pos);
                //UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                //UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                return float4(1,1,1,1);
            }
            ENDCG
        }
    }
}
