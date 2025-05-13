using UnityEngine;
using UnityEngine.AI;
using QFX.SFX;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
public class TankController: BaseAIController<TankController> {
    public float moveSpeed = 4.0f;
    public float patrolPointReachThreshold = 1.5f;
    public float coolDown = 2f;
    public float angleCorrectionLimit = 10f;

    [HideInInspector]
    public NavMeshAgent agent;
    private Vector3 currentPatrolTarget = Vector3.zero;
    private bool hasPatrolTarget = false;
    private GaussGunController gunController;
    private bool readyToFire = true;

    public TankIdleState IdleState { get; private set; }
    public TankDistancingState DistancingState { get; private set; }
    public TankSearchingState SearchingState { get; private set; }

    protected override void Awake() {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        gunController = gun.GetComponent<GaussGunController>();

        agent.speed = moveSpeed;
        agent.updateRotation = true;
        agent.updatePosition = true;

        IdleState = new TankIdleState(this);
        DistancingState = new TankDistancingState(this);
        SearchingState = new TankSearchingState(this);
    }

    protected override void Start() {
        InitializeState(IdleState);
    }

    protected override void Update() {
        if (IsPlayerInView()) playerLastKnownPosition = playerTarget.transform.position;
        base.Update();
    }

    public void SetAgentDestination(Vector3 targetPosition) {
        if (agent.isOnNavMesh) {
            agent.isStopped = false;
            agent.SetDestination(targetPosition);
        }
        else Debug.LogWarning("Agent not on NavMesh");
    }

    public void StopMovement() {
        if (agent.isOnNavMesh && !agent.isStopped) {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public bool PickNewPatrolPoint() {
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
            Debug.LogWarning("Could not find valid NavMesh point for patrol");
            return false;
        }
    }

    public Vector3 CalculateRetreatPoint() {
        if (playerTarget == null) return transform.position;

        Vector3 directionToPlayer = playerTarget.transform.position - transform.position;
        float currentDistance = directionToPlayer.magnitude;
        Vector3 directionAwayFromPlayer = -directionToPlayer.normalized;
        Vector3 idealRetreatPos = transform.position + directionAwayFromPlayer * (range - currentDistance);

        Debug.DrawLine(transform.position, playerTarget.transform.position, Color.red, 0.5f);
        Debug.DrawLine(transform.position, idealRetreatPos, Color.cyan, 0.5f);

        NavMeshHit hit;

        if (NavMesh.SamplePosition(idealRetreatPos, out hit, range, NavMesh.AllAreas)) return hit.position;
        else {
            Vector3 fallbackPos = transform.position + directionAwayFromPlayer * range;
            Debug.DrawLine(transform.position, fallbackPos, Color.magenta, 0.5f);

            if (NavMesh.SamplePosition(fallbackPos, out hit, range, NavMesh.AllAreas)) return hit.position;
            else return transform.position;
        }
    }

    public bool ReachedDestination(float threshold) {
        if (!agent.isOnNavMesh || agent.pathPending) return false;
        if (agent.remainingDistance <= threshold && !agent.hasPath && agent.velocity.sqrMagnitude < 0.1f) return true;
        return false;
    }

    public override void StartShooting() {
        if (!IsPlayerInRange() && playerLastKnownPosition != Vector3.zero) {
            Vector3 currentGunDirection = gun.transform.forward;

            Vector3 directionToLKP = (playerLastKnownPosition - gun.transform.position);

            if (directionToLKP.sqrMagnitude > 0.01f) {
                float angleDifference = Vector3.Angle(currentGunDirection, directionToLKP);

                if (angleDifference <= angleCorrectionLimit) {
                    Quaternion lookRotationToLKP_Global = Quaternion.LookRotation(directionToLKP);

                    Quaternion lookRotationToLKP_Local = Quaternion.Inverse(transform.rotation) * lookRotationToLKP_Global;

                    gun.transform.localRotation = lookRotationToLKP_Local;

                    float newPitch = gun.transform.localEulerAngles.x;
                    if (newPitch > 180f) newPitch -= 360f;
                    currentPitch = newPitch;
                }
            }
        }

        if (gunController.ReadyToFire) {
            this.GetComponent<Animator>().SetTrigger("Fire");
            gunController.Fire();
            StartCoroutine(OnCoolDown());
        } else if (this.readyToFire) {
            gunController.ChargeUp();
        }
    }

    public IEnumerator OnCoolDown() {
        readyToFire = false;
        yield return new WaitForSeconds(coolDown);
        readyToFire = true;
    }

    public override void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        if (projectile != null) {
            playerLastKnownPosition = projectile.CreationPosition;
            TakeDamage(projectile.damage);
            if (health <= 0) Die();
            else {

                if (IsPlayerInView()) {
                    if (CurrentState != DistancingState) ChangeState(DistancingState);
                }
                else {
                    if (CurrentState != SearchingState) ChangeState(SearchingState);
                }
            }
        }
        else base.BulletHit(bullet);
    }

    public class TankIdleState: BaseState<TankController> {
        public TankIdleState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.agent.speed = controller.moveSpeed;
            controller.agent.isStopped = true;
            if (controller.PickNewPatrolPoint()) controller.SetAgentDestination(controller.currentPatrolTarget);
        }

        public override void Execute() {

            if (controller.IsPlayerInView()) {
                controller.ChangeState(controller.DistancingState);
                return;
            }

            if (!controller.hasPatrolTarget || controller.ReachedDestination(controller.patrolPointReachThreshold)) {
                if (controller.PickNewPatrolPoint()) controller.SetAgentDestination(controller.currentPatrolTarget);
                else controller.StopMovement();
            }
        }

        public override void Exit() {
            controller.StopMovement();
        }
    }

    public class TankDistancingState: BaseState<TankController> {
        private float recalculateTimer;
        private const float RECALCULATE_INTERVAL = 1.0f;

        public TankDistancingState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.agent.speed = controller.moveSpeed;
            recalculateTimer = 0f;

        }

        public override void Execute() {
            controller.StartShooting();

            if (!controller.IsPlayerInView()) {
                controller.ChangeState(controller.SearchingState);
                return;
            }

            if (controller.playerTarget != null) {
                controller.TurnToTarget(controller.playerTarget.transform.position);
            }

            recalculateTimer -= Time.deltaTime;

            if (recalculateTimer <= 0f) {
                recalculateTimer = RECALCULATE_INTERVAL;

                float currentDistance = Vector3.Distance(controller.transform.position, controller.playerTarget.transform.position);
                float desiredDist = controller.range;
                const float buffer = 0.5f;

                if (currentDistance < desiredDist - buffer) {
                    Vector3 retreatPoint = controller.CalculateRetreatPoint();

                    if (Vector3.Distance(retreatPoint, controller.transform.position) > controller.agent.stoppingDistance) {
                        controller.SetAgentDestination(retreatPoint);
                    }
                    else controller.StopMovement();
                } else controller.StopMovement();
            }

        }

        public override void Exit() {
            controller.StopMovement();

        }
    }

    public class TankSearchingState: BaseState<TankController> {
        public TankSearchingState(TankController ctrl) : base(ctrl) { }

        public override void Enter() {
            controller.StopShooting();
            controller.agent.speed = controller.moveSpeed;
            if (controller.playerLastKnownPosition != Vector3.zero) controller.SetAgentDestination(controller.playerLastKnownPosition);
            else controller.ChangeState(controller.IdleState);
        }

        public override void Execute() {
            if (controller.IsPlayerInView()) controller.ChangeState(controller.DistancingState);
            else if (controller.ReachedDestination(controller.agent.stoppingDistance)) controller.ChangeState(controller.IdleState);
        }

        public override void Exit() {
            controller.StopMovement();
        }
    }
}