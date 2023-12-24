using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour {
    private Material planetMaterial;
    private Color leafColor;
    private Color barkColor;
    private static float hueRange = 10f;

    //structure bits
    private List<Branch> branches;
    private float trunkSize;
    private float trunkHeight;

    private void Start() {
        CelestialBodyPhysics planet = GravityHandler.GetClosestPlanet(transform.position);
        planetMaterial = planet.gameObject.GetComponent<MeshRenderer>().material;

        Random.InitState(Mathf.CeilToInt(255 * planet.color.linear.maxColorComponent));

        float planetHue, planetSat, planetVal;
        Color.RGBToHSV(planet.color, out planetHue, out planetSat, out planetVal);
        planetHue *= 360f;
        planetHue = Random.Range(planetHue - hueRange, planetHue + hueRange);
        while (planetHue < 0) {
            planetHue = 360f + planetHue;
        } 
        while (planetHue > 360f) {
            planetHue -= 360f;
        }
        planetHue /= 360f;

        leafColor = Color.HSVToRGB(planetHue, planetSat, planetVal);
        barkColor = Color.HSVToRGB(Random.Range(50,60) / 360f, Random.Range(0.5f, 0.6f), Random.Range(0, 0.4f));
    }

    // Update is called once per frame
    void Update() {
        
    }

    private void GenerateBranch(Vector3 bottom, Vector3 top, Vector3 planeOfAttatchment, float size, float sizeRatio, float bendiness, float bendPosJiggle, Vector2Int bendRanges, int branchiness, float branchPosJiggle, float branchLengthMultiplier, float branchLengthMultiplierJiggle, int depth, int maxDepth) {
        if(depth >= 0) {
            int numBends = bendRanges.x + Mathf.FloorToInt(Random.Range(0, 1) * (bendRanges.y - bendRanges.x + 1));
            if(numBends > bendRanges.y) {
                numBends = bendRanges.y;
            }
            Vector2 sizePair = new Vector2(size, (depth == 0) ? 0 : size * sizeRatio);
            float bbPosThresh = 1 - ((float)maxDepth - depth) / (maxDepth + 1); //bb for bend & branch
            Branch generatedBranch = new Branch(bottom, top, planeOfAttatchment, sizePair, bendiness, bendPosJiggle, bbPosThresh, numBends);
            branches.Add(generatedBranch);
            if(depth > 0) {
                //new branches based on this branch
                int numBranches = Mathf.Max(1, Random.Range(1, Mathf.FloorToInt((branchiness + 1) * (((maxDepth * maxDepth) + (maxDepth - depth)*(maxDepth - depth) - (maxDepth - 1)*(maxDepth - 1))/(maxDepth * maxDepth)))));
                for (int i = 0; i < numBranches; i++) {
                    float branchPoint;
                    if (bbPosThresh != 0) {
                        branchPoint = bbPosThresh + i * (1 - bbPosThresh) / (numBends + 1) + Random.Range(-branchPosJiggle, branchPosJiggle) / 2;
                    } else {
                        branchPoint = (i + 1) / (numBends + 1) + Random.Range(-branchPosJiggle, branchPosJiggle) / 2;
                    }
                    branchPoint = Mathf.Clamp(branchPoint, bbPosThresh, 1);
                    int endingIndex = generatedBranch.bendPoints.Length + 1;
                    for (int j = 0; j < generatedBranch.bendPoints.Length; j++) {
                        if(branchPoint <= generatedBranch.bendPoints[j]) {
                            endingIndex = j + 1;
                            break;
                        }
                    }
                    float branchSegStart = (endingIndex == 1) ? 0 : generatedBranch.bendPoints[endingIndex - 2];
                    float branchSegEnd = (endingIndex == generatedBranch.bendPoints.Length + 1) ? 1 : generatedBranch.bendPoints[endingIndex - 1];
                    Vector3 branchStart = generatedBranch.points[endingIndex - 1] + (generatedBranch.points[endingIndex] - generatedBranch.points[endingIndex - 1]) * (branchPoint - branchSegStart) / (branchSegEnd - branchSegStart);
                    float branchBaseSize = generatedBranch.pointMaxSizes[endingIndex - 1] + (generatedBranch.pointMaxSizes[endingIndex] - generatedBranch.pointMaxSizes[endingIndex - 1]) * (branchPoint - branchSegStart) / (branchSegEnd - branchSegStart);

                    Vector3 branchDir = (top - bottom).normalized;
                    Vector3 perpendicular = Vector3.Cross(branchDir, (branchDir == Vector3.up || branchDir == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
                    branchDir = MakeQuaternion(perpendicular, Random.Range(0.1f, Mathf.PI / 2)) * branchDir;
                    branchDir = MakeQuaternion(top - bottom, Random.Range(0, 2 * Mathf.PI)) * branchDir;

                    float lengthMultiplier = branchLengthMultiplier + Random.Range(-branchLengthMultiplierJiggle / 2, branchLengthMultiplierJiggle / 2);
                    lengthMultiplier = Mathf.Clamp01(lengthMultiplier);
                    Vector3 branchEnd = branchStart + branchDir * ((top - bottom).magnitude * lengthMultiplier);
                    GenerateBranch(branchStart, branchEnd, Vector3.Cross(Vector3.Cross(branchDir, top - bottom), top - bottom), branchBaseSize, sizeRatio, bendiness, bendPosJiggle, bendRanges, branchiness, branchPosJiggle, branchLengthMultiplier, branchLengthMultiplierJiggle, depth - 1, maxDepth);
                }
            }
        }
    }


    private static Quaternion MakeQuaternion(Vector3 axis, float angle) {
        Vector3 scaledAxis = axis.normalized * Mathf.Sin(angle / 2);
        return new Quaternion(scaledAxis.x, scaledAxis.y, scaledAxis.z, Mathf.Cos(angle / 2));
    }


    private struct Branch {
        public Vector3[] points;
        public float[] pointMaxSizes;
        public float[] bendPoints;
        public Vector3 planeOfAttatchment;

        public Branch(Vector3 a, Vector3 b, Vector3 planeOfAttatchment, Vector2 endSizes, float bendiness, float bendPosJiggle, float bendPosThresh, int numBends) {
            points = new Vector3[numBends + 2];
            points[0] = a;
            points[numBends + 1] = b;
            pointMaxSizes = new float[numBends + 2];
            pointMaxSizes[0] = endSizes.x;
            pointMaxSizes[numBends + 1] = endSizes.y;
            this.planeOfAttatchment = planeOfAttatchment.normalized;
            bendPoints = new float[numBends];

            Vector3 branchDirection = (b - a).normalized;
            Vector3 branchX = Vector3.Cross(branchDirection, (branchDirection == Vector3.up || branchDirection == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
            Vector3 branchY = Vector3.Cross(branchDirection, branchX);
            for (int i = 0; i < numBends; i++) {
                if (bendPosThresh != 0) {
                    bendPoints[i] = bendPosThresh + i * (1 - bendPosThresh) / (numBends + 1) + Random.Range(-bendPosJiggle, bendPosJiggle) / 2;
                } else {
                    bendPoints[i] = (i + 1) / (numBends + 1) + Random.Range(-bendPosJiggle, bendPosJiggle) / 2;
                }
                bendPoints[i] = Mathf.Clamp(bendPoints[i], bendPosThresh, 1);
                float angle = Random.Range(0, 2 * Mathf.PI);
                float displacement = Random.Range(0, bendiness * (endSizes.x + endSizes.y)/2);
                points[i + 1] = a + (b - a) * bendPoints[i] + (branchX * Mathf.Cos(angle) + branchY * Mathf.Sin(angle)) * displacement;
                pointMaxSizes[i + 1] = endSizes.x + (endSizes.y - endSizes.x) * bendPoints[i];
            }
        }
    }
}
