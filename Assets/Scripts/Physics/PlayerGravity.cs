using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerGravity : MonoBehaviour {
    Rigidbody rigidBody;

    void Start() {
        rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.useGravity = false;
        //keeps the rigidbody from doing its own rotation
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update() {
        Orient();
    }

    void FixedUpdate() {
        rigidBody.AddForce(GravityHandler.CalculateAcceleration(transform.position), ForceMode.Acceleration);
    }

    private void Orient() {
        CelestialBodyPhysics closestBody = GravityHandler.GetClosestPlanet(transform.position);
        Vector3 targetDirection = (closestBody.Position - transform.position).normalized;
        Vector3 bodyDown = -transform.up;
        //rotate so that its down points torwards the planet
        transform.rotation = Quaternion.FromToRotation(bodyDown, targetDirection) * transform.rotation;
    }
}
