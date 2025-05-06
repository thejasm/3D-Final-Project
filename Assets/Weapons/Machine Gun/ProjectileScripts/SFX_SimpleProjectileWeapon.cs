using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace QFX.SFX
{
    public sealed class SFX_SimpleProjectileWeapon : SFX_ControlledObject
    {

        public Transform LaunchTransform;
        public GameObject MuzzleFlash;

        public GameObject Projectile;

        public float FireRate = 0.5f;

        private bool _isFireAllowed = true;

        private ParticleSystem _launchPs;

        public override void Setup()
        {
            base.Setup();
        }

        public override void Run()
        {
            base.Run();
        }

        public override void Stop()
        {
            base.Stop();
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            if (_isFireAllowed)
                StartCoroutine("Fire");
        }

        private IEnumerator Fire()
        {
            _isFireAllowed = false;

            Vector3 position;
            Quaternion rotation;

            if (LaunchTransform != null)
            {
                position = LaunchTransform.position;
                rotation = LaunchTransform.rotation;
            }
            else
            {
                position = transform.position;
                rotation = transform.rotation;
            }

            var go = Instantiate(Projectile, position, rotation);
            //var emitterKeeper = go.GetComponent<SFX_IEmitterKeeper>();
            //if (emitterKeeper != null) emitterKeeper.EmitterTransform = transform;

            MuzzleFlash.GetComponent<Animator>().SetTrigger("flash");
            MuzzleFlash.transform.rotation = Quaternion.Euler(
                MuzzleFlash.transform.rotation.eulerAngles.x,
                MuzzleFlash.transform.rotation.eulerAngles.y,
                UnityEngine.Random.Range(0f, 360f)
            );

            yield return new WaitForSeconds(FireRate);

            _isFireAllowed = true;
        }
    }
}