using UnityEngine;

public class PlayerHUDCoordinator : MonoBehaviour {
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

            //if there are no bodies directly on the camera's forwards ray, then find the body with the smallest angle offset < 30 degrees
            //that has a clear line of sight from the camera to the body
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

            if (targetedBodyIndex != lockedOnBodyIndex && targetedBodyIndex != -2) {
                //the ship is targeting a planet that is not the one it's locked on to
                PlanetRelativeVelocityHUD.DrawPlanetHUD(bodies[targetedBodyIndex], ship.RigidBody.velocity, false);
                if (Input.GetKeyDown(KeyCode.C)) {
                    lockedOnBodyIndex = targetedBodyIndex;
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

            if (Input.GetKeyDown(KeyCode.Z)) {
                lockedOnBodyIndex = -1;
            }
        }
    }
}
