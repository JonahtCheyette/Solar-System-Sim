using UnityEngine;

[ExecuteAlways]
public class PlanetGradientDataPasser : MonoBehaviour {
    Material mat;
    bool applicationWasPlaying = false;

    // Update is called once per frame
    void Update() {
        if (mat == null) {
            if (Application.isPlaying) {
                mat = GetComponent<MeshRenderer>().material;
            } else {
                mat = GetComponent<MeshRenderer>().sharedMaterial;
            }
        } else {
            if (applicationWasPlaying && !Application.isPlaying) {
                //just became edit mode
                mat = GetComponent<MeshRenderer>().sharedMaterial;
            } else if (!applicationWasPlaying && Application.isPlaying) {
                //just started playing
                mat = GetComponent<MeshRenderer>().material;
            }
        }
        mat.SetFloat("minY", transform.position.y - transform.localScale.y);
        mat.SetFloat("maxY", transform.position.y + transform.localScale.y);
        applicationWasPlaying = Application.isPlaying;
    }
}
