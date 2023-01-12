using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shader Data Generators/Worley Shader Data Generator")]
public class WorleyShaderDataGenerator : BaseShaderDataGenerator {
    public int seed;
    public bool reverse;
    public Vector2 hueMinMax;
    public Vector2 saturationMinMax;
    public Vector2 valueMinMax;

    private ComputeBuffer seedBuffer;
    private int numSeeds;

    public override int GetNumOutputFloats() {
        return 3;
    }

    protected override void OnValidate() {
        hueMinMax.x = Mathf.Clamp01(hueMinMax.x);
        saturationMinMax.x = Mathf.Clamp01(saturationMinMax.x);
        valueMinMax.x = Mathf.Clamp01(valueMinMax.x);
        hueMinMax.y = Mathf.Clamp(hueMinMax.y, hueMinMax.x, 1);
        saturationMinMax.y = Mathf.Clamp(saturationMinMax.y, saturationMinMax.x, 1);
        valueMinMax.y = Mathf.Clamp(valueMinMax.y, valueMinMax.x, 1);
        base.OnValidate();
    }

    public override void Setup(bool generateTangents, float[,] input) {
        GetGeneratorAndKernel("WorleyShaderDataGenerator", generateTangents);
        base.Setup(generateTangents, input);
    }

    protected override void UnpackValuesFromShapeGenerator(float[,] input) {
        Random.InitState(seed);
        SetupSeedBuffer(input);
        numSeeds = input.GetUpperBound(0);
    }

    private void SetupSeedBuffer(float[,] input) {
        SeedValue[] seeds = new SeedValue[input.GetUpperBound(0)];
        for (int i = 1; i <= input.GetUpperBound(0); i++) {
            float hue;
            if (reverse) {
                hue = Random.Range(0, hueMinMax.x + (1 - hueMinMax.y));
                if(hue > hueMinMax.x) {
                    hue -= hueMinMax.x;
                    hue += hueMinMax.y;
                }
            } else {
                hue = Random.Range(hueMinMax.x, hueMinMax.y);
            }
            seeds[i - 1] = new SeedValue(new Vector3(input[i, 0], input[i, 1], input[i, 2]), hue, Random.Range(saturationMinMax.x, saturationMinMax.y), Random.Range(valueMinMax.x, valueMinMax.y));
        }

        if (seedBuffer == null || !seedBuffer.IsValid() || seedBuffer.count != Mathf.Max(1, seeds.Length)) {
            if (seedBuffer != null) {
                seedBuffer.Dispose();
            }
            seedBuffer = new ComputeBuffer(Mathf.Max(1, seeds.Length), SeedValue.Size());
        }

        if (seeds.Length > 0) {
            seedBuffer.SetData(seeds);
        } else {
            SeedValue[] temp = new SeedValue[1];
            temp[0] = new SeedValue(Vector3.zero, 0, 0, 0);
            seedBuffer.SetData(temp);
        }
    }

    protected override void SetStaticShaderValues(float[,] input) {
        generator.SetBuffer(kernel, "seedBuffer", seedBuffer);
        generator.SetInt("numSeeds", numSeeds);
    }

    protected override Mesh SetMeshValues(Mesh input, float[] shaderOutput) {
        List<Vector3> cols = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        for (int i = 0; i < shaderOutput.Length; i += GetNumOutputFloats() + 4) {
            cols.Add(new Vector3(shaderOutput[i], shaderOutput[i + 1], shaderOutput[i + 2]));
            tangents.Add(new Vector4(shaderOutput[i + 3], shaderOutput[i + 4], shaderOutput[i + 5], shaderOutput[i + 6]));
        }
        input.SetUVs(0, cols);
        input.SetTangents(tangents);

        return input;
    }

    protected override void DestroyGeneratorBuffers() {
        base.DestroyGeneratorBuffers();
        if(seedBuffer != null) {
            seedBuffer.Dispose();
        }
    }

    private struct SeedValue {
        Vector3 seed;
        Vector3 color;

        public SeedValue(Vector3 center, float hue, float saturation, float value) {
            seed = center;
            Color c = Color.HSVToRGB(hue, saturation, value);
            color = new Vector3(c.r, c.g, c.b);
        }

        public static int Size() {
            return sizeof(float) * 6;
        }
    }
}
