Shader "Unlit/Star"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull front
        LOD 100

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
            };

            static const float PI = 3.14159265f;

            float4 _Color;
            float coronaRadius;
            float starRadius;
            //just easier to have it set as a float4 from c# code, as that allows me to use SetVector
            float4 center;

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                float3 forward = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
                float angle = acos(dot(normalize(i.worldPos - center.xyz), forward));
                float distFromCenterOnCameraPlane = length(i.worldPos - center.xyz) * sin(angle);
                if (distFromCenterOnCameraPlane >= starRadius) {
                    float value = pow((coronaRadius - distFromCenterOnCameraPlane) / (coronaRadius - starRadius), 2);
                    return _Color * value;
                }
                
                return _Color;
            }
            ENDCG
        }
    }
}
