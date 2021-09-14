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
        float minSensingDistance = 25000f;
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
            for (int i = 0; i < bodies.Length; i++) {
                Vector3 offsetToPlanet = bodies[i].Position - cam.transform.position;
                if (Vector3.Dot(offsetToPlanet, cam.transform.forward) >= 0) {
                    if (offsetToPlanet.magnitude - bodies[i].radius < minSensingDistance) {
                        float angleToPlanet = Vector3.Angle(cam.transform.forward, offsetToPlanet);
                        if (angleToPlanet < minAngleToBeConsidered && angleToPlanet < minAngle && !Physics.Raycast(cam.transform.position, offsetToPlanet.normalized, offsetToPlanet.magnitude - bodies[i].radius * 1.1f)) {
                            minAngle = angleToPlanet;
                            targetedBodyIndex = i;
                        }
                    }
                }
            }
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
}
