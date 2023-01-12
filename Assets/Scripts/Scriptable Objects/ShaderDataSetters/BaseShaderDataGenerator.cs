using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseShaderDataGenerator : ScriptableObject {
    public bool autoUpdate;
    protected Material material;

    protected ComputeShader generator;
    private ComputeBuffer outputBuffer;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer normalBuffer;
    protected int kernel;

    [HideInInspector]
    public CelestialBodyMeshHandler meshHandler;

    
    //is called whenever the values are changed in the editor
    protected virtual void OnValidate() {
        //what these lines do is to tell unity to not compile these lines in a standalone build
        #if UNITY_EDITOR
        if (autoUpdate) {
            //the EditorApplication.update is every frame, and after recompile, so subscribing the method here and unsubscribing it when it gets called means the method gets called once, on the first frame after OnValidate is called or the recompile happens
            //we do it this way because the update for terrain on recompile needs to happen after the recompile of the shader, but shaders recompile after c# scripts.
            UnityEditor.EditorApplication.update += OnValuesChanged;
        }
        #endif
    }

    //call the action
    private void OnValuesChanged() {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= OnValuesChanged;
        if (!Application.isPlaying) {
            if (autoUpdate && meshHandler != null) {
                meshHandler.SetLowRezMeshValues();
            }
        }
        #endif
    }

    private void OnDestroy() {
        DestroyMaterialBuffers();
    }

    private void Awake() {
        DestroyMaterialBuffers();
    }

    public void SetMaterial(Material m) {
        material = m;
    }

    public virtual int GetNumOutputFloats() {
        return 0;
    }

    public virtual bool UsesRadiiInfo() {
        return false;
    }

    public virtual void Finish() {
        DestroyGeneratorBuffers();
    }

    protected virtual void DestroyGeneratorBuffers() {
        if (outputBuffer != null) {
            outputBuffer.Dispose();
        }
        if (positionBuffer != null) {
            positionBuffer.Dispose();
        }
        if (normalBuffer != null) {
            normalBuffer.Dispose();
        }
    }

    public virtual void DestroyMaterialBuffers() { }

    private void SetupOutputBuffer(int numVals, int numFloatsPerValue) {
        if (outputBuffer == null || !outputBuffer.IsValid() || outputBuffer.count != numVals) {
            if (outputBuffer != null) {
                outputBuffer.Dispose();
            }
            outputBuffer = new ComputeBuffer(numVals * numFloatsPerValue, sizeof(float));
        }
    }

    private void SetupBuffer(ref ComputeBuffer buffer, int numVals) {
        if (buffer == null || !buffer.IsValid() || buffer.count != numVals) {
            if (buffer != null) {
                buffer.Dispose();
            }
            buffer = new ComputeBuffer(numVals, sizeof(float) * 3);
        }
    }

    protected void GetGeneratorAndKernel(string generatorName, bool generateTangents) {
        generator = (ComputeShader)Resources.Load("Shaders/Compute Shaders/Shader Data Generators/" + generatorName);
        kernel = generator.FindKernel(generateTangents ? "GenerateWithTangents" : "GenerateWithoutTangents");
    }

    public virtual void Setup(bool generateTangents, float[,] input) {
        UnpackValuesFromShapeGenerator(input);
        SetStaticShaderValues(input);
        SendDataToMaterial(input);
    }

    protected virtual void UnpackValuesFromShapeGenerator(float[,] input) { }

    protected virtual void SetStaticShaderValues(float[,] input) { }

    protected virtual void SendDataToMaterial(float[,] input) { }

    public virtual void SetRadiiInfo(float maxRadius, float minRadius) {
        if (UsesRadiiInfo()) {
            material.SetFloat("maxRadius", maxRadius);
            material.SetFloat("minRadius", minRadius);
        }
    }

    private float[] RunShader(Mesh input) {
        int numFloatsPerVertex = GetNumOutputFloats() + 4;
        SetupPerVertexData(input, numFloatsPerVertex);

        generator.Dispatch(kernel, Mathf.CeilToInt(positionBuffer.count / 32f), 1, 1);
        return GetValuesFromShader();
    }

    private void SetupPerVertexData(Mesh m, int numFloatsPerValue) {
        SetupBuffers(m.vertices.Length, numFloatsPerValue, true);
        FillBuffers(m);
        SetDynamicShaderBuffers(true);
    }

    private void FillBuffers(Mesh m) {
        positionBuffer.SetData(m.vertices);
        normalBuffer.SetData(m.normals);
    }

    private float[] RunShader(Vector3[] input) {
        int numFloatsPerVertex = GetNumOutputFloats();
        SetupPerVertexData(input, numFloatsPerVertex);

        generator.Dispatch(kernel, Mathf.CeilToInt(positionBuffer.count / 32f), 1, 1);
        return GetValuesFromShader();
    }

    private void SetupPerVertexData(Vector3[] vertices, int numFloatsPerValue) {
        SetupBuffers(vertices.Length, numFloatsPerValue, false);
        FillBuffer(vertices);
        SetDynamicShaderBuffers(false);
    }

    private void FillBuffer(Vector3[] input) {
        positionBuffer.SetData(input);
    }

    private void SetupBuffers(int numValues, int numFloatsPerValue, bool useNormals) {
        SetupOutputBuffer(numValues, numFloatsPerValue);
        SetupBuffer(ref positionBuffer, numValues);
        if (useNormals) {
            SetupBuffer(ref normalBuffer, numValues);
        }
    }

    protected virtual void SetDynamicShaderBuffers(bool setNormals) {
        generator.SetBuffer(kernel, "positions", positionBuffer);
        if (setNormals) {
            generator.SetBuffer(kernel, "normals", normalBuffer);
        }
        generator.SetBuffer(kernel, "output", outputBuffer);
    }

    private float[] GetValuesFromShader() {
        float[] output = new float[outputBuffer.count];
        outputBuffer.GetData(output);
        return output;
    }

    protected virtual Mesh SetMeshValues(Mesh input, float[] shaderOutput) {
        List<Vector4> tangents = new List<Vector4>();
        for (int i = 0; i < shaderOutput.Length; i += GetNumOutputFloats() + 4) {
            tangents.Add(new Vector4(shaderOutput[i], shaderOutput[i + 1], shaderOutput[i + 2], shaderOutput[i + 3]));
        }
        input.SetTangents(tangents);

        return input;
    }

    public Mesh SetValues(Mesh input) {
        return SetMeshValues(input, RunShader(input));
    }

    public Vector4[,] GetValues(Vector3[] input) {
        if(GetNumOutputFloats() == 0) {
            return new Vector4[0, 0];
        }

        float[] outputVals = RunShader(input);

        Vector4[,] reformattedOutput = new Vector4[outputVals.Length / GetNumOutputFloats(), Mathf.CeilToInt(GetNumOutputFloats() / 4f)];
        for (int i = 0; i < reformattedOutput.Length; i++) {
            for (int j = 0; j < reformattedOutput.GetUpperBound(1) + 1; j++) {
                int baseIndex = i * GetNumOutputFloats() + j * 4;
                if (GetNumOutputFloats() % 4 == 0 || j != reformattedOutput.GetUpperBound(1)) {
                    reformattedOutput[i, j] = new Vector4(outputVals[baseIndex], outputVals[baseIndex + 1], outputVals[baseIndex + 2], outputVals[baseIndex + 3]);
                } else {
                    Vector4 newVec = Vector4.zero;
                    for (int k = 0; k < GetNumOutputFloats() % 4; k++) {
                        if (k == 0) {
                            newVec.x = outputVals[baseIndex];
                        } else if (k == 1) {
                            newVec.y = outputVals[baseIndex + k];
                        } else {
                            newVec.z = outputVals[baseIndex + k];
                        }
                    }
                    reformattedOutput[i, j] = newVec;
                }
            }
        }

        return reformattedOutput;
    }
}
