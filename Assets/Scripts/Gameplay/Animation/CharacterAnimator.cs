using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour {

    const float locomotionAnimationSmoothTime = 0.1f;

    Animator animator;
    FirstPersonController controller;

    void Start() {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<FirstPersonController>();
    }

    // Update is called once per frame
    void Update() {
        Vector2 inputs = controller.GetGroundSpeed() / controller.runSpeed;
        animator.SetFloat("forward", inputs.y, locomotionAnimationSmoothTime, Time.deltaTime);
        animator.SetFloat("right", inputs.x, locomotionAnimationSmoothTime, Time.deltaTime);
        animator.SetBool("grounded", controller.Grounded);
    }
}
