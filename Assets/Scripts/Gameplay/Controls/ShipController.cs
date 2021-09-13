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

    private KeyCode ascendKey = KeyCode.Space;
    private KeyCode descendKey = KeyCode.LeftShift;
    private KeyCode forwardKey = KeyCode.W;
    private KeyCode backwardKey = KeyCode.S;
    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;
    private KeyCode rollCounterKey = KeyCode.Q;
    private KeyCode rollClockwiseKey = KeyCode.E;

    private KeyCode leaveKey = KeyCode.X;

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
            CheckIfExiting();
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
        int thrustInputX = GetInputAxis(leftKey, rightKey);
        int thrustInputY = GetInputAxis(descendKey, ascendKey);
        int thrustInputZ = GetInputAxis(backwardKey, forwardKey);
        thrusterInput = new Vector3(thrustInputX, thrustInputY, thrustInputZ);

        // Rotation input
        Vector2 YawPitchInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")).normalized * rotSpeed;
        float rollInput = GetInputAxis(rollClockwiseKey, rollCounterKey) * rollSpeed * Time.deltaTime;

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

    private void CheckIfExiting() {
        if (Input.GetKeyDown(leaveKey)) {
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
        //rigidBody.interpolation = RigidbodyInterpolation.Interpolate; this is supposed to fix jitteriness, but for me it caused it instead
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