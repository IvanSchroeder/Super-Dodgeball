using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHittable {
    void Hit(Vector2 direction, float hitForce, TeamAssigner teamAssigner);
}