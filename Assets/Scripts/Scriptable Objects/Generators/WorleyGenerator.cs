using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Celestial Body Generators/Worley Generator")]
public class WorleyGenerator : CelestialBodyGenerator {
    private static float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

    public int seed;

    public bool useOcean;
    [Min(0)]
    public float oceanRadiusBoost;
    public Color oceanShallowColor;
    public Color oceanDeepColor;
    [Min(0)]
    public float oceanBlendMultiplier;
    [Min(0)]
    public float oceanAlphaMultiplier;

    [Min(0)]
    public int numWorleyPoints;
    [Range(0, 1)]
    public float amountPerturbed;
    [Range(0, Mathf.PI)]
    public float worleyThreshold;
    public Vector2 worleyBoostMinMax;
    public float worleyMultiplier;

    private ComputeBuffer worleyBuffer;
    private float maxWorleyBoostGenerated;

    public override bool HasOceanEffect() {
        return useOcean;
    }

    public override OceanDetails OceanDetails() {
        return new OceanDetails(radius - worleyBoostMinMax.y + oceanRadiusBoost, oceanBlendMultiplier, oceanAlphaMultiplier, ColToVec(oceanShallowColor), ColToVec(oceanDeepColor));
    }

    protected override void OnValidate() {
        worleyBoostMinMax.x = Mathf.Max(0, worleyBoostMinMax.x);
        worleyBoostMinMax.y = Mathf.Max(worleyBoostMinMax.y, worleyBoostMinMax.x);
        base.OnValidate();
    }

    public override void Setup() {
        GetGenerator("WorleyRadiusGenerator");
        base.Setup();

        SetupWorleyBuffer();

        generator.SetBuffer(kernel, "worleyBuffer", worleyBuffer);
        generator.SetFloat("worleyThreshold", worleyThreshold);
        generator.SetFloat("maxWorleyBoost", maxWorleyBoostGenerated);
        generator.SetFloat("worleyMultiplier", worleyMultiplier);
        generator.SetInt("numWorleyPoints", numWorleyPoints);
    }

    private void SetupWorleyBuffer() {
        Random.InitState(seed);

        if (worleyBuffer == null || !worleyBuffer.IsValid() || worleyBuffer.count != Mathf.Max(1, numWorleyPoints)) {
            if (worleyBuffer != null) {
                worleyBuffer.Dispose();
            }
            worleyBuffer = new ComputeBuffer(Mathf.Max(1, numWorleyPoints), WorleyPlate.Size());
        }
        WorleyPlate[] plates = new WorleyPlate[Mathf.Max(1, numWorleyPoints)];
        maxWorleyBoostGenerated = float.MinValue;

        float phi = 2 * Mathf.PI * goldenRatio;

        for (int i = 0; i < numWorleyPoints; i++) {
            float y = 2 * i / (numWorleyPoints - 1f) - 1; //goes from -1 to 1 (south pole to north pole)
            float scaledRadius = Mathf.Sqrt(1 - y * y);
            float angle = phi * i;

            float x = Mathf.Cos(angle) * scaledRadius;
            float z = Mathf.Sin(angle) * scaledRadius;
            Vector3 seed = (new Vector3(x, y, z) + Random.onUnitSphere * amountPerturbed).normalized;

            float boost = Random.Range(worleyBoostMinMax.x, worleyBoostMinMax.y);
            maxWorleyBoostGenerated = Mathf.Max(maxWorleyBoostGenerated, boost);

            plates[i] = new WorleyPlate(seed, boost);
        }

        if(numWorleyPoints == 0) {
            plates[0] = new WorleyPlate(Vector3.zero, 0);
        }

        worleyBuffer.SetData(plates);
    }

    public override void Finish() {
        base.Finish();
        DestroyWorleyBuffer();
    }

    private void DestroyWorleyBuffer() {
        if (worleyBuffer != null) {
            worleyBuffer.Dispose();
        }
    }

    private struct WorleyPlate {
        private float boost;
        private Vector3 seed;

        public WorleyPlate (Vector3 s, float b) {
            boost = b;
            seed = s;
        }

        public static int Size() {
            return sizeof(float) * 4;
        }
    }
}
