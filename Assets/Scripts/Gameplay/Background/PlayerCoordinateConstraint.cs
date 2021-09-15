using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCoordinateConstraint : MonoBehaviour {
    public float constraintLength = 100000f;

    // Update is called once per frame
    void Update() {
        if(transform.position.magnitude > constraintLength) {
            Vector3 currentPlayerPosition = transform.position;
            GameObject[] objects = FindObjectsOfType<GameObject>();
            foreach(GameObject obj in objects) {
                if (!obj.transform.parent) {
                    obj.transform.position -= currentPlayerPosition;
                }
            }
        }
    }
}
