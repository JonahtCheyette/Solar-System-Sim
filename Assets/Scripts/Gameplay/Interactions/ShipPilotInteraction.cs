using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipPilotInteraction : MonoBehaviour {

    private KeyCode interactionKey = KeyCode.X;

    //interaction functionality
    public float minInteractionDist = 5f;
    private GameObject player;
    private Vector3 shipCameraPosition;
    private ShipController shipController;
    private ShipDoorController shipDoor;
    private Vector3 chairPosition = new Vector3(0, 2.21f, 2.46f);
    private FirstPersonController playerController;
    private bool piloting;

    //animation variables
    private bool doingAnimation;
    private int animationStep;
    private Vector3 cameraPositionChange;
    private Quaternion finalCameraRotation;
    private Quaternion startCameraRotation;
    private int animationLength = 15;

    // Start is called before the first frame update
    void Start() {
        player = GameObject.Find("Player");
        shipController = GetComponent<ShipController>();
        shipDoor = GetComponent<ShipDoorController>();

        //find where the camera should be placed by finding the furthest forward collider on the ship
        BoxCollider[] colliders = transform.Find("Colliders").gameObject.GetComponents<BoxCollider>();
        shipCameraPosition = Vector3.forward * float.MinValue;

        foreach (BoxCollider c in colliders) {
            if (c.center.z > shipCameraPosition.z) {
                //this collider is further forward
                shipCameraPosition = c.center;
            }
        }

        playerController = player.GetComponent<FirstPersonController>();
        doingAnimation = false;
        animationStep = 0;
        piloting = false;
    }

    // Update is called once per frame
    void Update() {
        if (!doingAnimation) {
            Vector3 interactionPosition = transform.TransformPoint(chairPosition);
            if (player.activeInHierarchy) {
                InteractionHandler.AddInteractionIfInRange(StartPiloting, "Start Piloting The Spaceship", interactionKey, interactionPosition, minInteractionDist);
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
        shipDoor.Close();
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
}