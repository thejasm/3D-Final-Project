using UnityEngine;

/// T is the type of the AI Controller this state belongs to.
public abstract class BaseState<T> where T : BaseAIController<T> {
    protected T controller;

    public BaseState(T controller) {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}
