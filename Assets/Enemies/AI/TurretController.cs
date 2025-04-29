using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurretController: BaseAIController<TurretController> {
    // TODO: Add variables for turret
    public float turnSpeed = 90f;
    public float engagementTimeout = 5f;
    [HideInInspector]
    public float engagementTime = 0f;
    [HideInInspector]
    public Vector3 playerLastKnownPosition = Vector3.zero;


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

    //TODO: Add turret logic Functions
    public void TurnToTarget(Vector3 targetPosition) {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    public void TurnToPlayerLastKnownPosition() {
        TurnToTarget(playerLastKnownPosition);
    }

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
            }
            else if (IsPlayerInRange()) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            }
            else {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(this.transform.position, playerTarget.transform.position);
            }
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(playerLastKnownPosition, 1f);
    }
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
        } else if (controller.engagementTime < controller.engagementTimeout) {
            controller.engagementTime += Time.deltaTime;
            controller.TurnToPlayerLastKnownPosition();
        } else {
            controller.ChangeState(controller.IdleState);
        }
    }

    public override void Exit() {
        controller.StopShooting();    
    }


}
