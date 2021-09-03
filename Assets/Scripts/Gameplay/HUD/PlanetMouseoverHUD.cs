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
    private static Color lockedOnColor = Color.white;
    private static Color regularColor = Color.grey;
    private static float regularAlpha = 0.75f;
    
    private static Material Mat;
    private static float displayDistFromSurfaceOfPlanetInPixels = 25f;
    private static float thicknessInPixels = 8f;
    private static int HUDRes = 50;
    //the range of distances where the HUD will fade out as you approach the planet
    public static Vector2 fadeOutRange = new Vector2(400, 800);

    private static Camera playerCam;
    //we have to have meshes for both locked on and regular modes, because of the weird way Graphics.DrawMesh works with not drawing the mesh immediately
    private static Mesh lockedOnMesh;
    private static Mesh regularMesh;

    private static MaterialPropertyBlock materialProperties;

    private static float arrowThicknessInPixels = 8f;
    private static float arrowheadLengthInPixels = 16f;
    private static float arrowheadThicknessInPixels = 12;
    private static float maxArrowLengthInPixels = 200f;

    //make a mesh at the Planet's location, then display it on the screen
    public static void DrawPlanetHUD(CelestialBody body, Vector3 playerVelocity, bool lockedOn) {
        Initialize();
        if((playerCam.transform.position - body.transform.position).magnitude - body.radius < fadeOutRange.x) {
            //if we're close enough to the planet that the mesh is even gonna be drawn, otherwise why bother
            Vector3 dirToPlayer = (playerCam.transform.position - body.Position).normalized;
            Vector3 relativeVelocity = body.RigidBody.velocity - playerVelocity;
            CreateMesh(body, relativeVelocity, lockedOn);
            DrawMesh(body, dirToPlayer, relativeVelocity, lockedOn);
        }
    }

    private static void Initialize() {
        if (materialProperties == null) {
            materialProperties = new MaterialPropertyBlock();
        }
        if (playerCam == null) {
            playerCam = Camera.main;
        }
        if (lockedOnMesh == null) {
            lockedOnMesh = new Mesh();
        }
        if (regularMesh == null) {
            regularMesh = new Mesh();
        }
        if (Mat == null) {
            Mat = new Material(Shader.Find("Unlit/PlanetHUD"));
        }
    }

    private static void CreateMesh(CelestialBody body, Vector3 relativeVelocity, bool lockedOn) {
        Vector2 relativeOrthagonalVelocity = new Vector2(Vector3.Dot(relativeVelocity, playerCam.transform.right), Vector3.Dot(relativeVelocity, playerCam.transform.up));
        //since we're drawing the ring around the actual planet, we need to scale the size so it appears the same at any distance
        //divide a measurement (in pixels) by this to get how large it should be in units to display that large
        float pixelsPerUnit = (playerCam.WorldToScreenPoint(body.Position) - playerCam.WorldToScreenPoint(body.Position + playerCam.transform.up)).magnitude;
        float dstScaledByDistance = displayDistFromSurfaceOfPlanetInPixels / pixelsPerUnit;
        if (lockedOn) {
            dstScaledByDistance /= 2f;
        }
        float thicknessScaledByDistance = thicknessInPixels / pixelsPerUnit;
        float innerRadius = body.radius + dstScaledByDistance;
        float outerRadius = innerRadius + thicknessScaledByDistance;
        float arrowLengthInPixels = relativeOrthagonalVelocity.magnitude;
        float arrowThickness = arrowThicknessInPixels / pixelsPerUnit;
        float arrowheadThickness = arrowheadThicknessInPixels / pixelsPerUnit;
        float arrowheadLength = Mathf.Min(arrowheadLengthInPixels, arrowLengthInPixels) / pixelsPerUnit;
        float length = Mathf.Min(arrowLengthInPixels, maxArrowLengthInPixels) / pixelsPerUnit;

        int numSegments = Mathf.Max(8, HUDRes);
        float angleIncrement = 2 * Mathf.PI / numSegments;

        Vector3[] verts = new Vector3[numSegments * 2 + 7];
        // 3 * (2 * numSegments). 2 * numsegments is the actual # of triangles in the ring, the last 4 triangles are for the arrow
        int[] tris = new int[6 * numSegments + 12];

        //constructing the mesh
        for (int i = 0; i < numSegments; i++) {
            //vertices
            float pointAngle = i * angleIncrement;
            Vector3 dir = new Vector3(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));

            verts[i * 2] = dir * innerRadius;
            verts[i * 2 + 1] = dir * outerRadius;
        }

        //arrow time
        int startIndex = numSegments * 2;
        Vector3 arrowBaseOffset = (verts[3] - verts[1]).normalized * arrowThickness / (2f * Mathf.Cos(angleIncrement / 2f));
        //the base
        verts[startIndex] = verts[1] + arrowBaseOffset;
        verts[startIndex + 1] = new Vector3(verts[startIndex].x, -verts[startIndex].y);

        //the shaft
        verts[startIndex + 2] = new Vector3(verts[1].x + length - arrowheadLength, verts[startIndex].y);
        verts[startIndex + 3] = new Vector3(verts[startIndex + 2].x, -verts[startIndex].y);

        //the base of the arrowhead
        verts[startIndex + 4] = new Vector3(verts[startIndex + 2].x, (arrowThickness + arrowheadThickness) / 2f);
        verts[startIndex + 5] = new Vector3(verts[startIndex + 2].x, -verts[startIndex + 4].y);

        // the tip
        verts[startIndex + 6] = new Vector3(verts[1].x + length, 0);

        //creating the triangles
        for (int i = 0; i < verts.Length - 9; i++) {
            if (i % 2 == 1) {// I had to split it this way because unity calculates normals clockwise, and for some reason setting them by hand wasn't working
                tris[i * 3] = i;
                tris[i * 3 + 1] = i + 2;
                tris[i * 3 + 2] = i + 1;
            } else {
                tris[i * 3] = i;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
        }
        //whee, hard coding! I hope this doesn't bite me in the ass
        //the code to do the last 2 triangles that complete the ring
        tris[(verts.Length - 9) * 3] = verts.Length - 9;
        tris[(verts.Length - 9) * 3 + 1] = verts.Length - 8;
        tris[(verts.Length - 9) * 3 + 2] = 0;

        tris[(verts.Length - 8) * 3] = verts.Length - 8;
        tris[(verts.Length - 8) * 3 + 1] = 1;
        tris[(verts.Length - 8) * 3 + 2] = 0;

        //creating the triangles for the arrow
        //gotta love hard coding, but there is no better way to do this
        //these triangles make the arrow
        tris[tris.Length - 12] = 1;
        tris[tris.Length - 11] = startIndex + 2;
        tris[tris.Length - 10] = startIndex;
        tris[tris.Length - 9] = 1;
        tris[tris.Length - 8] = startIndex + 3;
        tris[tris.Length - 7] = startIndex + 2;
        tris[tris.Length - 6] = 1;
        tris[tris.Length - 5] = startIndex + 1;
        tris[tris.Length - 4] = startIndex + 3;
        tris[tris.Length - 3] = startIndex + 4;
        tris[tris.Length - 2] = startIndex + 5;
        tris[tris.Length - 1] = startIndex + 6;

        if (lockedOn) {
            lockedOnMesh.vertices = verts;
            lockedOnMesh.triangles = tris;
            lockedOnMesh.RecalculateNormals();
        } else {
            regularMesh.vertices = verts;
            regularMesh.triangles = tris;
            regularMesh.RecalculateNormals();
        }
    }

    private static void DrawMesh(CelestialBody body, Vector3 dirToPlayer, Vector3 relativeVelocity, bool lockedOn) {
        Vector3 worldOrthagonalVelocity = relativeVelocity - Vector3.Project(relativeVelocity, playerCam.transform.forward);
        Quaternion rot = Quaternion.AngleAxis(90, dirToPlayer) * Quaternion.LookRotation(dirToPlayer, worldOrthagonalVelocity.normalized);

        float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - body.transform.position).magnitude - body.radius));
        if (!lockedOn) {
            alpha *= regularAlpha;
        }
        Color HUDColor = lockedOn ? lockedOnColor : regularColor;

        HUDColor.a = alpha;
        materialProperties.SetColor("_Color", HUDColor);

        Graphics.DrawMesh(lockedOn ? lockedOnMesh : regularMesh, body.Position, rot, Mat, 0, null, 0, materialProperties, false, false, false);
    }
}
