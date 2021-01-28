using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FirstPersonController : MonoBehaviour {

    public float mouseSensitivityX = 250f;
    public float mouseSensitivityY = 250f;
    public float moveSpeed = 10f;
    public float jumpForce = 220f;

    //decides what counts as things the player can be grounded on
    public LayerMask groundedMask;

    Rigidbody rigidBody;

    float verticalLookRotation;

    Vector3 moveAmount;
    Vector3 smoothMoveVelocity;

    Transform cameraT;

    bool grounded;
    bool cursorIsLocked;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        cursorIsLocked = true;
    }

    // Start is called before the first frame update
    void Start() {
        cameraT = Camera.main.transform;
        rigidBody = GetComponentInChildren<Rigidbody>();
    }

    // Update is called once per frame
    void Update() {
        if (cursorIsLocked) {
            //rotating on the horizontal axis
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivityX);
            //rotating on the vertical axis
            verticalLookRotation += Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivityY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -60, 60);
            cameraT.localEulerAngles = Vector3.left * verticalLookRotation;
        }

        //capturing movement input
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 targetMoveAmount = moveDir * moveSpeed;
        moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref smoothMoveVelocity, 0.15f);

        //jumping
        if (Input.GetButtonDown("Jump") && grounded) {
            rigidBody.AddForce(transform.up * jumpForce);
        }

        //figure out if we're grounded
        grounded = false;
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 2.8f, groundedMask)) {
            grounded = true;
        }

        //changing whether the cursor is locked to the center of the screen
        if (Input.GetKeyDown(KeyCode.Escape)) {
            cursorIsLocked = false;
        }
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 && Input.mousePosition.x <= Handles.GetMainGameViewSize().x && Input.mousePosition.y <= Handles.GetMainGameViewSize().y) {
            cursorIsLocked = true;
        }
    }

    void FixedUpdate() {
        rigidBody.MovePosition(rigidBody.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }
}
