using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script is unnecessary now that I found the 3rd party bloom script.
//keeping it here in case I ever decide to do a custom star shader
[ExecuteAlways]
public class StarShaderDataPasser : MonoBehaviour {
    [Min(0)]
    public float coronaSize;

    Material starMaterial;
    // Start is called before the first frame update
    void Start() {
        if (Application.isPlaying) {
            starMaterial = gameObject.GetComponent<MeshRenderer>().material;
        } else {
            starMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        }
    }

    // Update is called once per frame
    void Update() {
        starMaterial.SetVector("center", new Vector4(transform.position.x, transform.position.y, transform.position.z));
        starMaterial.SetFloat("coronaRadius", transform.localScale.x / 2);
        starMaterial.SetFloat("starRadius", Mathf.Max((transform.localScale.x / 2) - coronaSize, 0));
    }
}
