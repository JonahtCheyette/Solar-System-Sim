Shader "Hidden/OceanShader" {
    Properties {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "..\Includes\Triplanar.cginc"

            static const float maxFloat = 3.402823466e+38;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                return o;
            }

            struct OceanDetails {
                float size;
                float blendMultiplier;
                float alphaMultiplier;
                float3 shallowCol;
                float3 deepCol;
            };

            StructuredBuffer<OceanDetails> oceans;
            StructuredBuffer<float3> positions; // last position is position of sun
            uint numOceans;

            float ambientLight;
            float normalMapScaleA;
            float normalMapScaleB;
            float smoothness;
            float waveStrength;
            float waveSpeed;

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D waveNormalMapA;
            sampler2D waveNormalMapB;

            //x is dist to sphere, y is dist through sphere
            //rayDir must be normalized
            //if inside sphere, x will be 0
            //if misses sphere, y will be 0
            float2 raySphereIntersection(float sphereSize, float3 sphereCenter, float3 rayOrigin, float3 rayDir) {
                float3 offset = rayOrigin - sphereCenter;
                float b = dot(rayDir, offset) * 2;
                float c = dot(offset, offset) - sphereSize * sphereSize;

                float delta = b * b - 4 * c;
                if (delta > 0) {
                    float s = sqrt(delta);
                    float distToSphereNear = max(0, (-b - s) / 2);
                    float distToSphereFar = (-b + s) / 2;
                    if (distToSphereFar >= 0) {
                        return float2(distToSphereNear, distToSphereFar - distToSphereNear);
                    }
                }
                return float2(maxFloat, 0);
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 col = tex2D(_MainTex, i.uv).xyz;//getting the initial color
                float viewLength = length(i.viewVector);
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)) * viewLength;

                float3 lightSource = positions[numOceans];

                float3 camPos = _WorldSpaceCameraPos;
                float3 viewDir = i.viewVector / viewLength;

                for (uint i = 0; i < numOceans; i++) {
                    float2 intersectionInfo = raySphereIntersection(oceans[i].size, positions[i], camPos, viewDir);
                    float oceanViewDepth = min(intersectionInfo.y, depth - intersectionInfo.x);
                    if (oceanViewDepth > 0) {
                        float alpha = 1 - exp(-oceanViewDepth / oceans[i].size * oceans[i].alphaMultiplier);

                        float t = 1 - exp(-oceanViewDepth / oceans[i].size * oceans[i].blendMultiplier);
                        float3 oceanCol = lerp(oceans[i].shallowCol, oceans[i].deepCol, t);

                        //lighting calculations
                        float3 worldPos = camPos + viewDir * intersectionInfo.x;
                        float3 objPos = worldPos - positions[i];
                        float3 baseNormal = normalize(objPos);
                        float3 toLight = normalize(lightSource - worldPos);
                        float light = saturate(dot(toLight, baseNormal));

                        float2 waveOffsetOne = float2(sin(_Time.x * waveSpeed - 0.5), cos(_Time.x * waveSpeed + 0.8));
                        float2 waveOffsetTwo = float2(cos(_Time.x * waveSpeed + 0.2), sin(_Time.x * waveSpeed - 0.9));

                        float3 waveNormal = triplanarNormal(objPos, baseNormal, normalMapScaleA / oceans[i].size, waveNormalMapA, waveOffsetOne);
                        waveNormal = triplanarNormal(objPos, waveNormal, normalMapScaleB / oceans[i].size, waveNormalMapB, waveOffsetTwo);
                        waveNormal = normalize(lerp(baseNormal, waveNormal, waveStrength));
                        
                        float3 c = waveNormal * dot(waveNormal, toLight);
                        float specularHighlight = pow(max(dot(2*c-toLight, -viewDir), 0), smoothness);

                        specularHighlight *= (intersectionInfo.x > 0);
                        light += specularHighlight;

                        light = max(light, ambientLight);
                        oceanCol *= light;

                        col = (1 - alpha) * col + alpha * oceanCol;
                        //I tried to make this faster (remove branching)
                        //by multiplying alpha by (oceanViewDepth > 0) instead of the if statement
                        //despite the fact that this should be identical, the code stopped rendering things that didn't overlap with the ocean
                    }
                }

                return fixed4(col.r, col.g, col.b, 1);
            }
            ENDCG
        }
    }
}
