using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(CelestialBodyPhysics))]
public class CelestialBodyMeshHandler : MonoBehaviour {
    [Min(0)]
    public float visibleChunkMinAngle = 0f;
    public float[] LODAngles = new float[4];
    [Min(1)]
    public int numChunksPerEdgeOfFace = 1;
    [Range(1, 6)]
    public int chunkSizeIndex = 1;

    public Transform viewer;
    [Min(0)]
    public float lowRezThreshhold;

    [Range(0, 90f)]
    public float viewerMoveThresholdForChunkUpdate = 5f;
    [Range(0, 4)]
    public int colliderLODIndex;
    [Range(0, 90f)]
    public float colliderMinAngle;

    public CelestialBodyGenerator celestialBodyGenerator;
    public BaseShaderDataGenerator shaderDataGenerator;

    private Material material;

    private CelestialBodyChunk[] chunks;
    private Mesh lowRezMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private bool wasLowRezLastFrame;
    private Vector3 oldRelativeViewerPosition;

    public float minRadius { get; private set; }
    public float maxRadius { get; private set; }

    //basic Icosahedron stuff. Diagram: http://1.bp.blogspot.com/_-FeuT9Vh6rk/Sj1WHbcQwxI/AAAAAAAAABw/xaFDct6AyOI/s400/icopoints.png
    private static float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
    private static Vector3[] icosahedronVerts = new Vector3[12] {
        new Vector3(-1, goldenRatio, 0),
        new Vector3(1, goldenRatio, 0),
        new Vector3(-1, -goldenRatio, 0),
        new Vector3(1, -goldenRatio, 0),

        new Vector3(0, -1, goldenRatio),
        new Vector3(0, 1, goldenRatio),
        new Vector3(0, -1, -goldenRatio),
        new Vector3(0, 1, -goldenRatio),

        new Vector3(goldenRatio, 0, -1),
        new Vector3(goldenRatio, 0, 1),
        new Vector3(-goldenRatio, 0, -1),
        new Vector3(-goldenRatio, 0, 1)
    };
    //face and opposite edge, meaning, if you look at a circle of 5 faces on the icosahedron, each face shares a point in the center of the circle, and has an edge that is orientated directly away from that face and is attatched to that shared point
    private static float angleBetweenFaceAndOppositeEdge = Vector3.Angle(Vector3.up, Vector3.up * goldenRatio - icosahedronVerts[5]);
    //defines the faces of the icosahedron by what points make up the faces.
    //ordered so that the pentagon around vertex 0 is first, then the faces that share an edge with that pentagon, then the pentagon around vertex 3
    //and the faces that share an edge with that pentagon.
    //the vertices within each face are ordered clockwise
    private static Vector3Int[] icosahedronFaces = new Vector3Int[20] {
        new Vector3Int(0, 11, 5),
        new Vector3Int(0, 10, 11),
        new Vector3Int(0, 7, 10),
        new Vector3Int(0, 1, 7),
        new Vector3Int(0, 5, 1),

        new Vector3Int(11, 4, 5),
        new Vector3Int(10, 2, 11),
        new Vector3Int(7, 6, 10),
        new Vector3Int(1, 8, 7),
        new Vector3Int(5, 9, 1),

        new Vector3Int(3, 9, 4),
        new Vector3Int(3, 8, 9),
        new Vector3Int(3, 6, 8),
        new Vector3Int(3, 2, 6),
        new Vector3Int(3, 4, 2),

        new Vector3Int(9, 5, 4),
        new Vector3Int(8, 1, 9),
        new Vector3Int(6, 7, 8),
        new Vector3Int(2, 10, 6),
        new Vector3Int(4, 11, 2)
    };

    private static Dictionary<Vector2Int, Vector2Int> edgeToFaces = new Dictionary<Vector2Int, Vector2Int>() { 
        [new Vector2Int(0,5)] = new Vector2Int(0,4),
        [new Vector2Int(0,11)] = new Vector2Int(0,1),
        [new Vector2Int(5,11)] = new Vector2Int(0,5),
        [new Vector2Int(0,10)] = new Vector2Int(1,2),
        [new Vector2Int(10,11)] = new Vector2Int(1,6),
        [new Vector2Int(0,7)] = new Vector2Int(2,3),
        [new Vector2Int(7,10)] = new Vector2Int(2,7),
        [new Vector2Int(0, 1)] = new Vector2Int(3, 4),
        [new Vector2Int(1, 7)] = new Vector2Int(3, 8),
        [new Vector2Int(1, 5)] = new Vector2Int(4, 9),

        [new Vector2Int(3, 4)] = new Vector2Int(10, 14),
        [new Vector2Int(3, 9)] = new Vector2Int(10, 11),
        [new Vector2Int(4, 9)] = new Vector2Int(10, 15),
        [new Vector2Int(3, 8)] = new Vector2Int(11, 12),
        [new Vector2Int(8, 9)] = new Vector2Int(11, 16),
        [new Vector2Int(3, 6)] = new Vector2Int(12, 13),
        [new Vector2Int(6, 8)] = new Vector2Int(12, 17),
        [new Vector2Int(2, 3)] = new Vector2Int(13, 14),
        [new Vector2Int(2, 6)] = new Vector2Int(13, 18),
        [new Vector2Int(2, 4)] = new Vector2Int(14, 19),

        [new Vector2Int(4, 11)] = new Vector2Int(5, 19),
        [new Vector2Int(4, 5)] = new Vector2Int(5, 15),
        [new Vector2Int(5, 9)] = new Vector2Int(9, 15),
        [new Vector2Int(1, 9)] = new Vector2Int(9, 16),
        [new Vector2Int(1, 8)] = new Vector2Int(8, 16),
        [new Vector2Int(7, 8)] = new Vector2Int(8, 17),
        [new Vector2Int(6, 7)] = new Vector2Int(7, 17),
        [new Vector2Int(6, 10)] = new Vector2Int(7, 18),
        [new Vector2Int(2, 10)] = new Vector2Int(6, 18),
        [new Vector2Int(2, 11)] = new Vector2Int(6, 19)
    };

    private List<Vector3[]> baseChunkVerts;
    private int[] chunkStartIndexes;
    private int tempVertsIndex;
    private int numChunksVerticesGenerated;
    //65535 is the limit to the number of thread groups that can be on my GPU at once, so the max number of verts the gpu can handle at once is that times the number of threads per thread group (32)
    private const int maxNumVertsPerArray = 32 * 65535;
    private bool finishedInitializing;

    // Start is called before the first frame update
    private void Start() {
        GetMaterial();

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        DestroyChunksIfTheyExist();
        InitializeChunks();
        finishedInitializing = false;
    }

    // Update is called once per frame
    private void Update() {
        if (finishedInitializing) {
            if ((transform.position - viewer.position).sqrMagnitude >= Mathf.Pow(maxRadius + lowRezThreshhold, 2)) {
                if (!wasLowRezLastFrame) {
                    meshFilter.mesh = lowRezMesh;
                    DisableChunks();
                }
                wasLowRezLastFrame = true;
            } else {
                Vector3 relativeViewerPosition = viewer.position - transform.position;
                if (wasLowRezLastFrame) {
                    //update chunks
                    meshFilter.mesh = null;
                    EnableChunks();
                    UpdateChunks();
                    oldRelativeViewerPosition = relativeViewerPosition;
                } else {
                    //update chunks if the angle between the old position and new is too much
                    if (Vector3.Angle(relativeViewerPosition, oldRelativeViewerPosition) > viewerMoveThresholdForChunkUpdate) {
                        UpdateChunks();
                        oldRelativeViewerPosition = relativeViewerPosition;
                    }
                }
                wasLowRezLastFrame = false;
            }
        }
    }

    private void OnValidate() {
        if (LODAngles.Length != 4) {
            float maxValue = 0;

            List<float> temp = new List<float>();
            for (int i = 0; i < 4; i++) {
                if (i < LODAngles.Length) {
                    temp.Add(LODAngles[i]);
                    if(maxValue < LODAngles[i]) {
                        maxValue = LODAngles[i];
                    }
                } else {
                    temp.Add(maxValue);
                }
            }

            LODAngles = temp.ToArray();
        }
        for (int i = 1; i < 4; i++) {
            if(LODAngles[i] < LODAngles[i - 1]) {
                LODAngles[i] = LODAngles[i - 1];
            }
        }
        visibleChunkMinAngle = Mathf.Min(visibleChunkMinAngle, LODAngles[3]);
        numChunksPerEdgeOfFace = Mathf.Min(numChunksPerEdgeOfFace, 78);
        colliderMinAngle = Mathf.Max(colliderMinAngle, viewerMoveThresholdForChunkUpdate);
        #if UNITY_EDITOR
        if (celestialBodyGenerator != null) {
            celestialBodyGenerator.meshHandler = this;
        }
        if (shaderDataGenerator != null) {
            shaderDataGenerator.meshHandler = this;
            GetMaterial();
        }
        #endif
    }

    private void OnApplicationQuit() {
        DestroyMaterialBuffers();
    }

    private void OnDestroy() {
        DestroyMaterialBuffers();
    }

    private void OnDisable() {
        DestroyMaterialBuffers();
    }

    private void DestroyMaterialBuffers() {
        if(shaderDataGenerator != null) {
            shaderDataGenerator.DestroyMaterialBuffers();
        }
    }

    public void Generate() {
        GetMaterial();

        numChunksVerticesGenerated = 0;
        DestroyChunksIfTheyExist();
        if(meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
        }
        meshFilter.mesh = null;

        minRadius = float.MaxValue; // this is to ensure that the min and max radii get set by generateLowRezMesh
        maxRadius = 0f;
        GetLowRezMesh();
        meshFilter.mesh = lowRezMesh;
    }

    public void GetMaterial() {
        if (Application.isPlaying) {
            material = GetComponent<MeshRenderer>().material;
        } else {
            material = GetComponent<MeshRenderer>().sharedMaterial;
        }
        if (shaderDataGenerator != null) {
            shaderDataGenerator.SetMaterial(material);
        }
    }

    private void InitializeChunks() {// :( this function is complex
        int chunkSize = 24 * chunkSizeIndex;

        int numChunksPerFace = numChunksPerEdgeOfFace * numChunksPerEdgeOfFace;
        int numPointsPerChunk = (chunkSize + 7) * (chunkSize + 8) / 2 - 3;
        int totalNumPoints = 20 * numChunksPerFace * numPointsPerChunk - 60;
        baseChunkVerts = new List<Vector3[]>();
        for (int i = 0; i < totalNumPoints / maxNumVertsPerArray; i++) {
            baseChunkVerts.Add(new Vector3[maxNumVertsPerArray]);
        }
        baseChunkVerts.Add(new Vector3[totalNumPoints % maxNumVertsPerArray]);
        tempVertsIndex = 0;

        chunks = new CelestialBodyChunk[20 * numChunksPerFace];
        chunkStartIndexes = new int[chunks.Length];
        for (int faceIndex = 0; faceIndex < 20; faceIndex++) {
            Vector3Int face = icosahedronFaces[faceIndex];
            Vector3 startVertex = icosahedronVerts[face.x];
            Vector3 faceRightEdgeIncrement = (icosahedronVerts[face.y] - startVertex) / numChunksPerEdgeOfFace;
            Vector3 faceLeftIncrement = (icosahedronVerts[face.z] - icosahedronVerts[face.y]) / numChunksPerEdgeOfFace;

            Vector3 faceNormal = Vector3.Cross(faceRightEdgeIncrement + faceLeftIncrement, faceRightEdgeIncrement).normalized;
            Vector3[] vertToVertIncrement = new Vector3[3] { faceRightEdgeIncrement / (chunkSize + 3), (faceRightEdgeIncrement + faceLeftIncrement) / (chunkSize + 3), faceLeftIncrement / (chunkSize + 3) }; // just the increments between the main vertices of each chunk. Order is going right and down, left and down, and directly left
            Vector3[] overFaceEdgeIncrement = new Vector3[3] { vertToVertIncrement[2], -vertToVertIncrement[2], vertToVertIncrement[0] }; // to get from the vertices on the edge of one chunk to the vertices of the next
            //for getting the correct point to use when calculating the normal of the vertexes of the base icosahedron. As such these initial values go from the corner of the included vertices of the chunk to the corner of the complete triangular grid
            //order is right bottom corner, left bottom corner, top corner
            Vector3[] facePointIncrement = new Vector3[3] { vertToVertIncrement[0] - vertToVertIncrement[2], vertToVertIncrement[2] + vertToVertIncrement[1], - (vertToVertIncrement[0] + vertToVertIncrement[1]) };
            for (int i = 0; i < 3; i++) {
                int vertex1 = i < 2 ? face.x : face.z;
                int vertex2 = i == 0 ? face.z : face.y;
                int angleDir = i == 1 ? -1 : 1;
                facePointIncrement[i] = Quaternion.AngleAxis(angleDir * angleBetweenFaceAndOppositeEdge, icosahedronVerts[vertex2] - icosahedronVerts[vertex1]) * facePointIncrement[i].normalized * vertToVertIncrement[0].magnitude;
                if(vertex1 > vertex2) {
                    int temp = vertex2;
                    vertex2 = vertex1;
                    vertex1 = temp;
                }
                Vector2Int edge = new Vector2Int(vertex1, vertex2);
                int otherFace = edgeToFaces[edge].x != faceIndex ? edgeToFaces[edge].x : edgeToFaces[edge].y;
                Vector3 otherFaceNormal = Vector3.Cross(icosahedronVerts[icosahedronFaces[otherFace].z] - icosahedronVerts[icosahedronFaces[otherFace].x], icosahedronVerts[icosahedronFaces[otherFace].y] - icosahedronVerts[icosahedronFaces[otherFace].x]).normalized;
                overFaceEdgeIncrement[i] = Quaternion.FromToRotation(faceNormal, otherFaceNormal) * overFaceEdgeIncrement[i];
            }

            for (int y = 0; y < numChunksPerEdgeOfFace; y++) {
                for (int x = 0; x < y + 1; x++) {
                    for (int triIndex = 0; triIndex < (x == y ? 1 : 2); triIndex++) {
                        bool upsideDown = triIndex == 1;
                        int chunkIndex = faceIndex * numChunksPerFace + x * 2 + y * y + triIndex;
                        Vector3 baseVertex = startVertex + y * faceRightEdgeIncrement + x * faceLeftIncrement;
                        Vector3[] corners = new Vector3[3] { baseVertex, baseVertex + faceRightEdgeIncrement, baseVertex + faceRightEdgeIncrement + faceLeftIncrement };
                        bool[] cornerOnPointOfFace = new bool[3] { y == 0, y == numChunksPerEdgeOfFace - 1 && x == 0, y == numChunksPerEdgeOfFace - 1 && x == y };
                        bool[] foldEdges = new bool[3] { x == 0, x == (upsideDown ? y - 1 : y), y == numChunksPerEdgeOfFace - 1 };
                        if (upsideDown) {
                            corners[1] = corners[2];
                            corners[2] = baseVertex + faceLeftIncrement;
                            for (int k = 0; k < 3; k++) {
                                cornerOnPointOfFace[k] = false;
                            }
                        }
                        chunks[chunkIndex] = new CelestialBodyChunk(transform, viewer, material, corners, visibleChunkMinAngle, LODAngles, colliderLODIndex, colliderMinAngle, chunkSize, cornerOnPointOfFace);
                        chunks[chunkIndex].UVMap = new Vector4[chunks[chunkIndex].vertexMap.Length, Mathf.CeilToInt(shaderDataGenerator.GetNumOutputFloats()/ 4f)];
                        ThreadedDataRequester.RequestData(() => GeneratePoints(cornerOnPointOfFace, facePointIncrement, overFaceEdgeIncrement, vertToVertIncrement, foldEdges, baseVertex, chunkSize, upsideDown), (object o) => FillBaseChunkVerts((Vector3[])o, chunkIndex));
                    }
                }
            }
        }
    }

    private Vector3[] GeneratePoints(bool[] cornerOnPointOfFace, Vector3[] facePointIncrement, Vector3[] overFaceEdgeIncrement, Vector3[] vertToVertIncrement, bool[] foldEdges, Vector3 baseVertex, int chunkSize, bool upsideDown) {
        int numPoints = (chunkSize + 7) * (chunkSize + 8) / 2 - 3;
        for (int i = 0; i < 3; i++) {
            if (cornerOnPointOfFace[i]) {
                numPoints--;
            }
        }

        Vector3[] points = new Vector3[numPoints];
        int vertIndex = 0;
        for (int y = 1; y < chunkSize + 7; y++) {
            for (int x = 0; x < y + 1; x++) {
                if (!(y == chunkSize + 6 && (x == 0 || x == y))) { // skipping the bottom 2 corners of the triangle
                    if (upsideDown || !((cornerOnPointOfFace[0] && y == 1 && x == 0) || (y == chunkSize + 6 && ((cornerOnPointOfFace[1] && x == 1) || (cornerOnPointOfFace[2] && x == y - 1))))) {
                        // skipping the verts missed cuz the 3 corners of the face only have 5 points around them as opposed to 6
                        Vector3 vertex = baseVertex + (y - 2) * vertToVertIncrement[0] + (x - 1) * vertToVertIncrement[2];
                        if (!upsideDown) {
                            bool topCornerOfFace = cornerOnPointOfFace[0] && y == 1 && x == 1;
                            bool rightCornerOfFace = cornerOnPointOfFace[1] && y == chunkSize + 5 && x == 0;
                            bool leftCornerOfFace = cornerOnPointOfFace[2] && y == chunkSize + 5 && x == y;
                            if (topCornerOfFace || rightCornerOfFace || leftCornerOfFace) {//if you're o
                                if (topCornerOfFace) {
                                    vertex = baseVertex + facePointIncrement[2];
                                } else if (rightCornerOfFace) {
                                    vertex += vertToVertIncrement[2] + facePointIncrement[0];
                                } else {
                                    vertex -= vertToVertIncrement[2];
                                    vertex += facePointIncrement[1];
                                }
                            } else {
                                if (x == 0 && foldEdges[0]) { // right edge of icosahedral face
                                    vertex += vertToVertIncrement[2];
                                    vertex += overFaceEdgeIncrement[1];
                                }
                                if (x == y && foldEdges[1]) { // left edge
                                    vertex -= vertToVertIncrement[2];
                                    vertex += overFaceEdgeIncrement[0];
                                }
                                if (y == chunkSize + 6 && foldEdges[2]) { // bottom edge
                                    vertex -= vertToVertIncrement[0];
                                    vertex += overFaceEdgeIncrement[2];
                                }
                            }
                        } else {
                            vertex = baseVertex + (y - 2) * vertToVertIncrement[1] - (x - 1) * vertToVertIncrement[0];
                            if (foldEdges[0] && y == 1) {// topright corner
                                vertex += vertToVertIncrement[2];
                                vertex += overFaceEdgeIncrement[1];
                            }
                            if (foldEdges[1] && ((y == chunkSize + 6 && x == y - 1) || (y == chunkSize + 5 && x == y))) {// topleft corner
                                vertex -= vertToVertIncrement[2];
                                vertex += overFaceEdgeIncrement[0];
                            }
                            if (foldEdges[2] && ((y == chunkSize + 6 && x == 1) || (y == chunkSize + 5 && x == 0))) {// bottom corner
                                vertex -= vertToVertIncrement[0];
                                vertex += overFaceEdgeIncrement[2];
                            }
                        }
                        points[vertIndex] = vertex; // doesn't matter if they get normalized, as they're normalized in the radius generator anyways
                        vertIndex++;
                    }
                }
            }
        }

        return points;
    }

    private void FillBaseChunkVerts(Vector3[] points, int chunkIndex) {
        chunkStartIndexes[chunkIndex] = tempVertsIndex;
        for (int i = 0; i < points.Length; i++) {
            baseChunkVerts[tempVertsIndex / maxNumVertsPerArray][tempVertsIndex % maxNumVertsPerArray] = points[i];
            tempVertsIndex++;
        }
        numChunksVerticesGenerated++;
        if(numChunksVerticesGenerated == chunks.Length) {
            FinishInitialization();
        }
    }

    private void FinishInitialization() {
        float minSqrRadius = float.MaxValue;
        float maxSqrRadius = float.MinValue;

        celestialBodyGenerator.Setup();
        shaderDataGenerator.Setup(false, celestialBodyGenerator.ProvideDataToShader());

        Vector4[,] UVData = new Vector4[(baseChunkVerts.Count - 1) * maxNumVertsPerArray + baseChunkVerts[baseChunkVerts.Count - 1].Length, shaderDataGenerator.GetNumOutputFloats()];
        int UVIndex = 0;
        for (int i = 0; i < baseChunkVerts.Count; i++) {
            for (int j = 0; j < baseChunkVerts[i].Length; j++) {
                Vector3 vert = baseChunkVerts[i][j];
                if (float.IsNaN(vert.x) || float.IsNaN(vert.y) || float.IsNaN(vert.z) || vert == Vector3.negativeInfinity || vert == Vector3.positiveInfinity) {
                    Debug.Log("NANI");
                }
            }
            baseChunkVerts[i] = celestialBodyGenerator.GeneratePoints(baseChunkVerts[i], ref minSqrRadius, ref maxSqrRadius);
            Vector4[,] batchUVData = shaderDataGenerator.GetValues(baseChunkVerts[i]);
            for (int j = 0; j < batchUVData.GetUpperBound(0) + 1; j++) {
                for (int k = 0; k < batchUVData.GetUpperBound(1) + 1; k++) {
                    UVData[UVIndex, k] = batchUVData[j, k];
                }
                UVIndex++;
            }
        }

        celestialBodyGenerator.Finish();
        shaderDataGenerator.Finish();

        minRadius = Mathf.Sqrt(minSqrRadius);
        maxRadius = Mathf.Sqrt(maxSqrRadius);

        shaderDataGenerator.SetRadiiInfo(maxRadius, minRadius);

        for (int i = 0; i < chunks.Length; i++) {
            for (int j = chunkStartIndexes[i]; j < chunkStartIndexes[i] + chunks[i].vertexMap.Length; j++) {
                Vector3 vert = baseChunkVerts[j / maxNumVertsPerArray][j % maxNumVertsPerArray];
                if (float.IsNaN(vert.x) || float.IsNaN(vert.y) || float.IsNaN(vert.z)) {
                    Debug.Log("what the fuck");
                }
                if (vert == Vector3.negativeInfinity) {
                    Debug.Log("is this motherfucking");
                }
                if (vert == Vector3.positiveInfinity) {
                    Debug.Log("horseshit");
                }
                chunks[i].vertexMap[j - chunkStartIndexes[i]] = baseChunkVerts[j / maxNumVertsPerArray][j % maxNumVertsPerArray];
                for (int k = 0; k < UVData.GetUpperBound(1) + 1; k++) {
                    chunks[i].UVMap[j - chunkStartIndexes[i], k] = UVData[j, k];
                }
            }
        }

        baseChunkVerts = null;
        GetLowRezMesh();

        finishedInitializing = true;

        if ((transform.position - viewer.position).sqrMagnitude >= Mathf.Pow(maxRadius + lowRezThreshhold, 2)) {
            meshFilter.mesh = lowRezMesh;
            DisableChunks();
            wasLowRezLastFrame = true;
        } else {
            Vector3 relativeViewerPosition = viewer.position - transform.position; 
            meshFilter.mesh = null;
            EnableChunks();
            UpdateChunks();
            oldRelativeViewerPosition = relativeViewerPosition;
            wasLowRezLastFrame = false;
        }
    }

    private void GetLowRezMesh() {
        // generating the low rezolution sphere
        lowRezMesh = SphereGenerator.IcoSphere(1, 79);
        float generatedMinValue = float.MaxValue;
        float generatedMaxRadius = float.MinValue;

        celestialBodyGenerator.Setup();
        lowRezMesh.vertices = celestialBodyGenerator.GeneratePoints(lowRezMesh.vertices, ref generatedMinValue, ref generatedMaxRadius, true);
        celestialBodyGenerator.Finish();

        if (generatedMinValue < minRadius * minRadius) {
            minRadius = Mathf.Sqrt(generatedMinValue);
        }
        if (generatedMaxRadius > maxRadius * maxRadius) {
            maxRadius = Mathf.Sqrt(generatedMaxRadius);
        }

        SetLowRezMeshValues();
    }

    public void SetLowRezMeshValues() {
        if(lowRezMesh == null) {
            Generate();
        } else {
            GetMaterial();
            lowRezMesh.RecalculateNormals();
            shaderDataGenerator.Setup(true, celestialBodyGenerator.ProvideDataToShader());
            shaderDataGenerator.SetRadiiInfo(maxRadius, minRadius);
            lowRezMesh = shaderDataGenerator.SetValues(lowRezMesh);
            shaderDataGenerator.Finish();
            lowRezMesh.RecalculateBounds();
        }
    }

    private void UpdateChunks() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].UpdateChunk();
        }
    }

    private void DisableChunks() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].DisableChunk();
        }
    }

    private void EnableChunks() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].EnableChunk();
        }
    }

    private void DestroyChunksIfTheyExist() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
            GameObject child = transform.GetChild(i).gameObject;
            if(child.name == "Celestial Body Chunk") {
                if (Application.isPlaying) {
                    Destroy(child);
                } else {
                    DestroyImmediate(child);
                }
            }
        }
    }

    public bool HasOceanEffect() {
        if(celestialBodyGenerator == null) {
            return false;
        }
        return celestialBodyGenerator.HasOceanEffect();
    }
}