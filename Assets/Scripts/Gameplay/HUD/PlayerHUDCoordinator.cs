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

        Camera cam = Camera.main;

        if (ship.piloted) {
            float minAngle = float.MaxValue;
            float minAngleToBeConsidered = 30f;
            float minDistance = 10000;
            int targetedBodyIndex = -2;

            for(int i = 0; i < bodies.Length; i++) {
                Vector3 offsetToPlanet = bodies[i].Position - cam.transform.position;
                Vector3 projection = cam.transform.forward * Vector3.Dot(offsetToPlanet, cam.transform.forward);
                Vector3 offsetToLine = bodies[i].Position - projection;
                if(offsetToLine.magnitude < bodies[i].radius) {
                    //the planet is on the camera's forwards ray
                    float dstToBody = (bodies[i].Position - cam.transform.position).magnitude;
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
                    float angleToPlanet = Vector3.Angle(cam.transform.forward, offsetToPlanet);
                    if (angleToPlanet < minAngleToBeConsidered && angleToPlanet < minAngle) {
                        minAngle = angleToPlanet;
                        targetedBodyIndex = i;
                    }
                }
            }

            if(targetedBodyIndex != lockedOnBodyIndex && targetedBodyIndex != -2) {
                PlanetMouseoverHUD.DrawPlanetHUD(bodies[targetedBodyIndex], false);
                if (Input.GetKeyDown(KeyCode.C)) {
                    lockedOnBodyIndex = targetedBodyIndex;
                }
            }
            if(lockedOnBodyIndex != -1) {
                PlanetMouseoverHUD.DrawPlanetHUD(bodies[lockedOnBodyIndex], true);
            }

            if (Input.GetKeyDown(KeyCode.Z)) {
                lockedOnBodyIndex = -1;
            }
        }
    }
}
