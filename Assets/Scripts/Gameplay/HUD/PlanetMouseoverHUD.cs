using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlanetMouseoverHUD {
    //planned HUD:
    //ring around planet that has mouseover
    //arrow in direction of movement, porportional to speed of movement (will need a maximum length)
    //ring is slightly see-through
    //ring shrinks and is brighter if locked on
    //lock on color?
    //text at top saying name of planet, distance, relative velocity (on plane perpendicular to direction planet is being viewed on), relative velocity (in the direction the planet is being viewed from, red if planet is going further away, blue otherwise)
    //need a more elegant system for relative velocity than the 2 numbers
    private static Color lockedOnColor = Color.blue;
    private static Color regularColor = Color.yellow;
    
    private static Material Mat;
    private static float displayDistFromSurfaceOfPlanetInPixels = 25;
    private static float thicknessInPixels = 8;
    private static int HUDRes = 50;
    //the range of distances where the HUD will fade out as you approach the planet
    public static Vector2 fadeOutRange = new Vector2(400, 800);

    private static Camera playerCam;
    private static Mesh lockOnMesh;

    private static MaterialPropertyBlock materialProperties;

    //make a mesh at the Planet's location, then display it on the screen
    public static void DrawPlanetHUD(CelestialBody body, bool lockedOn) {
        Initialize();

        Vector3 dirToPlayer = (playerCam.transform.position - body.Position).normalized;

        CreateLockOnMesh(body, body.Position);
        DrawMesh(body, dirToPlayer, body.Position, lockedOn);
    }

    private static void Initialize() {
        if (materialProperties == null) {
            materialProperties = new MaterialPropertyBlock();
        }
        if (playerCam == null) {
            playerCam = Camera.main;
        }
        if (lockOnMesh == null) {
            lockOnMesh = new Mesh();
        }
        if (Mat == null) {
            Mat = new Material(Shader.Find("Unlit/PlanetHUD"));
        }
    }

    private static void CreateLockOnMesh(CelestialBody body, Vector3 meshPosition) {
        //since we're drawing the circle just behind the actual planet, we need to scale the size so it appears the same at any distance
        //divide a measurement (in pixels) by this to get how large it should be in units to display that large
        float pixelsPerUnit = (playerCam.WorldToScreenPoint(meshPosition) - playerCam.WorldToScreenPoint(meshPosition + playerCam.transform.up)).magnitude;
        float dstScaledByDistance = displayDistFromSurfaceOfPlanetInPixels / pixelsPerUnit;
        float thicknessScaledByDistance = thicknessInPixels / pixelsPerUnit;

        float innerRadius = body.radius + dstScaledByDistance;
        float outerRadius = innerRadius + thicknessScaledByDistance;

        int numSegments = Mathf.Max(8, HUDRes);
        float angleIncrement = 2 * Mathf.PI / numSegments;

        Vector3[] verts = new Vector3[numSegments * 2];
        int[] tris = new int[6 * numSegments]; // 3 * (2 * numSegments). 2 * numsegments is the actual # of triangles

        //constructing the mesh
        for (int i = 0; i < numSegments; i++) {
            //vertices
            float pointAngle = i * angleIncrement;
            Vector3 dir = new Vector3(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));

            verts[i * 2] = dir * innerRadius;
            verts[i * 2 + 1] = dir * outerRadius;
        }

        //creating the triangles
        for (int i = 0; i < verts.Length - 2; i++) {
            if (i % 2 == 1) {// I had to do it this way because unity calculates normals clockwise, and for some reason setting them by hand wasn't working
                tris[i * 3] = i;
                tris[i * 3 + 1] = i + 2;
                tris[i * 3 + 2] = i + 1;
            } else {
                tris[i * 3] = i;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
        }
        tris[(verts.Length - 2) * 3] = verts.Length - 2;
        tris[(verts.Length - 2) * 3 + 1] = verts.Length - 1;
        tris[(verts.Length - 2) * 3 + 2] = 0;

        tris[(verts.Length - 1) * 3] = verts.Length - 1;
        tris[(verts.Length - 1) * 3 + 1] = 1;
        tris[(verts.Length - 1) * 3 + 2] = 0;

        lockOnMesh.vertices = verts;
        lockOnMesh.triangles = tris;
        lockOnMesh.RecalculateNormals();
    }

    private static void DrawMesh(CelestialBody body, Vector3 dirToPlayer, Vector3 meshPosition, bool lockedOn) {
        Quaternion rot = Quaternion.LookRotation(dirToPlayer, playerCam.transform.up);

        float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - body.transform.position).magnitude - body.radius));
        //Debug.Log(alpha);
        Color HUDColor = lockedOn ? lockedOnColor : regularColor;

        HUDColor.a = alpha;
        materialProperties.SetColor("_Color", HUDColor);

        Graphics.DrawMesh(lockOnMesh, meshPosition, rot, Mat, 0, null, 0, materialProperties, false, false, false);
    }
}
