﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarrySky : MonoBehaviour {
    public int numberOfStars = 1000;
    public int starResolution = 50;
    public Vector2 sizeRange = new Vector2(2, 4);

    public Color highlightColor1;
    public Color highlightColor2;

    private Camera playerCam;
    private Material mat;

    private Mesh starMesh;

    private ComputeBuffer transformationBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer brightnessBuffer;
    private ComputeBuffer scaleBuffer;

    private ComputeBuffer argBuffer;

    private Bounds starSphereBounds;

    // Start is called before the first frame update
    void Start() {
        GetCamera();
        CreateMesh();
        SetUpMaterial();
        SetUpBounds();
        SetUpArgsBuffer();
    }

    private void GetCamera() {
        playerCam = Camera.main;
    }

    private void CreateMesh() {
        Vector3[] verts = new Vector3[starResolution];
        //formula for number of triangles:
        //the circle is made up of 2 triangles one at the top and one at the bottom + a series of vertical segments of 4 points, 
        //each of which need 2 triangles each
        //numTris = numSegments * 2 + 2
        //numSegments = (starResolution - 2)/2
        //numTris = (starResolution - 2)/2 * 2 + 2 = starResolution
        int[] tris = new int[starResolution * 3];
        float angleIncrement = Mathf.PI * 2f / starResolution;
        for (int i = 0; i < starResolution; i++) {
            float angle = angleIncrement * i;
            verts[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        //getting a list of ints in the order 0, starResolution - 1, 1, starResolution - 2, 2 to make tris out of
        int[] orderedVerts = new int[starResolution];
        for (int i = 0; i < starResolution; i++) {
            if (i % 2 == 0) {
                orderedVerts[i] = i / 2;
            } else {
                orderedVerts[i] = starResolution - Mathf.CeilToInt(i / 2f);
            }
        }

        //NOTE: Unity uses a clockwise winding order
        for (int i = 0; i < starResolution - 2; i++) {
            if (i % 2 == 0) {
                tris[i * 3] = orderedVerts[i];
                tris[i * 3 + 1] = orderedVerts[i + 1];
                tris[i * 3 + 2] = orderedVerts[i + 2];
            } else {
                //have to split it up like this because unity uses a clockwise winding order
                tris[i * 3] = orderedVerts[i];
                tris[i * 3 + 1] = orderedVerts[i + 2];
                tris[i * 3 + 2] = orderedVerts[i + 1];
            }
        }

        starMesh = new Mesh();
        starMesh.vertices = verts;
        starMesh.triangles = tris;
    }

    private void SetUpMaterial() {
        mat = new Material(Shader.Find("Unlit/StarrySky"));
        SetUpBuffers();
        SetStaticMaterialProperties();
    }

    private void SetUpBuffers() {
        SetUpScaleBuffer();
        SetUpTransformationBuffer();
        SetUpColorBuffer();
        SetUpBrightnessBuffer();
    }

    private void SetUpTransformationBuffer() {
        if(transformationBuffer != null && transformationBuffer.count != numberOfStars) {
            transformationBuffer.Release();
        }
        if (transformationBuffer == null) {
            transformationBuffer = new ComputeBuffer(numberOfStars, sizeof(float) * 16);
        }

        transformationBuffer.SetData(CreateTransformations());
    }

    private Matrix4x4[] CreateTransformations() {
        Matrix4x4[] transformations = new Matrix4x4[numberOfStars];
        float[] scales = new float[numberOfStars];
        scaleBuffer.GetData(scales);

        for (int i = 0; i < numberOfStars; i++) {
            Vector3 positionOffset = Random.onUnitSphere * (playerCam.farClipPlane - 1f);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, positionOffset.normalized);
            Vector3 scale = Vector3.one * scales[i];
            transformations[i] = Matrix4x4.TRS(positionOffset, rotation, scale);
        }

        return transformations;
    }

    private void SetUpColorBuffer() {
        if (colorBuffer != null && colorBuffer.count != numberOfStars) {
            colorBuffer.Release();
        }
        if (colorBuffer == null) {
            colorBuffer = new ComputeBuffer(numberOfStars, sizeof(float));
        }

        colorBuffer.SetData(CreateColors());
    }

    private float[] CreateColors() {
        float[] colors = new float[numberOfStars];

        for (int i = 0; i < numberOfStars; i++) {
            colors[i] = Random.value;
        }

        return colors;
    }

    private void SetUpScaleBuffer() {
        if (scaleBuffer != null && scaleBuffer.count != numberOfStars) {
            scaleBuffer.Release();
        }
        if (scaleBuffer == null) {
            scaleBuffer = new ComputeBuffer(numberOfStars, sizeof(float));
        }

        scaleBuffer.SetData(CreateScales());
    }

    private float[] CreateScales() {
        float[] scales = new float[numberOfStars];

        for (int i = 0; i < numberOfStars; i++) {
            scales[i] = Mathf.Tan(Random.Range(sizeRange.x, sizeRange.y) * Mathf.Deg2Rad / 2f) * (playerCam.farClipPlane - 1f);
        }

        return scales;
    }

    private void SetUpBrightnessBuffer() {
        if (brightnessBuffer != null && brightnessBuffer.count != numberOfStars) {
            brightnessBuffer.Release();
        }
        if (brightnessBuffer == null) {
            brightnessBuffer = new ComputeBuffer(numberOfStars, sizeof(float));
        }

        brightnessBuffer.SetData(CreateBrightnesses());
    }

    private float[] CreateBrightnesses() {
        float[] brightnesses = new float[numberOfStars];

        for (int i = 0; i < numberOfStars; i++) {
            brightnesses[i] = Random.Range(0.5f, 1f);
        }

        return brightnesses;
    }

    private void SetStaticMaterialProperties() {
        mat.SetBuffer("transformations", transformationBuffer);
        mat.SetBuffer("colorParameters", colorBuffer);
        mat.SetBuffer("scales", scaleBuffer);
        mat.SetBuffer("brightnesses", brightnessBuffer);
        mat.SetVector("highlightColor1", new Vector4(highlightColor1.r, highlightColor1.g, highlightColor1.b));
        mat.SetVector("highlightColor2", new Vector4(highlightColor2.r, highlightColor2.g, highlightColor2.b));
    }

    private void SetUpBounds() {
        starSphereBounds = new Bounds(Vector3.zero, Vector3.one * playerCam.farClipPlane);
    }

    private void SetUpArgsBuffer() {
        if (argBuffer == null) {
            argBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        argBuffer.SetData(new uint[] { starMesh.GetIndexCount(0), (uint)numberOfStars, 0, 0, 0 });
    }

    private void Update() {
        UpdateBounds();
        UpdateDynamicMaterialValues();
        //Graphics.DrawMeshInstancedIndirect(starMesh, 0, mat, starSphereBounds, argBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, playerCam);
        Graphics.DrawMeshInstancedIndirect(starMesh, 0, mat, starSphereBounds, argBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, null);
    }

    private void UpdateBounds() {
        starSphereBounds.center = playerCam.transform.position;
    }

    private void UpdateDynamicMaterialValues() {
        mat.SetVector("cameraPos", playerCam.transform.position);
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
        if (transformationBuffer != null) {
            transformationBuffer.Release();
        }
        if (colorBuffer != null) {
            colorBuffer.Release();
        }
        if (scaleBuffer != null) {
            scaleBuffer.Release();
        }
        if (brightnessBuffer != null) {
            brightnessBuffer.Release();
        }
        if (argBuffer != null) {
            argBuffer.Release();
        }
    }
}
