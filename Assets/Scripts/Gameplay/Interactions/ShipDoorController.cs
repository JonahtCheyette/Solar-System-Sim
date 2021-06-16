using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDoorController : MonoBehaviour {
    public bool open;
    private BoxCollider doorCollider;

    // Start is called before the first frame update
    void Start() {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        float maxZcoord = float.MinValue;

        foreach(BoxCollider c in colliders) {
            if(c.center.z > maxZcoord) {
                //this collider is farther back
                maxZcoord = c.center.z;
                doorCollider = c;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        doorCollider.enabled = !open;
    }
}
