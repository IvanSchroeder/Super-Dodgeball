using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;
using ExtensionMethods;
using System;

public class PlayerGUIManager : MonoBehaviour {
    [SerializeField] public PlayerInputs playerInputs;
    private Color playerMainColor;
    [SerializeField] public FloatSO playerHealth;
    [SerializeField] public FloatSO playerMaxHealth;
    [SerializeField] public IntSO playerStacksAmount;
    [SerializeField] public TextMeshProUGUI playerNameText;
    [SerializeField] public Image portraitSprite;
    [SerializeField] public Image healthFillBarSprite;
    // [SerializeField] public Image healthFillBarMask;
    [SerializeField] public GameObject stacksGroup;
    [SerializeField] public List<GameObject> StacksList;
    [SerializeField] private Sprite emptyStackSprite;
    [SerializeField] private Sprite fullStackSprite;

    [SerializeField] private Renderer fillRenderer;

    Coroutine HealthFillCoroutine;

    public void Init(PlayerInputs _playerInputs, int amount, GameObject stackPrefab) {
        playerInputs = _playerInputs;
        playerMainColor = playerInputs.playerConfiguration.TeamAssigner.currentTeamConfiguration.Color;

        portraitSprite.material = playerInputs.playerConfiguration.ColorSwapMaterial;
        healthFillBarSprite.material = playerInputs.playerConfiguration.ColorSwapMaterial;

        playerNameText.text = playerInputs.playerNameTag;
        playerNameText.color = playerMainColor;

        playerHealth = playerInputs.playerConfiguration.HealthAsset;
        playerMaxHealth = playerInputs.playerConfiguration.MaxHealthAsset;
        playerStacksAmount = playerInputs.playerConfiguration.StacksAmountAsset;

        playerHealth.OnValueChange += UpdateHPFill;
        playerStacksAmount.OnValueChange += LoseStack;

        CreateStacks(amount, stackPrefab);
    }

    private void OnDisable() {
        playerHealth.OnValueChange -= UpdateHPFill;
        playerStacksAmount.OnValueChange -= LoseStack;
    }

    public void CreateStacks(int amount, GameObject stackPrefab) {
        StacksList = new List<GameObject>();

        for (int i = 0; i < amount; i++) {
            GameObject stack = Instantiate(stackPrefab, stacksGroup.transform);
            Image stackSprite = stack.GetComponent<Image>();
            stackSprite.sprite = fullStackSprite;
            stackSprite.material = playerInputs.playerConfiguration.ColorSwapMaterial;
            StacksList.Add(stack);
        }
    }

    private void LoseStack(int currentStack) {
        int count = 0;
        foreach (GameObject stack in StacksList) {
            Image stackSprite = stack.GetComponent<Image>();
            if (count < currentStack && stackSprite != fullStackSprite) stackSprite.sprite = fullStackSprite;
            else if (count >= currentStack && stackSprite != emptyStackSprite) stackSprite.sprite = emptyStackSprite;
            count++;
        }
    }

    private void UpdateHPFill(float currentHP) {
        float maxHP = playerMaxHealth.Value;
        float percentage = currentHP / maxHP;

        percentage = percentage.Clamp(0f, 1f);

        fillRenderer.material.SetFloat("FillAmount", percentage);

        // healthFillBarMask.fillAmount = percentage;
    }
}
