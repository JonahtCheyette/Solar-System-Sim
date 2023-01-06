﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class PlanetRelativeVelocityHUD {
    public static bool displayedLastFrame = false;

    private static Color lockedOnColor = Color.white;
    private static Color regularColor = Color.grey;
    private static float regularAlpha = 0.75f;
    
    private static Material mat;
    private static float displayDistFromSurfaceOfPlanetInPixels = 25f;
    private static float thicknessInPixels = 8f;
    private static int HUDRes = 50;
    //the range of distances where the HUD will fade out as you approach the planet
    public static Vector2 fadeOutRange = new Vector2(400, 800);

    private static Camera playerCam;
    //we have to have meshes for both locked on and regular modes, because of the weird way Graphics.DrawMesh works with not drawing the mesh immediately
    private static Mesh lockedOnMesh;
    private static Mesh regularMesh;
    private static Text lockedOnText;
    private static Text regularText;
    private static Image arrowToLockOnPlanet;
    private static CanvasSizeGetter canvasSizeGetter;

    private static MaterialPropertyBlock materialProperties;

    private static float arrowThicknessInPixels = 8f;
    private static float arrowheadLengthInPixels = 16f;
    private static float arrowheadThicknessInPixels = 12;
    private static float maxArrowLengthInPixels = 200f;

    private static float textDistanceFromRing = 60f;
    private static float arrowTextToleranceAngle = 20f;
    private static float textArrowLengthTolerance = 25f;

    //make a mesh at the Planet's location, then display it on the screen
    public static void DrawPlanetHUD(CelestialBodyPhysics body, Vector3 playerVelocity, bool lockedOn) {
        Initialize();
        if (lockedOn && !body.gameObject.GetComponent<MeshRenderer>().isVisible) {
            DrawArrowToPlanet(body);
            HideRelevantText(true);
        } else {
            float radius = body.Radius();
            if ((playerCam.transform.position - body.transform.position).magnitude - radius >= fadeOutRange.x) {
                //if we're close enough to the planet that the mesh is even gonna be drawn, otherwise why bother
                Vector3 dirToPlayer = (playerCam.transform.position - body.Position).normalized;
                Vector3 relativeVelocity = body.RigidBody.velocity - playerVelocity;
                //since we're drawing the ring around the actual planet, we need to scale the size so it appears the same at any distance
                //multiply a measurement (in units) by this to get how large it is in pixels
                float pixelsPerUnit = ((Vector2)(playerCam.WorldToScreenPoint(body.Position) - playerCam.WorldToScreenPoint(body.Position + playerCam.transform.up))).magnitude;
                //multiply a measurement (in pixels) by this to get how large it is in units
                float unitsPerPixel = 1 / pixelsPerUnit;

                CreateMesh(body, relativeVelocity, lockedOn, unitsPerPixel);
                DrawMesh(body, dirToPlayer, relativeVelocity, radius, lockedOn);
                DisplayRelativeForwardVelocity(body, relativeVelocity, radius, lockedOn, unitsPerPixel);
            } else {
                HideRelevantText(false);
            }
            HideArrowToPlanet();
        }
    }

    public static void Initialize() {
        if (playerCam == null) {
            playerCam = Camera.main;
        }
        InitializeMeshNecessities();
        InitializeTextBoxes();
        InitializeArrowToPlanet();
        InitializeCanvasSizeGetter();
    }

    private static void InitializeMeshNecessities() {
        if (lockedOnMesh == null) {
            lockedOnMesh = new Mesh();
        }
        if (regularMesh == null) {
            regularMesh = new Mesh();
        }
        if (mat == null) {
            mat = new Material(Shader.Find("Unlit/PlanetHUD"));
        }
        if (materialProperties == null) {
            materialProperties = new MaterialPropertyBlock();
        }
    }

    private static void InitializeArrowToPlanet() {
        if (arrowToLockOnPlanet == null) {
            arrowToLockOnPlanet = GameObject.Find("Arrow To Planet").GetComponent<Image>();
            arrowToLockOnPlanet.rectTransform.pivot = Vector2.one * 0.5f;
            arrowToLockOnPlanet.rectTransform.anchorMin = Vector2.one * 0.5f;
            arrowToLockOnPlanet.rectTransform.anchorMax = Vector2.one * 0.5f;
            arrowToLockOnPlanet.enabled = false;
        }
    }

    private static void InitializeTextBoxes() {
        if (lockedOnText == null) {
            lockedOnText = GameObject.Find("Locked On Planet Info Text").GetComponent<Text>();
            lockedOnText.text = "";
            lockedOnText.fontSize = 44;
            lockedOnText.color = lockedOnColor;
            lockedOnText.alignment = TextAnchor.MiddleCenter;
            lockedOnText.rectTransform.sizeDelta = new Vector2(600, 102);
            lockedOnText.rectTransform.pivot = Vector2.one * 0.5f;
            lockedOnText.rectTransform.anchorMin = Vector2.zero;
            lockedOnText.rectTransform.anchorMax = Vector2.zero;
        }
        if (regularText == null) {
            regularText = GameObject.Find("Regular Planet Info Text").GetComponent<Text>();
            regularText.text = "";
            regularText.fontSize = 44;
            Color col = regularColor;
            col.a = regularAlpha;
            regularText.color = col;
            regularText.alignment = TextAnchor.MiddleCenter;
            regularText.rectTransform.sizeDelta = new Vector2(600, 102);
            regularText.rectTransform.pivot = Vector2.one * 0.5f;
            regularText.rectTransform.anchorMin = Vector2.zero;
            regularText.rectTransform.anchorMax = Vector2.zero;
        }
    }

    private static void InitializeCanvasSizeGetter(){
        if(canvasSizeGetter == null) {
            canvasSizeGetter = Object.FindObjectOfType<CanvasSizeGetter>();
        }
    }

    private static void CreateMesh(CelestialBodyPhysics body, Vector3 relativeVelocity, bool lockedOn, float unitsPerPixel) {
        int numSegments = Mathf.Max(8, HUDRes);
        int arrowStartIndex = numSegments * 2;

        Vector3[] verts = CreateVerts(body, relativeVelocity, lockedOn, numSegments, arrowStartIndex, unitsPerPixel);
        int[] tris = CreateTris(verts.Length, numSegments, arrowStartIndex);

        AssignMeshVertsAndTris(verts, tris, lockedOn);
    }

    private static Vector3[] CreateVerts(CelestialBodyPhysics body, Vector3 relativeVelocity, bool lockedOn, int numSegments, int arrowStartIndex, float unitsPerPixel) {
        Vector2 relativeOrthagonalVelocity = new Vector2(Vector3.Dot(relativeVelocity, playerCam.transform.right), Vector3.Dot(relativeVelocity, playerCam.transform.up));
        float dstScaledByDistance = displayDistFromSurfaceOfPlanetInPixels * unitsPerPixel;
        if (lockedOn) {
            dstScaledByDistance /= 2f;
        }
        float thicknessScaledByDistance = thicknessInPixels * unitsPerPixel;
        float innerRadius = body.Radius() + dstScaledByDistance;
        float outerRadius = innerRadius + thicknessScaledByDistance;
        float arrowLengthInPixels = relativeOrthagonalVelocity.magnitude;
        float arrowThickness = arrowThicknessInPixels * unitsPerPixel;
        float arrowheadThickness = arrowheadThicknessInPixels * unitsPerPixel;
        float arrowheadLength = Mathf.Min(arrowheadLengthInPixels, arrowLengthInPixels) * unitsPerPixel;
        float length = Mathf.Min(arrowLengthInPixels, maxArrowLengthInPixels) * unitsPerPixel;
        float angleIncrement = 2 * Mathf.PI / numSegments;

        Vector3[] verts = new Vector3[numSegments * 2 + 7];

        //constructing the mesh
        for (int i = 0; i < numSegments; i++) {
            //vertices
            float pointAngle = i * angleIncrement;
            Vector3 dir = new Vector3(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));

            verts[i * 2] = dir * innerRadius;
            verts[i * 2 + 1] = dir * outerRadius;
        }

        //arrow time
        Vector3 arrowBaseOffset = (verts[3] - verts[1]).normalized * arrowThickness / (2f * Mathf.Cos(angleIncrement / 2f));
        //the base
        verts[arrowStartIndex] = verts[1] + arrowBaseOffset;
        verts[arrowStartIndex + 1] = new Vector3(verts[arrowStartIndex].x, -verts[arrowStartIndex].y);

        //the shaft
        verts[arrowStartIndex + 2] = new Vector3(verts[1].x + length - arrowheadLength, verts[arrowStartIndex].y);
        verts[arrowStartIndex + 3] = new Vector3(verts[arrowStartIndex + 2].x, -verts[arrowStartIndex].y);

        //the base of the arrowhead
        verts[arrowStartIndex + 4] = new Vector3(verts[arrowStartIndex + 2].x, (arrowThickness + arrowheadThickness) / 2f);
        verts[arrowStartIndex + 5] = new Vector3(verts[arrowStartIndex + 2].x, -verts[arrowStartIndex + 4].y);

        // the tip
        verts[arrowStartIndex + 6] = new Vector3(verts[1].x + length, 0);

        return verts;
    }

    private static int[] CreateTris(int vertsLength, int numSegments, int arrowStartIndex) {
        // 3 * (2 * numSegments). 2 * numsegments is the actual # of triangles in the ring, the last 4 triangles are for the arrow
        int[] tris = new int[6 * numSegments + 12];

        //creating the triangles
        //doing the ring triangles
        //essentially the same as creating a rectangle, just over and over again
        for (int i = 0; i < vertsLength - 9; i++) {
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
        //these two triangles link the last 2 vertices in the ring to the first 2
        tris[(vertsLength - 9) * 3] = vertsLength - 9;
        tris[(vertsLength - 9) * 3 + 1] = vertsLength - 8;
        tris[(vertsLength - 9) * 3 + 2] = 0;

        tris[(vertsLength - 8) * 3] = vertsLength - 8;
        tris[(vertsLength - 8) * 3 + 1] = 1;
        tris[(vertsLength - 8) * 3 + 2] = 0;

        //creating the triangles for the arrow
        //gotta love hard coding, but there is no better way to do this
        //these triangles make the arrow
        /*
         *           F\
         * B_________| \
         *  \        D  \
         *   A           >H
         *  /        E  /
         * C¯¯¯¯¯¯¯¯¯| /
         *           G/
         * table of Points to indecies
         * A = 1
         * B = arrowStartIndex
         * C = arrowStartIndex + 1
         * D = arrowStartIndex + 2
         * E = arrowStartIndex + 3
         * F = arrowStartIndex + 4
         * G = arrowStartIndex + 5
         * H = arrowStartIndex + 6
         * The code below makes traingles ADB, AED, ACE, and FGH in that order 
         */
        tris[tris.Length - 12] = 1;
        tris[tris.Length - 11] = arrowStartIndex + 2;
        tris[tris.Length - 10] = arrowStartIndex;
        tris[tris.Length - 9] = 1;
        tris[tris.Length - 8] = arrowStartIndex + 3;
        tris[tris.Length - 7] = arrowStartIndex + 2;
        tris[tris.Length - 6] = 1;
        tris[tris.Length - 5] = arrowStartIndex + 1;
        tris[tris.Length - 4] = arrowStartIndex + 3;
        tris[tris.Length - 3] = arrowStartIndex + 4;
        tris[tris.Length - 2] = arrowStartIndex + 5;
        tris[tris.Length - 1] = arrowStartIndex + 6;

        return tris;
    }

    private static void AssignMeshVertsAndTris(Vector3[] verts, int[] tris, bool lockedOn) {
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

    private static void DrawMesh(CelestialBodyPhysics body, Vector3 dirToPlayer, Vector3 relativeVelocity, float radius, bool lockedOn) {
        Vector3 worldOrthagonalVelocity = Vector3.ProjectOnPlane(relativeVelocity, playerCam.transform.forward);
        Quaternion rot = Quaternion.AngleAxis(90, dirToPlayer) * Quaternion.LookRotation(dirToPlayer, worldOrthagonalVelocity.normalized);

        float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - body.transform.position).magnitude - radius));
        if (!lockedOn) {
            alpha *= regularAlpha;
        }
        Color HUDColor = lockedOn ? lockedOnColor : regularColor;

        HUDColor.a = alpha;
        materialProperties.SetColor("_Color", HUDColor);

        Graphics.DrawMesh(lockedOn ? lockedOnMesh : regularMesh, body.Position, rot, mat, 0, null, 0, materialProperties, false, false, false);
    }

    private static void DisplayRelativeForwardVelocity(CelestialBodyPhysics body, Vector3 relativeVelocity, float radius, bool lockedOn, float unitsPerPixel) {
        int relativeForwardVelocity = Mathf.RoundToInt(Vector3.Dot(relativeVelocity, (playerCam.transform.position - body.Position).normalized));
        //positive means it's moving torwards you
        //negative means it's moving away from you

        Vector2 relativeOrthagonalVelocity = new Vector2(Vector3.Dot(relativeVelocity, playerCam.transform.right), Vector3.Dot(relativeVelocity, playerCam.transform.up));
        float angle = Mathf.Atan2(relativeOrthagonalVelocity.y, relativeOrthagonalVelocity.x) * Mathf.Rad2Deg;

        //otherwise, find the direction the arrow is pointing in screen space
        //if the direction is too far up, draw the text below the planet
        //otherwise or if the arrowhead is within the ring, put the text above the planet
        float ringDistFromSurfaceOfPlanetInPixels = displayDistFromSurfaceOfPlanetInPixels;
        if (lockedOn) {
            ringDistFromSurfaceOfPlanetInPixels /= 2;
        }
        float outerRadius = radius + (thicknessInPixels + ringDistFromSurfaceOfPlanetInPixels) * unitsPerPixel; //this is in units

        //finding the tip of the arrow
        Vector3[] verts = (lockedOn ? lockedOnMesh : regularMesh).vertices;
        float maxDistance = float.MinValue;

        foreach (Vector3 vert in verts) {
            if(vert.magnitude > maxDistance) {
                maxDistance = vert.magnitude;
            }
        }

        Vector2 textPos = GetTextPos(angle, maxDistance, outerRadius, body);
        DrawRelativeVelocityText(radius, lockedOn, relativeForwardVelocity, textPos, body);
    }

    private static Vector2 GetTextPos(float angleOfArrow, float maxDistance, float outerRadius, CelestialBodyPhysics body) {
        Vector2 canvasSize = canvasSizeGetter.getSize();
        Vector3 offsetToBody = body.Position - playerCam.transform.position;
        Vector3 correctDir = Vector3.Cross(Vector3.Cross(offsetToBody, playerCam.transform.up), offsetToBody).normalized;
        Vector2 textPos = playerCam.WorldToViewportPoint(body.Position);
        if (Mathf.Abs(angleOfArrow - 90) < arrowTextToleranceAngle && maxDistance > outerRadius + textArrowLengthTolerance) {
            textPos.y = playerCam.WorldToViewportPoint(body.Position - correctDir * outerRadius).y;
            textPos.y *= canvasSize.y;
            textPos.x *= canvasSize.x;
            textPos.y -= textDistanceFromRing;
        } else {
            textPos.y = playerCam.WorldToViewportPoint(body.Position + correctDir * outerRadius).y;
            textPos.y *= canvasSize.y;
            textPos.x *= canvasSize.x;
            textPos.y += textDistanceFromRing;
        }

        return textPos;
    }

    private static void DrawRelativeVelocityText(float radius, bool lockedOn, int relativeForwardVelocity, Vector2 textPos, CelestialBodyPhysics body) {
        if (lockedOn) {
            if (!lockedOnText.enabled) {
                lockedOnText.enabled = true;
            }
            float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - body.transform.position).magnitude - radius));
            Color HUDColor = lockedOnColor;
            HUDColor.a = alpha;
            lockedOnText.color = HUDColor;
            lockedOnText.text = $"{body.name}\n{relativeForwardVelocity} m/s";
            lockedOnText.rectTransform.anchoredPosition = textPos;
        } else {
            if (!regularText.enabled) {
                regularText.enabled = true;
            }
            float alpha = Mathf.InverseLerp(fadeOutRange.x, fadeOutRange.y, Mathf.Max(0, (playerCam.transform.position - body.transform.position).magnitude - radius));
            alpha *= regularAlpha;
            Color HUDColor = regularColor;
            HUDColor.a = alpha;
            regularText.color = HUDColor;
            regularText.text = $"{body.name}\n{relativeForwardVelocity} m/s";
            regularText.rectTransform.anchoredPosition = textPos;
        }
    }

    public static void HideRelevantText(bool lockedOn) {
        if (lockedOn) {
            lockedOnText.enabled = false;
        } else {
            regularText.enabled = false;
        }
    }

    public static void HideText() {
        lockedOnText.enabled = false;
        regularText.enabled = false;
    }

    private static void DrawArrowToPlanet(CelestialBodyPhysics body) {
        if (!arrowToLockOnPlanet.enabled) {
            arrowToLockOnPlanet.enabled = true;
        }
        Quaternion halfwayRotToPlanet = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(playerCam.transform.forward, (body.Position - playerCam.transform.position).normalized), 0.5f);
        Vector3 test = halfwayRotToPlanet * playerCam.transform.forward;
        test = Vector3.ProjectOnPlane(test, playerCam.transform.forward).normalized;
        float angle = Vector3.SignedAngle(playerCam.transform.right, test, playerCam.transform.forward);
        arrowToLockOnPlanet.rectTransform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        arrowToLockOnPlanet.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 400f;
    }

    private static void HideArrowToPlanet() {
        if (arrowToLockOnPlanet.enabled) {
            arrowToLockOnPlanet.enabled = false;
        }
    }
}
