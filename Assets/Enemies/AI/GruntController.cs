using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using QFX.SFX;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class GruntController: BaseAIController<GruntController> {
    public float shootFOV = 20f;
    public float moveSpeed = 400f;
    public float strafeSpeed = 400f;
    public float[] strafeDuration = { 1f, 3f };
    public float acceleration = 20f;
    public float brakingDrag = 800f;

    [HideInInspector]
    public NavMeshAgent agent;
    [HideInInspector]
    public Rigidbody rb;

    private Vector3 strafeDirection;
    private float nextStrafeTime = 0f;
    private float defaultDrag;
    private float defaultAngularDrag;
    private float defaultTurnSpeed;
    private bool isBraking = false;

    public GruntIdleState IdleState { get; private set; }
    public GruntPursuingState PursuingState { get; private set; }
    public GruntStrafingState StrafingState { get; private set; }

    protected override void Awake() {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        defaultDrag = rb.drag;
        defaultAngularDrag = rb.angularDrag;
        defaultTurnSpeed = turnSpeed;

        IdleState = new GruntIdleState(this);
        PursuingState = new GruntPursuingState(this);
        StrafingState = new GruntStrafingState(this);
    }

    protected override void Start() {
        InitializeState(IdleState);
        base.Start();
    }

    protected override void Update() {
        if (!rb.IsSleeping() && agent.isOnNavMesh) agent.nextPosition = rb.position;
        base.Update();
    }


    void FixedUpdate() {
        if (CurrentState == PursuingState) {
            MoveUsingAgent();
            if (playerLastKnownPosition != Vector3.zero) {
                Vector3 directionToLKP = playerLastKnownPosition - rb.position;
            }
        }
        else if (CurrentState == StrafingState) {
            ApplyStrafeMovement();
        }
    }

    private void LateUpdate() {
        if (CurrentState != IdleState) HandleFacingRotation();
    }

    private void MoveUsingAgent() {
        if (!agent.isOnNavMesh || !agent.hasPath || isBraking) return;

        Vector3 desiredVelocity = agent.desiredVelocity;
        if (desiredVelocity.sqrMagnitude < 0.1f) return;

        Vector3 targetVelocity = desiredVelocity.normalized * moveSpeed;
        Vector3 currentVelocity = rb.velocity;
        Vector3 velocityChange = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime) - currentVelocity;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void ApplyStrafeMovement() {
        if (isBraking) return;

        Vector3 targetVelocity = strafeDirection * strafeSpeed;
        Vector3 currentVelocity = rb.velocity;
        targetVelocity.y = currentVelocity.y;
        Vector3 velocityChange = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime) - currentVelocity;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    public bool isPlayerInShootView() {
        Vector3 directionToPlayer = (playerTarget.transform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) <= (shootFOV / 2)) return true;
        return false;
    }

    public bool isPLKPInView() {
        Vector3 directionToLKP = (playerLastKnownPosition - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToLKP) <= (shootFOV / 2)) return true;
        return false;
    }

    private void StartMoving() {
        isBraking = false;
        rb.drag = defaultDrag;
        rb.angularDrag = defaultAngularDrag;
        if (agent.isOnNavMesh) agent.isStopped = false;
    }


    public void SetAgentDestination(Vector3 targetPosition) {
        if (agent.isOnNavMesh) {
            agent.SetDestination(targetPosition);
        }
    }

    public void Brake() {
        isBraking = true;
        if (agent.isOnNavMesh) agent.isStopped = true;
        rb.drag = brakingDrag;
    }

    public void ChooseNewStrafeDirection() {
        float directionChoice = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        strafeDirection = transform.right * directionChoice;
    }

    public void CalculateNextStrafeTime() {
        nextStrafeTime = Time.time + Random.Range(strafeDuration[0], strafeDuration[1]);
    }

    private void HandleFacingRotation() {
        Vector3 targetPosition = Vector3.zero;
        bool hasTarget = false;

        if (IsPlayerInView() && playerTarget != null) {
            targetPosition = playerTarget.transform.position;
            hasTarget = true;
        }
        else if (playerLastKnownPosition != Vector3.zero) {
            targetPosition = playerLastKnownPosition;
            hasTarget = true;
        }

        if (hasTarget) {
            Vector3 direction = targetPosition - transform.position;

            if (direction.sqrMagnitude > 0.01f) {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }

    public override void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        if (projectile != null) {
            playerLastKnownPosition = projectile.CreationPosition;
            TakeDamage(projectile.damage);
            if (health <= 0) {
                Die();
            }
            else if (CurrentState == IdleState) {
                ChangeState(PursuingState);
            }
        }
    }


    public class GruntIdleState: BaseState<GruntController> {
        public GruntIdleState(GruntController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.Brake();
        }

        public override void Execute() {
            controller.IdleAnim();
            if (controller.IsPlayerInView()) {
                controller.playerLastKnownPosition = controller.playerTarget.transform.position;
                controller.ChangeState(controller.PursuingState);
            }
        }

        public override void Exit() { }
    }

    public class GruntPursuingState: BaseState<GruntController> {
        private const float TargetReachedThreshold = 1.5f;

        public GruntPursuingState(GruntController ctrl) : base(ctrl) { }

        public override void Enter() {

            controller.StartMoving();
        }

        public override void Execute() {
            // if(controller.IsPlayerInRange() || !controller.IsPlayerInView()) controller.ChangeState(controller.StrafingState);
            if (controller.IsPlayerInView()) {
                controller.turnSpeed = controller.defaultTurnSpeed;
                controller.playerLastKnownPosition = controller.playerTarget.transform.position;
                if (controller.isPlayerInShootView()) controller.StartShooting();
                if (controller.IsPlayerInRange()) {
                    controller.ChangeState(controller.StrafingState);
                    return;
                }
            }
            else {
                controller.turnSpeed = controller.defaultTurnSpeed * 3f;
                controller.StopShooting();
                // controller.TurnToPlayerLastKnownPosition();
                if (controller.isPLKPInView()) {
                    float distToLKP = Vector3.Distance(controller.rb.position, controller.playerLastKnownPosition);
                    // Debug.Log($"Distance to LKP: {distToLKP}");

                    bool Reached = controller.agent.hasPath &&
                                              !controller.agent.pathPending &&
                                              controller.agent.remainingDistance <= controller.agent.stoppingDistance &&
                                              controller.rb.velocity.sqrMagnitude < 0.5f;

                    if (distToLKP <= TargetReachedThreshold || Reached) {
                        controller.ChangeState(controller.IdleState);
                        return;
                    }
                }

            }

            controller.SetAgentDestination(controller.playerLastKnownPosition);
        }

        public override void Exit() {
        }
    }

    public class GruntStrafingState: BaseState<GruntController> {
        public GruntStrafingState(GruntController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.StartMoving();
            controller.ChooseNewStrafeDirection();
            controller.CalculateNextStrafeTime();
        }

        public override void Execute() {
            controller.TurnToPlayerLastKnownPosition();
            if (!controller.IsPlayerInView()) {
                controller.ChangeState(controller.PursuingState);
            }
            else if (!controller.IsPlayerInRange()) {
                controller.ChangeState(controller.PursuingState);
            }
            else {
                controller.playerLastKnownPosition = controller.playerTarget.transform.position;
                if (controller.isPlayerInShootView()) controller.StartShooting();
                if (Time.time >= controller.nextStrafeTime) {
                    controller.ChooseNewStrafeDirection();
                    controller.CalculateNextStrafeTime();
                }
            }
        }

        public override void Exit() {
        }
    }
}