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
    public Color lockedOnColor = Color.blue;
    public Color regularColor = Color.yellow;
    
    public Material Mat;
    [Min(0)]
    public float displayDistFromSurfaceOfPlanet;
    public int HUDRes = 50;
    //the range of distances where the HUD will fade out as you approach the planet
    public Vector2 fadeOutRange;

    Camera playerCam;
    Mesh lockOnMesh;

    MaterialPropertyBlock materialProperties;


    //make a mesh at the Planet's location, then display it on the screen
    public void DrawPlanetHUD(CelestialBody body) {
        if (materialProperties == null) {
            materialProperties = new MaterialPropertyBlock();
        }
        if (playerCam == null) {
            playerCam = Camera.main;
        }
        if(lockOnMesh == null) {
            lockOnMesh = new Mesh();
        }

        Vector3 bodyPosition = body.transform.position;
        //since we're drawing the mesh just behind the actual planet, we need to scale the thickness so it appears the same at any distance
        Vector3 dirToPlayer = (playerCam.transform.position - bodyPosition).normalized;
        Vector3 meshPosition = bodyPosition - dirToPlayer * body.radius * 1.2f;
        float pixelsPerUnit = (playerCam.WorldToScreenPoint(meshPosition) - playerCam.WorldToScreenPoint(meshPosition + playerCam.transform.up)).magnitude;
        float thicknessScaledByDistance = displayDistFromSurfaceOfPlanet / pixelsPerUnit;

        float circleRadius = body.radius * 1.2f + thicknessScaledByDistance;

        int numPoints = Mathf.Max(15, HUDRes);
        float angleIncrement = 2*Mathf.PI/numPoints;

        Vector3[] verts = new Vector3[numPoints];
        int[] tris = new int[3 * (numPoints - 2)];
        
        //constructing the mesh
        for (int i = 0; i < numPoints; i++) {
            //vertices
            float pointAngle = i * angleIncrement;
            Vector3 dir = new Vector3(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));

            verts[i] = dir * circleRadius;
        }

        //now for triangles
        //array to hold the vertex indicies in the order, 0, numPoints-1, 1, numPoints-2, 2, numPoints-3 order the triangles will be constructed from
        int[] orderedVertexIndicies = new int[numPoints];
        //because we're working with ints, the decimal gets dropped from the result of the division
        for (int i = 1; i < numPoints/2 + 1; i++) {
            orderedVertexIndicies[i * 2 - 1] = numPoints - i;
            if(i * 2 <= numPoints) {
                orderedVertexIndicies[i * 2] = i;
            }
        }
        //creating the triangles
        for (int i = 0; i < numPoints - 2; i++) {
            tris[i * 3] = orderedVertexIndicies[i];
            tris[i * 3 + 1] = orderedVertexIndicies[i + 1];
            tris[i * 3 + 2] = orderedVertexIndicies[i + 2];
        }

        lockOnMesh.vertices = verts;
        lockOnMesh.triangles = tris;

        Quaternion rot = Quaternion.LookRotation(dirToPlayer, playerCam.transform.up);

        float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - bodyPosition).magnitude - body.radius));
        Color HUDColor = lockedOnColor;

        HUDColor.a = alpha;
        materialProperties.SetColor("_Color", HUDColor);

        Graphics.DrawMesh(lockOnMesh, meshPosition, rot, Mat, 0, null, 0, materialProperties, false, false, false);
    }
}
