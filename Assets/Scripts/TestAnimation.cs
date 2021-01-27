using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAnimation : MonoBehaviour {
    Animator animator;
    public bool open;

    void Start() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        animator.SetBool("doorIsOpen", open);
    }

    private void OnValidate() {
        if (Application.isPlaying) {
            //animator.SetBool("doorIsOpen", open);
        }
    }
}
