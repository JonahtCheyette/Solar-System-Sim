using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Celestial Body Generators/Warped Planet Generator")]
public class WarpedGenerator : CelestialBodyGenerator {
    
    public int seed;

    public Color oceanShallowColor;
    public Color oceanDeepColor;
    [Min(0)]
    public float oceanBlendMultiplier;
    [Min(0)]
    public float oceanAlphaMultiplier;
    
    [Min(0)]
    public float oceanFloorDepth;
    [Range(0, 1)]
    public float oceanDamper;
    [Range(0, 1)]
    public float oceanSmoothing;
    [Min(0)]
    public float oceanDepthMultiplier;
    
    public float genShapeMultiplier;
    [Range(1, 8)]
    public int genShapeOctaves = 8;
    [Min(float.Epsilon)]
    public float genShapeScale = 350f;
    [Range(0, 1)]
    public float genShapePersistance = 0.5f;
    [Min(1)]
    public float genShapeLacunarity = 2f;
    private ComputeBuffer genShapeOctaveOffsets;

    [Range(1, 8)]
    public int warpNoiseOctaves = 8;
    [Min(float.Epsilon)]
    public float warpNoiseScale = 1;
    [Range(0, 1)]
    public float warpNoisePersistance = 0.5f;
    [Min(1)]
    public float warpNoiseLacunarity = 2;
    public float[] warpStrengths = new float[1];
    private ComputeBuffer warpOctaveOffsets;
    private ComputeBuffer warpBuffer;

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
    
    public Vector3 mountainMaskOffset;
    public float mountainMaskShift;
    [Min(float.Epsilon)]
    public float mountainMaskScale = 2f;
    
    private ComputeBuffer mountainMaskOffsetBuffer;
    
    public override bool HasOceanEffect() {
        return true;
    }
    
    public override OceanDetails OceanDetails() {
        return new OceanDetails(radius, oceanBlendMultiplier, oceanAlphaMultiplier, ColToVec(oceanShallowColor), ColToVec(oceanDeepColor));
    }
    
    protected override void OnValidate() {
        if (ridgeSharpness == 0) { ridgeSharpness = 0.001f; }
        base.OnValidate();
    }

    public override void Setup() {
        GetGenerator("WarpedRadiusGenerator");
        base.Setup();
        SetupNoiseBuffers();
        SetupWarpBuffer();

        generator.SetBuffer(kernel, "genShapeOctaveOffsets", genShapeOctaveOffsets);
        generator.SetFloats("genShapeNoiseSettings", new float[3] { genShapeScale, genShapePersistance, genShapeLacunarity });
        generator.SetFloat("genShapeMultiplier", genShapeMultiplier);

        generator.SetBuffer(kernel, "warpSettings", warpBuffer);
        generator.SetBuffer(kernel, "warpOctaveOffsets", warpOctaveOffsets);
        generator.SetFloats("warpNoiseSettings", new float[3] { warpNoiseScale, warpNoisePersistance, warpNoiseLacunarity });
        generator.SetInt("numWarps", warpBuffer.count);

        generator.SetBuffer(kernel, "ridgeOctaveOffsets", ridgeOctaveOffsets);
        generator.SetFloats("ridgeNoiseSettings", new float[3] { ridgeScale, ridgePersistance, ridgeLacunarity });
        generator.SetFloat("ridgeMultiplier", ridgeMultiplier);
        generator.SetFloat("ridgeGain", ridgeGain);
        generator.SetFloat("ridgeSharpness", ridgeSharpness);
    
        generator.SetFloat("oceanFloorDepth", oceanFloorDepth);
        generator.SetFloat("oceanDamper", oceanDamper);
        generator.SetFloat("oceanSmoothing", oceanSmoothing);
        generator.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
    
        generator.SetBuffer(kernel, "mountainMaskOffset", mountainMaskOffsetBuffer);
        generator.SetFloats("mountainMaskSettings", new float[3] { mountainMaskScale, 1, 1 });
        generator.SetFloat("mountainMaskShift", mountainMaskShift);
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
    
        if (mountainMaskOffsetBuffer == null || !mountainMaskOffsetBuffer.IsValid() || mountainMaskOffsetBuffer.count != 1) {
            if (mountainMaskOffsetBuffer != null) {
                mountainMaskOffsetBuffer.Dispose();
            }
            mountainMaskOffsetBuffer = new ComputeBuffer(1, sizeof(float) * 3);
        }
        offsets = new Vector3[1];
        offsets[0] = mountainMaskOffset;
        mountainMaskOffsetBuffer.SetData(offsets);

        if (warpOctaveOffsets == null || !warpOctaveOffsets.IsValid() || warpOctaveOffsets.count != genShapeOctaves) {
            if (warpOctaveOffsets != null) {
                warpOctaveOffsets.Dispose();
            }
            warpOctaveOffsets = new ComputeBuffer(warpNoiseOctaves, sizeof(float) * 3);
        }
        offsets = new Vector3[warpNoiseOctaves];
        for (int i = 0; i < warpNoiseOctaves; i++) {
            offsets[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10000f;
        }
        warpOctaveOffsets.SetData(offsets);
    }

    private void SetupWarpBuffer() {
        NoiseWarpSettings[] settings = new NoiseWarpSettings[Mathf.Max(1, warpStrengths.Length)];
        warpBuffer = new ComputeBuffer(settings.Length, NoiseWarpSettings.Size());
        for (int i = 0; i < warpStrengths.Length; i++) {
            settings[i].scale = warpStrengths[i];
            settings[i].offsetOne = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
            settings[i].offsetTwo = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
            settings[i].offsetThree = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f;
        }
        if (warpStrengths.Length == 0) {
            settings[0].scale = 0;
            settings[0].offsetOne = Vector3.zero;
            settings[0].offsetTwo = Vector3.zero;
            settings[0].offsetThree = Vector3.zero;
        }
        warpBuffer.SetData(settings);
    }

    public override void Finish() {
        base.Finish();
        DestroyBuffers();
    }
    
    private void DestroyBuffers() {
        if (genShapeOctaveOffsets != null) {
            genShapeOctaveOffsets.Dispose();
        }
        if (ridgeOctaveOffsets != null) {
            ridgeOctaveOffsets.Dispose();
        }
        if (mountainMaskOffsetBuffer != null) {
            mountainMaskOffsetBuffer.Dispose();
        }
        if (warpBuffer != null) {
            warpBuffer.Dispose();
        }
        if (warpOctaveOffsets != null) {
            warpOctaveOffsets.Dispose();
        }
    }
}

public struct NoiseWarpSettings {
    public float scale;
    public Vector3 offsetOne;
    public Vector3 offsetTwo;
    public Vector3 offsetThree;

    public static int Size() {
        return sizeof(float) * 10;
    }
}