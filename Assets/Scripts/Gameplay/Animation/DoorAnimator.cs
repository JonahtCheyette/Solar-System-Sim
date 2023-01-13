using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAnimator : MonoBehaviour {
    private Animator animator;
    private ShipPilotInteraction controller;

    void Start() {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<ShipPilotInteraction>();
    }

    private void Update() {
        animator.SetBool("doorIsOpen", controller.doorOpen);
    }
}
