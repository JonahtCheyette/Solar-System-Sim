using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDoorController : MonoBehaviour {
    private bool open;
    private BoxCollider doorCollider;
    private BoxCollider rampCollider;
    private GameObject rampObject;
    private bool openLastFrame;
    private Vector3 rampStartPosition = new Vector3(0, 1.29f, 7.25f);
    private Quaternion rampStartRotation = Quaternion.Euler(Vector3.right*90f);
    private Vector3 rampPositionStep;
    private Quaternion rampFinalRotation;
    private int rampStage;
    private int rampDir;

    // Start is called before the first frame update
    void Start() {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        float maxZcoord = float.MinValue;

        foreach (BoxCollider c in colliders) {
            if(c.center.z > maxZcoord) {
                //this collider is farther back
                maxZcoord = c.center.z;
                doorCollider = c;
            }
        }

        rampObject = transform.Find("Ramp").gameObject;
        rampCollider = rampObject.GetComponent<BoxCollider>();

        rampPositionStep = (rampObject.transform.localPosition - rampStartPosition) / 10f;
        rampFinalRotation = rampObject.transform.localRotation;

        rampStage = open ? 10 : 0;
        rampDir = open ? 1 : -1;

        openLastFrame = open;
    }

    // Update is called once per frame
    void Update() {
        doorCollider.enabled = !open;
        rampCollider.enabled = open;

        if(openLastFrame != open) {
            rampDir = open ? 1 : -1;
        }

        rampStage += rampDir;
        rampStage = Mathf.Clamp(rampStage, 0, 10);

        rampObject.transform.localPosition = rampStartPosition + rampPositionStep * rampStage;
        rampObject.transform.localRotation = Quaternion.Slerp(rampStartRotation, rampFinalRotation, rampStage / 10);

        openLastFrame = open;
        InteractionHandler.AddInteractionIfInRange(ChangeDoorState, open ? "Close Ship Door" : "Open Ship Door", KeyCode.C, transform.position + (transform.rotation * doorCollider.center * transform.localScale.x));
    }

    public bool isOpen {
        get {
            return open;
        }
    }

    private void ChangeDoorState() {
        open = !open;
    }
}
