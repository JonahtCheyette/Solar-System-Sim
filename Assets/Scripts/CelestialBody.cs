using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class CelestialBody : MonoBehaviour {
    Rigidbody rb;

    public float radius;
    public float surfaceGravity;
    public Vector3 initialVelocity;
    public Vector3 velocity { get; private set; }
    public float mass { get; private set; }

    public void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        velocity = initialVelocity;
        rb.useGravity = false;
        //keeps the rigidbody from doing its own rotation
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void UpdateVelocity(Vector3 acceleration, float timeStep) {
        velocity += acceleration * timeStep;
    }

    public void UpdatePosition(float timeStep) {
        rb.MovePosition(rb.position + velocity * timeStep);
    }

    public void OnValidate() {
        mass = (surfaceGravity * radius * radius) / Universe.gravitationalConstant;
        transform.localScale = Vector3.one * radius;
    }

    public Vector3 Position {
        get {
            return rb.position;
        }
    }

    public Rigidbody Rigidbody {
        get {
            return Rigidbody;
        }
    }
}
