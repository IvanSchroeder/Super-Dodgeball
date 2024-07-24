using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;
using ExtensionMethods;
using System;

public class GameplayUIManager : MonoBehaviour {
    [Header("Prefabs")]
    [SerializeField] private GameObject playerGUIPrefab;
    [SerializeField] private GameObject stackPrefab;

    [Header("References")]
    [SerializeField] public GameObject playerGUIPanelsGroup;
    [SerializeField] public GameObject gameplayScreen;
    [SerializeField] public GameObject pauseMenuScreen;
    [SerializeField] public GameObject winScreen;
    [SerializeField] public TextMeshProUGUI ballSpeedText;
    [SerializeField] public TextMeshProUGUI winText;
    [SerializeField] public Button resumeGameButton;
    [SerializeField] public Button mainMenuButton;

    [Header("Parameters")]
    [SerializeField] private FloatSO currentBallSpeed;

    private void OnEnable() {
        currentBallSpeed.OnValueChange += UpdateBallSpeed;

        BallController.OnTeamChanged += ChangeBallTextColor;

        GameManager.OnPlayerCreated += SetGUISprites;
        GameManager.OnGameStarted += ClearPlayerUIPanels;
        GameManager.OnReturnToMainMenu += ClearPlayerUIPanels;
    }

    private void OnDisable() {
        currentBallSpeed.OnValueChange -= UpdateBallSpeed;

        BallController.OnTeamChanged -= ChangeBallTextColor;

        GameManager.OnPlayerCreated -= SetGUISprites;
        GameManager.OnGameStarted -= ClearPlayerUIPanels;
        GameManager.OnReturnToMainMenu -= ClearPlayerUIPanels;
    }

    public void ClearPlayerUIPanels() {
        playerGUIPanelsGroup.transform.DestroyChildren();
    }

    private void UpdateBallSpeed(float speed) {
        ballSpeedText.text = $"Ball Speed \n{speed:00000}";
    }

    private void ChangeBallTextColor(Color color) {
        ballSpeedText.color = color;
    }

    private void DefaultBallTextColor() {
        ballSpeedText.color = Color.white;
    }

    public void SetGUISprites(PlayerInputs playerInputs) {
        var playerGUI = Instantiate(playerGUIPrefab, playerGUIPanelsGroup.transform);

        var guiManager = playerGUI.GetComponent<PlayerGUIManager>();

        int maxCount = GameManager.Instance.maxStacksAmount;
        guiManager.Init(playerInputs, maxCount, stackPrefab);
    }
}
