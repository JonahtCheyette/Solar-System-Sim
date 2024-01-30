using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seed : MonoBehaviour
{
    public LayerMask mask;
    public GameObject treePrefab;

    void OnCollisionEnter(Collision collision) {
        int stupid = 1;//have to do it this way because mask.value returns a bitmap
        for (int i = 0; i < collision.gameObject.layer; i++) {
            stupid *= 2;
        }
        if (stupid == mask.value) {
            Instantiate(treePrefab, collision.GetContact(0).point, transform.rotation);
            Destroy(gameObject);
        }
    }
}
