using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using QFX.SFX;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Drawing;
using Color = UnityEngine.Color;
using UnityEngine.VFX;
using MoreMountains.Feedbacks;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController: MonoBehaviour {
    [Header("Movement")]
    public float speed = 20f;
    public float jumpHeight = 8f;
    public float health = 100f;
    private float maxHealth;

    [Header("Camera Controller")]
    [SerializeField] public CinemachineVirtualCamera[] plyrCam;
    [HideInInspector]
    public CinemachineVirtualCamera zoomCam;
    private LensDistortion lensDistortion = null;

    [Header("Game Objects")]
    public Image healthBar;
    public GameObject deathExplosion;
    public VolumeProfile volumeProfile;
    public VisualEffect speedlines;
    public MMFeedbacks jumpFeedback;
    public MMFeedbacks landFeedback;


    [Header("Ground Check")]
    public float groundCheckDist = 0.6f;
    public Vector3 boxSize = new Vector3(0.4f, 0.1f, 0.4f);
    public LayerMask groundLayer;

    private Rigidbody rb;
    public Rigidbody PlayerRB => rb;
    private CinemachinePOV camPOV;
    private SphereCollider sphereCollider;

    private Vector2 moveInput;
    public Vector2 CurrentMoveInput => moveInput;

    private Vector3 _currentWorldMoveDirection = Vector3.zero;
    public Vector3 CurrentWorldMoveDirection => _currentWorldMoveDirection;

    private bool qJump = false;
    private bool GroundCheck = false;


    void Awake() {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        camPOV = plyrCam[0].GetCinemachineComponent<CinemachinePOV>();
        zoomCam = plyrCam[1];
        maxHealth = health;

        lensDistortion = volumeProfile.TryGet<LensDistortion>(out var distortion) ? distortion : null;
    }

    void Update() {
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // --- Jumping ---
        if (Input.GetButtonDown("Jump")) {
            if (GroundCheck) qJump = true;
        }

        // --- Zoom Camera ---
        if (Input.GetMouseButtonDown(1)) {
            zoomCam.Priority = 2;
            camPOV = zoomCam.GetCinemachineComponent<CinemachinePOV>();
        }
        else if (Input.GetMouseButtonUp(1)) {
            zoomCam.Priority = 0;
            camPOV = plyrCam[0].GetCinemachineComponent<CinemachinePOV>();
        }

        // --- Speed FX ---
        float speedFactor = Mathf.Clamp01(rb.velocity.magnitude / 50f);
        if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0.0f, -0.3f, speedFactor);
        speedlines.SetFloat("radius", Mathf.Lerp(5.0f, 2.9f, speedFactor));
    }



        void FixedUpdate() {
        PerformGroundCheck();

        // --- Movement ---
        if (camPOV != null && zoomCam != null) {
            float horizontalRotationAngle = camPOV.m_HorizontalAxis.Value;

            Quaternion cameraRotation = Quaternion.Euler(0, horizontalRotationAngle, 0);
            Vector3 localMovementDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
            Vector3 worldForceDirection = cameraRotation * localMovementDirection;
            _currentWorldMoveDirection = worldForceDirection;

            rb.AddForce(worldForceDirection * speed);
        }

        // --- Jumping ---
        if (qJump) {
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            jumpFeedback?.PlayFeedbacks();
            qJump = false;
        }
    }

    public bool isGrounded { get { return GroundCheck; } }

    void PerformGroundCheck() {
        Vector3 boxCenter = transform.position + (Vector3.down * groundCheckDist);

        GroundCheck = Physics.CheckBox(
            boxCenter,
            boxSize,
            Quaternion.identity,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (GroundCheck && rb.velocity.y < -7f) {
            landFeedback?.PlayFeedbacks();
        }
    }

    public virtual void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        if (health >= 0) TakeDamage(projectile.damage);
        else Die();
    }

    public void TakeDamage(float amount) {
        health -= amount;
        UpdateHealthBar();
    }

    public void UpdateHealthBar() {
        if (healthBar != null) {
            healthBar.fillAmount = (float)((health / maxHealth) * 0.5);
        }
    }

    public virtual void Die() {
        Instantiate(deathExplosion, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }


#if UNITY_EDITOR
    void OnDrawGizmos() {
        Vector3 boxCenter = transform.position + (Vector3.down * groundCheckDist);
        Vector3 boxFullSize = boxSize * 2.0f;
        Color gizmoColor = GroundCheck ? Color.green : Color.red;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(boxCenter, boxFullSize);

        if (Application.isPlaying) {
            //Handles.color = Color.blue;
            //Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(_currentWorldMoveDirection), 1.5f, EventType.Repaint);
        }
    }
#endif
}