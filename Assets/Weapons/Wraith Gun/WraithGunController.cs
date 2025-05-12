using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WraithGunController : MonoBehaviour
{
    public Animator gunAnimator;
    public GameObject wraithShotPrefab;
    public Transform shotOrigin;
    public float shotRadius = 15f;
    public float shotSpeed = 20f;
    public float shotDamage = 50f;
    public float shotForce = 1500f;

    public bool ReadyToFire = false;

    public void OnChargeUpComplete() {
        ReadyToFire = true;
    }

    public void ChargeUp() {
        ReadyToFire = false;
        gunAnimator.SetTrigger("ChargeUp");
    }

    public void Fire() {
        if (!ReadyToFire) {
            gunAnimator.ResetTrigger("ChargeUp");
            gunAnimator.SetTrigger("Cooldown");
        } else {
            wraithShotPrefab.GetComponent<ExplodeOnContact>().radius = shotRadius;
            wraithShotPrefab.GetComponent<ExplodeOnContact>().maxDamage = shotDamage;
            wraithShotPrefab.GetComponent<ExplodeOnContact>().maxForce = shotForce;
            GameObject shotInstance = Instantiate(wraithShotPrefab, shotOrigin.position, shotOrigin.rotation);
            Rigidbody rb = shotInstance.GetComponent<Rigidbody>();
            rb.velocity = shotOrigin.forward * shotSpeed;
            gunAnimator.SetTrigger("Fire");
            ReadyToFire = false;
        }
    }
}