using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using QFX.SFX;

public class TurretController: BaseAIController<TurretController> {
    public float engagementTimeout = 5f;
    public float coolDown = 3f;
    private bool readyToFire = true;
    [HideInInspector]
    public float engagementTime = 0f;

    private WraithGunController gunController;
    private const float EffectiveGravity = 8.82f;
    private float shotspeed = 0f;
    private float distance = 0f;


    public TurretIdleState IdleState { get; private set; }
    public TurretAttackingState AttackState { get; private set; }


    protected override void Awake() {
        base.Awake();

        IdleState = new TurretIdleState(this);
        AttackState = new TurretAttackingState(this);
        gunController = gun.GetComponent<WraithGunController>();
    }

    protected override void Start() {
        InitializeState(IdleState);
        base.Start();
    }

    // Update method is handled by BaseAIController

    public override void TurnToTarget(Vector3 targetPosition) {
        Vector3 directionToTargetHorizontal = targetPosition - transform.position;
        directionToTargetHorizontal.y = 0;

        Quaternion horizontalLookRotation = Quaternion.identity;
        if (directionToTargetHorizontal.sqrMagnitude > 0.01f) horizontalLookRotation = Quaternion.LookRotation(directionToTargetHorizontal.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, horizontalLookRotation, turnSpeed * Time.deltaTime);
    }

    public override void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        playerLastKnownPosition = projectile.CreationPosition;
        engagementTime = 0;
        ChangeState(AttackState);
        if (health >= 0) health -= projectile.damage;
        else Die();
    }

    public void CalculateFiringSolution() {
        Vector3 positionXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetXZ = new Vector3(playerLastKnownPosition.x, 0, playerLastKnownPosition.z);
        distance = Vector3.Distance(positionXZ, targetXZ);
        shotspeed = Mathf.Sqrt(distance * EffectiveGravity);

        gunController.shotSpeed = shotspeed;
        //Debug.Log($"Calculated shot speed: {shotspeed} for distance: {distance}");
    }

    public override void StartShooting() {
        if(gunController.ReadyToFire) {
            gunController.Fire();
            StartCoroutine(OnCoolDown());
        }else if(this.readyToFire) {
            gunController.ChargeUp();
        }
    }

    public IEnumerator OnCoolDown() {
        readyToFire = false;
        yield return new WaitForSeconds(coolDown);
        readyToFire = true;
    }



    public class TurretIdleState: BaseState<TurretController> {

    public TurretIdleState(TurretController controller) : base(controller) { }

    public override void Enter() {

    }


    public override void Execute() {
        controller.IdleAnim();

        if (controller.IsPlayerInView() && controller.IsPlayerInRange()) controller.ChangeState(controller.AttackState);
    }

    public override void Exit() { }
}

    public class TurretAttackingState: BaseState<TurretController> {

        public TurretAttackingState(TurretController controller) : base(controller) { }

        public override void Enter() {
        }

        public override void Execute() {
            controller.CalculateFiringSolution();
            controller.StartShooting();
            if (controller.IsPlayerInView()) {
                controller.playerLastKnownPosition = controller.playerTarget.transform.position;
                controller.engagementTime = 0f;
                controller.TurnToPlayerLastKnownPosition();
            }
            else if (controller.engagementTime < controller.engagementTimeout) {
                controller.engagementTime += Time.deltaTime;
                controller.TurnToPlayerLastKnownPosition();
            }
            else {
                controller.ChangeState(controller.IdleState);
            }
        }

        public override void Exit() {
        }
    }

}
