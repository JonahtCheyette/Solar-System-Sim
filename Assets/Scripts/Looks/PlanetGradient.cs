using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ImageEffectAllowedInSceneView] [ExecuteAlways]
public class PlanetGradient : MonoBehaviour {
    private CelestialBodyPhysics[] bodies;
    
    private List<PlanetPos> planetList;
    private List<Vector3> planetColors; //will be sent to the shader, the int named planetIndex/index in the various structs we have is simply the index of their color in this list

    private ComputeBuffer planetBuffer; // buffers for the various data we send to the shader
    private ComputeBuffer colBuffer;

    private Material mat;
    private Shader shader;
    private Camera cam;

    // current aspect ratio of the screen. needed because bottom left is always (0, 0) and topright is always (1, 1), no matter what the actual screen dimensions are
    private float aspectRatio;

    [Min(0)]
    public float haloRadiusMultiplier = 2;


    private void Start() {
        bodies = FindObjectsOfType<CelestialBodyPhysics>();

        planetList = new List<PlanetPos>();

        planetColors = new List<Vector3>();
        foreach (CelestialBodyPhysics body in bodies) { // getting the planet color data
            planetColors.Add(new Vector3(body.color.r, body.color.g, body.color.b));
        }

        shader = Shader.Find("Hidden/PlanetGradient");
        cam = Camera.current;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (Camera.current != null && Application.isPlaying && Camera.current != Camera.main) {
            // makes it so that the image effect only runs in scene view during play mode
            GetAspectRatioAndCamera(source);
            ResetList();
            UpdateCelstialBodyData();
            SetUpBuffer();
            SetBufferData();
            Render(source, destination);
        } else {
            Graphics.Blit(source, destination);
        }
    }

    private void GetAspectRatioAndCamera(RenderTexture source) {
        aspectRatio = (source.width) / ((float)source.height);
        cam = Camera.current;
    }

    private void ResetList() {
        planetList.Clear();
    }

    private void UpdateCelstialBodyData() {
        for (int i = 0; i < bodies.Length; i++) {
            UpdatePlanetList(i);
        }
    }

    private void UpdatePlanetList(int bodyIndex) {
        //getting the position and radius of the planet in viewportSpace
        Vector3 viewportPosition = cam.WorldToViewportPoint(bodies[bodyIndex].Position);
        float radius = cam.WorldToViewportPoint(bodies[bodyIndex].Position + cam.transform.up * bodies[bodyIndex].Radius()).y - viewportPosition.y;

        PlanetPos currentPlanet = new PlanetPos(viewportPosition, bodyIndex, radius);
        if (currentPlanet.OnScreen(aspectRatio, haloRadiusMultiplier)) {
            if (planetList.Count == 0) {
                planetList.Add(currentPlanet);
            } else {
                //ordering the planets from furthest away to closest;
                for (int i = 0; i < planetList.Count; i++) {
                    if (viewportPosition.z < planetList[i].Depth) {
                        planetList.Insert(i, currentPlanet);
                        break;
                    }
                    if (i == planetList.Count - 1) {
                        planetList.Add(currentPlanet);
                        break; // important! if this isn't here, this will cause a memory leak
                    }
                }
            }
        }
    }

    private void SetUpBuffer() {
        if (planetBuffer == null) {
            planetBuffer = new ComputeBuffer(Mathf.Max(planetList.Count, 1), PlanetData.GetSize());
        } else if (planetBuffer.count != planetList.Count) {
            planetBuffer.Release();
            planetBuffer = new ComputeBuffer(Mathf.Max(planetList.Count, 1), PlanetData.GetSize());
        }
    }

    private void SetBufferData() {
        planetBuffer.SetData(planetList.Select(x => new PlanetData(x)).ToArray());
    }

    private void Render(RenderTexture src, RenderTexture dest) {
        if (planetList.Count == 0 && shader != null) {
            Graphics.Blit(src, dest); // don't do any fancy effects, because you don't need to.
            return;
        }
        if (mat == null || mat.shader == null || mat.shader != shader) {
            mat = new Material(shader);
            SetStaticMaterialValues();
        }

        SetDynamicMaterialValues();
        Graphics.Blit(src, dest, mat);
    }

    private void SetStaticMaterialValues() {
        // I considered seting this up in the same way as all the other buffers,
        // but considering that the planetColors list literally never changes
        // I think it's fine doing it this way, considering that the fact that this function 
        // should only be called once, meaning that we aren't wasting time checking that this buffer exists
        // and re - setting the data in it every frame when the data in it isn't changing anyways
        if (colBuffer == null) {
            colBuffer = new ComputeBuffer(planetColors.Count, sizeof(float) * 3);
            colBuffer.SetData(planetColors.ToArray());
        }
        mat.SetBuffer("colors", colBuffer);
    }

    private void SetDynamicMaterialValues() {
        mat.SetBuffer("planets", planetBuffer);
        mat.SetInt("numPlanets", planetList.Count);
        mat.SetFloat("planetHaloSize", haloRadiusMultiplier);
    }

    private void OnDisable() {
        DestroyBuffers();
    }

    private void OnApplicationQuit() {
        DestroyBuffers();
    }

    private void OnDestroy() {
        DestroyBuffers();
    }

    private void DestroyBuffers() {
        if (planetBuffer != null) {
            planetBuffer.Release();
        }
        if (colBuffer != null) {
            colBuffer.Release();
        }
    }

    private struct PlanetPos {
        private Vector2 viewportPos;
        private float depth;
        private int index;
        private float radius;

        public PlanetPos(Vector3 pos, int i, float r) {
            viewportPos = new Vector2(pos.x, pos.y);
            depth = pos.z;
            index = i;
            radius = r;
        }

        public float Depth {
            get {
                return depth;
            }
        }

        public Vector2 ViewportPos {
            get {
                return viewportPos;
            }
        }

        public int Index {
            get {
                return index;
            }
        }

        public float Radius {
            get {
                return radius;
            }
        }

        public bool OnScreen(float aRatio, float radiusMultiplier) {
            return viewportPos.x * aRatio >= -radius * radiusMultiplier && viewportPos.x <= 1 + (radius * radiusMultiplier / aRatio) && viewportPos.y >= -radius * radiusMultiplier && viewportPos.y <= 1 + (radius * radiusMultiplier);
        }
    }

    private struct PlanetData {
        private Vector2 viewportPos;
        private int index;
        private float radius;

        public PlanetData(PlanetPos planet) {
            viewportPos = planet.ViewportPos;
            index = planet.Index;
            radius = planet.Radius;
        }

        public static int GetSize() {
            return sizeof(int) + 3 * sizeof(float);
        }
    }
}