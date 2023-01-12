using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Shader Data Generators/Planet Shader Data Generator")]
public class PlanetShaderDataGenerator : BaseShaderDataGenerator {
    public bool usePoleShading;
    public Color poleColor;
    [Range(0,1)]
    public float minPoleAlignment;
    [Range(0,1)]
    public float poleBlend;

    public LayerInfo[] layers;

    private ComputeBuffer layerBuffer;

    protected override void OnValidate() {
        base.OnValidate();
        for (int i = 0; i < layers.Length; i++) {
            if(i == 0) {
                layers[i].baseRadius = -1;
                layers[i].blendAmount = 0;
            } else {
                layers[i].baseRadius = Mathf.Max(layers[i].baseRadius, layers[i - 1].baseRadius);
            }
        }
    }

    public override bool UsesRadiiInfo() {
        return true;
    }

    public override void Setup(bool generateTangents, float[,] input) {
        GetGeneratorAndKernel("PlanetShaderDataGenerator", generateTangents);
        base.Setup(generateTangents, input);
    }

    protected override void SendDataToMaterial(float[,] input) {
        if(layerBuffer == null || !layerBuffer.IsValid() || layerBuffer.count != Mathf.Max(1, layers.Length)) {
            if (layerBuffer != null) {
                layerBuffer.Dispose();
            }
            layerBuffer = new ComputeBuffer(Mathf.Max(1, layers.Length), MatLayerInfo.Size());
        }
        MatLayerInfo[] layerInfos = layers.Select(x => new MatLayerInfo(x)).ToArray();
        if(layerInfos.Length == 0) {
            layerInfos = new MatLayerInfo[1];
        }
        layerBuffer.SetData(layerInfos);
        material.SetBuffer("layerInfo", layerBuffer);
        material.SetInt("numLayers", layers.Length);
        material.SetFloat("oceanRadius", input[0, 0]);
        if (usePoleShading) {
            material.SetVector("poleColor", new Vector4(poleColor.r, poleColor.g, poleColor.b, poleBlend));
            material.SetFloat("minPoleAlignment", minPoleAlignment);
        } else {
            material.SetVector("poleColor", new Vector4(poleColor.r, poleColor.g, poleColor.b, 0.001f));
            material.SetFloat("minPoleAlignment", 5);
        }
    }

    public override void DestroyMaterialBuffers() {
        if (layerBuffer != null) {
            layerBuffer.Dispose();
        }
    }

    [System.Serializable]
    public struct LayerInfo {
        [Range(-1,1)]
        public float baseRadius;
        [Range(0, 1)]
        public float blendAmount;
        public Color albedo;
        public bool useNormalColoration;
        [Range(0,1)]
        public float minAlignment;
        [Range(0,1)]
        public float normalBlendAmount;
        public Color normalAlbedo;
    }

    private struct MatLayerInfo {//layer info repackaged so that the shader can use it
        float baseRadius;
        float blendAmount;
        Vector3 albedo;
        int useNormalColoration;
        float minAlignment;
        float normalBlendAmount;
        Vector3 normalAlbedo;

        public MatLayerInfo(LayerInfo l) {
            baseRadius = l.baseRadius;
            blendAmount = l.blendAmount;
            albedo = new Vector3(l.albedo.r, l.albedo.g, l.albedo.b);
            useNormalColoration = l.useNormalColoration ? 1 : 0;
            minAlignment = l.minAlignment;
            normalBlendAmount = l.normalBlendAmount;
            normalAlbedo = new Vector3(l.normalAlbedo.r, l.normalAlbedo.g, l.normalAlbedo.b);
        }

        public static int Size() {
            return sizeof(int) + sizeof(float) * 10;
        }
    }
}