using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GunAimer : MonoBehaviour
{
    [SerializeField]
    public GameObject[] guns;
    public GameObject gunAnim;
    public float gunAnimDuration = 0.5f;
    private int gunIndex = 0;
    private bool ready = true;
    private Coroutine switchCoroutine;

    public Camera cam;
    RaycastHit hit;
    Ray ray;
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        for (int i = 0; i < guns.Length; i++) guns[i].SetActive(i == gunIndex);
        if (gunAnim != null) gunAnim.SetActive(false);
    }

    void Update() {
        ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit)) {
            guns[gunIndex].transform.LookAt(hit.point);
            gunAnim.transform.LookAt(hit.point);
            //gun.transform.position = new Vector3(2.4f, 0f, 2f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            if (gunIndex != 0 && ready) SwitchGun(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            if (gunIndex != 1 && ready) SwitchGun(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            if (gunIndex != 2 && ready) SwitchGun(2);
        }
    }

    void SetAnimFloatsForIndex(int i, Animator anim) {
        float blend = 0;
        float blend1 = 0;

        switch (i) {
            case 0:
                blend = 0;
                blend1 = 1;
                break;
            case 1:
                blend = -1;
                blend1 = -1;
                break;
            case 2:
                blend = 1;
                blend1 = -1;
                break;
            default:
                blend = 0;
                blend1 = 1;
                break;
        }
        anim.SetFloat("Blend", blend);
        anim.SetFloat("Blend1", blend1);
    }

    void SwitchGun(int i) {
        if(switchCoroutine != null) StopCoroutine(switchCoroutine);

        int prevGunIndex = gunIndex;

        gunAnim.SetActive(true);
        SetAnimFloatsForIndex(prevGunIndex, gunAnim.GetComponent<Animator>());
        guns[prevGunIndex].SetActive(false);
        ready = false;
        gunIndex = i;
        switchCoroutine = StartCoroutine(AnimateGunFloats(gunIndex, gunAnimDuration));
    }

    IEnumerator AnimateGunFloats(int targetIndex, float duration) {
        Animator anim = gunAnim.GetComponent<Animator>();
        if (anim == null) yield break;

        float startBlend = anim.GetFloat("Blend");
        float startBlend1 = anim.GetFloat("Blend1");

        float targetBlend = 0;
        float targetBlend1 = 0;

        switch (targetIndex) {
            case 0:
                targetBlend = 0;
                targetBlend1 = 1;
                break;
            case 1:
                targetBlend = -1;
                targetBlend1 = -1;
                break;
            case 2:
                targetBlend = 1;
                targetBlend1 = -1;
                break;
            default:
                yield break;
        }
        //Debug.Log("targetBlend: " + targetBlend + " targetBlend1: " + targetBlend1);

        float time = 0;
        while (time < duration) {
            float t = time / duration;
            float smoothT = Mathf.SmoothStep(0.0f, 1.0f, t);

            anim.SetFloat("Blend", Mathf.Lerp(startBlend, targetBlend, smoothT));
            anim.SetFloat("Blend1", Mathf.Lerp(startBlend1, targetBlend1, smoothT));

            time += Time.deltaTime;
            yield return null;
        }

        anim.SetFloat("Blend", targetBlend);
        anim.SetFloat("Blend1", targetBlend1);

        guns[gunIndex].SetActive(true);
        gunAnim.SetActive(false);
        ready = true;
    }

}
