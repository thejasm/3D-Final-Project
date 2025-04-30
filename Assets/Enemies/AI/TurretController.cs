using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using QFX.SFX;

public class TurretController: BaseAIController<TurretController> {
    // TODO: Add variables for turret
    public float engagementTimeout = 5f;
    [HideInInspector]
    public float engagementTime = 0f;


    public TurretIdleState IdleState { get; private set; }
    public TurretAttackingState AttackState { get; private set; }


    protected override void Awake() {
        base.Awake();

        IdleState = new TurretIdleState(this);
        AttackState = new TurretAttackingState(this);
    }

    protected override void Start() {
        InitializeState(IdleState);
        base.Start();
    }

    // Update method is handled by BaseAIController

    public override void BulletHit(GameObject bullet) {
        SFX_SimpleProjectile projectile = bullet.GetComponent<SFX_SimpleProjectile>();
        playerLastKnownPosition = projectile.CreationPosition;
        ChangeState(AttackState);
        if (health >= 0) health -= projectile.damage;
        else Die();
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
            controller.StartShooting();
        }

        public override void Execute() {
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
            controller.StopShooting();
        }
    }

}
