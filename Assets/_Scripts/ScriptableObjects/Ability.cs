using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability : ScriptableObject {
    public new string name;
    public float cooldownTime;
    public float activeTime;

    public virtual void Activate(GameObject agent) {}

    public virtual void Activate2(PlayerController controller) {}
}

public enum AbilityState {
    Ready,
    Active,
    Cooldown
}
