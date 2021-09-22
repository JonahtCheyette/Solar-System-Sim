using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunShadowCaster : MonoBehaviour {
    private Transform trackedObject;

    void Start() {
        trackedObject = Camera.main.transform;
    }

    void LateUpdate() {
        transform.LookAt(trackedObject);
    }
}
