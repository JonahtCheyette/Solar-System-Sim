﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerGravity : MonoBehaviour {
    CelestialBody[] planets;
    Rigidbody rigidBody;

    void Start() {
        planets = FindObjectsOfType<CelestialBody>();
        rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.useGravity = false;
        //keeps the rigidbody from doing its own rotation
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update() {
        Orient();
    }

    void FixedUpdate() {
        DoGravity();
    }

    private void Orient() {
        float minDist = float.MaxValue;
        int index = 0;
        for (int i = 0; i < planets.Length; i++) {
            float sqrDist = (planets[i].Position - transform.position).sqrMagnitude;
            if (sqrDist < minDist) {
                minDist = sqrDist;
                index = i;
            }
        }
        Vector3 targetDirection = (planets[index].Position - transform.position).normalized;
        Vector3 bodyDown = -transform.up;
        //rotate so that its down points torwards the planet
        transform.rotation = Quaternion.FromToRotation(bodyDown, targetDirection) * transform.rotation;
    }

    private void DoGravity() {
        foreach(CelestialBody planet in planets) {
            Vector3 targetDirection = (planet.Position - transform.position).normalized;
            rigidBody.AddForce(targetDirection * (Universe.gravitationalConstant * rigidBody.mass * planet.mass) / (transform.position - planet.transform.position).sqrMagnitude);
        }
    }

    public float mass {
        get {
            return rigidBody.mass;
        }
    }
}
