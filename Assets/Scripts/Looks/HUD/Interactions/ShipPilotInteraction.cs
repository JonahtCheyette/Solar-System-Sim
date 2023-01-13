using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipPilotInteraction : MonoBehaviour {

    //interaction functionality
    public float minPilotDistance = 5f;
    public float minLeaveDistance = 5f;
    public float minDoorDistance = 10f;

    private GameObject player;
    private Vector3 shipCameraPosition;
    private ShipController shipController;
    private Vector3 chairPosition = new Vector3(0, 2f, 3f);
    private FirstPersonController playerController;
    private bool piloting;

    //animation variables
    private bool doingAnimation;
    private int animationStep;
    private Vector3 cameraPositionChange;
    private Quaternion finalCameraRotation;
    private Quaternion startCameraRotation;
    private int animationLength = 15;

    //Door variables
    private Vector3 rampStartPosition = new Vector3(0, 1.29f, 7.25f);
    private Quaternion rampStartRotation = Quaternion.Euler(Vector3.right * 90f);
    private Vector3 rampPositionStep;
    private Quaternion rampFinalRotation;
    private int rampStage;
    [HideInInspector]
    public bool doorOpen { private set; get; }
    private BoxCollider doorCollider;
    private BoxCollider rampCollider;
    private GameObject rampObject;

    // Start is called before the first frame update
    void Start() {
        player = GameObject.Find("Player");
        shipController = GetComponent<ShipController>();

        //find where the camera should be placed by finding the furthest forward collider on the ship
        BoxCollider[] colliders = transform.Find("Colliders").gameObject.GetComponents<BoxCollider>();
        shipCameraPosition = Vector3.forward * float.MinValue;
        float minZcoord = float.MaxValue;

        foreach (BoxCollider c in colliders) {
            if (c.center.z > shipCameraPosition.z) {
                //this collider is further forward
                shipCameraPosition = c.center;
            }
            if (c.center.z < minZcoord) {
                //this collider is farther back
                minZcoord = c.center.z;
                doorCollider = c;
            }
        }

        playerController = player.GetComponent<FirstPersonController>();
        doingAnimation = false;
        animationStep = 0;
        piloting = false;

        rampObject = transform.Find("Ramp").gameObject;
        rampCollider = rampObject.GetComponent<BoxCollider>();

        rampPositionStep = (rampObject.transform.localPosition - rampStartPosition) / 10f;
        rampFinalRotation = rampObject.transform.localRotation;

        rampStage = 0;

        doorOpen = false;
    }

    // Update is called once per frame
    void Update() {
        if (!doingAnimation) {
            Vector3 pilotPosition = transform.TransformPoint(chairPosition);
            Vector3 leavePosition = transform.TransformPoint(Vector3.up * 2);
            if (player.activeInHierarchy) {
                InteractionHandler.AddInteractionIfInRange(StartPiloting, "Start Piloting The Spaceship", Controls.startPilotingShipKey, pilotPosition, minPilotDistance);
                InteractionHandler.AddInteractionIfInRange(LeaveShip, "Leave The Spaceship", Controls.leaveShipKey, leavePosition, minLeaveDistance);
            }
        } else {
            animationStep++;

            Camera.main.transform.localPosition += cameraPositionChange;
            Camera.main.transform.localRotation = Quaternion.Slerp(startCameraRotation, finalCameraRotation, ((float)animationStep) / animationLength);
            
            if (animationStep == animationLength) {
                doingAnimation = false;
                if (!piloting) {
                    playerController.enabled = true;
                }
            }
        }

        if (!shipController.hovering) {
            doorCollider.enabled = !doorOpen;
            rampCollider.enabled = doorOpen;

            rampStage += doorOpen ? 1 : -1;
            rampStage = Mathf.Clamp(rampStage, 0, 10);

            rampObject.transform.localPosition = rampStartPosition + rampPositionStep * rampStage;
            rampObject.transform.localRotation = Quaternion.Slerp(rampStartRotation, rampFinalRotation, rampStage / 10);

            Vector3 interactionPosition = transform.TransformPoint(doorCollider.center - Vector3.up * doorCollider.size.y / 2f);
            InteractionHandler.AddInteractionIfInRange(ChangeDoorState, doorOpen ? "Close Ship Door" : "Open Ship Door", Controls.doorKey, interactionPosition, minDoorDistance);
        }
    }

    private void StartPiloting() {
        //setting up animation stuff
        Camera.main.transform.parent = gameObject.transform;
        cameraPositionChange = (shipCameraPosition - Camera.main.transform.localPosition) / animationLength;
        finalCameraRotation = Quaternion.AngleAxis(0f, Vector3.up);
        startCameraRotation = Camera.main.transform.localRotation;
        doingAnimation = true;
        animationStep = 0;

        player.SetActive(false);

        //shouldn't leave the planet with an open door!
        Close();
        //turn on the ship
        shipController.piloted = true;
        piloting = true;
    }

    public void StopPiloting() {
        if (piloting) {
            player.SetActive(true);

            //setting up the player's position
            //where the player should be started based on the floor collider
            player.transform.position = transform.TransformPoint(new Vector3(-1.2f, 1.37f, chairPosition.z));
            //setting player velocity
            playerController.rb.velocity = shipController.RigidBody.velocity;

            //animation stuff
            playerController.ResetCameraParent();
            cameraPositionChange = (playerController.FPSCameraPosition - Camera.main.transform.localPosition) / animationLength;
            finalCameraRotation = playerController.FPSCameraRotation;
            startCameraRotation = Camera.main.transform.localRotation;
            doingAnimation = true;
            animationStep = 0;

            //have to do this so the FPS camera rotation control doesn't take priority over the animation
            //gets turned on at the end of the animation
            playerController.enabled = false;

            //turn off the ship
            shipController.piloted = false;
            piloting = false;
        }
    }

    private void LeaveShip() {
        playerController.transform.position = transform.TransformPoint(Vector3.down * 6);
    }

    private void ChangeDoorState() {
        doorOpen = !doorOpen;
    }

    public void Close() {
        doorOpen = false;
    }
}