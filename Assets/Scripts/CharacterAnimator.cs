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
        float speedPercent = controller.getSpeed() / controller.runSpeed;
        animator.SetFloat("speedPercent", speedPercent, locomotionAnimationSmoothTime, Time.deltaTime);
        animator.SetBool("grounded", controller.grounded);
    }
}
