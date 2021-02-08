using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialBody : MonoBehaviour {
    public const float gravityStrength = 1;

    public float mass;

    private CelestialBody[] bodies;

    public void Start() {
        List<CelestialBody> temp = new List<CelestialBody>();
        bodies = FindObjectsOfType<CelestialBody>();
        for(int i = bodies.Length - 1; i >= 0; i--) {
            if(bodies[i] != this) {
                temp.Add(bodies[i]);
            }
        }
        bodies = temp.ToArray();
    }
    /*
    public void AttractPlayer(Transform body, Rigidbody rigidBody, float playerMass) {
        Vector3 targetDirection = (transform.position - body.position).normalized;
        Vector3 bodyDown = -body.up;
        //rotate the body so that its down points torwards the planet
        body.rotation = Quaternion.FromToRotation(bodyDown, targetDirection) * body.rotation;
        rigidBody.AddForce(targetDirection * (gravityStrength * mass * playerMass)/(body.position - transform.position).sqrMagnitude);
    }*/
}
