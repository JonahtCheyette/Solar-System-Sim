using System.Linq;
using UnityEngine;

public class CelestialBodyGenerator : ScriptableObject {
    public bool autoUpdate;
    public float radius;
    
    protected ComputeShader generator;
    private ComputeBuffer pointBuffer;
    protected int kernel;

    [HideInInspector]
    public CelestialBodyMeshHandler meshHandler;

    private static ComputeShader perturbShader;

    public virtual bool HasOceanEffect() {
        return false;
    }

    public virtual OceanDetails OceanDetails() {
        return new OceanDetails(0, 0, 0, Vector3.zero, Vector3.zero);
    }

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
        if (autoUpdate && meshHandler != null) {
            meshHandler.gameObject.GetComponent<CelestialBodyPhysics>().OnValidate();
        }
    }

    //call the action
    private void OnValuesChanged() {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= OnValuesChanged;
        if (!Application.isPlaying) {
            if (autoUpdate && meshHandler != null) {
                meshHandler.Generate();
            }
        }
        #endif
    }

    private void OnDestroy() {
        DestroyPointBuffer();
    }

    public virtual void Setup() {
        FindKernel();
        generator.SetFloat("baseRadius", radius);
    }

    private void FindKernel() {
        kernel = generator.FindKernel("Generate");
    }

    public Vector3[] GeneratePoints(Vector3[] points, bool perturb = false) {
        DispatchGenerator(points, perturb);
        return GetPointsFromGenerator();
    }

    public Vector3[] GeneratePoints(Vector3[] points, ref float minSqrRadius, ref float maxSqrRadius, bool perturb = false) {
        DispatchGenerator(points, perturb);
        return GetPointsFromGenerator(ref minSqrRadius, ref maxSqrRadius);
    }

    private void DispatchGenerator(Vector3[] points, bool perturb) {
        SetupPointBuffer(points.Length);
        pointBuffer.SetData(points);
        if (perturb) {
            PerturbPoints();
        }
        GenerateRadii();
    }

    private void PerturbPoints() {
        if(perturbShader == null) {
            perturbShader = (ComputeShader)Resources.Load("Shaders/Compute Shaders/PerturbShader");
        }
        int perturbShaderKernel = perturbShader.FindKernel("Generate");
        perturbShader.SetBuffer(perturbShaderKernel, "points", pointBuffer);
        perturbShader.SetFloat("seed", 0f);
        perturbShader.Dispatch(perturbShaderKernel, Mathf.CeilToInt(pointBuffer.count / 32f), 1, 1);
    }

    private void GenerateRadii() {
        generator.SetBuffer(kernel, "points", pointBuffer);
        generator.Dispatch(kernel, Mathf.CeilToInt(pointBuffer.count / 32f), 1, 1);
    }

    private void SetupPointBuffer(int numPoints) {
        if (pointBuffer == null || !pointBuffer.IsValid() || pointBuffer.count != numPoints) {
            if (pointBuffer != null) {
                pointBuffer.Dispose();
            }
            pointBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        }
    }

    private Vector3[] GetPointsFromGenerator() {
        Vector3[] points = new Vector3[pointBuffer.count];
        pointBuffer.GetData(points);
        return points;
    }

    private Vector3[] GetPointsFromGenerator(ref float minSqrRadius, ref float maxSqrRadius) {
        Vector3[] points = new Vector3[pointBuffer.count];
        pointBuffer.GetData(points);
        for (int i = 0; i < points.Length; i++) {
            float sqrRadius = points[i].sqrMagnitude;
            if(sqrRadius < minSqrRadius) { minSqrRadius = sqrRadius; }
            if(sqrRadius > maxSqrRadius) { maxSqrRadius = sqrRadius; }
        }
        return points;
    }

    public virtual void Finish() {
        DestroyPointBuffer();
    }

    private void DestroyPointBuffer() {
        if (pointBuffer != null) {
            pointBuffer.Dispose();
        }
    }

    protected void GetGenerator(string generatorName) {
        generator = (ComputeShader)Resources.Load("Shaders/Compute Shaders/Radii Generators/" + generatorName);
    }

    public virtual float[,] ProvideDataToShader() {
        float[,] returnVal = new float[1, 1];
        returnVal[0, 0] = radius;
        return returnVal;
    }

    protected static Vector3 ColToVec(Color c) {
        return new Vector3(c.r, c.g, c.b);
    }
}