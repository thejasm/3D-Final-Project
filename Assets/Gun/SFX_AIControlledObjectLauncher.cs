using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace QFX.SFX
{
    public class SFX_AIControlledObjectLauncher : MonoBehaviour
    {
        public SFX_ControlledObject[] ControlledObjects;


        public void StartShooting(float rate) {
            //Debug.Log("firing");
            foreach (var controlledObject in ControlledObjects) {
                controlledObject.GetComponent<SFX_SimpleProjectileWeapon>().FireRate = rate;
                controlledObject.Setup();
                controlledObject.Run();
            }
        }

        public void StopShooting() {
            foreach (var controlledObject in ControlledObjects) {
                controlledObject.Stop();
            }
        }
    }
}