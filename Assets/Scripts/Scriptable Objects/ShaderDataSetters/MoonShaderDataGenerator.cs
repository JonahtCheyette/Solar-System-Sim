using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shader Data Generators/Moon Shader Data Generator")]
public class MoonShaderDataGenerator : BaseShaderDataGenerator {
    public int seed = 1;

    [Range(1, 8)]
    public int octaves = 8;
    [Min(float.Epsilon)]
    public float scale = 1;
    [Range(0, 1)]
    public float persistance = 0.5f;
    [Min(1)]
    public float lacunarity = 2;
    [Range(0,10)]
    public int numEjectaCraters = 3;
    [Range(1, 5)]
    public float ejectaRadiusThreshold = 2;

    public float[] warpScales = new float[1];

    private ComputeBuffer offsetBuffer;
    private ComputeBuffer warpBuffer;
    private ComputeBuffer ejectaBuffer;

    public override int GetNumOutputFloats() {
        return 1;
    }

    public override void Setup(bool generateTangents, float[,] input) {
        GetGeneratorAndKernel("MoonShaderDataGenerator", generateTangents);
        Random.InitState(seed);
        base.Setup(generateTangents, input);
        Random.InitState(seed);
        SetStaticShaderVariables();
    }

    protected override void UnpackValuesFromShapeGenerator(float[,] input) {
        SetEjectaBuffer(input);
    }

    private void SetEjectaBuffer(float[,] input) {
        List<EjectaCrater> selectedCraters = new List<EjectaCrater>();
        if (input.GetUpperBound(0) != 0) {
            for (int craterIndex = 0; craterIndex < numEjectaCraters; craterIndex++) {
                for (int i = 0; i < 20; i++) {//20 is arbitrary
                    int index = Mathf.FloorToInt(Random.Range(1, input.GetUpperBound(0) + 0.99999f));
                    Vector3 testCenter = new Vector3(input[index, 0], input[index, 1], input[index, 2]);
                    bool tooClose = false;
                    for (int j = 0; j < selectedCraters.Count; j++) {
                        if (Vector3.Angle(selectedCraters[j].center, testCenter) * Mathf.Deg2Rad < (selectedCraters[j].radius + input[index, 3]) * ejectaRadiusThreshold) {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) {
                        Vector3 localUp = Vector3.Cross(Random.onUnitSphere, testCenter).normalized;
                        while (localUp == Vector3.zero) {
                            localUp = Vector3.Cross(Random.onUnitSphere, testCenter).normalized;
                        }
                        selectedCraters.Add(new EjectaCrater(input[index, 3], testCenter, localUp));
                        break;
                    }
                }
            }
        }
        if (selectedCraters.Count == 0) {
            selectedCraters.Add(new EjectaCrater(0, Vector3.zero, Vector3.zero));
        }
        if(ejectaBuffer == null || !ejectaBuffer.IsValid() || ejectaBuffer.count != selectedCraters.Count) {
            if(ejectaBuffer != null) {
                ejectaBuffer.Dispose();
            }
            ejectaBuffer = new ComputeBuffer(selectedCraters.Count, sizeof(float) * 10);
        }
        ejectaBuffer.SetData(selectedCraters.ToArray());
    }

    protected override void SendDataToMaterial(float[,] input) {
        material.SetFloat("ejectaThreshold", ejectaRadiusThreshold);
        material.SetFloat("bodyRadius", input[0,0]);
        material.SetBuffer("ejectaCraters", ejectaBuffer);
        material.SetInt("numEjectaCraters", ejectaBuffer.count);
    }

    private void SetStaticShaderVariables() {
        GenerateOffsets();
        SetWarpSettings();
        generator.SetBuffer(kernel, "offsets", offsetBuffer);
        generator.SetBuffer(kernel, "warpSettings", warpBuffer);
        generator.SetInt("numWarps", warpBuffer.count);
        generator.SetFloats("noiseSettings", new float[3] { scale, persistance, lacunarity });
    }

    private void GenerateOffsets() {
        offsetBuffer = new ComputeBuffer(octaves, sizeof(float) * 3);
        Vector3[] offsets = new Vector3[octaves];
        for (int i = 0; i < octaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        offsetBuffer.SetData(offsets);
    }

    private void SetWarpSettings() {
        NoiseWarpSettings[] settings = new NoiseWarpSettings[Mathf.Max(1, warpScales.Length)];
        warpBuffer = new ComputeBuffer(settings.Length, sizeof(float) * 10);
        for (int i = 0; i < warpScales.Length; i++) {
            settings[i].scale = warpScales[i];
            settings[i].offsetOne = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
            settings[i].offsetTwo = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
            settings[i].offsetThree = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
        }
        if (warpScales.Length == 0) {
            settings[0].scale = 0;
            settings[0].offsetOne = Vector3.zero;
            settings[0].offsetTwo = Vector3.zero;
            settings[0].offsetThree = Vector3.zero;
        }
        warpBuffer.SetData(settings);
    }

    public override void DestroyMaterialBuffers() {
        if (ejectaBuffer != null) {
            ejectaBuffer.Dispose();
        }
    }

    protected override Mesh SetMeshValues(Mesh input, float[] shaderOutput) {
        List<Vector2> UVs = new List<Vector2>();
        List<Vector4> tangents = new List<Vector4>();
        for (int i = 0; i < shaderOutput.Length; i += GetNumOutputFloats() + 4) {
            UVs.Add(Vector2.right * shaderOutput[i]);
            tangents.Add(new Vector4(shaderOutput[i + 1], shaderOutput[i + 2], shaderOutput[i + 3], shaderOutput[i + 4]));
        }
        input.SetUVs(0, UVs);
        input.SetTangents(tangents);

        return input;
    }
    
    protected override void DestroyGeneratorBuffers() {
        base.DestroyGeneratorBuffers();
        if (offsetBuffer != null) {
            offsetBuffer.Dispose();
        }
        if (warpBuffer != null) {
            warpBuffer.Dispose();
        }
    }

    private struct NoiseWarpSettings {
        public float scale;
        public Vector3 offsetOne;
        public Vector3 offsetTwo;
        public Vector3 offsetThree;
    }
    
    private struct EjectaCrater {
        public float radius;
        public Vector3 center;
        public Vector3 localUp;
        public Vector3 localRight;
        public EjectaCrater(float r, Vector3 c, Vector3 l) {
            radius = r;
            center = c;
            localUp = l;
            localRight = Vector3.Cross(c, l);
        }
    }
}
