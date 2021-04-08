using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//meant to do all the gravity calculations for everything
//should be attatched to a single empty gameobject
public class GravityHandler : MonoBehaviour {
    private CelestialBody[] bodies;

    private void Awake() {
        bodies = FindObjectsOfType<CelestialBody>();
        Time.fixedDeltaTime = Universe.physicsTimeStep;
    }

    void FixedUpdate() {
        for (int i = 0; i < bodies.Length; i++) {
            Vector3 acceleration = CalculateAcceleration(bodies[i].Position, bodies[i]);
            bodies[i].UpdateVelocity(acceleration, Universe.physicsTimeStep);
        }

        for (int i = 0; i < bodies.Length; i++) {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }
    }

    Vector3 CalculateAcceleration(Vector3 position, CelestialBody ignoreBody) {
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
}
