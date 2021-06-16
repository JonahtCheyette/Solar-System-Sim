using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipPhysics : MonoBehaviour {
    CelestialBody[] planets;
    Rigidbody rigidBody;

    private Vector3 groundVelocity;
    private bool grounded;
    public LayerMask groundedMask;
    private Vector3 moveAmount;
    private Vector3 smoothMoveVelocity;

    // Start is called before the first frame update
    void Start() {
        planets = FindObjectsOfType<CelestialBody>();
        rigidBody = gameObject.GetComponent<Rigidbody>();

        rigidBody.useGravity = false;
    }

    // Update is called once per frame
    void Update() {
        CheckIfGrounded();
        DoFriction();
    }

    void FixedUpdate() {
        DoGravity();
        rigidBody.position += transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
    }

    private void DoGravity() {
        foreach (CelestialBody planet in planets) {
            Vector3 targetDirection = (planet.Position - transform.position).normalized;
            rigidBody.AddForce(targetDirection * (Universe.gravitationalConstant * rigidBody.mass * planet.mass) / (transform.position - planet.transform.position).sqrMagnitude);
        }
    }

    private void CheckIfGrounded() {
        grounded = false;
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 0.05f, groundedMask)) {
            grounded = true;
            groundVelocity = transform.InverseTransformDirection(hit.rigidbody.GetPointVelocity(hit.point) - rigidBody.velocity);
        } else {
            groundVelocity = Vector3.zero;
        }
    }

    private void DoFriction() {
        if (grounded) {
            //friction with ground
            moveAmount = Vector3.SmoothDamp(moveAmount, groundVelocity, ref smoothMoveVelocity, 1 / groundVelocity.sqrMagnitude);
        }
    }

    public Rigidbody RigidBody {
        get {
            return rigidBody;
        }
    }
}
