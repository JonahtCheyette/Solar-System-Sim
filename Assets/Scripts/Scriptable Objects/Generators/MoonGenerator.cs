using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Celestial Body Generators/Moon Generator")]
public class MoonGenerator : CelestialBodyGenerator {
    public int seed;
    [Min(1)]
    public int numCraters = 100;
    public float craterMultiplier;
    public float craterSmoothing;
    public Vector2 craterRadMinMax = new Vector2(0, 0.25f);
    public Vector2 rimSteepnessMinMax = new Vector2(5, 15);
    public Vector2 rimWidthMinMax = new Vector2(0.23f, 0.4f);

    public float genShapeMultiplier;
    [Range(1, 8)]
    public int genShapeOctaves = 8;
    [Min(float.Epsilon)]
    public float genShapeScale = 350f;
    [Range(0,1)]
    public float genShapePersistance = 0.5f;
    [Min(1)]
    public float genShapeLacunarity = 2f;
    private ComputeBuffer genShapeOctaveOffsets;

    public float detailMultiplier;
    [Range(1, 8)]
    public int detailOctaves = 8;
    [Min(float.Epsilon)]
    public float detailScale = 2f;
    [Range(0, 1)]
    public float detailPersistance = 0.5f;
    [Min(1)]
    public float detailLacunarity = 2f;
    private ComputeBuffer detailOctaveOffsets;

    public float ridgeMultiplier;
    [Range(1, 8)]
    public int ridgeOctaves = 8;
    [Min(float.Epsilon)]
    public float ridgeScale = 2f;
    [Range(0, 1)]
    public float ridgePersistance = 0.5f;
    [Min(1)]
    public float ridgeLacunarity = 2f;
    [Min(float.Epsilon)]
    public float ridgeGain = 1f;
    public float ridgeSharpness = 1f;
    private ComputeBuffer ridgeOctaveOffsets;

    private Crater[] craters;
    private ComputeBuffer craterBuffer;

    protected override void OnValidate() {
        craterRadMinMax.x = Mathf.Clamp01(craterRadMinMax.x);
        craterRadMinMax.y = Mathf.Clamp(craterRadMinMax.y, craterRadMinMax.x, 1f);
        rimSteepnessMinMax.x = Mathf.Max(0, rimSteepnessMinMax.x);
        rimSteepnessMinMax.y = Mathf.Max(rimSteepnessMinMax.y, rimSteepnessMinMax.x);
        rimWidthMinMax.x = Mathf.Clamp01(rimWidthMinMax.x);
        rimWidthMinMax.y = Mathf.Clamp(rimWidthMinMax.y, rimWidthMinMax.x, 1f);
        if(ridgeSharpness == 0) { ridgeSharpness = 0.001f; }
        base.OnValidate();
    }

    public override void Setup() {
        GetGenerator("MoonRadiusGenerator");
        base.Setup();
        SetupCraterBuffer();
        SetupNoiseBuffers();

        generator.SetBuffer(kernel, "craters", craterBuffer);
        generator.SetInt("numCraters", numCraters);
        generator.SetFloat("craterSmoothing", craterSmoothing);
        generator.SetFloat("craterMultiplier", craterMultiplier);

        generator.SetBuffer(kernel, "genShapeOctaveOffsets", genShapeOctaveOffsets);
        generator.SetFloats("genShapeNoiseSettings", new float[3] { genShapeScale, genShapePersistance, genShapeLacunarity });
        generator.SetFloat("genShapeMultiplier", genShapeMultiplier);

        generator.SetBuffer(kernel, "detailOctaveOffsets", detailOctaveOffsets);
        generator.SetFloats("detailNoiseSettings", new float[3] { detailScale, detailPersistance, detailLacunarity });
        generator.SetFloat("detailMultiplier", detailMultiplier);

        generator.SetBuffer(kernel, "ridgeOctaveOffsets", ridgeOctaveOffsets);
        generator.SetFloats("ridgeNoiseSettings", new float[3] { ridgeScale, ridgePersistance, ridgeLacunarity });
        generator.SetFloat("ridgeMultiplier", ridgeMultiplier);
        generator.SetFloat("ridgeGain", ridgeGain);
        generator.SetFloat("ridgeSharpness", ridgeSharpness);
    }

    private void SetupCraterBuffer() {
        if (craterBuffer == null || !craterBuffer.IsValid() || craterBuffer.count != numCraters) {
            if (craterBuffer != null) {
                craterBuffer.Dispose();
            }
            craterBuffer = new ComputeBuffer(numCraters, sizeof(float) * 7);
        }
        Random.InitState(seed);
        craters = new Crater[numCraters];
        for (int i = 0; i < numCraters; i++) {
            craters[i].radius = Mathf.Lerp(craterRadMinMax.x, craterRadMinMax.y, BiasFunction(Random.value, 0.6f));
            craters[i].rimSteepness = Random.Range(rimSteepnessMinMax.x, rimSteepnessMinMax.y);
            craters[i].floorHeight = Random.Range(-0.95f, -0.5f);
            craters[i].rimWidth = Random.Range(rimWidthMinMax.x, rimWidthMinMax.y);
            craters[i].center = Random.onUnitSphere;
        }
        craterBuffer.SetData(craters);
    }

    private float BiasFunction(float x, float bias) {
        float k = Mathf.Pow(1 - bias, 3);
        return x * k / (x * k - x + 1);
    }

    private void SetupNoiseBuffers() {
        Random.InitState(seed);

        if (genShapeOctaveOffsets == null || !genShapeOctaveOffsets.IsValid() || genShapeOctaveOffsets.count != genShapeOctaves) {
            if (genShapeOctaveOffsets != null) {
                genShapeOctaveOffsets.Dispose();
            }
            genShapeOctaveOffsets = new ComputeBuffer(genShapeOctaves, sizeof(float) * 3);
        }
        Vector3[] offsets = new Vector3[genShapeOctaves];
        for (int i = 0; i < genShapeOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        genShapeOctaveOffsets.SetData(offsets);

        if (detailOctaveOffsets == null || !detailOctaveOffsets.IsValid() || detailOctaveOffsets.count != detailOctaves) {
            if (detailOctaveOffsets != null) {
                detailOctaveOffsets.Dispose();
            }
            detailOctaveOffsets = new ComputeBuffer(detailOctaves, sizeof(float) * 3);
        }
        offsets = new Vector3[detailOctaves];
        for (int i = 0; i < detailOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        detailOctaveOffsets.SetData(offsets);

        if (ridgeOctaveOffsets == null || !ridgeOctaveOffsets.IsValid() || ridgeOctaveOffsets.count != ridgeOctaves) {
            if (ridgeOctaveOffsets != null) {
                ridgeOctaveOffsets.Dispose();
            }
            ridgeOctaveOffsets = new ComputeBuffer(ridgeOctaves, sizeof(float) * 3);
        }
        offsets = new Vector3[ridgeOctaves];
        for (int i = 0; i < ridgeOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        ridgeOctaveOffsets.SetData(offsets);
    }

    public override void Finish() {
        base.Finish();
        DestroyCraterBuffer();
        DestroyNoiseBuffers();
    }

    private void DestroyCraterBuffer() {
        if(craterBuffer != null) {
            craterBuffer.Dispose();
        }
    }

    private void DestroyNoiseBuffers() {
        if (genShapeOctaveOffsets != null) {
            genShapeOctaveOffsets.Dispose();
        }
        if (detailOctaveOffsets != null) {
            detailOctaveOffsets.Dispose();
        }
        if (ridgeOctaveOffsets != null) {
            ridgeOctaveOffsets.Dispose();
        }
    }

    public override float[,] ProvideDataToShader() {
        float[,] returnVal = new float[craters.Length + 1, 4];
        returnVal[0, 0] = radius;
        for (int i = 0; i < craters.Length; i++) {
            returnVal[i + 1, 0] = craters[i].center.x;
            returnVal[i + 1, 1] = craters[i].center.y;
            returnVal[i + 1, 2] = craters[i].center.z;
            if(craters[i].rimSteepness == 1) {
                returnVal[i + 1, 3] = craters[i].radius * (craters[i].rimWidth * craters[i].rimWidth + 2 * craters[i].rimWidth + 2) / (2 * (craters[i].rimWidth + 1));
            } else {
                returnVal[i + 1, 3] = craters[i].radius * ((craters[i].rimSteepness * (craters[i].rimWidth + 1)) - Mathf.Sqrt(craters[i].rimSteepness * craters[i].rimWidth * (craters[i].rimWidth + 2) + 1))/(craters[i].rimSteepness - 1);
            }
        }
        return returnVal;
    }

    private struct Crater {
        public float radius;
        public float rimSteepness;
        public float floorHeight;
        public float rimWidth;
        public Vector3 center;
    }
}
