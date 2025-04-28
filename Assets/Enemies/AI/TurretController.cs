using UnityEngine;
using System.Collections;

public class TurretController: BaseAIController<TurretController> {
    // TODO: Add variables for turret

    public TurretIdleState IdleState { get; private set; }
    public TurretAttackingState AttackingState { get; private set; }


    protected override void Awake() {
        base.Awake(); //Empty for now

        IdleState = new TurretIdleState(this);
        AttackingState = new TurretAttackingState(this);
    }

    protected override void Start() {
        InitializeState(IdleState);
        base.Start();
    }

    // Update method is handled by BaseAIController

    //TODO: Add turret logic Functions
}


public class TurretIdleState: BaseState<TurretController> {

    public TurretIdleState(TurretController controller) : base(controller) { }

    public override void Enter() { }

    public override void Execute() { }

    public override void Exit() { }
}

public class TurretAttackingState: BaseState<TurretController> {

    public TurretAttackingState(TurretController controller) : base(controller) { }

    public override void Enter() { }

    public override void Execute() { }

    public override void Exit() { }
}
