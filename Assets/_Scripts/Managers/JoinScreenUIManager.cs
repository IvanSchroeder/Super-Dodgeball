using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;
using ExtensionMethods;
using System;

public class JoinScreenUIManager : MonoBehaviour {
    [SerializeField] public GameObject currentSelectionPanelsGroup;
    [SerializeField] private GameObject startGamePromptPanel;
    [SerializeField] private TextMeshProUGUI countdownTimerText;

    [SerializeField] private float secondsToStart = 5f;
    [SerializeField] private FloatSO countdownTimer;

    WaitForEndOfFrame waitForEndOfFrame;
    Coroutine CountdownCoroutine;

    [Header("Format Settings")]
    [SerializeField] private bool hasFormat = true;
    [SerializeField] private TimerFormats format;
    [SerializeField] private SerializedDictionary<TimerFormats, string> timeFormats = new SerializedDictionary<TimerFormats, string>();

    private void OnEnable() {
        PlayerConfigurationManager.OnAllPlayersReady += EnableStartGamePrompt;
        PlayerConfigurationManager.OnPlayerUnready += DisableStartGamePrompt;
        PlayerConfigurationManager.OnPlayerJoined += DisableStartGamePrompt;
        PlayerConfigurationManager.OnStartGame += InitializeCountdownText;

        GameManager.OnGameStarted += ClearSelectionPanels;
        GameManager.OnGameStarted += DisableAllPrompts;
        GameManager.OnReturnToMainMenu += ClearSelectionPanels;

        countdownTimer.OnValueChange += UpdateTimerText;
    }

    private void OnDisable() {
        PlayerConfigurationManager.OnAllPlayersReady -= EnableStartGamePrompt;
        PlayerConfigurationManager.OnPlayerUnready -= DisableStartGamePrompt;
        PlayerConfigurationManager.OnPlayerJoined -= DisableStartGamePrompt;
        PlayerConfigurationManager.OnStartGame -= InitializeCountdownText;

        GameManager.OnGameStarted -= ClearSelectionPanels;
        GameManager.OnGameStarted -= DisableAllPrompts;
        GameManager.OnReturnToMainMenu -= ClearSelectionPanels;

        countdownTimer.OnValueChange -= UpdateTimerText;
    }

    private void Start() {
        timeFormats.Add(TimerFormats.Whole, "0");
        timeFormats.Add(TimerFormats.TenthDecimal, "0.0");
        timeFormats.Add(TimerFormats.HundrethDecimal, "0.00");
        countdownTimer.Value = secondsToStart;
        startGamePromptPanel.SetActive(false);
        countdownTimerText.gameObject.SetActive(false);
    }

    public void ClearSelectionPanels() {
        currentSelectionPanelsGroup.transform.DestroyChildren();
    }

    private void DisableAllPrompts() {
        countdownTimerText.gameObject.SetActive(false);
        startGamePromptPanel.SetActive(false);
    }

    private void EnableStartGamePrompt() {
        countdownTimerText.gameObject.SetActive(false);
        startGamePromptPanel.SetActive(true);
    }

    private void DisableStartGamePrompt() {
        countdownTimerText.gameObject.SetActive(false);
        startGamePromptPanel.SetActive(false);

        if (CountdownCoroutine != null) StopCoroutine(CountdownCoroutine);
        countdownTimer.Value = secondsToStart;
    }

    public void InitializeCountdownText() {
        countdownTimerText.gameObject.SetActive(true);
        startGamePromptPanel.SetActive(false);
    }

    private void UpdateTimerText(float countdownTimer) {
        string timerValue = hasFormat ? countdownTimer.ToString(timeFormats[format]) : countdownTimer.ToString();
        countdownTimerText.text = $"Starting Game in... {timerValue}";;
    }
}

public enum TimerFormats {
    Whole,
    TenthDecimal,
    HundrethDecimal
}
