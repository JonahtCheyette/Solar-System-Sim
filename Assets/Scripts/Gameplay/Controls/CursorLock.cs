using UnityEngine;

public static class CursorLock {
    public static void Reset() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void HandleCursor(bool lockCursor = true) {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            Debug.Break();
        }
    }
}
