using UnityEngine;

public class UILookAtCamera: MonoBehaviour {
    public Camera targetCamera;

    void LateUpdate() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        // Make the UI face the camera
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                         targetCamera.transform.rotation * Vector3.up);

        // Fix possible mirroring
        transform.Rotate(0, 180f, 0);
    }
}