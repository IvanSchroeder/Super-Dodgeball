using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using UnityEngine.Events;

public class FakeHeightObject : MonoBehaviour {
    public UnityEvent onGroundHitEvent;

    public Transform transformObject;
    public Transform transformBody;
    public Transform transformShadow;

    public float gravity = -10;
    public Vector2 groundVelocity;
    public float verticalVelocity;
    private float lastVerticalVelocity;
    private float lastHorizontalVelocity;
    public float verticalDivisionFactor;
    public float horizontalDivisionFactor;

    public bool isGrounded;
    public bool isStatic;

    void Update() {
        UpdatePositions();
        CheckGroundHit();
    }

    public void Initialize(Vector2 groundVelocity, float verticalVelocity) {
        isGrounded = false;
        this.groundVelocity = groundVelocity;
        this.verticalVelocity = verticalVelocity;
        lastVerticalVelocity = verticalVelocity;
    }

    void UpdatePositions() {
        if (!isGrounded) {
            verticalVelocity += gravity * Time.deltaTime;
            transformBody.position += new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;
        }

        transformObject.position += (Vector3)groundVelocity * Time.deltaTime;
    }

    void CheckGroundHit() {
        if (transformBody.position.y < transformShadow.position.y && !isGrounded) {
            transformBody.position = transformShadow.position;
            isGrounded = true;
            GroundHit();
        }
    }

    void GroundHit() {
        onGroundHitEvent.Invoke();
    }

    public void Stick() {
        groundVelocity = Vector2.zero;
        isGrounded = true;
        this.gameObject.Destroy(0f);
    }

    public void DestroyInmediatly() {
        this.gameObject.Destroy();
    }

    public void VerticalBounce() {
        isGrounded = false;
    }

    public void CollisionBounce() {

    }

    public void VerticalBounceWithSlowdown() {
        isGrounded = false;
        verticalVelocity /= verticalDivisionFactor;
        lastVerticalVelocity = verticalVelocity;
        Initialize(groundVelocity, verticalVelocity);
    }

    public void SlowdownGroundVelocity() {
        groundVelocity /= horizontalDivisionFactor;
        Initialize(groundVelocity, verticalVelocity);

        if (groundVelocity == Vector2.zero) {
            this.gameObject.Destroy(1f);
        }
    }
}
