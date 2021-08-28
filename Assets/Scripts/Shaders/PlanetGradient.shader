Shader "Hidden/PlanetGradient"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct planet {
                float2 viewportPos;
                int index;
                float radius;
            };

            // the frame we're doing the postProcessing on
            sampler2D _MainTex;
            StructuredBuffer<planet> planets;

            // in order to not have to pass a float3 for color for each planet, I instead have them come with a planetIndex int, which is simply
            // the index of their color in this buffer
            StructuredBuffer<float3> colors;

            uint numPlanets;

            //the size of the halo relative to the planet's radius
            static const float planetHaloSize = 2;

            fixed4 frag(v2f v) : SV_Target {
                fixed4 col = tex2D(_MainTex, v.uv);

                float3 color = col.rgb;

                //the nice planet shading
                for (uint i = 0; i < numPlanets; i++) {
                    float2 offset = v.uv - planets[i].viewportPos;
                    // because (at least on my screen) the width and the height of the viewport are different, and because the bottom left corner is (0, 0)
                    // and top right corner is (1, 1) no matter what the relative width and height of the screen is,
                    // I need to scale the x relative to the y
                    offset.x *= _ScreenParams.x/_ScreenParams.y;
                    float dstToPlanet = length(offset);
                    if (dstToPlanet <= planets[i].radius) {
                        float relativeHeightChange = v.uv.y - planets[i].viewportPos.y; // this makes the top of the planet brighter than the bottom
                        float colorStrength = ((relativeHeightChange * 7) / (planets[i].radius * 16) + 0.65625); //scales from [-radius, radius] to [0.125, 1]
                        color = colors[planets[i].index] * colorStrength;
                    } else if (dstToPlanet <= planets[i].radius * planetHaloSize) {
                        float x = (dstToPlanet - planets[i].radius) / (planets[i].radius * (planetHaloSize - 1));
                        //gets the distance from planet surface, where 0 is the surface and 1 is as far as we're allowing the halo to be
                        float alpha = pow(x - 1, 2) * 0.1; //put this into desmos if you need to, but gives us a nice slope from 0.1 to 0 as x increases
                        color = (colors[planets[i].index] * alpha) + (color * (1 - alpha)); //alpha blending to give us a nice halo effect
                    }
                }

                col = fixed4(color.xyz, 1);
                return col;
            }
            ENDCG
        }
    }
}