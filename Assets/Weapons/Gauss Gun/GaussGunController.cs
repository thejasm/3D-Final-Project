using QFX.SFX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public class GaussGunController : MonoBehaviour
{
    public Animator gunAnimator;
    public GameObject MuzzleFlash;
    public GameObject Projectile;
    public float shotSpeed = 500f;
    public float shotDamage = 20f;
    public float chargeUpSpeed = 1f;
    private Transform shotOrigin;

    public bool ReadyToFire = false;

    public void Awake() {
        shotOrigin = MuzzleFlash.transform;
    }

    public void OnChargeUpComplete() {
        ReadyToFire = true;
    }

    public void ChargeUp() {
        ReadyToFire = false;
        gunAnimator.SetTrigger("ChargeUp");
        gunAnimator.speed = 1f / chargeUpSpeed;
    }

    public void Fire() {
        if (!ReadyToFire) { 
            gunAnimator.ResetTrigger("ChargeUp"); 
            gunAnimator.SetTrigger("Cooldown");
        } else {
            Projectile.GetComponent<SFX_SimpleProjectile>().Speed = shotSpeed;
            Projectile.GetComponent<SFX_SimpleProjectile>().damage = shotDamage;
            var go = Instantiate(Projectile, shotOrigin.position, shotOrigin.rotation);

            MuzzleFlash.GetComponent<Animator>().SetTrigger("flash");
            MuzzleFlash.transform.rotation = Quaternion.Euler(
                MuzzleFlash.transform.rotation.eulerAngles.x,
                MuzzleFlash.transform.rotation.eulerAngles.y,
                UnityEngine.Random.Range(0f, 360f)
            );
            gunAnimator.SetTrigger("Fire");
            ReadyToFire = false;
        }
    }
}