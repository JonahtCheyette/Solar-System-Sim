using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAnimator : MonoBehaviour {
    private Animator animator;
    private ShipDoorController controller;

    void Start() {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<ShipDoorController>();
    }

    private void Update() {
        animator.SetBool("doorIsOpen", controller.isOpen);
    }
}
