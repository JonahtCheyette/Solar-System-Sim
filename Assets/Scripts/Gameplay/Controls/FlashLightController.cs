using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLightController : MonoBehaviour {
    private Light flashLight;

    // Start is called before the first frame update
    private void Start() {
        flashLight = transform.Find("Flash Light").GetComponent<Light>();
        flashLight.enabled = false;
        flashLight.transform.localEulerAngles = Camera.main.transform.localEulerAngles - Vector3.up * 180;
    }

    // Update is called once per frame
    private void Update() {
        if (Input.GetKeyDown(Controls.flashlightKey)) {
            flashLight.enabled = !flashLight.enabled;
        }
    }
}
