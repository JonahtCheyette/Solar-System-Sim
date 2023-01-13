using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class CelestialBodyPhysics : MonoBehaviour {
    private Rigidbody rb;

    public float surfaceGravity;
    public Vector3 initialVelocity;
    public Vector3 velocity { get; private set; }
    public float mass { get; private set; }
    public Color color;


    private CelestialBodyMeshHandler handler;

    public void Awake() {
        rb = GetComponent<Rigidbody>();
        mass = (surfaceGravity * Radius() * Radius()) / Universe.gravitationalConstant;
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
        mass = (surfaceGravity * Radius() * Radius()) / Universe.gravitationalConstant;
        if (rb == null) {
            rb = GetComponent<Rigidbody>();
        }
        rb.mass = mass;
    }

    public Vector3 Position {
        get {
            return rb.position;
        }
    }

    public Rigidbody RigidBody {
        get {
            return rb;
        }
    }

    public float Radius() {
        if (handler == null) {
            handler = GetComponent<CelestialBodyMeshHandler>();
        }
        if (handler != null) {
            if (handler.celestialBodyGenerator != null) {
                return handler.celestialBodyGenerator.radius;
            }
        }
        return transform.localScale.x;
    }
}
