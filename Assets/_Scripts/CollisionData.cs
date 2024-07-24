using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public struct CollisionData {
    public GameObject CollidedWith { get; set; }
}

[Serializable]
public class CollisionEvent : UnityEvent<CollisionData> { }
