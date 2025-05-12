using QFX.SFX;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public abstract class BaseAIController<T>: MonoBehaviour where T : BaseAIController<T> {
    public BaseState<T> CurrentState { get; private set; }

    [SerializeField] private string currentStateName;

    public GameObject playerTarget;
    public float health = 100f;
    public GameObject deathExplosion;
    public LayerMask visionObstructionLayer;
    public float range = 20f;
    public float FOV = 80f;
    public GameObject gun;
    public float fireRate = 1f;
    public float turnSpeed = 90f;

    public float IdleLookTime = 3f;
    public float idleRotationSpeed = 90f;

    protected float nextIdleLookTime = 0f;
    protected Quaternion targetIdleRotation;
    protected Vector3 playerLastKnownPosition = Vector3.zero;
    protected float currentPitch = 0f;

    public bool IsPlayerInRange(){
        if(Vector3.Distance(this.transform.position, playerTarget.transform.position) <= range){
            return true;
        }
        return false;
    }
    public bool IsPlayerInView(){
        if (playerTarget == null) return false;

        Vector3 positionToCheckFrom = transform.position;
        Vector3 directionToPlayer = (playerTarget.transform.position - positionToCheckFrom).normalized;
        float distanceToPlayer = Vector3.Distance(positionToCheckFrom, playerTarget.transform.position);

        if (Vector3.Angle(transform.forward, directionToPlayer) <= (FOV / 2)) {
            if (!Physics.Raycast(positionToCheckFrom, directionToPlayer, distanceToPlayer, visionObstructionLayer)) {
                return true;
            }
        }

        return false;
    }

    public virtual void TurnToTarget(Vector3 targetPosition) {
        Vector3 directionToTarget = targetPosition - transform.position;

        if (directionToTarget.sqrMagnitude > 0.01f) {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

    }

    public virtual void Recenter() {
        currentPitch = Mathf.MoveTowardsAngle(gun.transform.localEulerAngles.x, 0, turnSpeed * Time.deltaTime);
        gun.transform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
    }

    public void TurnToPlayerLastKnownPosition() {
        TurnToTarget(playerLastKnownPosition);
    }

    public virtual void StartShooting() { }
    public virtual void StopShooting() { }

    public virtual void IdleAnim(){
        if (Time.time >= nextIdleLookTime) {
            targetIdleRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            nextIdleLookTime = Time.time + IdleLookTime;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetIdleRotation, idleRotationSpeed * Time.deltaTime);
    }

    public virtual void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        if (health >= 0) health -= projectile.damage;
        else Die();
    }

    public void TakeDamage(float amount){
        health -= amount;
    }

    public virtual void Die(){
        Instantiate(deathExplosion, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }


    // STATE MACHINE METHODS -------------------------------------------------------------------------------------
    protected virtual void Awake() {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
    }

    protected virtual void Start() {
        if (CurrentState == null && this.enabled) {
            Debug.LogError("ERROR: InitializeState is not set");
            this.enabled = false;
        }
    }

    protected virtual void Update() {
        CurrentState?.Execute();

        if (CurrentState != null) {
            currentStateName = CurrentState.GetType().Name;
        }
    }

    protected void InitializeState(BaseState<T> startingState) {
        if (startingState == null) {
            Debug.LogError($"NULL starting state: {gameObject.name}.", this);
            this.enabled = false;
            return;
        }
        CurrentState = startingState;
        CurrentState.Enter();
        Debug.Log($"[{gameObject.name}]: {startingState.GetType().Name}");
    }

    public void ChangeState(BaseState<T> newState) {
        if (newState == null) {
            Debug.LogError($"ERROR: Change to NULL state for {gameObject.name}!", this);
            return;
        }

        if (CurrentState == newState) {
            Debug.LogWarning($"[{gameObject.name}] Attempted to change to the same state: {newState.GetType().Name}", this);
            return;
        }

        CurrentState?.Exit();
        Debug.Log($"[{gameObject.name}] Changing state from {CurrentState?.GetType().Name ?? "None"} to {newState.GetType().Name}");

        CurrentState = newState;
        CurrentState.Enter();
    }
    // STATE MACHINE METHODS -------------------------------------------------------------------------------------



    // DEBUG GIZMOS ---------------------------------------------------------------------------------------------
    private void OnDrawGizmos() {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(this.transform.position, Vector3.up, range);


        if (playerTarget != null) {
            if (IsPlayerInView()) {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            } else  if (IsPlayerInRange()) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            }
            Gizmos.color = Color.yellow;
            Vector3 fovL = Quaternion.Euler(0, -FOV / 2, 0) * transform.forward;
            Vector3 fovR = Quaternion.Euler(0, FOV / 2, 0) * transform.forward;
            Handles.color = new Color(1, 1, 0, 0.1f);
            Handles.DrawSolidArc(transform.position, Vector3.up, fovL, FOV, range);

        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(playerLastKnownPosition, 1f);
    }
}
