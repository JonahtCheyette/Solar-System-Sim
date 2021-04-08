using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAnimator : MonoBehaviour {
    Animator animator;
    public bool open;

    void Start() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        animator.SetBool("doorIsOpen", open);
    }
}
