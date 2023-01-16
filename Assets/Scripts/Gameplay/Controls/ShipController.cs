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

    [Header("Operation")]
    public bool lockCursor = true;
    [Min(20)]
    public float hoverDist = 40;

    private Quaternion targetRot;
    private Quaternion smoothedRot;
    private Vector3 thrusterInput;

    private ShipPilotInteraction pilotInteraction;

    private List<string> collidingWith;

    public bool hovering { private set; get; }
    private CelestialBodyPhysics hoverBase;
    private Vector3 hoverOffset;

    private bool goingToHoverPosition;
    private const int hoverAnimationLength = 10;
    private int hoverAnimationStep = 0;
    private Quaternion finalHoverRotation;
    private Quaternion initialHoverRotation;
    private Vector3 initialHoverPosition;

    [Header("Hover Beam Stuff")]
    public Transform hoverBeam;
    [Range(0, 3)]
    public float hoverBeamSize;

    // Start is called before the first frame update
    private void Awake() {
        pilotInteraction = GetComponent<ShipPilotInteraction>();

        rigidBody = GetComponent<Rigidbody>();

        InitializeRigidBody();
    }

    private void Start() {
        piloted = false;
        FindStartingPosition();
        PlacePlayer();
        targetRot = transform.rotation;
        smoothedRot = transform.rotation;
        collidingWith = new List<string>();
        goingToHoverPosition = false;
        hoverBeam.localScale = new Vector3(hoverBeamSize, hoverDist * 2, hoverBeamSize);
        hoverBeam.localPosition = Vector3.down * (hoverDist * 2 - 1);
        hoverBeam.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update() {
        CheckForCrash();
        if (!goingToHoverPosition) {
            if (piloted) {
                if (!hovering) {
                    HandleMovement();

                    CheckIfShouldHover();
                } else {
                    CheckIfShouldLeave();

                    CheckToStopHovering();
                }
            }
            if (hovering) {
                rigidBody.freezeRotation = true;
                Hover();
            } else {
                rigidBody.freezeRotation = false;
            }
        } else {
            hoverAnimationStep++;

            transform.position += (hoverBase.transform.position + hoverOffset - initialHoverPosition) / hoverAnimationLength;
            transform.rotation = Quaternion.Slerp(initialHoverRotation, finalHoverRotation, ((float)hoverAnimationStep / hoverAnimationLength));
            if(hoverAnimationStep == hoverAnimationLength) {
                goingToHoverPosition = false;
            }
        }
    }

    void FixedUpdate() {
        if (!hovering) {
            rigidBody.AddForce(GravityHandler.CalculateAcceleration(transform.position), ForceMode.Acceleration);

            // Thrusters
            Vector3 thrustDir = transform.TransformVector(thrusterInput);
            rigidBody.AddForce(thrustDir * thrustStrength, ForceMode.Acceleration);
            if (collidingWith.Count == 0) {
                rigidBody.MoveRotation(smoothedRot);
            }
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

    private bool CanHover() {
        CelestialBodyPhysics closestPlanet = GravityHandler.GetClosestPlanet(transform.position);
        float adjustedRadius = closestPlanet.Radius() + hoverDist;
        return (closestPlanet.transform.position - transform.position).sqrMagnitude <= adjustedRadius * adjustedRadius;
    }

    private void CheckIfShouldHover() {
        if (Input.GetKeyDown(Controls.hoverKey) && CanHover()) {
            hoverBase = GravityHandler.GetClosestPlanet(transform.position);
            float hoverRadius = hoverBase.Radius() + hoverDist;
            hoverOffset = (transform.position - hoverBase.transform.position).normalized * hoverRadius;

            initialHoverRotation = transform.rotation;
            finalHoverRotation = Quaternion.FromToRotation(Vector3.up, hoverOffset / hoverRadius);
            initialHoverPosition = transform.position;

            hoverAnimationStep = 0;

            goingToHoverPosition = true;
            hovering = true;
            hoverBeam.gameObject.SetActive(true);
        }
    }

    private void Hover() {
        transform.position = hoverBase.transform.position + hoverOffset;
        rigidBody.velocity = hoverBase.velocity;
        targetRot = transform.rotation;
        smoothedRot = transform.rotation;
    }

    private void CheckIfShouldLeave() {
        if (Input.GetKeyDown(Controls.stopPilotingKey)) {
            pilotInteraction.StopPiloting();
        }
    }

    private void CheckToStopHovering() {
        if (Input.GetKeyDown(Controls.hoverKey)) {
            rigidBody.velocity = hoverBase.velocity;
            hovering = false;
            hoverBeam.gameObject.SetActive(false);
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
        rigidBody.interpolation = RigidbodyInterpolation.None;
        rigidBody.useGravity = false;
        rigidBody.isKinematic = false;
        rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private void FindStartingPosition() {
        hoverBase = GravityHandler.GetClosestPlanet(transform.position);
        float hoverRadius = hoverBase.Radius() + hoverDist;
        hoverOffset = (transform.position - hoverBase.Position).normalized * hoverRadius;
        transform.position = hoverBase.Position + hoverOffset;
        transform.rotation *= Quaternion.FromToRotation(-transform.up, -hoverOffset.normalized);
        rigidBody.velocity = hoverBase.velocity;

        hovering = true;
    }

    private void PlacePlayer() {
        FirstPersonController player = FindObjectOfType<FirstPersonController>();
        player.transform.position = transform.TransformPoint(Vector3.up * 1.5f);
    }

    private void CheckForCrash() {
        if(collidingWith.Count > 0) {
            print("wth");
            Application.Quit(); // will have to replace with proper death
        }
    }

    public Rigidbody RigidBody {
        get {
            return rigidBody;
        }
    }
}