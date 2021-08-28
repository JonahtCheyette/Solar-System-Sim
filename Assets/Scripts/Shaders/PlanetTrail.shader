Shader "Hidden/PlanetTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            // a shader I made to try and copy the effect I saw here: https://www.youtube.com/watch?v=7axImc1sxa0&t=150s
            // I never got the trails quite looking right, unfortunately
            // I could probably do so with enough time and tinkering, but that doesn't solve the fact that
            // if my trails get very long, my computer starts really struggling
            // I've already spent too much time and effort on what's really just a minor visual flair thing, so I'm just going to take the part that works
            // (the planet shading/halos) and leave the rest as-is for the time being. with that being said,
            // There are a few possibilities for what I'm doing wrong, and how he might be creating the effect:
            // Firstly, Considering that when scrolling through his code on github,
            // I couldn't find any file that would give off that effect, so it's possible he made it in postproduction using video editing software
            // Secondly, This implementation, along with the PlanetTrail.cs file, allows for the movement of the camera
            // without any problems. This is something He never does in video, which would allow him to do things such as saving the last rendered frame
            // as a texture, and using that to draw the trails. this would be much less computationally intensive than my implementation, at the cost of
            // not being able to move the camera without screwing the effect up, and the fact that the trails would probably end up not looking a uniform width
            // unless he took snapshots of their position a TON
            // Thirdly, he has a better computer than me, and can thus save and send more of the previous locations of the planets
            // to the shader than me, allowing for longer trails
            // Fourthly, he found some faster method to drawing the trails that isn't as computationally intensive
            // Fithly, he's probably recording on an actual build of the project, and the extra speed of that probably helps for longer trails
            // Sixthly, and this is unlikely, it's possible that isn't an actual unity project that he's recording, and is instead
            // some other 2d gravity implementation he whipped up.
            // Finally, there could be some Unity module I don't know about allowing him to do this

            // I might be able to do the trails as a shader for the a lineRenderer component as opposed to a PostProcessing effect
            // that way, it wouldn't waste any time on pixels unaffected by the trails
            // but I'm kinda done with the planet-trail thing for now, and I'll be honest, I don't really like the lineRenderer component.
            // If I come back to it later, I'll definitely try that.
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
            
            struct trailSegment {
                float2 start;
                float2 end;
                int planetIndex;
                float thickness;
            };

            struct planet {
                float2 viewportPos;
                int index;
                float radius;
            };

            // the frame we're doing the postProcessing on
            sampler2D _MainTex;
            StructuredBuffer<trailSegment> segments;
            StructuredBuffer<planet> planets;

            // in order to not have to pass a float3 for color for each segment/planet, I instead have them come with a planetIndex int, which is simply
            // the index of their color in this buffer
            StructuredBuffer<float3> colors;

            // that same planetIndex goes into this buffer to get back how many segments from that planet should be onscreen right now.
            StructuredBuffer<int> totalSegsOfPlanet;
            uint numSegments;
            uint numPlanets;

            //the size of the halo relative to the planet's radius
            float planetHaloSize;

            float getSqrDistToSegment(float2 start, float2 end, float2 p) {
                float2 offset = end - start;
                float sqrSegLength = dot(offset, offset);
                float t = max(0, min(1, dot(p - start, offset) / sqrSegLength));
                //so, if you drew a straight line from p to the line defined by start and end, t is how far along the line it would be,
                //where 0 is directly at start and 1 is directly at end. the way this works is you get the vector from start to p.
                //you then take the dot product of that vector and the offset from start to end. you then have to divide it by the length of
                //offset twice, once because the dot operation scales by length, another time to scale it to the range we want
                //the min and max functions simply clamp the result to [0, 1]
                float2 projection = start + t * offset; //where p is projected to be on the line defined by start and end, the closest point on that line
                float2 offsetToSegment = p - projection;
                offsetToSegment.x *= _ScreenParams.x / _ScreenParams.y;
                return dot(offsetToSegment, offsetToSegment); //the distance squared!
            }

            float2 getFurthestPointOnSegmentInRange(float2 start, float2 end, float2 p, float range, float sqrDstToSeg) {
                //gets the point between start and end within range of p that's as close to the end as possible
                float2 offset = end - start;
                float sqrSegLength = dot(offset, offset);
                //how far "up" (closer to the end of the segment) can we go before we're out of range, in "offset-lengths" (the length of the line segment)
                float lineRange = sqrt(((range * range) - (sqrDstToSeg)) / sqrSegLength);
                float t = max(0, min(1, dot(p - start, offset) / sqrSegLength) + lineRange);
                // we add lineRange to our dot product to get the point as far along the line segment as possible while still being in range
                return start + t * offset;
            }
            
            fixed4 frag(v2f v) : SV_Target {
                fixed4 col = tex2D(_MainTex, v.uv);
                
                float3 color = col.rgb;
                
                //for keeping track of how many segments for each planet have been tested
                int numSegsOfPlanetSoFar[20];
                for (uint k = 0; k < 20; k++) {
                    numSegsOfPlanetSoFar[k] = 0;
                }
                
                // the shading of the planet trails
                for (uint i = 0; i < numSegments; i++) {
                    float sqrDstToSeg = getSqrDistToSegment(segments[i].start, segments[i].end, v.uv);
                    if (sqrDstToSeg <= segments[i].thickness * segments[i].thickness) {// this pixel is in range of the segment
                        //the furthest point along that segment that is within range of the pixel
                        float2 lastPointInRange = getFurthestPointOnSegmentInRange(segments[i].start, segments[i].end, v.uv, segments[i].thickness, sqrDstToSeg);

                        // we want the top of the circle around the trail to be shaded lighter than the bottom
                        float relativeHeightChange = v.uv.y - lastPointInRange.y;
                        float colorStrength = ((relativeHeightChange * 3) / (segments[i].thickness * 8) + 0.625) * 0.5; //scales from [-thickness, thickness] to [0.125, 0.5]
                        colorStrength -= ((float)(totalSegsOfPlanet[segments[i].planetIndex] - numSegsOfPlanetSoFar[segments[i].planetIndex])) / ((float)(totalSegsOfPlanet[segments[i].planetIndex])); //makes it fade out
                        color = colors[segments[i].planetIndex] * max(colorStrength + 0.1, 0);
                    }
                    numSegsOfPlanetSoFar[segments[i].planetIndex] ++;
                }
                
                //the nice planet shading
                for (uint j = 0; j < numPlanets; j++) {
                    float2 offset = v.uv - planets[j].viewportPos;
                    offset.x *= _ScreenParams.x / _ScreenParams.y; // the scaling I was talking about
                    float dstToPlanet = length(offset);
                    if (dstToPlanet <= planets[j].radius) {
                        float relativeHeightChange = v.uv.y - planets[j].viewportPos.y; // this makes the top of the planet brighter than the bottom
                        float colorStrength = ((relativeHeightChange * 7) / (planets[j].radius * 16) + 0.65625); //scales from [-radius, radius] to [0.125, 1]
                        color = colors[planets[j].index] * colorStrength;
                    } else if (dstToPlanet <= planets[j].radius * planetHaloSize) {
                        float x = (dstToPlanet - planets[j].radius) / (planets[j].radius * (planetHaloSize - 1));
                        //gets the distance from planet surface, where 0 is the surface and 1 is as far as we're allowing the halo to be
                        float alpha = pow(x - 1, 2) * 0.1; //put this into desmos if you need to, but gives us a nice slope from 0.1 to 0 as x increases
                        color = (colors[planets[j].index] * alpha) + (color * (1 - alpha)); //alpha blending to give us a nice halo effect
                    }
                }

                col = fixed4(color.xyz, 1);
                return col;
            }
            ENDCG
        }
    }
}
