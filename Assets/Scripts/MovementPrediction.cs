using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MovementPrediction : MonoBehaviour {
    [Min(0)]
    public int numSteps;
    public float width;
    public bool autoUpdate;
    public CelestialBody relativeTo;
    public bool isRelative;

    private int relativeIndex;
    private CelestialBodyData[] bodies;
    private Vector3[][] points;
    private LineRenderer[] renderers;

    void Initialize() {
        CelestialBody[] tempBodies = FindObjectsOfType<CelestialBody>();
        bodies = new CelestialBodyData[tempBodies.Length];
        renderers = new LineRenderer[tempBodies.Length];
        for(int i = 0; i < tempBodies.Length; i++) {
            bodies[i] = new CelestialBodyData(tempBodies[i]);
            renderers[i] = tempBodies[i].GetComponent<LineRenderer>();
            if (isRelative && relativeTo != null) {
                if(tempBodies[i] == relativeTo) {
                    relativeIndex = i;
                }
            }
        }
        points = new Vector3[bodies.Length][];
    }

    private void Update() {
        if (!Application.isPlaying) {
            if (autoUpdate) {
                Initialize();
                Simulate();
                DrawPaths();
            }
        } else {
            DeletePaths();
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
            renderers[i].enabled = true;
            renderers[i].positionCount = points[i].Length;
            renderers[i].SetPositions(points[i]);
            renderers[i].widthMultiplier = width;
        }
    }

    private void DeletePaths() {
        CelestialBody[] tempBodies = FindObjectsOfType<CelestialBody>();
        for (int i = 0; i < tempBodies.Length; i++) {
            LineRenderer renderer = tempBodies[i].GetComponent<LineRenderer>();
            renderer.enabled = false;
            renderer.positionCount = 0;
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
        public float mass;
        public float radius;
        public Vector3 position;
        Vector3 velocity;

        public CelestialBodyData(CelestialBody cb) {
            mass = cb.mass;
            radius = cb.radius;
            position = cb.Position;
            velocity = cb.initialVelocity;
        }

        public void UpdateVelocity(Vector3 acceleration) {
            velocity += acceleration * Universe.physicsTimeStep;
        }

        public void UpdatePosition() {
            position += velocity * Universe.physicsTimeStep;
        }
    }
}
