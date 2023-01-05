using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FirstPersonController : MonoBehaviour {
    //makes it so the character can walk around on the planets
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
    private Vector3 cameraLocalPosition;
    private Transform cameraParent;

    private bool grounded;

    private Vector3 targetMoveAmount;

    private void Awake() {
        CursorLock.Reset();
    }

    // Start is called before the first frame update
    private void Start() {
        cameraT = Camera.main.transform;
        cameraParent = cameraT.parent;
        cameraLocalPosition = cameraT.localPosition;
        rigidBody = GetComponentInChildren<Rigidbody>();
        FindStartingPosition();
    }

    // Update is called once per frame
    private void Update() {
        CursorLock.HandleCursor();
        GetMovementInput();
        Jump();
        CheckIfGrounded();
    }

    private void FixedUpdate() {
        rigidBody.position += transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
    }

    public Vector2 GetGroundSpeed() {
        return new Vector2(moveAmount.x, moveAmount.z);
    }

    public void ResetCameraParent() {
        cameraT.parent = cameraParent;
    }

    private void CheckIfGrounded() {
        grounded = false;
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 0.1f, groundedMask)) {
            grounded = true;
        }
    }

    private void GetMovementInput() {
        targetMoveAmount = Vector3.zero;
        if (CursorLock.CursorIsLocked()) {
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
        if (Input.GetKeyDown(Controls.jumpKey) && grounded) {
            rigidBody.AddForce(transform.up * jumpForce);
        }
    }

    private void FindStartingPosition() {
        CelestialBodyPhysics startingPlanet = GravityHandler.GetClosestPlanet(transform.position);
        Vector3 dirFromPlanetToPlayer = (transform.position - startingPlanet.Position).normalized;
        rigidBody.position = startingPlanet.Position + dirFromPlanetToPlayer * (1.1f * startingPlanet.Radius());
        rigidBody.velocity = startingPlanet.initialVelocity;

        Vector3 targetDirection = (startingPlanet.transform.position - transform.position).normalized;
        //rotate so that the player's down points torwards the planet
        transform.rotation *= Quaternion.FromToRotation(-transform.up, targetDirection);
    }

    public bool Grounded {
        get {
            return grounded;
        }
    }

    public Vector3 FPSCameraPosition {
        get {
            return cameraLocalPosition;
        }
    }

    public Quaternion FPSCameraRotation {
        get {
            verticalLookRotation += Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivityY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -60, 60);
            return Quaternion.Euler(Vector3.left * verticalLookRotation + new Vector3(0, 180, 0));
        }
    }

    public Rigidbody rb {
        get {
            return rigidBody;
        }
    }
}
