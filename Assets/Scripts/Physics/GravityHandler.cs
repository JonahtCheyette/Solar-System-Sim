using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//meant to do all the gravity calculations for everything
//should be attatched to a single empty gameobject
public class GravityHandler : MonoBehaviour {
    private static CelestialBody[] bodies;

    private void Awake() {
        bodies = FindObjectsOfType<CelestialBody>();
        Time.fixedDeltaTime = Universe.physicsTimeStep;
    }

    void FixedUpdate() {
        //doing the gravity simulations for the planets
        for (int i = 0; i < bodies.Length; i++) {
            Vector3 acceleration = CalculateAccelerationForPlanets(bodies[i].Position, bodies[i]);
            bodies[i].UpdateVelocity(acceleration, Universe.physicsTimeStep);
        }

        for (int i = 0; i < bodies.Length; i++) {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }
    }

    private Vector3 CalculateAccelerationForPlanets(Vector3 position, CelestialBody ignoreBody) {
        //ignore body is there because we don't want any objects to attract themselves
        Vector3 acceleration = Vector3.zero;
        foreach (var body in bodies) {
            if (body != ignoreBody) {
                float sqrDst = (body.Position - position).sqrMagnitude;
                Vector3 forceDir = (body.Position - position).normalized;
                acceleration += forceDir * Universe.gravitationalConstant * body.mass / sqrDst;
            }
        }

        return acceleration;
    }

    public static Vector3 CalculateAcceleration (Vector3 position) {
        Vector3 acceleration = Vector3.zero;
        foreach (var body in bodies) {
            float sqrDst = (body.Position - position).sqrMagnitude;
            Vector3 forceDir = (body.Position - position).normalized;
            acceleration += forceDir * Universe.gravitationalConstant * body.mass / sqrDst;
        }

        return acceleration;
    }

    public static CelestialBody GetClosestPlanet(Vector3 position) {
        float minDistance = float.MaxValue;
        CelestialBody closestBody = bodies[0];
        foreach (var body in bodies) {
            float dist = (body.Position - position).magnitude;
            if(minDistance > dist) {
                minDistance = dist;
                closestBody = body;
            }
        }

        return closestBody;
    }
}
