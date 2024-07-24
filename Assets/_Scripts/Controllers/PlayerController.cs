using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ExtensionMethods;
using System;

public struct ShaderPropertyIDs {
    public int _MainColor;
    public int _AscentsColor;
    public int _EyesColor;
}

public class PlayerController : MonoBehaviour {
    [Header("--- References ---")]
    private Camera mainCamera;
    public PlayerInputs playerInputs;
    [SerializeField] public PlayerHealth playerHealth;
    [SerializeField] public SpriteRenderer playerSprite;
    [SerializeField] public SpriteRenderer playerShadow;
    [SerializeField] public SpriteRenderer slashSprite;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private Transform handCenter;
    [SerializeField] private Transform handPoint;
    [HideInInspector] private Rigidbody2D rb;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator handAnimator;
    [HideInInspector] public TeamAssigner teamAssigner;
    [SerializeField] private LayerMask hittableMask;
    [SerializeField] public PlayerConfiguration playerConfiguration;
    private bool helpersActive = true;

    private readonly int Slash = Animator.StringToHash("VFX_Slash");
    // private int currentAnimationState;
    // private static readonly int Idle = Animator.StringToHash("Idle");
    // private static readonly int Move = Animator.StringToHash("Movement");
    // private static readonly int ChargeStart = Animator.StringToHash("ChargeStart");
    // private static readonly int ChargeLoop = Animator.StringToHash("ChargeLoop");
    // private static readonly int Attack = Animator.StringToHash("Attack");
    // private static readonly int Hurt = Animator.StringToHash("Hurt");
    // private static readonly int Death = Animator.StringToHash("Death");

    [Header("--- Movement System ---")]
    [Header("Speed Parameters")]
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private float initialMaxMoveSpeed;
    [SerializeField] private float currentMaxMoveSpeed;
    [SerializeField] private float idleMovespeedThreshold;
    [SerializeField] private float movementLerpAmount;
    [SerializeField] private float accelerationRate;
    [SerializeField] private float desaccelerationRate;
    [SerializeField] private bool canDodge = true;
    [SerializeField] private float dodgeCooldownSeconds;
    [SerializeField] private float currentDodgeMoveSpeed;
    [SerializeField] private float initialDodgeMoveSpeed;
    [SerializeField] private float targetDodgeMoveTime;
    [SerializeField] private float dodgeMoveRate;
    [SerializeField] private float currentSwingMoveSpeed;
    [SerializeField] private float initialSwingMoveSpeed;
    [SerializeField] private float targetSwingMoveTime;
    [SerializeField] private float swingRate;
    [Header("Velocity Parameters")]
    private bool isMoving = false;
    private bool canMove = true;
    private Vector2 playerVelocity;
    private Vector2 targetDirection;
    [Header("Verticallity Parameters")]
    [SerializeField] private int height = 0;
    [SerializeField] private float distanceFromGround = 0f;
    public bool isGrounded;
    public bool canJump;
    public float gravity = 9.8f;
    public Transform objectTransform;
    public Transform spriteTransform;
    public Transform shadowTransform;
    public float jumpForce;
    public float initialJumpForce;

    [Header("--- Attack System ---")]
    [Header("Aim Parameters")]
    [SerializeField] public Vector2 mouseWorldPosition;
    [SerializeField] public Vector2 lastDodgeDirection;
    private bool isAiming = false;
    [Header("Swipe Parameters")]
    [SerializeField] private bool canSwing = true;
    [SerializeField] private bool isHoldingSwing = false;
    [SerializeField] private float chargeElapsedTime;
    [SerializeField] private float swingCooldownSeconds;
    [SerializeField] private float elapsedSwingTime;
    [SerializeField] private float targetSwingTime;
    [SerializeField] private float accumulatedForce;
    [SerializeField] private float cumulativeForceMultiplier;
    [Range(0f, 3f)]
    [SerializeField] private float maxChargeTime;
    [Range(0f, 10f)]
    [SerializeField] private float maxSwingForce;
    [Range(1f, 5f)]
    [SerializeField] private float swingRadius;
    [SerializeField] private float maxSwingAngle;
    [SerializeField] private float centerAngle;
    [SerializeField] private float swingConeAngle1;
    [SerializeField] private float swingConeAngle2;
    [SerializeField] private Vector2 swingConeSide1;
    [SerializeField] private Vector2 swingConeSide2;
    [SerializeField] private Vector2 swingDirection;
    // [Header("Bunt Parameters")]
    // [SerializeField] private Vector2 verticalDispenseVelocity;
    // [SerializeField] private Vector2 groundDispenseVelocity;

    [Header("--- Health System ---")]
    [Header("Knockback Parameters")]
    private bool isKnockbacked;
    [SerializeField] private Vector2 lastHitDirection;
    [SerializeField] private float currentKnockbackSpeed;
    [SerializeField] private float initialKnockbackSpeed;
    [SerializeField] private float elapsedKnockbackTime;
    [SerializeField] private float targetKnockbackTime;
    [SerializeField] private float knockbackRate;
    [Header("Respawn Parameters")]
    [SerializeField] private Vector2 startingRespawnPoint;
    [SerializeField] private Vector2 startingLookDirection;

    public event Action OnKnockbackEnd;
    public event Action OnDodgeStart;
    public event Action OnDodgeEnd;

    private WaitForSeconds dodgeCooldown;
    private WaitForSeconds swingCooldown;
    private WaitForSeconds hitDetectionOffset;

    private Coroutine DodgeCoroutine;
    private Coroutine SwingChargeCoroutine;
    private Coroutine SwingReleaseCoroutine;

    // [Header("Abilities Parameters")]
    // public Ability ability;
    // float cooldownTime;
    // float activeTime;

    // AbilityState state = AbilityState.Ready;

    // public KeyCode key;

    public void Init(PlayerInputs inputs, Vector2 lookDirection) {
        playerInputs = inputs;
        playerConfiguration = playerInputs.playerConfiguration;

        playerInputs.move.performed += Movement;
        playerInputs.move.canceled += Movement;

        playerInputs.aim.performed += Aim;
        playerInputs.aim.canceled += Aim;

        playerInputs.swing.started += ChargeSwipeSwing;
        playerInputs.swing.canceled += ReleaseSwipeSwing;

        playerInputs.bunt.started += BuntSwing;

        playerInputs.dodge.started += Dodge;

        //playerInputs.jump.started += Jump;

        playerInputs.rollTypeToggle.started += ToggleRollType;

        startingRespawnPoint = transform.position;
        startingLookDirection = lookDirection;

        playerHealth = playerInputs.playerConfiguration.Health;

        playerHealth.OnPlayerDamaged += Knockback;
        playerHealth.OnPlayerDead += SetPlayerDeath;
        playerHealth.OnPlayerRespawn += Respawn;

        OnKnockbackEnd += playerInputs.EnableGameplayActions;

        GameManager.OnGamePaused += playerInputs.DisableGameplayActions;
        GameManager.OnGameResumed += playerInputs.EnableGameplayActions;
        GameManager.OnLoadScreenEnd += playerInputs.EnableGameplayActions;

        playerSprite.material = playerConfiguration.ColorSwapMaterial;
        slashSprite.material = playerConfiguration.ColorSwapMaterial;
    }

    private void OnDisable() {
        playerInputs.move.performed -= Movement;
        playerInputs.move.canceled -= Movement;

        playerInputs.aim.performed -= Aim;
        playerInputs.aim.canceled -= Aim;

        playerInputs.swing.started -= ChargeSwipeSwing;
        playerInputs.swing.canceled -= ReleaseSwipeSwing;

        playerInputs.bunt.started -= BuntSwing;

        playerInputs.dodge.started -= Dodge;

        //playerInputs.jump.started -= Jump;

        playerInputs.rollTypeToggle.started -= ToggleRollType;

        playerInputs.playerControls.Disable();

        playerHealth.OnPlayerDamaged -= Knockback;
        playerHealth.OnPlayerDead -= SetPlayerDeath;
        playerHealth.OnPlayerRespawn -= Respawn;

        OnKnockbackEnd -= playerInputs.EnableGameplayActions;

        GameManager.OnGamePaused -= playerInputs.DisableGameplayActions;
        GameManager.OnGameResumed -= playerInputs.EnableGameplayActions;
        GameManager.OnLoadScreenEnd -= playerInputs.EnableGameplayActions;

        if (playerConfiguration.ColorSwapMaterial != null) Destroy(playerConfiguration.ColorSwapMaterial);
    }

    private void Awake() {
        if (mainCamera == null) mainCamera = this.GetMainCamera();
        if (rb == null) rb = this.GetComponentInHierarchy<Rigidbody2D>();
        if (playerAnimator == null) playerAnimator = this.GetComponentInHierarchy<Animator>();
        // if (teamAssigner == null) teamAssigner = this.GetComponentInHierarchy<TeamAssigner>();
        // if (playerHealth == null) playerHealth = this.GetComponentInHierarchy<PlayerHealth>();
    }

    private void Start() {
        dodgeCooldown = new WaitForSeconds(dodgeCooldownSeconds);
        swingCooldown = new WaitForSeconds(swingCooldownSeconds);
        hitDetectionOffset = new WaitForSeconds(0.05f);

        currentMaxMoveSpeed = initialMaxMoveSpeed;
        currentDodgeMoveSpeed = initialDodgeMoveSpeed;
        currentSwingMoveSpeed = initialSwingMoveSpeed;
        currentKnockbackSpeed = initialKnockbackSpeed;

        playerSprite.gameObject.layer = playerInputs.playerConfiguration.TeamAssigner.currentTeamConfiguration.Layer;

        objectTransform = this.transform;
        spriteTransform = playerSprite.transform;
        shadowTransform = playerShadow.transform;

        distanceFromGround = shadowTransform.localPosition.y + spriteTransform.localPosition.y;
        height = distanceFromGround.ToInt();
        playerSprite.sortingOrder = 1 + height;

        if (distanceFromGround > 0f) {
            canJump = false;
            isGrounded = false;
        }
        else if (distanceFromGround <= 0f) {
            canJump = true;
            isGrounded = true;
            spriteTransform.position = shadowTransform.position;
        }

        jumpForce = 0f;
    }

    private void Update() {
        if (!isGrounded) {
            jumpForce += gravity * Time.deltaTime;
            spriteTransform.position += new Vector3(0f, jumpForce, 0f) * Time.deltaTime;
        }

        distanceFromGround = shadowTransform.localPosition.y + spriteTransform.localPosition.y;
        height = distanceFromGround.ToInt();
        playerSprite.sortingOrder = 1 + height;

        if (spriteTransform.position.y < shadowTransform.position.y && !isGrounded) {
            spriteTransform.position = shadowTransform.position;
            isGrounded = true;
            canJump = true;
            spriteTransform.position = shadowTransform.position;
            this.GetComponent<Collider2D>().enabled = true;
        }

        // switch (state) {
        //     case AbilityState.Ready:
        //         if (Input.GetKeyDown(key)) {
        //             //ability.Activate(gameObject);
        //             ability.Activate2(this);
        //             state = AbilityState.Active;
        //             activeTime = ability.activeTime;
        //         }
        //     break;

        //     case AbilityState.Active:
        //         if (activeTime > 0) {
        //             activeTime -= Time.deltaTime;
        //         }
        //         else if (activeTime <= 0) {
        //             state = AbilityState.Cooldown;
        //             cooldownTime = ability.cooldownTime;
        //         }
        //     break;

        //     case AbilityState.Cooldown:
        //         if (cooldownTime > 0) {
        //             cooldownTime -= Time.deltaTime;
        //         }
        //         else if (cooldownTime <= 0) {
        //             state = AbilityState.Ready;
        //         }
        //     break;
        // }

        if (playerInputs.controllerType == ControllerType.PC) {
            mouseWorldPosition = mainCamera.ScreenToWorld(playerInputs.MousePositionInput);
            playerInputs.AimDirection = (mouseWorldPosition - playerCenter.position.ToVector2()).normalized;
            playerInputs.LastAimDirection = playerInputs.AimDirection;
        }
        else if (playerInputs.controllerType != ControllerType.PC && !isAiming) {
            if (isMoving) playerInputs.AimDirection = playerInputs.MoveDirection;
            else playerInputs.AimDirection = playerInputs.LastAimDirection;
        }
        
        playerInputs.LastAimDirection = playerInputs.AimDirection;

        float angle = Mathf.Atan2(playerInputs.AimDirection.y, playerInputs.AimDirection.x) * Utils.RAD_TO_DEG;
        playerCenter.eulerAngles = new Vector3(0, 0, angle);

        Vector2 targetMoveDirection = Vector2.zero;
        float targetSpeed = currentMoveSpeed;
        bool smoothMoveSpeed = false;
        int targetAnimationState = 0;
        Vector2 targetAnimationDirection = Vector2.zero;

        switch (playerInputs.playerState) {
            case PlayerState.Idle:
                targetMoveDirection = playerInputs.LastMoveDirection;
                if (isMoving && canMove) playerInputs.playerState = PlayerState.Moving;
                else if (!isMoving) currentMoveSpeed = 0f;
                targetSpeed = currentMoveSpeed;
                smoothMoveSpeed = true;
                targetAnimationState = playerInputs.Idle;
                targetAnimationDirection = playerInputs.AimDirection;
            break;
            case PlayerState.Moving:
                targetMoveDirection = playerInputs.MoveDirection;

                if (isMoving) {
                    currentMoveSpeed += accelerationRate * Time.deltaTime;

                    if (currentMoveSpeed >= currentMaxMoveSpeed) currentMoveSpeed = currentMaxMoveSpeed;
                }
                else if (!isMoving) {
                    currentMoveSpeed -= desaccelerationRate * Time.deltaTime;
                    targetMoveDirection = playerInputs.LastMoveDirection;

                    if (currentMoveSpeed <= currentMaxMoveSpeed * idleMovespeedThreshold) {
                        currentMoveSpeed = 0f;
                        playerInputs.playerState = PlayerState.Idle;
                    }
                }

                targetSpeed = currentMoveSpeed;
                smoothMoveSpeed = true;
                targetAnimationState = playerInputs.Move;
                targetAnimationDirection = targetMoveDirection;
            break;
            case PlayerState.Dodging:
                targetMoveDirection = lastDodgeDirection;
                float dodgeSpeedMinimum = initialDodgeMoveSpeed * 0.1f;
                currentDodgeMoveSpeed -= (dodgeMoveRate * targetDodgeMoveTime) * Time.deltaTime;

                targetSpeed = currentDodgeMoveSpeed;
                smoothMoveSpeed = false;
                targetAnimationState = playerInputs.Move;
                targetAnimationDirection = targetMoveDirection;

                if (currentDodgeMoveSpeed <= dodgeSpeedMinimum) {
                    currentDodgeMoveSpeed = initialDodgeMoveSpeed;
                    OnDodgeEnd?.Invoke();

                    if (isHoldingSwing) {
                        currentMoveSpeed = 0f;
                        playerInputs.playerState = PlayerState.ChargingSwing;
                    }
                    else {
                        canMove = true;
                        if (isMoving) {
                            currentMoveSpeed = currentMaxMoveSpeed;
                            playerInputs.playerState = PlayerState.Moving;
                        }
                        else {
                            currentMoveSpeed = 0f;
                            playerInputs.LastMoveDirection = targetMoveDirection;
                            playerInputs.playerState = PlayerState.Idle;
                        }
                    }
                }
            break;
            case PlayerState.ChargingSwing:
                isHoldingSwing = true;
                
                targetMoveDirection = swingDirection;

                currentMoveSpeed -= desaccelerationRate * Time.deltaTime;

                if (currentMoveSpeed <= currentMaxMoveSpeed * idleMovespeedThreshold)
                    currentMoveSpeed = 0f;

                targetSpeed = currentMoveSpeed;
                smoothMoveSpeed = false;
                targetAnimationState = playerInputs.ChargeStart;
                targetAnimationDirection = playerInputs.AimDirection;

                centerAngle = playerInputs.AimDirection.AngleFloat();
                swingConeAngle1 = centerAngle + (maxSwingAngle / 2);
                swingConeAngle2 = centerAngle - (maxSwingAngle / 2);

                swingConeSide1 = swingConeAngle1.AngleFloatToVector2(false, false);
                swingConeSide2 = swingConeAngle2.AngleFloatToVector2(false, false);

                chargeElapsedTime += Time.deltaTime;

                if (accumulatedForce < maxSwingForce) {
                    if (chargeElapsedTime <= (maxChargeTime * 0.33f)) {
                        accumulatedForce = 1f;
                    }
                    else if (chargeElapsedTime <= (maxChargeTime * 0.66f)) {
                        accumulatedForce = 0.75f * 2;
                    }
                    else if (chargeElapsedTime < (maxChargeTime)) {
                        accumulatedForce = 0.75f * 3;
                        Swing();
                    }
                }
            break;
            case PlayerState.Swinging:
                float swingMoveSpeedMinimum = initialSwingMoveSpeed * 0.1f;
                float force = accumulatedForce.Clamp(1f, maxSwingForce);

                targetMoveDirection = swingDirection;

                if (force < 1f) force = 1f;
                currentSwingMoveSpeed -= (swingRate / (force * 2) / targetSwingMoveTime) * Time.deltaTime;

                if (currentSwingMoveSpeed <= swingMoveSpeedMinimum) {
                    currentSwingMoveSpeed = 0f;
                }

                targetSpeed = currentSwingMoveSpeed;
                smoothMoveSpeed = false;
                targetAnimationState = playerInputs.Attack;
                targetAnimationDirection = targetMoveDirection;
            break;
            case PlayerState.Hurt:
                targetMoveDirection = lastHitDirection;
                float knockbackSpeedMinimum = initialKnockbackSpeed * 0.1f;

                currentKnockbackSpeed -= (knockbackRate * targetKnockbackTime) * Time.deltaTime;

                if (currentKnockbackSpeed <= knockbackSpeedMinimum) {
                    currentKnockbackSpeed = 0f;
                }

                targetSpeed = currentKnockbackSpeed;
                smoothMoveSpeed = false;
                targetAnimationState = playerInputs.Hurt;
                targetAnimationDirection = targetMoveDirection;

                if (elapsedKnockbackTime < targetKnockbackTime) elapsedKnockbackTime += Time.deltaTime;
                else {
                    isKnockbacked = false;
                    currentKnockbackSpeed = initialKnockbackSpeed;
                    elapsedKnockbackTime = 0f;
                    OnKnockbackEnd?.Invoke();
                    canMove = true;
                    currentMoveSpeed = 0f;
                    if (playerInputs.isDead) playerInputs.playerState = PlayerState.Dead;
                    else if (isMoving) playerInputs.playerState = PlayerState.Moving;
                    else if (!isMoving) playerInputs.playerState = PlayerState.Idle;
                }
            break;
            case PlayerState.Dead:
                targetMoveDirection = Vector2.zero;
                targetSpeed = currentMoveSpeed;
                smoothMoveSpeed = false;
                targetAnimationState = playerInputs.Death;
                targetAnimationDirection = targetMoveDirection;
            break;
        }

        SetPlayerVelocity(targetMoveDirection, targetSpeed, smoothMoveSpeed);
        AnimatePlayer(targetAnimationState, targetAnimationDirection);
    }

    private void FixedUpdate() {
        rb.velocity = playerVelocity;
    }

    private void AnimatePlayer(int state, Vector2 animationDirection) {
        playerAnimator.SetFloat("MoveX", animationDirection.x);
        playerAnimator.SetFloat("MoveY", animationDirection.y);

        if (state == playerInputs.currentAnimationState) return;
        //if (isAnimationPlaying(animator, currentState)) return;
        //animator.CrossFade(state, 0, 0);
        playerAnimator.Play(state);
        playerInputs.currentAnimationState = state;
    }

    private bool isAnimationPlaying(Animator animator, int stateName) {
        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateName && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;
        else
            return false;
    }

    private void Spawn() {
        playerInputs.MoveDirection = playerInputs.MoveDirection.SetXY(0f, 0f);
        playerInputs.LastMoveDirection = startingLookDirection.normalized;
        playerInputs.AimDirection = startingLookDirection.normalized;
        playerInputs.LastAimDirection = startingLookDirection.normalized;
        playerInputs.MousePositionInput = playerInputs.MousePositionInput.SetXY(0f, 0f);
        playerInputs.playerState = PlayerState.Idle;

        currentMoveSpeed = 0f;

        transform.position = startingRespawnPoint;

        playerInputs.rollType = RollType.TowardsMovement;

        isKnockbacked = false;

        SetHelpers(true);

        Vector2 targetDirection = playerInputs.LastMoveDirection;
        float targetDirectionAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Utils.RAD_TO_DEG;
        playerCenter.eulerAngles = new Vector3(0, 0, targetDirectionAngle);

        playerAnimator.SetFloat("MoveX", playerInputs.LastMoveDirection.x);
        playerAnimator.SetFloat("MoveY", playerInputs.LastMoveDirection.y);
    }

    private void Respawn() {
        Spawn();
        playerInputs.EnableGameplayActions();
    }

    private void SetPlayerVelocity(Vector2 direction, float speed, bool smoothMovement = false) { 
        var targetVelocity = direction * speed;
        playerVelocity = smoothMovement ? Vector2.MoveTowards(playerVelocity, targetVelocity, movementLerpAmount * Time.deltaTime) : targetVelocity;
    }

    private void ToggleRollType(InputAction.CallbackContext context) {
        switch (playerInputs.rollType) {
            case RollType.TowardsMovement:
                playerInputs.rollType = RollType.TowardsAim;
            break;
            case RollType.TowardsAim:
                playerInputs.rollType = RollType.TowardsMovement;
            break;
        }
    }

    private void SetHelpers(bool state) {
        helpersActive = state;
        handPoint.gameObject.SetActive(helpersActive);
    }

    private void Movement(InputAction.CallbackContext context) {
        if (context.ReadValue<Vector2>().magnitude > 0f) {
            isMoving = true;
            playerInputs.MoveDirection = context.ReadValue<Vector2>();
        }
        else
            isMoving = false;

        if (canMove) {
            playerInputs.MoveDirection = context.ReadValue<Vector2>();

            if (context.ReadValue<Vector2>().magnitude > 0f) {
                playerInputs.LastMoveDirection = playerInputs.MoveDirection;
            }
        }
    }

    private void Aim(InputAction.CallbackContext context) {
        var aimDirection = playerInputs.LastAimDirection;

        if (context.ReadValue<Vector2>().magnitude > 0f) {
            if (playerInputs.controllerType == ControllerType.PC) {
                playerInputs.MousePositionInput = context.ReadValue<Vector2>();
            }
            else if (playerInputs.controllerType != ControllerType.PC) {
                aimDirection = context.ReadValue<Vector2>();
            }
            isAiming = true;
        }
        else isAiming = false;

        playerInputs.AimDirection = aimDirection;
        playerInputs.LastAimDirection = playerInputs.AimDirection;
    }

    private void ChargeSwipeSwing(InputAction.CallbackContext context) {
        if (canSwing && !isKnockbacked) {
            chargeElapsedTime = 0f;
            accumulatedForce = 0f;
            swingDirection = playerInputs.LastMoveDirection;
            playerInputs.playerState = PlayerState.ChargingSwing;
        }
    }

    private void ReleaseSwipeSwing(InputAction.CallbackContext context) {
        if (isHoldingSwing) {
            Swing();
        }
    }

    private void Swing() {
        canMove = false;
        isHoldingSwing = false;
        StartCoroutine(SwingCooldown());
        elapsedSwingTime = 0f;
        swingDirection = playerInputs.AimDirection;

        if (SwingReleaseCoroutine != null) StopCoroutine(SwingReleaseCoroutine);
        SwingReleaseCoroutine = StartCoroutine(SwipeSwing());
        playerInputs.playerState = PlayerState.Swinging;
    }

    private void BuntSwing(InputAction.CallbackContext context) {
        playerInputs.LastAimDirection = playerInputs.AimDirection;
    }

    private IEnumerator SwingCooldown() {
        canSwing = false;
        yield return swingCooldown;
        canSwing = true;
    }

    private void Dodge(InputAction.CallbackContext context) {
        if (canDodge) {
            canMove = false;
            StartCoroutine(DodgeCooldown());
            OnDodgeStart?.Invoke();
            currentDodgeMoveSpeed = initialDodgeMoveSpeed;
            Vector2 targetDodgeDirection = Vector2.zero;
            if (playerInputs.rollType == RollType.TowardsAim)
                targetDodgeDirection = ((playerInputs.LastAimDirection * 10000f * initialDodgeMoveSpeed) - transform.position.ToVector2()).normalized;
            else if (playerInputs.rollType == RollType.TowardsMovement)
                targetDodgeDirection = ((playerInputs.LastMoveDirection * 10000f * initialDodgeMoveSpeed) - transform.position.ToVector2()).normalized;
            lastDodgeDirection = targetDodgeDirection;
            playerInputs.playerState = PlayerState.Dodging;
        }
    }

    private IEnumerator DodgeCooldown() {
        canDodge = false;
        yield return dodgeCooldown;
        canDodge = true;
    }

    private void Knockback(Vector2 hitDirection) {
        isKnockbacked = true;
        lastHitDirection = hitDirection;
        isHoldingSwing = false;
        playerInputs.DisableGameplayActions();
        playerInputs.playerState = PlayerState.Hurt;
    }

    private void SetPlayerDeath() {
        SetHelpers(false);
        playerInputs.DisableGameplayActions();
        playerInputs.playerState = PlayerState.Dead;
    }

    private void Jump(InputAction.CallbackContext context) {
        if (canJump) {
            jumpForce = initialJumpForce;
            isGrounded = false;
            canJump = false;
            this.GetComponent<CapsuleCollider2D>().enabled = false;
        }
    }

    public IEnumerator SwipeSwing() {
        RaycastHit2D hit;
        bool hasHittedSomething = false;
        float force = accumulatedForce.Clamp(1f, maxSwingForce);
        elapsedSwingTime = 0f;

        //yield return hitDetectionOffset;

        handCenter.gameObject.SetActive(true);
        handCenter.eulerAngles = playerCenter.eulerAngles;
        handAnimator.Play(Slash);

        while (elapsedSwingTime < targetSwingTime) {
            hit = Physics2D.CircleCast(playerCenter.position, swingRadius, swingDirection, 0f, hittableMask);

            if (hit && !hasHittedSomething) {
                float hitDotProduct = Vector2.Dot((hit.GetPoint() - playerCenter.position.ToVector2()).normalized, swingDirection.normalized);
                float coneDotProduct = Vector2.Dot(swingConeSide2.normalized, swingDirection.normalized);
                
                if (hitDotProduct > coneDotProduct) {
                    IHittable hittable = hit.transform.GetComponentInHierarchy<IHittable>();
                    BallController ball = hit.transform.GetComponentInHierarchy<BallController>();

                    if (ball.height.IsBetweenInclusive(height, height + 1)) {
                        // Stop character in place; disable all actions except aiming, and hit detection; limit finalDirection between the cone;
                        ball.transform.position = hit.GetPoint();

                        bool hasReleaseDelay = false;
                        if (ball.currentSpeed >= ball.dynamicMaxSpeed * 0.1f) hasReleaseDelay = true;

                        if (hasReleaseDelay) {
                            playerInputs.dodge.Disable();
                            playerInputs.swing.Disable();
                            playerHealth.EnableInvulnerability();
                            
                            ball.ballState = BallState.Delayed;

                            handAnimator.speed = 0f;
                            handCenter.gameObject.SetActive(false);

                            yield return StartCoroutine(DelayBallRelease(ball));

                            handCenter.gameObject.SetActive(true);
                            handAnimator.speed = 1f;

                            playerInputs.dodge.Enable();
                            playerInputs.swing.Enable();
                            playerHealth.DisableInvulnerability();
                        }

                        Vector2 finalDirection = swingDirection;
                        hittable.Hit(finalDirection, force, teamAssigner);
                        hasHittedSomething = true;
                        currentSwingMoveSpeed /= 2f;
                    }
                }
            }

            elapsedSwingTime += Time.deltaTime;

            if (elapsedSwingTime >= targetSwingTime) {
                ResetSwingParameters();
                handCenter.gameObject.SetActive(false);
                canMove = true;
                currentMoveSpeed = 0f;

                if (playerInputs.isDead) playerInputs.playerState = PlayerState.Dead;
                if (isMoving) playerInputs.playerState = PlayerState.Moving;
                else if (!isMoving) playerInputs.playerState = PlayerState.Idle;

                break;
            }

            //yield return waitForEndOfFrame;
            yield return Utils.waitForEndOfFrame;
        }
    }

    private IEnumerator DelayBallRelease(BallController ball) {
        float releaseElapsedTime = 0f;
        float targetElapsedTime = ball.currentSpeed / 100f;
        // show release bar above player

        Vector2 startingSwingDirection = swingDirection;
        centerAngle = swingDirection.AngleFloat();
        Vector2 releaseDirection = centerAngle.AngleFloatToVector2(false, false);

        while (releaseElapsedTime < targetElapsedTime) {
            if (isAiming) centerAngle = playerInputs.AimDirection.AngleFloat();
            else centerAngle = startingSwingDirection.AngleFloat();
            releaseDirection = centerAngle.AngleFloatToVector2(false, false);
            swingDirection = releaseDirection;

            Debug.DrawRay(playerCenter.position, releaseDirection * swingRadius, playerConfiguration.TeamAssigner.currentTeamConfiguration.Color);
            Debug.DrawRay(playerCenter.position, startingSwingDirection * swingRadius, playerConfiguration.TeamAssigner.currentTeamConfiguration.Color);

            releaseElapsedTime += Time.deltaTime;
            // update release bar fill based on percentage completion

            yield return Utils.waitForEndOfFrame;
        }
        
        yield return null;
    }

    public float ModularClamp(float value, float min, float max, float rangeMin = -180f, float rangeMax = 180f) {
        var modulus = Mathf.Abs(rangeMax - rangeMin);
        if ((value %= modulus) < 0f) value += modulus;
        return Mathf.Clamp(value + Mathf.Min(rangeMin, rangeMax), min, max);
    }

    public float ClampAngle(float current, float min, float max) {
        float dtAngle = (Mathf.Abs((min - max) + 180) % 360 - 180);
        float hdtAngle = dtAngle * 0.5f;
        float midAngle = min + hdtAngle;

        float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
        if (offset > 0) current = Mathf.MoveTowardsAngle(current, midAngle, offset);

        return current;
    }

    // private IEnumerator BuntSwing() {
    //     swingElapsedTime = 0f;

    //     Vector2 directionFromHand = (mouseWorldPosition - handPoint.position.ToVector2()).normalized;
    //     Vector2 swingDirection = (mouseWorldPosition - transform.position.ToVector2()).normalized;
    //     RaycastHit2D hit;

    //     playerInputs.move.Disable();

    //     currentMaxMoveSpeed = initialMaxMoveSpeed;

    //     playerState = PlayerState.Swinging;

    //     bool hasHittedSomething = false;

    //     while (swingElapsedTime < swingTargetTime) {
    //         hit = Physics2D.CircleCast(handCenter.position, swingRadius, directionFromHand, 0f, hittableMask);

    //         if (hit) {
    //             IHittable hittable = hit.transform.GetComponentInHierarchy<IHittable>();
    //             BallController ball = hittable as BallController;

    //             if (hittable.IsNotNull() && ball.height.IsBetweenInclusive(height - 1, height + 1)) {
    //                 /*if (hasSlowDown.Enabled && hasSlowDown.Value) {
    //                     if (SlowdownCoroutine != null) StopCoroutine(SlowdownCoroutine);
    //                     SlowdownCoroutine = StartCoroutine(Slowdown(slowTimeTarget.Value, speedTimeTarget.Value, targetSlow.Value, secondsToRestore.Value));
    //                 }*/

    //                 lastHitPosition = hit.GetPoint();

    //                 Vector2 finalDirection = Vector3.zero;

    //                 finalDirection = swingDirection;

    //                 hittable.Hit(finalDirection, accumulatedForce, teamAssigner);
    //                 playerInputs.LastMoveDirection = swingDirection;

    //                 hasHittedSomething = true;
    //             }
    //         }

    //         swingElapsedTime += Time.deltaTime;

    //         if ((swingElapsedTime >= swingTargetTime && !hasHittedSomething) || hasHittedSomething) {
    //             playerInputs.move.Enable();

    //             ResetSwingParameters();

    //             break;
    //         }

    //         yield return waitForEndOfFrame;
    //     }
    // }

    private void ResetSwingParameters() {
        chargeElapsedTime = 0f;
        elapsedSwingTime = 0f;
        currentSwingMoveSpeed = initialSwingMoveSpeed;
        accumulatedForce = 0f;
    }

    // private IEnumerator DodgeRoll(float targetTime) {
    //     float elapsedTime = 0f;

    //     playerInputs.LastMoveDirection = playerInputs.MoveDirection;

    //     playerInputs.move.Disable();
    //     playerInputs.swing.Disable();
    //     playerInputs.dodge.Disable();

    //     Vector2 startingPosition = transform.position;
    //     Vector2 targetDirection = startingPosition + (mouseWorldPosition - startingPosition).normalized * currentDodgeMoveSpeed;

    //     while (elapsedTime < targetTime) {
            
    //         elapsedTime += Time.deltaTime;

    //         if (elapsedTime >= targetTime || (transform.position.ToVector2() == targetDirection)) {
    //             playerInputs.move.Enable();
    //             playerInputs.swing.Enable();
    //             playerInputs.dodge.Enable();

    //             transform.position = targetDirection;
    //             break;
    //         }

    //         yield return waitForEndOfFrame;
    //     }
    // }

    // private IEnumerator DodgeTeleport(float targetTime) {
    //     float elapsedTime = 0f;

    //     playerInputs.LastMoveDirection = playerInputs.MoveDirection;
        
    //     playerInputs.move.Disable();
    //     playerInputs.swing.Disable();
    //     playerInputs.dodge.Disable();

    //     while (elapsedTime < targetTime) {
    //         playerVelocity = Vector2.Lerp(playerVelocity, Vector2.zero, elapsedTime);
    //         rb.velocity = playerVelocity;

    //         elapsedTime += Time.deltaTime;

    //         if (elapsedTime >= targetTime) {
    //             playerInputs.move.Enable();
    //             playerInputs.swing.Enable();
    //             playerInputs.dodge.Enable();

    //             transform.position = mouseWorldPosition;

    //             break;
    //         }

    //         yield return waitForEndOfFrame;
    //     }
    // }

    /*Vector3 camPosition_original;
    public float shakeAmount = 0.1f;
    public float shakeTargetTime = 1f;
    public float shakeTimeIntervals = 0.05f;

    IEnumerator CamAction() {
        camPosition_original = mainCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < shakeTargetTime) {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x + Random.Range(-shakeAmount, shakeAmount),
            mainCamera.transform.position.y + Random.Range(-shakeAmount, shakeAmount),
            mainCamera.transform.position.z);

            mainCamera.transform.position = camPosition_original;

            elapsedTime += Time.unscaledDeltaTime;

            if (elapsedTime >= shakeTargetTime) {
                mainCamera.transform.position = camPosition_original;
                break;
            }

            yield return waitForEndOfFrame;
        }
    }*/

    // private void OnDrawGizmos() {
    //     if (playerInputs.playerState == PlayerState.ChargingSwing || playerInputs.playerState == PlayerState.Swinging) {
    //         Utils.DrawWireSphere(handCenter.position, swingRadius, teamAssigner.currentTeamConfiguration.Color, Quaternion.identity);
    //         Debug.DrawRay(handCenter.position.ToVector2(), swingConeSide1 * swingRadius, teamAssigner.currentTeamConfiguration.Color);
    //         Debug.DrawRay(handCenter.position.ToVector2(), swingConeSide2 * swingRadius, teamAssigner.currentTeamConfiguration.Color);
    //     }
    // }
}