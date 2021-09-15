using System.Collections.Generic;
using UnityEngine;

public class PlayerHUDCoordinator : MonoBehaviour {
    private KeyCode clearLockOn = KeyCode.Z;
    private KeyCode lockOnKey = KeyCode.C;
    private ShipController ship;
    private CelestialBody[] bodies;
    private int lockedOnBodyIndex;

    // Start is called before the first frame update
    void Start() {
        InteractionHandler.Initialize(transform);
        PlanetRelativeVelocityHUD.Initialize();
        //PlanetRelativeVelocityHUD.Initialize();
        ship = FindObjectOfType<ShipController>();
        bodies = FindObjectsOfType<CelestialBody>();
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
        }
    }

    private int GetIndexOfPlanetCameraIsLookingAt(Camera cam) {
        // this function returns the index of the planet the camera is looking at, if the camera isn't looking at any body, returns -2
        // the way the function decides what planet is being looked at is as follows:
        // if there are planets directly along the camera's forwards ray, it returns the closest one
        // else, it looks through all the planets that are less than 30 degrees off the camera's forwards ray,
        // and returns the one with the smallest angle offset that also has unbroken line of sight
        float minAngle = float.MaxValue;
        float minAngleToBeConsidered = 30f;
        float minSensingDistance = 250000f;
        float minDistance = minSensingDistance;
        int targetedBodyIndex = -2;

        for (int i = 0; i < bodies.Length; i++) {
            Vector3 offsetToPlanet = bodies[i].Position - cam.transform.position;
            if (Vector3.Dot(offsetToPlanet, cam.transform.forward) >= 0) {
                Vector3 projection = cam.transform.position + cam.transform.forward * Vector3.Dot(offsetToPlanet, cam.transform.forward);
                Vector3 offsetToLine = bodies[i].Position - projection;
                if (offsetToLine.magnitude < bodies[i].radius) {
                    //the planet is on the camera's forwards ray
                    float dstToBody = (bodies[i].Position - cam.transform.position).magnitude - bodies[i].radius;
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
            // getting all the bodies within minAngle of the camera's forwards ray
            // and save the data of the circle it will make on the screen
            List<BodyData> bodiesWithinMinAngle = new List<BodyData>();
            for (int i = 0; i < bodies.Length; i++) {
                Vector3 offsetToPlanet = bodies[i].Position - cam.transform.position;
                float distanceToPlanetSurface = offsetToPlanet.magnitude - bodies[i].radius;
                if (distanceToPlanetSurface < minSensingDistance) {
                    float angleToPlanet = Vector3.Angle(cam.transform.forward, offsetToPlanet);
                    if (angleToPlanet <= minAngleToBeConsidered && !Physics.Raycast(cam.transform.position, offsetToPlanet.normalized, distanceToPlanetSurface - 1f)) {
                        Vector2 screenPos = cam.WorldToScreenPoint(bodies[i].Position);
                        float screenRadius = (screenPos - (Vector2)cam.WorldToScreenPoint(bodies[i].Position + cam.transform.up * bodies[i].radius)).magnitude;
                        bodiesWithinMinAngle.Add(new BodyData(i, angleToPlanet, screenRadius, screenPos));
                    }
                }
            }

            //detecting if there's a line of sight between the body and the camera
            //the way it works is it checks if the body's angle from the camera's forwards angle is smaller than the smallest one recorded so far
            //if so, it gets the circle data saved above and check if any of the other bodies' circle that are within min angle completely cover it up
            //if not, it becomes the new targeted body
            for (int i = 0; i < bodiesWithinMinAngle.Count; i++) {
                if (bodiesWithinMinAngle[i].angle < minAngle) {
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
                        minAngle = bodiesWithinMinAngle[i].angle;
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
            PlanetRelativeVelocityHUD.HideText(false);
        }

        if (lockedOnBodyIndex != -1) {
            //the planet is locked on to a planet
            PlanetRelativeVelocityHUD.DrawPlanetHUD(bodies[lockedOnBodyIndex], ship.RigidBody.velocity, true);
        } else {
            PlanetRelativeVelocityHUD.HideText(true);
        }
    }

    private void CheckIfClearingLockOn() {
        if (Input.GetKeyDown(clearLockOn)) {
            lockedOnBodyIndex = -1;
        }
    }

    private struct BodyData {
        public int index;
        public float angle;
        public float screenRadius;
        public Vector2 screenPos;

        public BodyData(int bodyIndex, float angleFromCamForwardTobody, float radius, Vector2 positionOnScreen) {
            index = bodyIndex;
            angle = angleFromCamForwardTobody;
            screenRadius = radius;
            screenPos = positionOnScreen;
        }
    }
}
