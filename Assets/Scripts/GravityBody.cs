using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour {
    GravityAttractor planet;
    Rigidbody rigidBody;

    void Awake() {
        planet = GameObject.FindGameObjectWithTag("Planet").GetComponent<GravityAttractor>();
        rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.useGravity = false;
        //keeps the rigidbody from doing its own rotation
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate() {
        planet.Attract(transform, rigidBody);
    }
}
