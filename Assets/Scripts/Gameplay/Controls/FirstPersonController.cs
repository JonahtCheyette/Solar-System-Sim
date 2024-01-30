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
    public float seedThrowingForce = 100f;
    public GameObject seedPrefab;

    private int seedCooldown;
    private const int seedCooldownMax = 100;

    //decides what counts as things the player can be grounded on
    public LayerMask groundedMask;
    public LayerMask spaceShipMask;// I want to be able to check if the player is grounded on the spaceship

    private Rigidbody rigidBody;

    private float verticalLookRotation;

    private Vector3 moveAmount;
    private Vector3 smoothMoveVelocity;

    private Transform cameraT;
    private Vector3 cameraLocalPosition;
    private Transform cameraParent;

    private bool grounded;
    private bool onSpaceShip;

    private Vector3 targetMoveAmount;

    private void Awake() {
        CursorLock.Reset();
        seedCooldown = 0;
    }

    // Start is called before the first frame update
    private void Start() {
        cameraT = Camera.main.transform;
        cameraParent = cameraT.parent;
        cameraLocalPosition = cameraT.localPosition;
        rigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update() {
        CursorLock.HandleCursor();
        GetMovementInput();
        CheckThrowing();
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
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;
        
        bool onGround = Physics.Raycast(ray, out hit, 0.1f, groundedMask);
        onSpaceShip = Physics.Raycast(ray, out hit, 0.1f, spaceShipMask);
        grounded = onGround || onSpaceShip;
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
            if (Input.GetKey(Controls.sprintKey)) {
                targetMoveAmount *= runSpeed;
            } else {
                targetMoveAmount *= walkSpeed;
            }
        }
        moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref smoothMoveVelocity, 0.15f);
    }

    private void CheckThrowing() {
        seedCooldown--;
        seedCooldown = Mathf.Clamp(seedCooldown, 0, seedCooldownMax);
        if(grounded && !onSpaceShip && seedCooldown == 0 && Input.GetKeyDown(Controls.seedKey)) {
            seedCooldown = seedCooldownMax;
            GameObject seed = Instantiate(seedPrefab, transform.position + transform.forward + transform.up * 5, transform.rotation);
            seed.GetComponent<Rigidbody>().velocity = rigidBody.velocity + cameraT.forward * seedThrowingForce;
        }
    }

    private void Jump() {
        if (Input.GetKeyDown(Controls.jumpKey) && grounded) {
            rigidBody.AddForce(transform.up * jumpForce);
        }
    }

    public bool Grounded {
        get {
            return grounded;
        }
    }

    public bool OnSpaceShip {
        get {
            return onSpaceShip;
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
