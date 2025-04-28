using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class GunAimer : MonoBehaviour
{
    public GameObject gun;
    public Camera cam;
    RaycastHit hit;
    Ray ray;
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update() {
        ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit)) {
            gun.transform.LookAt(hit.point);
            //gun.transform.position = new Vector3(2.4f, 0f, 2f);
        }
    }
}
