using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MovementPrediction : MonoBehaviour {
    [Min(0)]
    public int numSteps;
    public bool autoUpdate;
    public bool isRelative;
    public CelestialBody relativeTo;
    public bool useDefaultTimeStep;
    public float customTimeStep;

    private int relativeIndex;
    private CelestialBodyData[] bodies;
    private Vector3[][] points;

    void Initialize() {
        CelestialBody[] tempBodies = FindObjectsOfType<CelestialBody>();
        bodies = new CelestialBodyData[tempBodies.Length];
        float timeStep = useDefaultTimeStep ? Universe.physicsTimeStep : customTimeStep;
        for(int i = 0; i < tempBodies.Length; i++) {
            bodies[i] = new CelestialBodyData(tempBodies[i], timeStep);
            if (isRelative && relativeTo != null) {
                if(tempBodies[i] == relativeTo) {
                    relativeIndex = i;
                }
            }
        }
        points = new Vector3[bodies.Length][];
    }

    private void Update() {
        if (autoUpdate) {
            if (!Application.isPlaying) {
                Initialize();
                Simulate();
                DrawPaths();
            }
        }
    }

    public void PredictMovement() {
        if(!(autoUpdate || Application.isPlaying)) {
            Initialize();
            Simulate();
            DrawPaths();
        }
    }

    private void Simulate() {
        for (int i = 0; i < bodies.Length; i++) {
            points[i] = new Vector3[numSteps];
        }

        Vector3 relativeBodyInitialPosition = Vector3.zero;
        if(isRelative && relativeTo != null) {
            relativeBodyInitialPosition = bodies[relativeIndex].position;
        }

        for (int i = 0; i < numSteps; i++) {
            Vector3 relativeBodyPosition = (isRelative && relativeTo != null) ? bodies[relativeIndex].position : Vector3.zero;

            for (int j = 0; j < bodies.Length; j++) {
                Vector3 acceleration = CalculateAcceleration(bodies[j].position, j);
                bodies[j].UpdateVelocity(acceleration);
            }

            for (int j = 0; j < bodies.Length; j++) {
                bodies[j].UpdatePosition();
                Vector3 bodyPos = bodies[j].position;

                if (isRelative && relativeTo != null) {
                    bodyPos -= (relativeBodyPosition - relativeBodyInitialPosition);
                    if (j == relativeIndex) {
                        bodyPos = relativeBodyInitialPosition;
                    }
                }

                points[j][i] = bodyPos;
            }
        }
    }

    private void DrawPaths() {
        for (int i = 0; i < bodies.Length; i++) {
            for (int j = 1; j < points[i].Length; j++) {
                Debug.DrawLine(points[i][j - 1], points[i][j], bodies[i].color);
            }
        }
    }

    private Vector3 CalculateAcceleration(Vector3 position, int ignoreBodyIndex) {
        //ignore body is there because we don't want any objects to attract themselves
        Vector3 acceleration = Vector3.zero;
        for(var i = 0; i < bodies.Length; i++) {
            if (i != ignoreBodyIndex) {
                float sqrDst = (bodies[i].position - position).sqrMagnitude;
                Vector3 forceDir = (bodies[i].position - position).normalized;
                acceleration += forceDir * Universe.gravitationalConstant * bodies[i].mass / sqrDst;
            }
        }

        return acceleration;
    }

    private struct CelestialBodyData {
        public float mass { get; private set; }
        public float radius { get; private set; }
        public Vector3 position { get; private set; }
        public Color color;
        private Vector3 velocity;
        private float timeStep;

        public CelestialBodyData(CelestialBody cb, float physTimeStep) {
            mass = cb.mass;
            radius = cb.radius;
            position = cb.Position;
            velocity = cb.initialVelocity;
            timeStep = physTimeStep;
            color = cb.color;
            color.a = 1f;
        }

        public void UpdateVelocity(Vector3 acceleration) {
            velocity += acceleration * timeStep;
        }

        public void UpdatePosition() {
            position += velocity * timeStep;
        }
    }
}
