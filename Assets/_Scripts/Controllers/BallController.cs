using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using System;

public enum BallState {
    Floating,
    Moving,
    Bunted,
    Delayed
}

public class BallController : MonoBehaviour, IHittable, IBuntable {
    public ShaderPropertyIDs shaderProperties;

    public Material material;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer ballSprite;
    [SerializeField] private SpriteRenderer ballShadow;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField]public TeamAssigner ballTeamAssigner;
    [SerializeField] private LayerMask hitCollisionMask;
    [SerializeField] private LayerMask terrainCollisionMask;
    [SerializeField] private LayerMask buntedCollisionMask;
    [SerializeField] public BallState ballState;

    [Header("Speed Parameters")]
    [SerializeField] private FloatSO currentBallSpeed;
    [SerializeField] public float currentSpeed;
    [SerializeField] private float initialSpeed;
    [SerializeField] private float targetMoveSpeed;
    [SerializeField] private float defaultMaxSpeed;
    [SerializeField] private bool hasDynamicSpeed = true;
    [SerializeField] public float dynamicMaxSpeed;
    [SerializeField] private float dynamicSpeedIncrementalFactor = 1f;
    [SerializeField] private float buntSpeed = 2f;
    [SerializeField] private float previousCurrentSpeed;
    [SerializeField] private float accelerationTime;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private float trailRendererThreshold = .5f;

    [Header("Direction Parameters")]
    [SerializeField] private Vector2 direction;
    [SerializeField] private Vector2 ballVelocity;
    [SerializeField] private Vector2 previousPosition;
    [SerializeField] private Vector2 currentPosition;
    [SerializeField] private Vector2 changeInPosition;

    [Header("Verticallity Parameters")]
    [SerializeField] private Optional<bool> canBounce = new Optional<bool>(true);
    [SerializeField] public int height = 0;
    [SerializeField] private float distanceFromGround = 0f;
    public bool isGrounded;
    public float gravity = -98;
    public Transform objectTransform;
    public Transform spriteTransform;
    public Transform shadowTransform;
    public float verticalVelocity;
    public float initialVerticalVelocity;

    [Header("Collision Detection Parameters")]
    [SerializeField] private bool showRays = true;
    [SerializeField] private bool dealsDamage = true;
    [SerializeField] private float hitPredictionDistance;
    [SerializeField] private float ballCollisionRadius;
    [SerializeField] private float ballCollisionDistance;
    [SerializeField] private float ballCollisionOffset;
    [SerializeField] private float finalDistance;

    [Header("Bounce Parameters")]
    [SerializeField] private Optional<float> bounceDelay = new Optional<float>(0f);
    [SerializeField] private int underThresholdBouncesCount;
    [SerializeField] private int maxBouncesUnderThreshold;

    public static event Action<Color> OnTeamChanged;
    public static event Action OnBallHit;

    private WaitForEndOfFrame waitForEndOfFrame;
    private Coroutine LerpSpeedCoroutine;
    private Coroutine DelayedBallCoroutine;
    private Coroutine RandomizeBounceCoroutine;

    private void Awake() {
        if (animator == null) animator = this.GetComponentInHierarchy<Animator>();
        if (trailRenderer == null) trailRenderer = this.GetComponentInHierarchy<TrailRenderer>();
        if (ballTeamAssigner == null) ballTeamAssigner = this.GetComponentInHierarchy<TeamAssigner>();
    }

    private void Start() {
        objectTransform = this.transform;
        spriteTransform = ballSprite.transform;
        shadowTransform = ballShadow.transform;

        currentSpeed = 0f;
        currentBallSpeed.Value = currentSpeed;

        ballState = BallState.Floating;

        distanceFromGround = ballShadow.transform.localPosition.y + ballSprite.transform.localPosition.y;
        isGrounded = distanceFromGround > 0f ? false : true;

        if (!isGrounded) {
            verticalVelocity = 0f;
        }
        else if (isGrounded) {
            spriteTransform.position = shadowTransform.position;
            verticalVelocity = initialVerticalVelocity;
        }

        shaderProperties = new ShaderPropertyIDs() {
            _MainColor = Shader.PropertyToID("_MainColor"),
            _AscentsColor = Shader.PropertyToID("_AscentsColor"),
            _EyesColor = Shader.PropertyToID("_EyesColor"),
        };

        material = Instantiate(ballSprite.material);
        ballSprite.material = material;
        ballSprite.material.SetColor(shaderProperties._MainColor, ballTeamAssigner.currentTeamConfiguration.Color);

        OnTeamChanged?.Invoke(ballTeamAssigner.currentTeamConfiguration.Color);
    }

    private void UpdatePositions() {
        if (!isGrounded) {
            verticalVelocity += gravity * Time.deltaTime;
            spriteTransform.localPosition = spriteTransform.localPosition.IncrementY(verticalVelocity * Time.deltaTime);
        }

        if (spriteTransform.position.y <= shadowTransform.position.y && !isGrounded) {
            spriteTransform.position = shadowTransform.position;
            isGrounded = true;
            GroundHit();
        }

        UpdateHeight();
    }

    private void UpdateHeight() {
        distanceFromGround = shadowTransform.localPosition.y + spriteTransform.localPosition.y;

        height = distanceFromGround.ToInt();
        //ballSprite.sortingOrder = 1 + height;
        ballSprite.sortingOrder = 1 + distanceFromGround.ToIntCeil();
        trailRenderer.sortingOrder = ballSprite.sortingOrder;
    }

    private void GroundHit() {
        //onGroundHitEvent.Invoke();
        if (canBounce.Enabled && canBounce.Value) VerticalBounce();
    }

    public void VerticalBounce() {
        verticalVelocity = initialVerticalVelocity;
        isGrounded = false;
        animator.SetFloat("CollisionY", -1f);
        animator.SetTrigger("Collided");
    }

    private void Update() {
        if (ballState == BallState.Floating) {
            UpdateHeight();
        }
        else if (ballState == BallState.Moving || ballState == BallState.Bunted) {
            UpdatePositions();
            float deltaTime = Time.deltaTime;
            ballVelocity = direction.normalized * currentSpeed;
            Vector2 movement = ballVelocity * deltaTime;
            float movementMagnitude = movement.magnitude;

            previousPosition = transform.position;
            currentPosition = (Vector2)transform.position + movement;
            changeInPosition = currentPosition - previousPosition;

            finalDistance = ballCollisionDistance * movementMagnitude;
            RaycastHit2D hitCast = Physics2D.CircleCast(previousPosition, ballCollisionRadius, direction, finalDistance, hitCollisionMask);
            RaycastHit2D terrainCast = Physics2D.CircleCast(previousPosition, ballCollisionRadius, direction, finalDistance, terrainCollisionMask);

            if (showRays) {
                RaycastHit2D predictionHit = Physics2D.CircleCast(previousPosition, ballCollisionRadius, direction, hitPredictionDistance, terrainCollisionMask);

                var predictionHitPoint = predictionHit.GetCentroid();
                var predictionHitNormal = predictionHit.GetNormal();
                var distanceToHit = predictionHit.GetDistance();

                if (predictionHit) {
                    predictionHitPoint = predictionHit.GetPoint();

                    //Draw red ray from ball to Hitpoint, denoting remaining Distance and direction;
                    Debug.DrawRay(transform.position, direction.normalized * distanceToHit, Color.red);
                    //Draw green ray from Hitpoint to Normal, denoting collision Normal direction;
                    Debug.DrawRay(predictionHitPoint, predictionHitNormal, Color.green);
                    //Draw magenta ray from HitPoint to Reflected direction, denoting next direction;
                    Vector2 reflectedPredictionDirection = Vector2.Reflect(direction, predictionHitNormal).normalized;
                    Debug.DrawRay(predictionHitPoint, reflectedPredictionDirection.normalized * (hitPredictionDistance - distanceToHit), Color.magenta, 0.01f);
                }
            }

            var hitPoint = hitCast.GetCentroid();
            var hitNormal = hitCast.GetNormal();

            if (hitCast) {
                IDamageable damageable = hitCast.transform.GetComponentInHierarchy<IDamageable>();
                if (damageable != null) {
                    var hitDirection = (hitCast.transform.position.ToVector2() - currentPosition).normalized;
                    if (dealsDamage) damageable.Damage(currentSpeed, hitDirection);
                    else damageable.Damage(0f, hitDirection);
                }
            }

            var terrainPoint = terrainCast.GetCentroid();
            var terrainNormal = terrainCast.GetNormal();

            if (terrainCast) {
                HorizontalBounce(terrainPoint, terrainNormal);
            }

            transform.position = currentPosition;
        }
        else if (ballState == BallState.Delayed) {
            direction = Vector2.zero;
        }
    }

    private void HorizontalBounce(Vector2 point, Vector2 normal) {
        float angleOfHit = Vector3.Angle(normal, Vector3.up);

        Vector2 reflectDirection = Vector2.Reflect(direction, normal).normalized;
        Vector2 reflectedNormal = Vector2.Reflect(normal, normal).normalized;

        animator.SetFloat("CollisionX", reflectedNormal.x);
        animator.SetFloat("CollisionY", reflectedNormal.y);
        animator.SetTrigger("Collided");

        direction = reflectDirection;

        currentPosition = point + (normal * ballCollisionOffset);

        if (bounceDelay.Enabled) {
            if (DelayedBallCoroutine != null) StopCoroutine(DelayedBallCoroutine);
            DelayedBallCoroutine = StartCoroutine(DelayBall(bounceDelay.Value));
        }
    }

    private void DestroyTiles() {
        /*if (hit.transform.HasComponent<TileDestroyer>()) {
            TileDestroyer tilemap = hit.transform.GetComponent<TileDestroyer>();
            tilemap.DestroyTile(reflectedNormal * 0.1f);
        }*/
    }

    public void Hit(Vector2 direction, float hitForce, TeamAssigner teamAssigner) => HitBall(direction, hitForce, teamAssigner);
    public void Bunt(Vector2 direction, float verticalVelocity, TeamAssigner teamAssigner) => BuntBall(direction, verticalVelocity, teamAssigner);

    public void HitBall(Vector2 _direction, float _hitForce, TeamAssigner hitterTeamAssigner) {
        direction = _direction;

        TeamConfiguration newConfig = hitterTeamAssigner.currentTeamConfiguration;
        TeamConfiguration previousConfig = ballTeamAssigner.currentTeamConfiguration;
        bool differentTeam = false;

        if (newConfig.TeamFaction != previousConfig.TeamFaction) {
            differentTeam = true;
            SetBallTeam(newConfig);
            Color teamColor = newConfig.Color;
            SetBallColor(teamColor);
            OnTeamChanged?.Invoke(newConfig.Color);
        }

        if (currentSpeed == 0f) CheckTargetSpeed(initialSpeed, _hitForce, differentTeam);
        else CheckTargetSpeed(currentSpeed, _hitForce, differentTeam);

        OnBallHit?.Invoke();

        animator.SetFloat("CollisionX", _direction.x);
        animator.SetFloat("CollisionY", _direction.y);
        animator.SetTrigger("Collided");

        ballState = BallState.Moving;
    }

    public void BuntBall(Vector2 _direction, float _verticalVelocity, TeamAssigner hitterTeamAssigner) {
        direction = _direction;

        previousCurrentSpeed = currentSpeed;
        currentSpeed = buntSpeed;

        isGrounded = false;
        verticalVelocity = _verticalVelocity;

        TeamConfiguration newConfig = hitterTeamAssigner.currentTeamConfiguration;
        SetBallTeam(newConfig);

        Color teamColor = ballTeamAssigner.team.defaultTeamConfiguration.Color;
        SetBallColor(teamColor);

        animator.SetFloat("CollisionY", -1f);
        animator.SetTrigger("Collided");

        ballState = BallState.Bunted;
    }

    public void SetBallTeam(TeamConfiguration newTeamConfiguration) {
        int previousBitmask = ballTeamAssigner.currentTeamConfiguration.Layer;
        int currentBitmask = newTeamConfiguration.Layer;

        hitCollisionMask = hitCollisionMask.RemoveLayerFromLayerMask(currentBitmask);
        hitCollisionMask = hitCollisionMask.AddLayerToLayerMask(previousBitmask);
        ballTeamAssigner.SetTeamConfiguration(newTeamConfiguration);
    }

    public void SetBallColor(Color color) {
        //ballSprite.SetSpriteColor(color);
        ballSprite.material.SetColor(shaderProperties._MainColor, ballTeamAssigner.currentTeamConfiguration.Color);
        trailRenderer.startColor = color;
        trailRenderer.endColor = color.SetColorA(0f);
    }

    private void CheckTargetSpeed(float speed, float _hitForce, bool _differentTeam) {
        if (hasDynamicSpeed) {
            if (currentSpeed < dynamicMaxSpeed) {
                currentSpeed = targetMoveSpeed;
                currentBallSpeed.Value = currentSpeed;

                if (_differentTeam) targetMoveSpeed = speed + (speed * 0.5f * dynamicSpeedIncrementalFactor * _hitForce);
                else targetMoveSpeed = speed + (1f * _hitForce);

                if (targetMoveSpeed >= dynamicMaxSpeed) {
                    targetMoveSpeed = dynamicMaxSpeed;
                }

                if (LerpSpeedCoroutine != null) StopCoroutine(LerpSpeedCoroutine);
                LerpSpeedCoroutine = StartCoroutine(LerpSpeed(currentSpeed, targetMoveSpeed));
            }
            else if (currentSpeed >= dynamicMaxSpeed) {
                currentSpeed = dynamicMaxSpeed;
                currentBallSpeed.Value = currentSpeed;
                targetMoveSpeed = dynamicMaxSpeed;
            }
            currentSpeed = currentSpeed.Clamp(currentSpeed, dynamicMaxSpeed);
            currentBallSpeed.Value = currentSpeed;

            CheckTrailSpeed(speed);
        }
        else {
            currentSpeed = defaultMaxSpeed;
            currentBallSpeed.Value = currentSpeed;
        }
    }

    private void CheckTrailSpeed(float speed) {
        if (speed < (dynamicMaxSpeed * trailRendererThreshold)) {
            trailRenderer.emitting = false;
        }
        else if (speed >= (dynamicMaxSpeed * trailRendererThreshold)) {
            trailRenderer.emitting = true;
        }
    }

    private IEnumerator LerpSpeed(float previousSpeed, float targetMoveSpeed) {
        currentSpeed = previousSpeed;
        currentBallSpeed.Value = currentSpeed;

        float elapsedTime = 0f;
        float percentageComplete = 0f;

        while (elapsedTime < accelerationTime) {
            currentSpeed = Mathf.Lerp(currentSpeed, targetMoveSpeed, accelerationCurve.Evaluate(percentageComplete));
            currentBallSpeed.Value = currentSpeed;

            elapsedTime += Time.deltaTime;
            percentageComplete = elapsedTime / accelerationTime;

            if (elapsedTime >= accelerationTime) {
                currentSpeed = targetMoveSpeed;
                currentBallSpeed.Value = currentSpeed;
                break;
            }

            yield return waitForEndOfFrame;
        }
    }

    private IEnumerator DelayBall(float delay, bool resetTimeScale = true) {
        float elapsedTime = 0f;
        Vector2 previousDirection = direction;

        direction = Vector2.zero;

        if (resetTimeScale) Time.timeScale = 1f;

        while (elapsedTime < delay) {
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= delay) {
                elapsedTime = delay;
                direction = previousDirection;
            }

            yield return waitForEndOfFrame;
        }
    }

    private IEnumerator RandomizeBounce(float targetTime) {
        float elapsedTime = 0f;

        while (elapsedTime < targetTime) {
            elapsedTime += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
    }
}
