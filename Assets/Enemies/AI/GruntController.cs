using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using QFX.SFX;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class GruntController: BaseAIController<GruntController> {
    public float moveSpeed = 5f;
    public float strafeSpeed = 3f;
    public float[] strafeDuration = { 1f, 3f };
    public float acceleration = 10f;
    public float brakingDrag = 5f;
    public float brakingAngularDrag = 5f;

    [HideInInspector]
    public NavMeshAgent agent;
    [HideInInspector]
    public Rigidbody rb;

    private Vector3 strafeDirection;
    private float nextStrafeTime = 0f;
    private float defaultDrag;
    private float defaultAngularDrag;
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
            RotateTowards(agent.desiredVelocity);
        }
        else if (CurrentState == StrafingState) {
            ApplyStrafeMovement();
            if (playerTarget != null) RotateTowards(playerTarget.transform.position - rb.position);
        }
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


    private void RotateTowards(Vector3 direction) {
        if (isBraking) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion currentRotation = rb.rotation;
        Quaternion newRotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRotation);
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
        rb.angularDrag = brakingAngularDrag;
    }

    public void ChooseNewStrafeDirection() {
        float directionChoice = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        strafeDirection = transform.right * directionChoice;
    }

    public void CalculateNextStrafeTime() {
        nextStrafeTime = Time.time + Random.Range(strafeDuration[0], strafeDuration[1]);
    }

    public override void Die() {
        base.Die();
        Brake();
        StopShooting();
        this.enabled = false;
        if (agent != null) agent.enabled = false;
        rb.isKinematic = true; // Freeze physics
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
            Execute();
        }

        public override void Execute() {
            Vector3 targetPosition;
            bool targetVisible = controller.IsPlayerInView();

            if (targetVisible) {
                targetPosition = controller.playerTarget.transform.position;
                controller.playerLastKnownPosition = targetPosition;
                if (controller.IsPlayerInRange()) {
                    controller.ChangeState(controller.StrafingState);
                    return;
                }
            }
            else {
                targetPosition = controller.playerLastKnownPosition;
                float distToLKP = Vector3.Distance(controller.rb.position, targetPosition);

                bool destinationReached = !controller.agent.pathPending &&
                                     controller.agent.remainingDistance <= controller.agent.stoppingDistance &&
                                     (!controller.agent.hasPath || controller.rb.velocity.sqrMagnitude < 0.5f); // Check Rigidbody velocity

                if (distToLKP <= TargetReachedThreshold || destinationReached) {
                    controller.ChangeState(controller.IdleState);
                    return;
                }
            }

            controller.SetAgentDestination(targetPosition);
        }

        public override void Exit() {
            // Movement stop/brake handled by entering state
        }
    }

    public class GruntStrafingState: BaseState<GruntController> {
        public GruntStrafingState(GruntController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.StartMoving();
            controller.ChooseNewStrafeDirection();
            controller.CalculateNextStrafeTime();
            controller.StartShooting();
        }

        public override void Execute() {
            controller.TurnToPlayerLastKnownPosition();
            if (!controller.IsPlayerInView()) {
                controller.ChangeState(controller.IdleState);
            }
            else if (!controller.IsPlayerInRange()) {
                controller.ChangeState(controller.PursuingState);
            }
            else {
                controller.playerLastKnownPosition = controller.playerTarget.transform.position;

                if (Time.time >= controller.nextStrafeTime) {
                    controller.ChooseNewStrafeDirection();
                    controller.CalculateNextStrafeTime();
                }
            }
        }

        public override void Exit() {
            controller.StopShooting();
        }
    }
}