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

    private int numCollisionTouches;

    private KeyCode ascendKey = KeyCode.Space;
    private KeyCode descendKey = KeyCode.LeftShift;
    private KeyCode rollCounterKey = KeyCode.Q;
    private KeyCode rollClockwiseKey = KeyCode.E;
    private KeyCode forwardKey = KeyCode.W;
    private KeyCode backwardKey = KeyCode.S;
    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;

    // Start is called before the first frame update
    private void Awake() {
        rigidBody = GetComponent<Rigidbody>();

        InitializeRigidBody();
    }

    void Start() {
        piloted = false;
        FindStartingPosition();
        targetRot = transform.rotation;
        smoothedRot = transform.rotation;
    }

    // Update is called once per frame
    void Update() {
        if (piloted) {
            HandleMovement();
        }
    }

    void FixedUpdate() {
        rigidBody.AddForce(GravityHandler.CalculateAcceleration(transform.position), ForceMode.Acceleration);

        // Thrusters
        Vector3 thrustDir = transform.TransformVector(thrusterInput);
        rigidBody.AddForce(thrustDir * thrustStrength, ForceMode.Acceleration);

        if (numCollisionTouches == 0) {
            rigidBody.MoveRotation(smoothedRot);
        }
    }

    private void HandleMovement() {
        CursorLock.HandleCursor(lockCursor);
        // Thruster input
        int thrustInputX = GetInputAxis(leftKey, rightKey);
        int thrustInputY = GetInputAxis(descendKey, ascendKey);
        int thrustInputZ = GetInputAxis(backwardKey, forwardKey);
        //have to reverse z input since the spaceship model is rotated 180 degrees
        thrusterInput = new Vector3(thrustInputX, thrustInputY, -thrustInputZ);

        // Rotation input
        float yawInput = Input.GetAxisRaw("Mouse X") * rotSpeed;
        float pitchInput = Input.GetAxisRaw("Mouse Y") * rotSpeed;
        float rollInput = GetInputAxis(rollCounterKey, rollClockwiseKey) * rollSpeed * Time.deltaTime;

        // Calculate rotation
        if (numCollisionTouches == 0) {
            var yaw = Quaternion.AngleAxis(yawInput, transform.up);
            var pitch = Quaternion.AngleAxis(pitchInput, transform.right);
            var roll = Quaternion.AngleAxis(-rollInput, transform.forward);

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

    private void OnCollisionEnter(Collision other) {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer))) {
            numCollisionTouches++;
        }
    }

    private void OnCollisionExit(Collision other) {
        if (groundedMask == (groundedMask | (1 << other.gameObject.layer))) {
            numCollisionTouches--;
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