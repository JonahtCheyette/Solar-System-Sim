using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this, as well as the sunShadowCaster class is ripped from Sebastian Lague
//but there's really only so many ways to get the sunlight effect
//when unity's point light shadows are so shit (especially over long distances, like what I'm doing now)
public class AmbientSunlight : MonoBehaviour {
    public float maxIntensity = 1;

    private SunShadowCaster sunlight;
    private Transform playerCamT;
    private Light ambient;

    // Start is called before the first frame update
    void Start() {
        sunlight = FindObjectOfType<SunShadowCaster>();
        ambient = GetComponent<Light>();
        playerCamT = Camera.main.transform;
        transform.rotation = CalculateAmbientLightRot();
    }

    void LateUpdate() {
        transform.rotation = CalculateAmbientLightRot();
        float alignmentWithSunlight = Vector3.Dot(sunlight.transform.forward, transform.forward);
        //this next bit makes the ambient light have intensity 0 if it's perpendicular to the sunlight, and 1 if it's parallel
        float alignmentClamped = Mathf.Clamp01(alignmentWithSunlight);
        float intensityMultiplier = Mathf.Clamp01((alignmentClamped - 0.5f) * 2f);
        ambient.intensity = maxIntensity * intensityMultiplier;
    }

    private Quaternion CalculateAmbientLightRot() {
        CelestialBody closestBodyToPlanet = GravityHandler.GetClosestPlanet(playerCamT.position);
        Vector3 targetDir = (closestBodyToPlanet.Position - playerCamT.position).normalized;
        return Quaternion.LookRotation(targetDir);
    }
}
