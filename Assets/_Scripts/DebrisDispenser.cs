using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
//using GD.MinMaxSlider;

public class DebrisDispenser : MonoBehaviour {
    [Header("Refenrences")]
    public GameObject debrisParticlePrefab;
    public GameObject debrisMarkPrefab;

    [Header("Debris Parameters")]
    public Color debrisParticleColor;
    public Color debrisMarkColor;
    //[MinMaxSlider(0f, 100f)]
    public Vector2 debrisGroundVelocityRange;
    //[MinMaxSlider(0f, 100f)]
    public Vector2 debrisVerticalVelocityRange;
    //[MinMaxSlider(0f, 5f)]
    public Vector2 debrisParticleSizeRange;
    //[MinMaxSlider(0f, 10f)]
    public Vector2 debrisMarkSizeRange;
    //[MinMaxSlider(0, 20)]
    public Vector2Int debrisAmountRange;
    public bool dispenseInCone = true;
    //[MinMaxSlider(0f, 360f)]
    public Vector2 debrisDispenseDegrees;

    public Vector2 moveDirection;
    public Vector2 lastPosition;
    private Vector2 debrisDirection = new Vector2();

    void Update() {
        moveDirection = ((Vector2)transform.position - lastPosition).normalized;
    }

    void LateUpdate() {
        lastPosition = transform.position;
    }

    public void DispenseDebris() {
        if (debrisMarkPrefab != null) {
            GameObject debrisMark = Instantiate(debrisMarkPrefab, transform.position, Quaternion.identity);
            debrisMark.GetComponentInHierarchy<SpriteRenderer>().color = debrisMarkColor;
            debrisMark.transform.localScale = Vector3.one * Random.Range(debrisMarkSizeRange.x, debrisMarkSizeRange.y);
        }
        
        int debrisAmount = Random.Range(debrisAmountRange.x, debrisAmountRange.y);

        float angle1 = 0f;
        float angle2 = 0f;
        float directionAngle1 = 0f;
        float directionAngle2 = 0f;
        Vector2 debrisDirection1 = new Vector2();
        Vector2 debrisDirection2 = new Vector2();

        float randomDispenseDegree = Random.Range(debrisDispenseDegrees.x, debrisDispenseDegrees.y);

        if (dispenseInCone) {
            angle1 = -randomDispenseDegree / 2;
            angle2 = randomDispenseDegree / 2;
            directionAngle1 = Mathf.Atan2(moveDirection.y, moveDirection.x) + (angle1 * Mathf.Deg2Rad);
            directionAngle2 = Mathf.Atan2(moveDirection.y, moveDirection.x) + (angle2 * Mathf.Deg2Rad);

            debrisDirection1.x = Mathf.Cos(directionAngle1);
            debrisDirection1.y = Mathf.Sin(directionAngle1);

            debrisDirection2.x = Mathf.Cos(directionAngle2);
            debrisDirection2.y = Mathf.Sin(directionAngle2);

            Debug.DrawRay(transform.position, debrisDirection1.normalized * 10f, Color.red, 3f);
            Debug.DrawRay(transform.position, debrisDirection2.normalized * 10f, Color.red, 3f);
        }

        for (int i = 0; i < debrisAmount; i++) {
            FakeHeightObject debrisParticle = Instantiate(debrisParticlePrefab, transform.position, Quaternion.identity).GetComponentInHierarchy<FakeHeightObject>();
            debrisParticle.transform.localScale = Vector3.one * Random.Range(debrisParticleSizeRange.x, debrisParticleSizeRange.y);

            if (!dispenseInCone) {
                debrisParticle.Initialize(Random.insideUnitCircle * Random.Range(debrisGroundVelocityRange.x, debrisGroundVelocityRange.y),
                    Random.Range(debrisVerticalVelocityRange.x, debrisVerticalVelocityRange.y));
            }
            else if (dispenseInCone) {
                float randomAngle = Random.Range(angle1, angle2);
                float randomizedDirectionAngle = Mathf.Atan2(moveDirection.y, moveDirection.x)
                    + (randomAngle * Mathf.Deg2Rad);
                debrisDirection.x = Mathf.Cos(randomizedDirectionAngle);
                debrisDirection.y = Mathf.Sin(randomizedDirectionAngle);

                debrisParticle.Initialize(debrisDirection.normalized * Random.Range(debrisGroundVelocityRange.x, debrisGroundVelocityRange.y),
                    Random.Range(debrisVerticalVelocityRange.x, debrisVerticalVelocityRange.y));
            }
        }

    }
}
