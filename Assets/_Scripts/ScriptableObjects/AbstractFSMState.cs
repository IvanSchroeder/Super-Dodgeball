using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExecutionState {
    None,
    Active,
    Completed,
    Terminated,
};

public abstract class AbstractFSMState : ScriptableObject {
    public ExecutionState ExecutionState { get; protected set; }

    public virtual void OnEnable() {
        ExecutionState = ExecutionState.None;
    }

    public virtual bool EnterState() {
        ExecutionState = ExecutionState.Active;
        return true;
    }

    public abstract void UpdateState();

    public virtual bool ExitState() {
        ExecutionState = ExecutionState.Completed;
        return true;
    }
}
