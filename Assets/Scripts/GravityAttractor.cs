using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityAttractor : MonoBehaviour {

    public float gravity = 10f;

    public void Attract(Transform body, Rigidbody rigidBody) {
        Vector3 targetDirection = (transform.position - body.position).normalized;
        Vector3 bodyDown = -body.up;
        //rotate the body so that its down points torwards the planet
        body.rotation = Quaternion.FromToRotation(bodyDown, targetDirection) * body.rotation;
        rigidBody.AddForce(targetDirection * gravity);
    }
}
