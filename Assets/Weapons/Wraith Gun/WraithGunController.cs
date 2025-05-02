using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WraithGunController : MonoBehaviour
{
    public Animator gunAnimator;
    public GameObject wraithShotPrefab;
    public Transform shotOrigin;
    public float shotSpeed = 20f;

    public bool ReadyToFire = false;

    public void OnChargeUpComplete() {
        ReadyToFire = true;
    }

    public void ChargeUp() {
        ReadyToFire = false;
        gunAnimator.SetTrigger("ChargeUp");
    }

    public void Fire() {
        GameObject shotInstance = Instantiate(wraithShotPrefab, shotOrigin.position, shotOrigin.rotation);
        Rigidbody rb = shotInstance.GetComponent<Rigidbody>();
        rb.velocity = shotOrigin.forward * shotSpeed;
        gunAnimator.SetTrigger("Fire");
        ReadyToFire = false;
    }
}