using MoreMountains.Feedbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GunPlayerController: MonoBehaviour
{
    [SerializeField]
    public GameObject[] guns;
    public GameObject gunAnim;
    public GameObject healthbarUI;
    public float gunAnimDuration = 0.5f;
    public int gunIndex = 0;
    // 0 = Machine Gun, 1 = Wraith, 2 = Gauss
    private bool ready = true;
    private Coroutine switchCoroutine;

    public Camera cam;
    public Animator reticle;
    private PlayerController playerController;
    RaycastHit hit;
    Ray ray;

    public MMFeedbacks MGFeedback;
    public MMFeedbacks WFeedback;
    public MMFeedbacks GFeedback;


    void Start() {
        playerController = GetComponent<PlayerController>();
        Cursor.lockState = CursorLockMode.Locked;
        for (int i = 0; i < guns.Length; i++) guns[i].SetActive(i == gunIndex);
        if (gunAnim != null) gunAnim.SetActive(false);
    }

    void Update() {
        ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit)) {
            if (gunIndex == 1) {
                guns[gunIndex].transform.LookAt(hit.point);
                guns[gunIndex].transform.rotation *= Quaternion.Euler(-10, 0, 0);
                gunAnim.transform.LookAt(hit.point);
            }
            else {
                guns[gunIndex].transform.LookAt(hit.point);
                gunAnim.transform.LookAt(hit.point);
            }

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

        if(gunIndex == 0) {
            if (Input.GetMouseButtonDown(0)) MGFeedback?.PlayFeedbacks();
            if (Input.GetMouseButtonUp(0)) MGFeedback?.StopFeedbacks();
        }

        if (gunIndex == 1) {
            if (Input.GetMouseButtonDown(0)) guns[gunIndex].GetComponent<WraithGunController>().ChargeUp();
            if (Input.GetMouseButtonUp(0)) guns[gunIndex].GetComponent<WraithGunController>().Fire();
        }
        else if (gunIndex == 2) {
            if (Input.GetMouseButtonDown(0)) guns[gunIndex].GetComponent<GaussGunController>().ChargeUp();
            if (Input.GetMouseButtonUp(0)) guns[gunIndex].GetComponent<GaussGunController>().Fire();
        }

        if (Input.GetMouseButtonDown(1)) {
            reticle.SetBool("zoom", true);
        } else if (Input.GetMouseButtonUp(1)) {
            reticle.SetBool("zoom", false);
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

        reticle.SetInteger("gun", i);
        reticle.speed = 10f;

        playerController.zoomCam.Priority = 0;
        playerController.zoomCam = playerController.plyrCam[i + 1];

        int prevGunIndex = gunIndex;

        gunAnim.SetActive(true);
        healthbarUI.transform.SetParent(gunAnim.transform, true);
        SetAnimFloatsForIndex(prevGunIndex, gunAnim.GetComponent<Animator>());
        guns[prevGunIndex].SetActive(false);
        ready = false;
        gunIndex = i;
        switchCoroutine = StartCoroutine(AnimateGunFloats(gunIndex, gunAnimDuration));
        
        if (gunIndex > 0) {
            guns[gunIndex].GetComponent<Animator>().ResetTrigger("ChargeUp");
            guns[gunIndex].GetComponent<Animator>().ResetTrigger("Fire");
            guns[gunIndex].GetComponent<Animator>().ResetTrigger("Cooldown");
        }
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
        healthbarUI.transform.SetParent(guns[gunIndex].transform, true);
        gunAnim.SetActive(false);
        reticle.speed = 1f;
        ready = true;
    }

}

