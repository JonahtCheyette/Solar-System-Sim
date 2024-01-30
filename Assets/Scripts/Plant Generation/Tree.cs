using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Tree : MonoBehaviour {
    private Material planetMaterial;
    private Color leafColor;
    private Color barkColor;
    private static float hueRange = 10f;

    //structure bits
    private List<Branch> branches;
    private float trunkSize = 1f;
    private float trunkHeight = 10f;
    private const int resolution = 8;

    //growth bits
    private float completionPercent;
    private const float growthSpeed = 0.001f;
    private bool meshHasBeenGenerated;
    private List<BranchMeshHandler> branchMeshHandlers;

    private void Start() {
        branches = new List<Branch>();
        branchMeshHandlers = new List<BranchMeshHandler>();
        meshHasBeenGenerated = false;
        CelestialBodyPhysics planet = GravityHandler.GetClosestPlanet(transform.position);
        transform.parent = planet.transform; // fixes the tree in position

        planetMaterial = planet.gameObject.GetComponent<MeshRenderer>().material;

        Random.InitState(Time.frameCount);

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

        gameObject.GetComponent<MeshRenderer>().material.color = barkColor;

        completionPercent = 0;

        float sizeRatio = 0.7f;
        Vector2Int bendRanges = new Vector2Int(2, 5);

        BranchGenInfo info = new BranchGenInfo();
        info.start = Vector3.zero;
        info.end = trunkHeight * Vector3.up;
        info.planeOfAttatchment = Vector3.up;
        info.endSizes = new Vector2(trunkSize, trunkSize * sizeRatio);
        info.bendParameters = new Vector3(0.2f, 0.05f, 0.5f);
        info.numBends = bendRanges.x + Mathf.FloorToInt(Random.Range(0, 1) * (bendRanges.y - bendRanges.x + 1));
        if (info.numBends > bendRanges.y) {
            info.numBends = bendRanges.y;
        }
        info.startingPercent = 0;
        info.hasLeaves = false;
        GenerateBranch(info, bendRanges, sizeRatio, 7, 0.1f, 0.6f, 0.2f, 5, 5);
    }

    // Update is called once per frame
    void Update() {
        if (completionPercent <= 1) {
            completionPercent += growthSpeed;
            bool done = false;
            if(completionPercent > 1) {
                done = true;
            }
            completionPercent = Mathf.Clamp01(completionPercent);
            Mesh treeMesh = GetComponent<MeshFilter>().mesh;
            if(treeMesh == null) {
                treeMesh = new Mesh();
                GetComponent<MeshFilter>().mesh = treeMesh;
            }
            if (!meshHasBeenGenerated) {
                List<Vector3> vertices = new List<Vector3>();
                List<int> tris = new List<int>();
                for (int i = 0; i < branches.Count; i++) {
                    branchMeshHandlers.Add(new BranchMeshHandler(branches[i], resolution));
                    (Vector3[], int[]) data = branchMeshHandlers[i].GetMeshData(completionPercent);
                    for (int j = 0; j < data.Item2.Length; j++) {
                        tris.Add(data.Item2[j] + vertices.Count);
                    }
                    vertices.AddRange(data.Item1);
                }
                treeMesh.vertices = vertices.ToArray();
                treeMesh.triangles = tris.ToArray();
                treeMesh.RecalculateNormals();
                treeMesh.RecalculateBounds();
                meshHasBeenGenerated = true;
            } else {
                List<Vector3> vertices = new List<Vector3>();
                for (int i = 0; i < branchMeshHandlers.Count; i++) {
                    vertices.AddRange(branchMeshHandlers[i].GetVertexData(completionPercent));
                }
                treeMesh.vertices = vertices.ToArray();
                treeMesh.RecalculateNormals();
                treeMesh.RecalculateBounds();
            }
            if (done) {
                completionPercent = 2;
            }
        }
    }

    private void GenerateBranch(BranchGenInfo info, Vector2Int bendRanges, float sizeRatio, int branchiness, float branchPosJiggle, float branchLengthMultiplier, float branchLengthMultiplierJiggle, int depth, int maxDepth) {
        if(depth >= 0) {
            Branch generatedBranch = new Branch(info);
            branches.Add(generatedBranch);
            
            float bbPosThresh = 1 - (1 + (float)maxDepth - depth) / (maxDepth + 2); //bb for bend & branch
            float finalStartingPercent = (depth == maxDepth) ? 0 : info.startingPercent;
            
            if(depth > 0) {
                //new branches based on this branch
                int numBranches = Mathf.Max(1, Random.Range(1, branchiness + 1));
                for (int i = 0; i < numBranches; i++) {

                    BranchGenInfo derivedInfo = new BranchGenInfo();
                    derivedInfo.bendParameters = info.bendParameters;
                    derivedInfo.numBends = bendRanges.x + Mathf.FloorToInt(Random.Range(0, 1) * (bendRanges.y - bendRanges.x + 1));
                    if (derivedInfo.numBends > bendRanges.y) {
                        derivedInfo.numBends = bendRanges.y;
                    }

                    float branchPoint;
                    if (bbPosThresh != 0) {
                        branchPoint = bbPosThresh + i * (1 - bbPosThresh) / (derivedInfo.numBends + 1) + Random.Range(-branchPosJiggle, branchPosJiggle) / 2;
                    } else {
                        branchPoint = (i + 1) / (derivedInfo.numBends + 1) + Random.Range(-branchPosJiggle, branchPosJiggle) / 2;
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

                    Vector3 branchDir = (info.end - info.start).normalized;
                    Vector3 perpendicular = Vector3.Cross(branchDir, (branchDir == Vector3.up || branchDir == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
                    branchDir = MakeQuaternion(perpendicular, Random.Range(0.1f, Mathf.PI / 2)) * branchDir;
                    branchDir = MakeQuaternion(info.end - info.start, Random.Range(0, 2 * Mathf.PI)) * branchDir;

                    float lengthMultiplier = branchLengthMultiplier + Random.Range(-branchLengthMultiplierJiggle / 2, branchLengthMultiplierJiggle / 2);
                    lengthMultiplier = Mathf.Clamp01(lengthMultiplier);
                    Vector3 branchEnd = branchStart + branchDir * ((info.end - info.start).magnitude * lengthMultiplier);

                    float percentageStartingPoint = finalStartingPercent + ((1 - finalStartingPercent) * branchPoint);

                    derivedInfo.start = branchStart;
                    derivedInfo.end = branchEnd;
                    derivedInfo.planeOfAttatchment = perpendicular;
                    derivedInfo.endSizes = new Vector2(branchBaseSize, (depth == 1) ? 0 : branchBaseSize * sizeRatio);
                    derivedInfo.startingPercent = percentageStartingPoint;
                    derivedInfo.hasLeaves = false;

                    GenerateBranch(derivedInfo, bendRanges, sizeRatio, branchiness + 1, branchPosJiggle, branchLengthMultiplier, branchLengthMultiplierJiggle, depth - 1, maxDepth);
                }
            }
        }
    }

    private static Quaternion MakeQuaternion(Vector3 axis, float angle) {
        Vector3 scaledAxis = axis.normalized * Mathf.Sin(angle / 2);
        return new Quaternion(scaledAxis.x, scaledAxis.y, scaledAxis.z, Mathf.Cos(angle / 2));
    }

    private struct BranchMeshHandler {
        private Vector3[] vertexEndPositions;
        private int[] tris;
        private float[] ringMaxSizes;
        private float[] ringPercentBuffers;
        private float[] ringFinishingPercents;
        private float startingPercent;
        private const float branchMaxPercentBuffer = 0.05f; // required for the branches, since the ends of the branches aren't required to be points
        private Vector3 branchX;
        private Vector3 branchY;
        private Vector3 branchDir;
        private int resolution;

        public BranchMeshHandler(Branch derived, int resolution) {
            this.resolution = resolution;
            vertexEndPositions = new Vector3[derived.points.Length * resolution];
            tris = new int[3*(2*(resolution - 2) + (derived.points.Length - 1) * resolution * 2)];
            ringMaxSizes = derived.pointMaxSizes;
            startingPercent = derived.startingPercent;
            ringPercentBuffers = new float[ringMaxSizes.Length];
            ringFinishingPercents = new float[ringMaxSizes.Length];

            for (int i = 0; i < ringMaxSizes.Length; i++) {
                ringPercentBuffers[i] = branchMaxPercentBuffer * ringMaxSizes[i] / ringMaxSizes[0];
                ringFinishingPercents[i] = ringPercentBuffers[i] + (derived.points[i] - derived.points[0]).magnitude / (derived.points[derived.points.Length - 1] - derived.points[0]).magnitude;
            }

            for (int i = 0; i < ringMaxSizes.Length; i++) {
                ringPercentBuffers[i] /= ringFinishingPercents[ringFinishingPercents.Length - 1];
                ringFinishingPercents[i] /= ringFinishingPercents[ringFinishingPercents.Length - 1]; // a little gross, but I want the #s between 0 and 1.
            }

            branchDir = (derived.points[derived.points.Length - 1] - derived.points[0]).normalized;
            branchX = Vector3.Cross(branchDir, (branchDir == Vector3.up || branchDir == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
            branchY = Vector3.Cross(branchDir, branchX);

            float angleIncrement = 2 * Mathf.PI / resolution;

            int triIndex = 0;
            for (int i = 0; i < vertexEndPositions.Length; i++) {
                int ringIndex = i / resolution;
                int indexOnRing = i % resolution;
                vertexEndPositions[i] = derived.points[ringIndex] + ringMaxSizes[ringIndex] * (branchX * Mathf.Cos(angleIncrement * indexOnRing) + branchY * Mathf.Sin(angleIncrement * indexOnRing));
                
                if(ringIndex < derived.points.Length - 1) {
                    tris[triIndex] = i;
                    tris[triIndex + 1] = i + resolution;
                    tris[triIndex + 2] = i - 1 + resolution;
                    if (indexOnRing == 0) {
                        tris[triIndex + 2] += resolution;
                    }
                    triIndex += 3;
                    tris[triIndex] = i;
                    tris[triIndex + 1] = i + 1;
                    tris[triIndex + 2] = i + resolution;
                    if(indexOnRing == resolution - 1) {
                        tris[triIndex + 1] = ringIndex * resolution;
                    }
                    triIndex += 3;
                }
            }

            for (int i = 0; i < 2 * (resolution - 2); i++) { // attatching the top and bottom caps
                bool bottom = i < resolution - 2;
                tris[triIndex] = bottom ? 0 : resolution * (derived.points.Length - 1);
                tris[bottom ? triIndex + 2 : triIndex + 1] = tris[triIndex] + 1 + (i % (resolution - 2));
                tris[bottom ? triIndex + 1 : triIndex + 2] = tris[bottom ? triIndex + 2 : triIndex + 1] + 1; //I have to do a ton of switching due to winding order
                triIndex += 3;
            }
        }

        public (Vector3[], int[]) GetMeshData(float completionPercent) {
            return (GetVertexData(completionPercent), tris);
        }

        public Vector3[] GetVertexData(float completionPercent) {
            if (completionPercent == 1) {
                return vertexEndPositions;
            }
            if(completionPercent < startingPercent) {
                return new Vector3[vertexEndPositions.Length];
            }
            float branchPercent = (completionPercent - startingPercent) / (1 - startingPercent);
            Vector3[] returnVal = new Vector3[vertexEndPositions.Length];
            vertexEndPositions.CopyTo(returnVal, 0);
            float angleIncrement = 2 * Mathf.PI / resolution;
            for (int i = 0; i < ringFinishingPercents.Length; i++) {
                if (i != ringFinishingPercents.Length - 1) {
                    if (branchPercent >= ringFinishingPercents[i] - ringPercentBuffers[i] && branchPercent < ringFinishingPercents[i + 1]) {
                        //end of the branch growing out from the last ring
                        Vector3 gap = returnVal[i * resolution] - returnVal[(i + 1) * resolution];
                        for (int j = 0; j < resolution; j++) {
                            returnVal[(i + 1) * resolution + j] += gap * (1 - (branchPercent - ringFinishingPercents[i] + ringPercentBuffers[i]) / (ringFinishingPercents[i + 1] - ringFinishingPercents[i] + ringPercentBuffers[i]));
                        }
                    }
                }

                if (branchPercent < ringFinishingPercents[i]) {
                    //closing the ring entirely
                    for (int j = 0; j < resolution; j++) {
                        returnVal[i * resolution + j] -= ringMaxSizes[i] * (branchX * Mathf.Cos(angleIncrement * j) + branchY * Mathf.Sin(angleIncrement * j));
                    }
                    if (branchPercent >= ringFinishingPercents[i] - ringPercentBuffers[i]) {
                        //ring needs to finish growing, open it up a little
                        for (int j = 0; j < resolution; j++) {
                            returnVal[i * resolution + j] += ((branchPercent - ringFinishingPercents[i] + ringPercentBuffers[i]) / ringPercentBuffers[i]) * ringMaxSizes[i] * (branchX * Mathf.Cos(angleIncrement * j) + branchY * Mathf.Sin(angleIncrement * j));
                        }
                    }
                }
            }

            return returnVal;
        }
    }

    private struct Branch {
        public Vector3[] points;
        public float[] pointMaxSizes;
        public float[] bendPoints;
        public Vector3 planeOfAttatchment;
        public bool hasLeaves;
        public float startingPercent;

        public Branch(BranchGenInfo info) {
            points = new Vector3[info.numBends + 2];
            points[0] = info.start;
            points[info.numBends + 1] = info.end;
            pointMaxSizes = new float[info.numBends + 2];
            pointMaxSizes[0] = info.endSizes.x;
            pointMaxSizes[info.numBends + 1] = info.endSizes.y;
            planeOfAttatchment = info.planeOfAttatchment.normalized;
            bendPoints = new float[info.numBends];
            hasLeaves = info.hasLeaves;
            startingPercent = info.startingPercent;

            Vector3 branchDirection = (points[info.numBends + 1] - points[0]).normalized;
            Vector3 branchX = Vector3.Cross(branchDirection, (branchDirection == Vector3.up || branchDirection == Vector3.down) ? Vector3.forward : Vector3.up).normalized;
            Vector3 branchY = Vector3.Cross(branchDirection, branchX);
            for (int i = 0; i < info.numBends; i++) {
                if (info.bendParameters.z != 0) {
                    bendPoints[i] = info.bendParameters.z + i * (1 - info.bendParameters.z) / (info.numBends + 1);
                } else {
                    bendPoints[i] = (i + 1) / (info.numBends + 1);
                }
                bendPoints[i] += Random.Range(-info.bendParameters.y, info.bendParameters.y) / 2;
                bendPoints[i] = Mathf.Clamp(bendPoints[i], info.bendParameters.z, 1);
                float angle = Random.Range(0, 2 * Mathf.PI);
                float displacement = Random.Range(0, info.bendParameters.x * (info.endSizes.x + info.endSizes.y)/2);
                points[i + 1] = info.start + (info.end - info.start) * bendPoints[i] + (branchX * Mathf.Cos(angle) + branchY * Mathf.Sin(angle)) * displacement;
                pointMaxSizes[i + 1] = info.endSizes.x + (info.endSizes.y - info.endSizes.x) * bendPoints[i];
            }
        }
    }

    private struct BranchGenInfo {
        public Vector3 start;
        public Vector3 end;
        public Vector3 planeOfAttatchment;
        public Vector2 endSizes;
        public Vector3 bendParameters;
        /*bendiness, bendPosJiggle, bendPosThresh. 
         * How much the bends are shifted around horizontally (assuming the branch is vertical) as a percentage vs total height, 
         * how much the bends are shifted around vertically (as a percentage of total height), 
         * what is the lowest spot on the branch a bend can be
        */
        public int numBends;
        public float startingPercent; //when should this branch start appearing, as a percentage of total tree growth. All branches are assumed to end at 100%
        public bool hasLeaves;
    }
}
