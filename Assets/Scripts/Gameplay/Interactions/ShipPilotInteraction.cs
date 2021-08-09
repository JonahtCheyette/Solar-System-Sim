using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipPilotInteraction : MonoBehaviour {
    public float minInteractionDist = 5f;
    private GameObject player;
    private Vector3 cameraPosition;
    private ShipController shipController;
    private ShipDoorController shipDoor;
    private Vector3 chairPosition = new Vector3(0, 2.21f, -2.46f);
    private FirstPersonController playerController;

    // Start is called before the first frame update
    void Start() {
        player = GameObject.Find("Player");
        shipController = GetComponent<ShipController>();
        shipController.enabled = false;
        shipDoor = GetComponent<ShipDoorController>();

        //find where the camera should be placed by finding the furthest forward collider on the ship
        BoxCollider[] colliders = transform.Find("Colliders").gameObject.GetComponents<BoxCollider>();
        cameraPosition = Vector3.forward * float.MaxValue;

        foreach (BoxCollider c in colliders) {
            if (c.center.z < cameraPosition.z) {
                //this collider is further forward
                cameraPosition = c.center;
            }
        }

        playerController = player.GetComponent<FirstPersonController>();
    }

    // Update is called once per frame
    void Update() {
        Vector3 interactionPosition = transform.TransformPoint(chairPosition);
        if (player.activeInHierarchy) {
            InteractionHandler.AddInteractionIfInRange(StartPiloting, "Start Piloting The Spaceship", KeyCode.P, interactionPosition, minInteractionDist);
        } else {
            InteractionHandler.AddInteractionIfInRange(StopPiloting, "Stop Piloting The Spaceship", KeyCode.P, interactionPosition, minInteractionDist + 300f);
        }
    }

    private void StartPiloting() {
        Camera.main.transform.parent = gameObject.transform;
        Camera.main.transform.localPosition = cameraPosition;
        Camera.main.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
        shipController.enabled = true;
        shipDoor.CloseDoor();
        player.SetActive(false);
    }

    private void StopPiloting() {
        player.SetActive(true);
        player.transform.rotation = transform.rotation * Quaternion.AngleAxis(180f, Vector3.up);
        //where the player should be started based on the floor collider
        player.transform.position = transform.TransformPoint(new Vector3(1.2f, 1.37f, chairPosition.z));
        playerController.ResetCamera();
        shipController.enabled = false;
    }
}