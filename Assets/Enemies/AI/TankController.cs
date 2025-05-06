using UnityEngine;
using UnityEngine.AI;
using QFX.SFX;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class TankController: BaseAIController<TankController> {
    public float moveSpeed = 4.0f;
    public float patrolPointReachThreshold = 1.5f;

    [HideInInspector]
    public NavMeshAgent agent;
    private Vector3 currentPatrolTarget = Vector3.zero;
    private bool hasPatrolTarget = false;

    public TankIdleState IdleState { get; private set; }
    public TankDistancingState DistancingState { get; private set; }
    public TankSearchingState SearchingState { get; private set; }

    // --- Unity Methods ---
    protected override void Awake() {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
        agent.updateRotation = true;
        agent.updatePosition = true;

        // Initialize States
        IdleState = new TankIdleState(this);
        DistancingState = new TankDistancingState(this);
        SearchingState = new TankSearchingState(this);
    }

    protected override void Start() {
        InitializeState(IdleState);
    }

    protected override void Update() {
        if (IsPlayerInView()) {
            playerLastKnownPosition = playerTarget.transform.position;
        }
        base.Update();
    }

    // --- AI Helper Methods ---

    public void SetAgentDestination(Vector3 targetPosition) {
        if (agent.isOnNavMesh) {
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }
        else {
            Debug.LogWarning($"{gameObject.name}: Agent not on NavMesh, cannot set destination.", this);
        }
    }

    public void StopMovement() {
        if (agent.isOnNavMesh && !agent.isStopped) {
            agent.isStopped = true;
            // Optional: Reset path if needed, but stopping is often sufficient
            // agent.ResetPath();
        }
    }

    public bool PickNewPatrolPoint() {
        // Use controller.range as the radius for patrolling
        float patrolRadius = range;
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas)) {
            currentPatrolTarget = hit.position;
            hasPatrolTarget = true;
            return true;
        }
        else {
            currentPatrolTarget = transform.position;
            hasPatrolTarget = false;
            Debug.LogWarning($"{gameObject.name}: Could not find valid NavMesh point for patrol within range {patrolRadius}.", this);
            return false;
        }
    }

    public Vector3 CalculateRetreatPoint() {
        if (playerTarget == null) {
            Debug.LogWarning($"[{gameObject.name}] CalculateRetreatPoint: Player target is null. Returning current position.");
            return transform.position;
        }

        float desiredDist = range; // Use the 'range' variable from BaseAIController
        Vector3 directionToPlayer = playerTarget.transform.position - transform.position;
        float currentDistance = directionToPlayer.magnitude;

        // Calculate the ideal position directly away from the player at the desired distance
        // We want to move FROM currentDistance TO desiredDist, so the distance to move is (desiredDist - currentDistance)
        // The direction is AWAY from the player (-directionToPlayer.normalized)
        Vector3 directionAwayFromPlayer = -directionToPlayer.normalized;
        Vector3 idealRetreatPos = transform.position + directionAwayFromPlayer * (desiredDist - currentDistance);

        // --- DEBUGGING START ---
        // Debug.DrawLine(transform.position, playerTarget.transform.position, Color.red, 0.5f); // Line to player
        // Debug.DrawLine(transform.position, idealRetreatPos, Color.cyan, 0.5f); // Line to ideal retreat point
        // --- DEBUGGING END ---


        NavMeshHit hit;
        float sampleRadius = 4.0f; // Increased sample radius

        // Try to find a valid NavMesh point near the ideal position
        if (NavMesh.SamplePosition(idealRetreatPos, out hit, sampleRadius, NavMesh.AllAreas)) {
            // Found a valid point near the ideal retreat spot
            Debug.Log($"[{gameObject.name}] CalculateRetreatPoint: Found point near ideal: {hit.position} (Distance from current: {Vector3.Distance(transform.position, hit.position)})"); // DEBUG
            return hit.position;
        }
        else {
            Debug.LogWarning($"[{gameObject.name}] CalculateRetreatPoint: Failed to sample near ideal position: {idealRetreatPos}. Trying fallback."); // DEBUG

            // Fallback: Calculate a point further away in the retreat direction if sampling failed
            float fallbackDistance = 5.0f; // Try moving a bit further for fallback
            Vector3 fallbackPos = transform.position + directionAwayFromPlayer * fallbackDistance;
            // --- DEBUGGING START ---
            // Debug.DrawLine(transform.position, fallbackPos, Color.magenta, 0.5f); // Line to fallback point
            // --- DEBUGGING END ---

            if (NavMesh.SamplePosition(fallbackPos, out hit, sampleRadius, NavMesh.AllAreas)) {
                // Use this slightly further point if valid
                Debug.LogWarning($"[{gameObject.name}] CalculateRetreatPoint: Found point near fallback: {hit.position} (Distance from current: {Vector3.Distance(transform.position, hit.position)})"); // DEBUG
                return hit.position;
            }
            else {
                // Last resort: Stay put if no valid retreat point found
                Debug.LogError($"[{gameObject.name}] CalculateRetreatPoint: Failed to find ANY valid retreat point on NavMesh. Returning current position."); // DEBUG
                return transform.position;
            }
        }
    }

    public bool ReachedDestination(float threshold) {
        if (!agent.isOnNavMesh || agent.pathPending) return false;
        // Check remaining distance and if the agent has stopped trying to pathfind
        if (agent.remainingDistance <= threshold && !agent.hasPath && agent.velocity.sqrMagnitude < 0.1f) {
            return true;
        }
        return false;
    }

    public override void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        if (projectile != null) {
            playerLastKnownPosition = projectile.CreationPosition;
            TakeDamage(projectile.damage);
            if (health <= 0) {
                Die();
            }
            else {
                // If hit, immediately check if player is visible
                if (IsPlayerInView()) {
                    // Player seen, immediately go to Distancing
                    if (CurrentState != DistancingState) ChangeState(DistancingState);
                }
                else {
                    // Player not seen, go search LKP
                    if (CurrentState != SearchingState) ChangeState(SearchingState);
                }
            }
        }
        else {
            base.BulletHit(bullet); // Fallback
        }
    }

    // --- State Definitions ---

    public class TankIdleState: BaseState<TankController> {
        public TankIdleState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.agent.speed = controller.moveSpeed;
            controller.agent.isStopped = true;
            if (controller.PickNewPatrolPoint()) {
                controller.SetAgentDestination(controller.currentPatrolTarget);
            }
        }

        public override void Execute() {
            // Check for player first
            if (controller.IsPlayerInView()) {
                // Seeing player immediately triggers distancing
                controller.ChangeState(controller.DistancingState);
                return;
            }

            // Manage patrolling
            if (!controller.hasPatrolTarget || controller.ReachedDestination(controller.patrolPointReachThreshold)) {
                if (controller.PickNewPatrolPoint()) {
                    controller.SetAgentDestination(controller.currentPatrolTarget);
                }
                else {
                    controller.StopMovement(); // Stop if no valid point found
                }
            }
        }

        public override void Exit() {
            controller.StopMovement();
        }
    }

    // ShootingState is removed. DistancingState handles shooting.

    public class TankDistancingState: BaseState<TankController> {
        private float recalculateTimer; // Timer to track when to recalculate destination
        private const float RECALCULATE_INTERVAL = 1.0f; // Recalculate every 1 second

        public TankDistancingState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            Debug.Log($"[{controller.gameObject.name}] Entering Distancing State."); // For debugging
            controller.agent.speed = controller.moveSpeed; // Ensure speed is set correctly
            controller.StartShooting();
            recalculateTimer = 0f; // Force immediate calculation on enter
                                   // Optional: Stop movement initially until first calculation?
                                   // controller.StopMovement();
        }

        public override void Execute() {
            // --- Check Player Visibility ---
            if (!controller.IsPlayerInView()) {
                controller.ChangeState(controller.SearchingState);
                return; // Exit early after state change
            }

            // --- Aiming ---
            // Player is in view, always aim at them
            if (controller.playerTarget != null) {
                controller.TurnToTarget(controller.playerTarget.transform.position);
            }

            // --- Movement Decision (Timed Recalculation) ---
            recalculateTimer -= Time.deltaTime;

            if (recalculateTimer <= 0f) {
                recalculateTimer = RECALCULATE_INTERVAL; // Reset timer

                // Calculate distance ONLY when recalculating movement decision
                float currentDistance = Vector3.Distance(controller.transform.position, controller.playerTarget.transform.position);
                float desiredDist = controller.range;
                const float buffer = 0.5f; // Buffer to prevent jitter

                // Decide whether to move away or stop
                if (currentDistance < desiredDist - buffer) {
                    // Player is too close, calculate retreat point and move
                    Vector3 retreatPoint = controller.CalculateRetreatPoint();

                    // Check if the calculated point is valid and reasonably far enough to warrant moving
                    if (Vector3.Distance(retreatPoint, controller.transform.position) > controller.agent.stoppingDistance) {
                        Debug.Log($"[{controller.gameObject.name}] Distancing: Player too close ({currentDistance} < {desiredDist - buffer}). Moving to retreat point: {retreatPoint}"); // Debugging
                        controller.SetAgentDestination(retreatPoint);
                        // SetAgentDestination should handle agent.isStopped = false;
                    }
                    else {
                        // Calculated point is too close or invalid, stop moving
                        Debug.Log($"[{controller.gameObject.name}] Distancing: Retreat point too close or invalid. Stopping."); // Debugging
                        controller.StopMovement();
                    }
                }
                else {
                    // Player is at or beyond the desired range, stop moving
                    Debug.Log($"[{controller.gameObject.name}] Distancing: Player in range ({currentDistance} >= {desiredDist - buffer}). Stopping movement."); // Debugging
                    controller.StopMovement();
                }
            }
            // --- End Movement Decision ---

            // Note: The agent continues moving towards the last set destination
            // between recalculation intervals if SetAgentDestination was called.
        }

        public override void Exit() {
            Debug.Log($"[{controller.gameObject.name}] Exiting Distancing State."); // Debugging
            controller.StopMovement(); // Ensure movement stops when leaving state
                                       // Optionally stop shooting here if SearchingState doesn't handle it
                                       // controller.StopShooting(); (depends if you want to stop immediately or let Searching handle it)
        }
    }

    public class TankSearchingState: BaseState<TankController> {
        public TankSearchingState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.StopShooting(); // Stop shooting when searching
            controller.agent.speed = controller.moveSpeed;
            if (controller.playerLastKnownPosition != Vector3.zero) {
                controller.SetAgentDestination(controller.playerLastKnownPosition);
            }
            else {
                controller.ChangeState(controller.IdleState); // No LKP, go idle
            }
        }

        public override void Execute() {
            if (controller.IsPlayerInView()) {
                // Found player, switch to distancing
                controller.ChangeState(controller.DistancingState);
            }
            else if (controller.ReachedDestination(controller.agent.stoppingDistance)) {
                // Reached LKP without finding player, go idle
                controller.ChangeState(controller.IdleState);
            }
            // Else: continue moving towards LKP
        }

        public override void Exit() {
            controller.StopMovement();
        }
    }
}