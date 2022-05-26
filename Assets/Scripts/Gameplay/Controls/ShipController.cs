using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour {
    private Rigidbody rigidBody;

    public LayerMask groundedMask;

    [HideInInspector]
    public bool piloted;

    [Header("Handling")]
    public float thrustStrength = 200;
    public float rotSpeed = 5;
    public float rollSpeed = 30;
    public float rotSmoothSpeed = 10;
    public bool lockCursor = true;

    private Quaternion targetRot;
    private Quaternion smoothedRot;
    private Vector3 thrusterInput;

    private ShipPilotInteraction pilotInteraction;

    private List<string> collidingWith;

    // Start is called before the first frame update
    private void Awake() {
        pilotInteraction = GetComponent<ShipPilotInteraction>();

        rigidBody = GetComponent<Rigidbody>();

        InitializeRigidBody();
    }

    void Start() {
        piloted = false;
        FindStartingPosition();
        targetRot = transform.rotation;
        smoothedRot = transform.rotation;
        collidingWith = new List<string>();
    }

    // Update is called once per frame
    void Update() {
        if (piloted) {
            HandleMovement();
            if (ShouldBeAbleToExit()) {
                CheckIfExiting();
            }
        }
    }

    void FixedUpdate() {
        rigidBody.AddForce(GravityHandler.CalculateAcceleration(transform.position), ForceMode.Acceleration);

        // Thrusters
        Vector3 thrustDir = transform.TransformVector(thrusterInput);
        rigidBody.AddForce(thrustDir * thrustStrength, ForceMode.Acceleration);

        if (collidingWith.Count == 0) {
            rigidBody.MoveRotation(smoothedRot);
        }
    }

    private void HandleMovement() {
        CursorLock.HandleCursor(lockCursor);
        // Thruster input
        int thrustInputX = GetInputAxis(Controls.leftKey, Controls.rightKey);
        int thrustInputY = GetInputAxis(Controls.descendKey, Controls.ascendKey);
        int thrustInputZ = GetInputAxis(Controls.backwardKey, Controls.forwardKey);
        thrusterInput = new Vector3(thrustInputX, thrustInputY, thrustInputZ);

        // Rotation input
        Vector2 YawPitchInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")).normalized * rotSpeed;
        float rollInput = GetInputAxis(Controls.rollClockwiseKey, Controls.rollCounterKey) * rollSpeed * Time.deltaTime;

        // Calculate rotation
        if (collidingWith.Count == 0) {
            var yaw = Quaternion.AngleAxis(YawPitchInput.x, transform.up);
            var pitch = Quaternion.AngleAxis(-YawPitchInput.y, transform.right);
            var roll = Quaternion.AngleAxis(rollInput, transform.forward);

            targetRot = yaw * pitch * roll * targetRot;
            smoothedRot = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSmoothSpeed);
        } else {
            targetRot = transform.rotation;
            smoothedRot = transform.rotation;
        }
    }

    private int GetInputAxis(KeyCode negativeAxis, KeyCode positiveAxis) {
        int axis = 0;
        if (Input.GetKey(positiveAxis)) {
            axis++;
        }
        if (Input.GetKey(negativeAxis)) {
            axis--;
        }
        return axis;
    }

    private bool ShouldBeAbleToExit() {
        return collidingWith.Count > 0;
    }

    private void CheckIfExiting() {
        if (Input.GetKeyDown(Controls.leaveKey)) {
            pilotInteraction.StopPiloting();
        }
    }

    private void OnCollisionEnter(Collision other) {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer))) {
            if (!collidingWith.Contains(other.gameObject.name)) {
                collidingWith.Add(other.gameObject.name);
            }
        }
    }

    private void OnCollisionExit(Collision other) {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer))) {
            collidingWith.Remove(other.gameObject.name);
        }
    }

    private void InitializeRigidBody() {
        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidBody.useGravity = false;
        rigidBody.isKinematic = false;
        rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void FindStartingPosition() {
        CelestialBody startingPlanet = GravityHandler.GetClosestPlanet(transform.position);
        Vector3 dirFromPlanetToShip = (transform.position - startingPlanet.Position).normalized;
        Vector3 targetDirection = -dirFromPlanetToShip;

        rigidBody.position = startingPlanet.Position + dirFromPlanetToShip * (0.1f + startingPlanet.radius);
        transform.rotation *= Quaternion.FromToRotation(-transform.up, targetDirection);
        rigidBody.velocity = startingPlanet.initialVelocity;
    }

    public Rigidbody RigidBody {
        get {
            return rigidBody;
        }
    }
}