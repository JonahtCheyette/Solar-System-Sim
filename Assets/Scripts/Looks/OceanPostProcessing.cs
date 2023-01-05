using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

[ExecuteAlways, ImageEffectAllowedInSceneView, RequireComponent(typeof(Camera))]
public class OceanPostProcessing : MonoBehaviour {
    public Transform sun;
    [Range(0,1)]
    public float ambientLight;
    public Texture waveNormalMapA;
    [Min(0.000001f)]
    public float normalMapScaleA;
    public Texture waveNormalMapB;
    [Min(0.000001f)]
    public float normalMapScaleB;
    [Range(0,200)]
    public float smoothness;
    [Range(0, 1)]
    public float waveStrength;
    public float waveSpeed;

	private Material mat;
    List<CelestialBodyMeshHandler> bodiesWithOceans;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer oceanDetailBuffer;
    private Camera cam;

    private void Awake() {
        Init();
    }

    private void Start() {
        Init();
    }

    private void OnEnable() {
        Init();
    }

    private void OnValidate() {
        Init();
    }

    private void Init(){
        FillBodies();
        if (bodiesWithOceans.Count > 0) {
            InitCam();
            InitMaterial();
            InitializeOceanBuffer();
            InitializePositionBuffer();
            UpdateBuffers();
            SetMaterialProperties();
        }
    }

    private void FillBodies() {
        CelestialBodyMeshHandler[] potentialBodies = FindObjectsOfType<CelestialBodyMeshHandler>();
        bodiesWithOceans = new List<CelestialBodyMeshHandler>();
        for (int i = 0; i < potentialBodies.Length; i++) {
            if (potentialBodies[i].HasOceanEffect()) {
                bodiesWithOceans.Add(potentialBodies[i]);
            }
        }
    }

    private void InitCam() {
        cam = gameObject.GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    private void InitMaterial() {
        Shader materialShader = (Shader)Resources.Load("Shaders/Post Processing/OceanShader");
        if (mat == null || mat.shader != materialShader) {
            mat = new Material(materialShader);
        }
    }

    private void InitializeOceanBuffer() {
        if (oceanDetailBuffer == null || !oceanDetailBuffer.IsValid() || oceanDetailBuffer.count != bodiesWithOceans.Count) {
            if (oceanDetailBuffer != null) {
                oceanDetailBuffer.Dispose();
            }
            oceanDetailBuffer = new ComputeBuffer(bodiesWithOceans.Count, OceanDetails.Size());
        }
    }

    private void InitializePositionBuffer() {
        if (positionBuffer == null || !positionBuffer.IsValid() || positionBuffer.count != bodiesWithOceans.Count + 1) {
            if (positionBuffer != null) {
                positionBuffer.Dispose();
            }
            positionBuffer = new ComputeBuffer(bodiesWithOceans.Count + 1, sizeof(float) * 3);
        }
    }

    private void UpdateBuffers() {
        OceanDetails[] deets = new OceanDetails[bodiesWithOceans.Count];
        Vector3[] positions = new Vector3[bodiesWithOceans.Count + 1];

        float[] sqrDists = new float[bodiesWithOceans.Count];
        for (int i = 0; i < bodiesWithOceans.Count; i++) {
            //this code sorts the data so that the data is in order of whichever planet is farthest -> last
            float sqrDistToCam = (cam.transform.position - bodiesWithOceans[i].gameObject.transform.position).sqrMagnitude;
            int spot = -1;
            for (int j = 0; j < i; j++) {
                if(sqrDistToCam > sqrDists[j]) {
                    spot = j;
                    break;
                }
            }
            CelestialBodyGenerator generator = bodiesWithOceans[i].celestialBodyGenerator;
            if (spot == -1) {
                //if we didn't find a spot, then put it at the end of the list
                positions[i] = bodiesWithOceans[i].gameObject.transform.position;
                deets[i] = generator.OceanDetails();
            } else {
                for (int j = i; j > spot; j--) {
                    positions[j] = positions[j - 1];
                    deets[j] = deets[j - 1];
                }

                positions[spot] = bodiesWithOceans[i].gameObject.transform.position;
                deets[spot] = generator.OceanDetails();
            }
        }
        if (sun != null) {
            positions[positions.Length - 1] = sun.position;
        } else {
            positions[positions.Length - 1] = Vector3.zero;
        }

        positionBuffer.SetData(positions);
        oceanDetailBuffer.SetData(deets);
    }

    private void SetMaterialProperties() {
        mat.SetBuffer("oceans", oceanDetailBuffer);
        mat.SetBuffer("positions", positionBuffer);
        mat.SetInt("numOceans", oceanDetailBuffer.count);
        mat.SetFloat("ambientLight", ambientLight);
        mat.SetTexture("waveNormalMapA", waveNormalMapA);
        mat.SetTexture("waveNormalMapB", waveNormalMapB);
        mat.SetFloat("normalMapScaleA", normalMapScaleA);
        mat.SetFloat("normalMapScaleB", normalMapScaleB);
        mat.SetFloat("smoothness", smoothness);
        mat.SetFloat("waveStrength", waveStrength);
        mat.SetFloat("waveSpeed", waveSpeed);
    }

    private void OnDestroy() {
        DestroyBuffers();
    }

    private void OnApplicationQuit() {
        DestroyBuffers();
    }

    private void OnDisable() {
        DestroyBuffers();
    }

    private void DestroyBuffers() {
        if (positionBuffer != null) {
            positionBuffer.Dispose();
        }
        if (oceanDetailBuffer != null) {
            oceanDetailBuffer.Dispose();
        }
    }

    [ImageEffectOpaque]
	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if(bodiesWithOceans.Count == 0 || bodiesWithOceans[0] == null) {
            Init();
        }
        if (bodiesWithOceans.Count > 0) {
            UpdateBuffers();
            if (mat != null) {
                Graphics.Blit(src, dest, mat);
            } else {
                Graphics.Blit(src, dest);
            }
        } else {
            Graphics.Blit(src, dest);
        }
	}
}

public struct OceanDetails {
    float size;
    float blendMultiplier;
    float alphaMultiplier;
    Vector3 shallowColor;
    Vector3 deepColor;

    public OceanDetails(float s, float b, float a, Vector3 sc, Vector3 dc) {
        size = s;
        blendMultiplier = b;
        alphaMultiplier = a;
        shallowColor = sc;
        deepColor = dc;
    }

    public static int Size() {
        return sizeof(float) * 9;
    }
}
