using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FirstPersonController : MonoBehaviour {

    public CelestialBody startingPlanet;

    public float mouseSensitivityX = 250f;
    public float mouseSensitivityY = 250f;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 220f;

    //decides what counts as things the player can be grounded on
    public LayerMask groundedMask;

    private Rigidbody rigidBody;

    private float verticalLookRotation;

    private Vector3 moveAmount;
    private Vector3 smoothMoveVelocity;

    private Transform cameraT;

    private bool grounded;
    private bool cursorIsLocked;
    private Vector3 groundVelocity;

    private Vector3 targetMoveAmount;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        cursorIsLocked = true;
    }

    // Start is called before the first frame update
    void Start() {
        cameraT = Camera.main.transform;
        rigidBody = GetComponentInChildren<Rigidbody>();
        FindStartingPosition();
    }

    // Update is called once per frame
    void Update() {
        GetMovementInput();
        DoFriction();
        Jump();
        CheckIfGrounded();
        CheckCursorLockState();
    }

    private void FixedUpdate() {
        rigidBody.position += transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
    }

    public Vector2 getSpeed() {
        return new Vector2(moveAmount.x, moveAmount.z);
    }

    private void CheckCursorLockState() {
        //changing whether the cursor is locked to the center of the screen
        if (Input.GetKeyDown(KeyCode.Escape)) {
            cursorIsLocked = false;
        }
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 && Input.mousePosition.x <= Handles.GetMainGameViewSize().x && Input.mousePosition.y <= Handles.GetMainGameViewSize().y) {
            cursorIsLocked = true;
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
            moveAmount = Vector3.SmoothDamp(moveAmount, groundVelocity, ref smoothMoveVelocity, 1/groundVelocity.sqrMagnitude);
        }
    }

    private void GetMovementInput() {
        targetMoveAmount = Vector3.zero;
        if (cursorIsLocked) {
            //rotating on the horizontal axis
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivityX);
            //rotating on the vertical axis
            verticalLookRotation += Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivityY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -60, 60);
            cameraT.localEulerAngles = Vector3.left * verticalLookRotation + new Vector3(0, 180, 0);

            //capturing movement input
            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            //running/walking
            targetMoveAmount = moveDir;
            if (Input.GetKey(KeyCode.LeftShift)) {
                targetMoveAmount *= runSpeed;
            } else {
                targetMoveAmount *= walkSpeed;
            }
        }
        moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref smoothMoveVelocity, 0.15f);
    }

    private void Jump() {
        if (Input.GetKeyDown(KeyCode.Space) && grounded) {
            rigidBody.AddForce(transform.up * jumpForce);
        }
    }

    private void FindStartingPosition() {
        Vector3 dirFromPlanetToPlayer = (transform.position - startingPlanet.Position).normalized;
        rigidBody.position = startingPlanet.Position + dirFromPlanetToPlayer * (0.1f + startingPlanet.radius);
        rigidBody.velocity = startingPlanet.initialVelocity;

        Vector3 targetDirection = (startingPlanet.transform.position - transform.position).normalized;
        //rotate so that the player's down points torwards the planet
        transform.rotation *= Quaternion.FromToRotation(-transform.up, targetDirection);

        ShipPhysics spaceShip = FindObjectOfType<ShipPhysics>();
        spaceShip.transform.position = rigidBody.position + transform.right * 6;
        spaceShip.transform.rotation = Quaternion.FromToRotation(-spaceShip.transform.up, targetDirection);
        spaceShip.RigidBody.velocity = startingPlanet.initialVelocity;
    }

    public bool Grounded {
        get {
            return grounded;
        }
    }
}
