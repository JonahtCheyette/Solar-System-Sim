using UnityEngine;

public class PlayerHUDCoordinator : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        InteractionHandler.Initialize(transform);
    }

    // Update is called once per frame
    void Update() {
        InteractionHandler.RunInteractions();
    }
}
