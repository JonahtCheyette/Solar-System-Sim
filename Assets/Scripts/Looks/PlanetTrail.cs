using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTrail : MonoBehaviour {
    [Min(0)]
    public int length;
    private LineRenderer trail;
    private Vector3[] tempPositions;
    [Range(0, 1)]
    public float strength;

    void Start() {
        Color planetCol = GetComponent<CelestialBody>().color;
        trail = GetComponent<LineRenderer>();
        trail.startWidth = transform.localScale.x;
        trail.endWidth = transform.localScale.x;
        trail.endColor = Color.black;
        trail.startColor = new Color(planetCol.r * strength, planetCol.g * strength, planetCol.b * strength);
        trail.positionCount = 0;
    }

    // Update is called once per frame
    void Update() {
        if(trail.positionCount < length) {
            tempPositions = new Vector3[trail.positionCount + 1];
            trail.positionCount += 1;
        }
        tempPositions[0] = transform.position;
        for (int i = 1; i < tempPositions.Length; i++) {
            tempPositions[i] = trail.GetPosition(i - 1);
        }
        trail.SetPositions(tempPositions);
        //print(tempPositions.Length);
    }
}
