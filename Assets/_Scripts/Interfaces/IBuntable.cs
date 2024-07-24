using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuntable {
    void Bunt(Vector2 direction, float verticalVelocity, TeamAssigner teamAssigner);
}
