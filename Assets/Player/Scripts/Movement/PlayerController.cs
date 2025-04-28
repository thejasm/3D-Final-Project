using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController: MonoBehaviour {
    [Header("Movement")]
    public float speed = 20f;
    public float jumpHeight = 8f;

    [Header("Camera Controller")]
    [SerializeField] public CinemachineVirtualCamera[] plyrCam;
    public Animator crosshair;
    private int camNo = 0;

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

        if (plyrCam[camNo] != null) {
            camPOV = plyrCam[camNo].GetCinemachineComponent<CinemachinePOV>();
            if (camPOV == null) {
                Debug.LogError("Cinemachine POV component not found on the Virtual Camera!", plyrCam[camNo]);
            }
        }
        else {
            Debug.LogError("Player Camera (plyrCam) reference is not set in the inspector!", this);
        }
    }



    void Update() {
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // --- Jumping ---
        if (Input.GetButtonDown("Jump")) {
            if (GroundCheck) {
                qJump = true;
            }
        }

        // --- Zoom Camera and Crosshair Contril ---
        if (Input.GetMouseButtonDown(1)) {
            camNo = 1;
            plyrCam[1].Priority = 2;
            camPOV = plyrCam[camNo].GetCinemachineComponent<CinemachinePOV>();
            crosshair.SetBool("zoom", true);
            //Debug.Log("zoom");
        } else if (Input.GetMouseButtonUp(1)) {
            camNo = 0;
            plyrCam[1].Priority = 0;
            camPOV = plyrCam[camNo].GetCinemachineComponent<CinemachinePOV>();
            crosshair.SetBool("zoom", false);
        }
    }

    void FixedUpdate() {
        PerformGroundCheck();

        // --- Movement ---
        if (camPOV != null && plyrCam[camNo] != null) {
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