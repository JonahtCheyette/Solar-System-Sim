using System.Collections.Generic;
using UnityEngine;

public class PlayerHUDCoordinator : MonoBehaviour {
    public float minAngleToBeConsidered = 30f;
    public float minSensingDistance = 250000f;

    private KeyCode clearLockOn = KeyCode.Z;
    private KeyCode lockOnKey = KeyCode.C;
    private ShipController ship;
    private CelestialBodyPhysics[] bodies;
    private int lockedOnBodyIndex;

    // Start is called before the first frame update
    void Start() {
        InteractionHandler.Initialize(transform);
        PlanetRelativeVelocityHUD.Initialize();
        //PlanetRelativeVelocityHUD.Initialize();
        ship = FindObjectOfType<ShipController>();
        bodies = FindObjectsOfType<CelestialBodyPhysics>();
        lockedOnBodyIndex = -1;
    }

    // Update is called once per frame
    void Update() {
        InteractionHandler.RunInteractions();
        DisplayPlanetHUD();
    }

    private void DisplayPlanetHUD() {
        Camera cam = Camera.main;

        if (ship.piloted) {
            int lookedAtBodyIndex = GetIndexOfPlanetCameraIsLookingAt(cam);
            DrawPlanetHUD(lookedAtBodyIndex);
            CheckIfClearingLockOn();
        } else {
            PlanetRelativeVelocityHUD.HideText();
        }
    }

    private int GetIndexOfPlanetCameraIsLookingAt(Camera cam) {
        // this function returns the index of the planet the camera is looking at, if the camera isn't looking at any body, returns -2
        // the way the function decides what planet is being looked at is as follows:
        // if there are planets directly along the camera's forwards ray, it returns the closest one
        // else, it looks through all the planets that are less than 30 degrees off the camera's forwards ray,
        // and returns the one with the smallest angle offset that also has unbroken line of sight
        float minAlignment = Mathf.Cos(minAngleToBeConsidered * Mathf.Deg2Rad);
        float minDistance = minSensingDistance;
        int targetedBodyIndex = -2;

        Vector3 camPosition = cam.transform.position;
        Vector3 camForwards = cam.transform.forward;

        for (int i = 0; i < bodies.Length; i++) {
            Vector3 offsetToPlanet = bodies[i].Position - camPosition;
            if (Vector3.Dot(offsetToPlanet, camForwards) >= 0) {
                Vector3 projection = camPosition + camForwards * Vector3.Dot(offsetToPlanet, camForwards);
                Vector3 offsetToLine = bodies[i].Position - projection;
                float radius = bodies[i].Radius();
                if (offsetToLine.magnitude < radius) {
                    //the planet is on the camera's forwards ray
                    float dstToBody = (bodies[i].Position - camPosition).magnitude - radius;
                    if (dstToBody < minDistance) {
                        minDistance = dstToBody;
                        targetedBodyIndex = i;
                    }
                }
            }
        }

        // if there are no bodies directly on the camera's forwards ray, then find the body with the smallest angle offset < 30 degrees
        // that has a clear line of sight from the camera to the body
        // and use that instead
        if (targetedBodyIndex == -2) {
            // getting all the bodies within minAngleToBeConsidered of the camera's forwards ray
            // and save the data of the circle it will make on the screen
            List<BodyData> bodiesWithinMinAngle = new List<BodyData>();
            for (int i = 0; i < bodies.Length; i++) {
                Vector3 offsetToPlanet = bodies[i].Position - camPosition;
                float radius = bodies[i].Radius();
                float distanceToPlanetSurface = offsetToPlanet.magnitude - radius;
                if (distanceToPlanetSurface < minSensingDistance) {
                    float alignmentWithCamera = Vector3.Dot(camForwards, offsetToPlanet.normalized);
                    if (alignmentWithCamera >= minAlignment && !Physics.Raycast(camPosition + camForwards, offsetToPlanet.normalized, distanceToPlanetSurface - 2f)) {
                        Vector2 screenPos = cam.WorldToScreenPoint(bodies[i].Position);
                        float screenRadius = (screenPos - (Vector2)cam.WorldToScreenPoint(bodies[i].Position + cam.transform.up * radius)).magnitude;
                        bodiesWithinMinAngle.Add(new BodyData(i, alignmentWithCamera, screenRadius, screenPos));
                    }
                }
            }


            //detecting if there's a line of sight between the body and the camera
            //the way it works is it checks if the body's angle from the camera's forwards angle is smaller than the smallest one recorded so far
            //if so, it gets the circle data saved above and check if any of the other bodies' circle that are within min angle completely cover it up
            //if it isn't completely covered up, it becomes the new targeted body
            float maxAlignment = float.MinValue;
            for (int i = 0; i < bodiesWithinMinAngle.Count; i++) {
                if (bodiesWithinMinAngle[i].alignment > maxAlignment) {
                    bool completelyCoveredUp = false;
                    for (int j = 0; j < bodiesWithinMinAngle.Count; j++) {
                        if(i != j && bodiesWithinMinAngle[j].screenRadius > bodiesWithinMinAngle[i].screenRadius) {
                            if((bodiesWithinMinAngle[i].screenPos - bodiesWithinMinAngle[j].screenPos).sqrMagnitude < Mathf.Pow(bodiesWithinMinAngle[j].screenRadius - bodiesWithinMinAngle[i].screenRadius, 2)) {
                                completelyCoveredUp = true;
                                break;
                            }
                        }
                    }
                    if (!completelyCoveredUp) {
                        maxAlignment = bodiesWithinMinAngle[i].alignment;
                        targetedBodyIndex = bodiesWithinMinAngle[i].index;
                    }
                }
            }

            //limitations of this technique:
            //1. it assumes only one planet might be covering any other given planet. If two planets collectively completely cover up the targeted planet,
            //it will say the planet isn't completely covered up when, in fact, it is
            //2. the planets don't make perfect circles on the screen, they make ellipses, whose eccentricity is greatest near the edges of the screen
            //this isn't such a big problem because we're only considering bodies close to the center of the screen, but even there it's technically innacurate
        }

        return targetedBodyIndex;
    }

    private void DrawPlanetHUD(int targetBodyIndex) {
        if (targetBodyIndex != lockedOnBodyIndex && targetBodyIndex != -2) {
            //the ship is targeting a planet that is not the one it's locked on to
            PlanetRelativeVelocityHUD.DrawPlanetHUD(bodies[targetBodyIndex], ship.RigidBody.velocity, false);
            if (Input.GetKeyDown(lockOnKey)) {
                lockedOnBodyIndex = targetBodyIndex;
            }
        } else {
            PlanetRelativeVelocityHUD.HideRelevantText(false);
        }

        if (lockedOnBodyIndex != -1) {
            //the planet is locked on to a planet
            PlanetRelativeVelocityHUD.DrawPlanetHUD(bodies[lockedOnBodyIndex], ship.RigidBody.velocity, true);
        } else {
            PlanetRelativeVelocityHUD.HideRelevantText(true);
        }   
    }

    private void CheckIfClearingLockOn() {
        if (Input.GetKeyDown(clearLockOn)) {
            lockedOnBodyIndex = -1;
        }
    }

    private struct BodyData {
        public int index;
        public float alignment;
        public float screenRadius;
        public Vector2 screenPos;

        public BodyData(int bodyIndex, float alignmentFromCamForwardToBody, float radius, Vector2 positionOnScreen) {
            index = bodyIndex;
            alignment = alignmentFromCamForwardToBody;
            screenRadius = radius;
            screenPos = positionOnScreen;
        }
    }
}
