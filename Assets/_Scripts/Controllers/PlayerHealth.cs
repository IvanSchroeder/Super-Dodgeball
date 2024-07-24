using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using System;
using Object = UnityEngine.Object;

public class PlayerHealth : MonoBehaviour, IDamageable {
    [Header("References")]
    public PlayerInputs playerInputs;
    public PlayerController playerController;
    [SerializeField] public Collider2D hurtboxTrigger;
    [SerializeField] public SpriteRenderer playerSprite;

    [Header("Health Parameters")]
    [SerializeField] private float healthPercentage;
    [SerializeField] public FloatSO playerHealth;
    [SerializeField] public FloatSO playerMaxHealth;
    [SerializeField] public IntSO playerStacksAmount;

    [Header("Damage Parameters")]
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private float invulnerabilityCooldown;
    [SerializeField] private float deathSeconds;

    public static event Action OnPlayerHit;
    public static event Action OnPlayerDeath;

    public event Action<Vector2> OnPlayerDamaged;
    public event Action OnPlayerDead;
    public event Action OnPlayerRespawn;

    private Coroutine PlayerInvulnerabilityCoroutine;

    private WaitForSeconds secondsInvulnerable;
    private WaitForSeconds secondsInDeath;

    // private void OnEnable() {
    //     playerController.OnKnockbackEnd += SetInvulnerability;
    //     playerController.OnDodgeStart += EnableInvulnerability;
    //     playerController.OnDodgeEnd += DisableInvulnerability;
    //     OnPlayerDead += EnableInvulnerability;
    //     OnPlayerRespawn += SetInvulnerability;

    //     GameManager.OnAllPlayersRespawn += Spawn;
    // }

    public void Init(PlayerInputs inputs) {
        playerInputs = inputs;
        playerController = playerInputs.playerConfiguration.Controller;

        playerHealth = playerInputs.playerConfiguration.HealthAsset;
        playerMaxHealth = playerInputs.playerConfiguration.MaxHealthAsset;
        playerStacksAmount = playerInputs.playerConfiguration.StacksAmountAsset;

        playerController.OnKnockbackEnd += SetInvulnerability;
        playerController.OnDodgeStart += EnableInvulnerability;
        playerController.OnDodgeEnd += DisableInvulnerability;
        OnPlayerDead += EnableInvulnerability;
        OnPlayerRespawn += SetInvulnerability;

        GameManager.OnAllPlayersRespawn += Spawn;

        secondsInvulnerable = new WaitForSeconds(invulnerabilityCooldown);
        secondsInDeath = new WaitForSeconds(deathSeconds);
    }

    private void OnDisable() {
        playerController.OnKnockbackEnd -= SetInvulnerability;
        playerController.OnDodgeStart -= EnableInvulnerability;
        playerController.OnDodgeEnd -= DisableInvulnerability;
        OnPlayerDead -= EnableInvulnerability;
        OnPlayerRespawn -= SetInvulnerability;

        GameManager.OnAllPlayersRespawn -= Spawn;
    }

    private void Spawn() {
        if (playerInputs.isDead && !playerInputs.isTerminated) {
            playerHealth.Value = playerMaxHealth.Value;
            playerInputs.isDead = false;
            playerInputs.wasChecked = false;
            OnPlayerRespawn?.Invoke();
        }
    }

    public void Damage(float damageAmount, Vector2 hitDirection) => DamageHP(damageAmount, hitDirection);

    private void DamageHP(float amount, Vector2 hitDirection) {
        if (!isInvulnerable && !playerInputs.isDead) {
            EnableInvulnerability();
            var resultHP = playerHealth.Value - amount;

            if (resultHP > 0f) PlayerHit(resultHP, hitDirection);
            else if (resultHP <= 0f) PlayerDeath();
        }
    }

    public void EnableInvulnerability() {
        isInvulnerable = true;
        hurtboxTrigger.enabled = false;
    }

    public void DisableInvulnerability() {
        isInvulnerable = false;
        hurtboxTrigger.enabled = true;
    }

    private void SetInvulnerability() {
        if (PlayerInvulnerabilityCoroutine != null) {
            StopCoroutine(PlayerInvulnerabilityCoroutine);
            DisableInvulnerability();
        }

        PlayerInvulnerabilityCoroutine = StartCoroutine(Invulnerability());
    }

    private IEnumerator Invulnerability() {
        float originalAlpha = playerSprite.color.GetAValue();
        float alpha = originalAlpha / 2f;
        playerSprite.color = playerSprite.color.SetColorA(alpha);

        EnableInvulnerability();

        yield return secondsInvulnerable;

        DisableInvulnerability();
        playerSprite.color = playerSprite.color.SetColorA(originalAlpha);

        yield return null;
    }

    private void PlayerHit(float finalHP, Vector2 knockbackDirection) {
        playerHealth.Value = finalHP;
        OnPlayerHit?.Invoke();
        OnPlayerDamaged?.Invoke(knockbackDirection);
    }

    private void PlayerDeath() {
        playerHealth.Value = 0f;
        playerInputs.isDead = true;
        int stacks = playerStacksAmount.Value;
        stacks--;

        if (stacks <= 0) {
            stacks = 0;
            playerInputs.isTerminated = true;
        }

        playerStacksAmount.Value = stacks;
        OnPlayerDead?.Invoke();
        OnPlayerDeath?.Invoke();
    }

    private void HealHP(float amount) {
        var currentHealth = playerHealth.Value + amount;
        currentHealth = currentHealth.Clamp(0f, playerMaxHealth.Value);

        playerHealth.Value = currentHealth;
    }
}
