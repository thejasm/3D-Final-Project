using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlyrMeshSpinner: MonoBehaviour {
    public PlayerController playerController;

    [Header("Spin Settings")]
    public float inpSpinSpd = 720f;
    public float inpThresh = 0.1f;
    public float meshRadius = 0.5f;
    public float minPhysRollSpd = 0.1f;
    public float spinDampTime = 0.1f;

    [Header("Sparks")]
    public ParticleSystem sparksPSys;
    public float spkAngleThresh = 10f;
    public float minSpkSpd = 2f;
    public float maxSpkSpd = 10f;
    public float spkMaxBoostAngle = 90f;

    private Transform parent;
    private float currSpin = 0f;
    private float spinVel = 0.0f;
    private ParticleSystem.EmissionModule spkEmission;
    private ParticleSystem.MainModule spkMain;


    void Start() {
        parent = transform.parent;

        if (playerController == null) {
            playerController = parent.GetComponent<PlayerController>();
        }

        if (sparksPSys != null) {
            spkEmission = sparksPSys.emission;
            spkMain = sparksPSys.main;
            spkEmission.enabled = false;
        }
    }

    void Update() {
        if (playerController == null || playerController.PlayerRB == null || meshRadius <= 0f) return;

        Vector3 worldMoveDirection = playerController.CurrentWorldMoveDirection.normalized;
        Rigidbody rb = playerController.PlayerRB;
        Vector3 rbVel = rb.velocity;
        Vector3 horVel = new Vector3(rbVel.x, 0f, rbVel.z);
        float physSpd = horVel.magnitude;
        Vector3 physicsDirection = (physSpd > 0.01f) ? horVel.normalized : Vector3.zero;


        bool hasInput = playerController.CurrentWorldMoveDirection.sqrMagnitude > inpThresh * inpThresh;


        Vector3 inpWorldAxis = Vector3.zero;
        if (hasInput && worldMoveDirection != Vector3.zero) inpWorldAxis = Vector3.Cross(Vector3.up, worldMoveDirection).normalized;

        Vector3 physicsWorldAxis = Vector3.zero;

        if (physSpd > minPhysRollSpd && physicsDirection != Vector3.zero) physicsWorldAxis = Vector3.Cross(Vector3.up, physicsDirection).normalized;


        float targetSpin = 0f;
        Vector3 targetWorldAxis = Vector3.forward;
        if (hasInput && inpWorldAxis != Vector3.zero) {
            targetSpin = inpSpinSpd;
            targetWorldAxis = inpWorldAxis;
        }
        else if (physSpd > minPhysRollSpd && physicsWorldAxis != Vector3.zero) {
            targetWorldAxis = physicsWorldAxis;
            targetSpin = physSpd / meshRadius * Mathf.Rad2Deg;
        }


        currSpin = Mathf.SmoothDamp(currSpin, targetSpin, ref spinVel, spinDampTime);
        if (targetWorldAxis != Vector3.zero) {
            Vector3 localMeshSpinAxis = transform.InverseTransformDirection(targetWorldAxis);

            if (Mathf.Abs(currSpin) > 0.01f) transform.Rotate(localMeshSpinAxis, currSpin * Time.deltaTime, Space.Self);
        }


        // SparksFX ----------------------------------------------------------------------------------------------------------------------
        if (sparksPSys != null) {
            if (playerController.isGrounded == false) {
                sparksPSys.Stop();
            } else {
                if(sparksPSys.isPlaying == false) sparksPSys.Play();
            }
            bool emitParticle = false;
            float dirAngle = 0f;


            if (hasInput &&
                physSpd > 0.01f &&
                worldMoveDirection != Vector3.zero &&
                physicsDirection != Vector3.zero) {

                dirAngle = Vector3.Angle(worldMoveDirection, physicsDirection);

                if (dirAngle > spkAngleThresh) emitParticle = true;
            }

            if (spkEmission.enabled != emitParticle)  spkEmission.enabled = emitParticle;

            if (emitParticle) {
                float angleRange = Mathf.Max(1f, spkMaxBoostAngle - spkAngleThresh);
                float normalizedBoost = Mathf.Clamp01((dirAngle - spkAngleThresh) / angleRange);
                float targetSpeed = Mathf.Lerp(minSpkSpd, maxSpkSpd, normalizedBoost);

                spkMain.startSpeed = targetSpeed;


                // Rotation Control
                if (worldMoveDirection != Vector3.zero) sparksPSys.transform.rotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up);
            }
        }
    }
}