using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetMouseoverHUD : MonoBehaviour {
    //planned HUD:
    //circle around planet that has mouseover
    //arrow in direction of movement, porportional to speed of movement (will need a maximum length)
    //cirlce and arrow are yellow if not locked on, blue if locked on (need to think about color choice)
    //white text at top saying name of planet, distance, relative velocity (on plane perpendicular to direction planet is being viewed on), relative velocity (in the direction the planet is being viewed from, red if planet is going further away, blue otherwise)
    //need a more elegant system for relative velocity than the 2 numbers
    public Material Mat;
    [Min(0)]
    public float thickness;
    public int HUDRes = 50;
    [Range(0, 90)]
    public float segmentLength;

    Camera cam;
    Mesh lockOnMesh;


    //make a mesh at the Planet's location, then display it on the screen
    public void DrawPlanetHUD(CelestialBody body) {
        if (cam == null) {
            cam = Camera.main;
        }
        if(lockOnMesh == null) {
            lockOnMesh = new Mesh();
        }

        Vector3 bodyPosition = body.transform.position;
        //since we're drawing the mesh at the actual planet, we need to scale the thickness so it appears the same at any distance
        float pixelsPerUnit = (cam.WorldToScreenPoint(bodyPosition) - cam.WorldToScreenPoint(bodyPosition + cam.transform.up)).magnitude;
        float thicknessScaledByDistance = thickness / pixelsPerUnit;

        float innerRadius = body.radius * 1.2f;
        float outerRadius = innerRadius + thicknessScaledByDistance;

        //num increments is the number of points on either the inside or outside edge of one of the segments of the HUD
        int numIncrements = Mathf.Max(5, HUDRes);
        float angleIncrement = segmentLength/(numIncrements-1);
        Vector3[] verts = new Vector3[2 * numIncrements];
        int[] tris = new int[6 * (numIncrements - 1)];
        //constructing the mesh
        for (int i = 0; i < numIncrements; i++) {
            float pointAngle = (segmentLength / 2f) + (i * angleIncrement) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));

            verts[i * 2] = dir * innerRadius;
            verts[i * 2 + 1] = dir * outerRadius;

            if(i < numIncrements - 1) {
                tris[i * 6] = i * 2;
                tris[i * 6 + 1] = i * 2 + 1;
                tris[i * 6 + 2] = i * 2 + 2;

                tris[i * 6 + 3] = i * 2 + 1;
                tris[i * 6 + 4] = i * 2 + 2;
                tris[i * 6 + 5] = i * 2 + 3;
            }
        }

        lockOnMesh.vertices = verts;
        lockOnMesh.triangles = tris;
    }
}
