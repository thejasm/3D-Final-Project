using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public abstract class BaseAIController<T>: MonoBehaviour where T : BaseAIController<T> {
    public BaseState<T> CurrentState { get; private set; }

    [SerializeField] private string currentStateName;

    // TODO: Add variables for AI (Attack range, FOV object, health, etc.)
    public GameObject playerTarget;
    public float health = 100f;
    public LayerMask visionObstructionLayer;
    public float range = 20f;
    public float FOV = 80f;
    public GameObject gun;
    public float fireRate = 3f;
    public float IdleLookTime = 3f;

    // TODO: Implement methods for AI
    public bool IsPlayerInRange(){
        if(Vector3.Distance(this.transform.position, playerTarget.transform.position) <= range){
            return true;
        }
        return false;
    }
    public bool IsPlayerInView(){
        if (IsPlayerInRange()) {
            Vector3 dirToTarget = (playerTarget.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle <= FOV / 2) {
                return true;
            }
        }
        return false;
    }
    public void ShootAtPlayer() { return; }
    public virtual void IdleAnim(){ return; }
    public void TakeDamage(float amount){ return; }
    public virtual void Die(){ return; }

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
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, range);
        Gizmos.color = new Color(1, 1, 0, 1f);
        Gizmos.DrawWireSphere(transform.position, range);


        if (playerTarget != null) {
            if (IsPlayerInView()) {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            } else  if (IsPlayerInRange()) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            } else {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            }
        }
    }
}
