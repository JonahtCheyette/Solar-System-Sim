using UnityEngine;

public class PlayerHUDCoordinator : MonoBehaviour {
    private ShipController ship;
    private CelestialBody[] bodies;
    private int lockedOnBodyIndex;

    // Start is called before the first frame update
    void Start() {
        InteractionHandler.Initialize(transform);
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

            //if there are no bodies directly on the camera's forwards ray, then find the body with the smallest angle offset < 30 degrees
            // and use that instead
            if (targetedBodyIndex == -2) {
                for (int i = 0; i < bodies.Length; i++) {
                    Vector3 offsetToPlanet = bodies[i].Position - cam.transform.position;
                    if (offsetToPlanet.magnitude - bodies[i].radius < minSensingDistance) {
                        float angleToPlanet = Vector3.Angle(cam.transform.forward, offsetToPlanet);
                        if (angleToPlanet < minAngleToBeConsidered && angleToPlanet < minAngle) {
                            minAngle = angleToPlanet;
                            targetedBodyIndex = i;
                        }
                    }
                }
            }

            if (targetedBodyIndex != lockedOnBodyIndex && targetedBodyIndex != -2) {
                //the ship is targeting a planet that is not the one it's locked on to
                PlanetMouseoverHUD.DrawPlanetHUD(bodies[targetedBodyIndex], ship.RigidBody.velocity, false);
                if (Input.GetKeyDown(KeyCode.C)) {
                    lockedOnBodyIndex = targetedBodyIndex;
                }
            }

            if (lockedOnBodyIndex != -1) {
                //the planet is locked on to a planet
                PlanetMouseoverHUD.DrawPlanetHUD(bodies[lockedOnBodyIndex], ship.RigidBody.velocity, true);
            }

            if (Input.GetKeyDown(KeyCode.Z)) {
                lockedOnBodyIndex = -1;
            }
        }
    }
}
