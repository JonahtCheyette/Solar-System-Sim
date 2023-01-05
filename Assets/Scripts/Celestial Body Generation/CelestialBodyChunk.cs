using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialBodyChunk {
    private LODMesh[] LODMeshes;
    private MeshRenderer renderer;
    private MeshFilter filter;

    private Vector3 centerOfChunkDir;

    private int chunkSize;
    public Vector3[] vertexMap;
    public Vector4[,] UVMap;
    private bool[] cornersOnVertexOfFace;

    private Transform viewer;
    private Transform parentTransform;

    private float minAngleToBeVisible;

    private float[] LODAngles;

    private int previousLODIndex = -1;

    private bool active = false;

    private MeshCollider collider;
    private int colliderLODIndex;
    private float colliderMinAngle;

    // Start is called before the first frame update
    public CelestialBodyChunk(Transform parent, Transform viewer, Material material, Vector3[] cornersOnCube, float minAngleToBeVisible, float[] LODAngles, int colliderLODIndex, float colliderMinAngle, int chunkSize, bool[] cornersOnVertexOfFace) {
        GameObject meshObject = new GameObject("Celestial Body Chunk");
        renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.material = material;
        filter = meshObject.AddComponent<MeshFilter>();
        meshObject.transform.parent = parent;
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.layer = parent.gameObject.layer;

        this.colliderLODIndex = colliderLODIndex;
        this.colliderMinAngle = colliderMinAngle;
        collider = meshObject.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.enabled = false;

        this.viewer = viewer;
        parentTransform = parent;

        LODMeshes = new LODMesh[5];
        for (int i = 0; i < LODMeshes.Length; i++) {
            LODMeshes[i] = new LODMesh(i);
            LODMeshes[i].UpdateCallBack += UpdateChunk;
            LODMeshes[i].isCollider = i == colliderLODIndex;
        }

        this.chunkSize = chunkSize;
        this.cornersOnVertexOfFace = cornersOnVertexOfFace;
        this.minAngleToBeVisible = minAngleToBeVisible;
        this.LODAngles = LODAngles;

        centerOfChunkDir = Vector3.zero;
        for (int i = 0; i < cornersOnCube.Length; i++) {
            centerOfChunkDir += cornersOnCube[i].normalized;
        }
        centerOfChunkDir = centerOfChunkDir.normalized;

        int numCornersOnVertexOfFace = 0;
        for (int i = 0; i < 3; i++) {
            if (cornersOnVertexOfFace[i]) {
                numCornersOnVertexOfFace++;
            }
        }
        vertexMap = new Vector3[(chunkSize + 7) * (chunkSize + 8) / 2 - 3 - numCornersOnVertexOfFace];
    }

    public void UpdateChunk() {
        if (active) {
            float angleToViewer = AngleToChunk(viewer.position - parentTransform.position);
            bool visible = angleToViewer <= minAngleToBeVisible;
            SetVisible(visible);
            if (visible) {
                int lod = LODAngles.Length;
                for (int k = 0; k < LODAngles.Length; k++) {
                    if (angleToViewer < LODAngles[k]) {
                        lod = k;
                        break;
                    }
                }
                if (previousLODIndex != lod) {
                    UpdateMesh(lod);
                    if (lod < colliderLODIndex && !LODMeshes[colliderLODIndex].hasRequestedMesh) {
                        LODMeshes[colliderLODIndex].RequestMesh(chunkSize, vertexMap, UVMap, cornersOnVertexOfFace);
                    }
                }
                if(angleToViewer <= colliderMinAngle) {
                    if(collider.sharedMesh == null) {
                        if (LODMeshes[colliderLODIndex].hasMesh) {
                            collider.sharedMesh = LODMeshes[colliderLODIndex].mesh;
                        }
                    }
                    collider.enabled = true;
                } else {
                    collider.enabled = false;
                }
            }
        }
    }

    public float AngleToChunk(Vector3 positionRelativeToSphere) {
        return Vector3.Angle(centerOfChunkDir, positionRelativeToSphere);
    }

    public void EnableChunk() {
        active = true;
        SetVisible(true);
    }

    public void DisableChunk() {
        active = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible) {
        renderer.gameObject.SetActive(visible);
    }

    private void UpdateMesh(int lod) {
        if (LODMeshes[lod].hasMesh) {
            filter.mesh = LODMeshes[lod].mesh;
            previousLODIndex = lod;
        } else if (!LODMeshes[lod].hasRequestedMesh) {
            LODMeshes[lod].RequestMesh(chunkSize, vertexMap, UVMap, cornersOnVertexOfFace);
        }
    }

    private class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;
        public event System.Action UpdateCallBack;
        public bool isCollider;

        public LODMesh (int lod) {
            this.lod = lod;
        }

        private void OnMeshDataRecieved(object meshDataObject) {
            mesh = ((MeshData)meshDataObject).createMesh();
            hasMesh = true;
            UpdateCallBack();
        }

        public void RequestMesh(int chunkSize, Vector3[] verts, Vector4[,] uvs, bool[] cornersOnVertexOfFace) {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateMeshData(chunkSize, lod, verts, uvs, cornersOnVertexOfFace), OnMeshDataRecieved);
        }
    }
}
