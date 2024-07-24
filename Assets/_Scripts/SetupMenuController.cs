using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ExtensionMethods;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.DualShock;

public enum JoiningState {
    Selected,
    Ready
}

public class SetupMenuController : MonoBehaviour {
    [SerializeField] private PlayerInputs playerInputs;
    [SerializeField] private GameObject configurationObject;
    [SerializeField] private Team selectedTeam;
    [SerializeField] private Image playerUISprite;
    [SerializeField] private Animator playerUISpriteAnimator;
    [SerializeField] private JoiningState joiningState;

    [SerializeField] private Image menuPanelImage;
    [SerializeField] private Color playerReadyPanelColor;
    [SerializeField] private Color defaultPanelColor;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI controllerTypeText;
    [SerializeField] private GameObject readySelectButtonsPromptPanel;
    [SerializeField] private GameObject readyCancelButtonsPromptPanel;
    [SerializeField] private TextMeshProUGUI readyText;
    
    [SerializeField] private List<GameObject> SelectButtonsPromptList;
    [SerializeField] private GameObject spacebarButtonPrompt;
    [SerializeField] private GameObject crossButtonPrompt;
    [SerializeField] private GameObject aButtonPrompt;
    [SerializeField] private GameObject currentSelectButtonPrompt;
    [SerializeField] private List<GameObject> CancelButtonsPromptList;
    [SerializeField] private GameObject escapeButtonPrompt;
    [SerializeField] private GameObject circlerossButtonPrompt;
    [SerializeField] private GameObject bButtonPrompt;
    [SerializeField] private GameObject currentCancelButtonPrompt;

    private readonly int Turnaround = Animator.StringToHash("Turnaround");
    private readonly int IdleSouth = Animator.StringToHash("IdleSouth");

    private void OnEnable() {
        PlayerConfigurationManager.OnPlayerUnready += BindButtons;
        GameManager.OnCountdownEnd += UnbindButtons;
    }

    private void OnDisable() {
        PlayerConfigurationManager.OnPlayerUnready -= BindButtons;
        GameManager.OnCountdownEnd -= UnbindButtons;
    }

    private void Start() {
        joiningState = JoiningState.Selected;
        playerReadyPanelColor = defaultPanelColor;
        menuPanelImage.color = playerReadyPanelColor;
        playerUISprite.gameObject.SetActive(true);
        playerUISpriteAnimator.CrossFade(Turnaround, 0, 0);
    }

    private void Submit(InputAction.CallbackContext context) {
        switch(joiningState) {
            case JoiningState.Selected:
                ReadyPlayer();
                playerUISpriteAnimator.CrossFade(IdleSouth, 0, 0);
            break;
        }
    }

    private void Cancel(InputAction.CallbackContext context) {
        switch(joiningState) {
            case JoiningState.Ready:
                UnreadyPlayer();
                playerUISpriteAnimator.CrossFade(Turnaround, 0, 0);
            break;
            case JoiningState.Selected:
                UnbindButtons();
                PlayerConfigurationManager.Instance.RemovePlayer(playerInputs);

                if (playerInputs.controllerType == ControllerType.PlayStation) {
                    var gamepad = (DualShockGamepad)playerInputs.device;
                    gamepad.SetLightBarColor(Color.white);
                }

                GameObject.Destroy(configurationObject);
                GameObject.Destroy(this.gameObject);
            break;
        }
    }

    public void SetPlayerInput(PlayerInput input, PlayerInputs inputs, GameObject _configurationObject) {
        int index = input.playerIndex;
        playerInputs = inputs;

        string name = $"P{index + 1}_Inputs";
        playerInputs.name = name;
        PlayerInputActions controls = new PlayerInputActions();
        InputDevice device = input.devices[0];
        string controllersScheme = input.currentControlScheme;
        ControllerType controllerType = ControllerType.Xbox;

        if (controllersScheme.Contains("Keyboard")) controllerType = ControllerType.PC;
        else if (controllersScheme.Contains("Controller")) {
            if (device.description.manufacturer.Contains("Sony")) {
                controllerType = ControllerType.PlayStation;
            }
            else if (!device.description.manufacturer.Contains("Sony")) {
                controllerType = ControllerType.Xbox;
            }
        }

        playerInputs.Init(controls, input, name, index, device, controllersScheme, controllerType, _configurationObject);
        playerInputs.DisableGameplayActions();
        playerInputs.EnableUIActions();

        selectedTeam = PlayerConfigurationManager.Instance.TeamsList[index];
        playerInputs.playerConfiguration.Team = selectedTeam;
        configurationObject = _configurationObject;

        playerInputs.playerConfiguration.ColorSwapMaterial = Instantiate(GameManager.Instance.colorSwapMat);
        playerInputs.playerConfiguration.ColorSwapShaderProperties = new ShaderPropertyIDs() {
            _MainColor = Shader.PropertyToID("_MainColor"),
            _AscentsColor = Shader.PropertyToID("_AscentsColor"),
            _EyesColor = Shader.PropertyToID("_EyesColor")
        };
        playerInputs.playerConfiguration.ColorSwapMaterial.SetColor(playerInputs.playerConfiguration.ColorSwapShaderProperties._MainColor, selectedTeam.defaultTeamConfiguration.Color);

        playerUISprite.material = playerInputs.playerConfiguration.ColorSwapMaterial;

        switch (playerInputs.controllerType) {
            case ControllerType.PC:
                controllerTypeText.SetText("PC Keyboard");
                currentSelectButtonPrompt = spacebarButtonPrompt;
                currentCancelButtonPrompt = escapeButtonPrompt;
                spacebarButtonPrompt.SetActive(true);
                escapeButtonPrompt.SetActive(true);
            break;
            case ControllerType.PlayStation:
                controllerTypeText.SetText("PlayStation Controller");
                var gamepad = (DualShockGamepad)playerInputs.device;
                gamepad.SetLightBarColor(selectedTeam.defaultTeamConfiguration.Color);
                currentSelectButtonPrompt = crossButtonPrompt;
                currentCancelButtonPrompt = circlerossButtonPrompt;
                crossButtonPrompt.SetActive(true);
                circlerossButtonPrompt.SetActive(true);
            break;
            case ControllerType.Xbox:
                controllerTypeText.SetText("Xbox Controller");
                currentSelectButtonPrompt = aButtonPrompt;
                currentCancelButtonPrompt = bButtonPrompt;
                aButtonPrompt.SetActive(true);
                bButtonPrompt.SetActive(true);
            break;
        }

        readySelectButtonsPromptPanel.SetActive(true);
        readyCancelButtonsPromptPanel.SetActive(false);

        titleText.SetText($"- Player {index + 1} -");
        titleText.faceColor = selectedTeam.defaultTeamConfiguration.Color;
        playerReadyPanelColor = defaultPanelColor;
        menuPanelImage.color = playerReadyPanelColor;

        readyText.SetText("Not Ready!");
        readyText.faceColor = Color.red;
        joiningState = JoiningState.Selected;

        BindButtons();
        PlayerConfigurationManager.Instance.JoinPlayer(playerInputs);
    }

    private void ReadyPlayer() {
        readySelectButtonsPromptPanel.SetActive(false);
        readyCancelButtonsPromptPanel.SetActive(true);
        
        playerReadyPanelColor = selectedTeam.defaultTeamConfiguration.Color;
        menuPanelImage.color = playerReadyPanelColor;
        menuPanelImage.color = menuPanelImage.color.SetA(defaultPanelColor.a);

        readyText.SetText("Ready!");
        readyText.faceColor = Color.green;

        PlayerConfigurationManager.Instance.SetPlayerReady(playerInputs);

        joiningState = JoiningState.Ready;
    }

    private void UnreadyPlayer() {
        readySelectButtonsPromptPanel.SetActive(true);
        readyCancelButtonsPromptPanel.SetActive(false);

        playerReadyPanelColor = defaultPanelColor;
        menuPanelImage.color = playerReadyPanelColor;

        PlayerConfigurationManager.Instance.SetPlayerUnready(playerInputs);

        joiningState = JoiningState.Selected;
    }

    private void BindButtons() {
        playerInputs.submitUI.started += Submit;
        playerInputs.cancelUI.started += Cancel;
    }

    private void UnbindButtons() {
        playerInputs.submitUI.started -= Submit;
        playerInputs.cancelUI.started -= Cancel;
    }

    // private void SetButtonColors(Button button, Color normalColor, Color highlightedColor, Color selectedColor, Color pressedColor, Color disabledColor) {
    //     ColorBlock colors = button.colors;
    //     colors.normalColor = normalColor;
    //     colors.highlightedColor = highlightedColor;
    //     colors.selectedColor = selectedColor;
    //     colors.pressedColor = pressedColor;
    //     colors.disabledColor = disabledColor;
    //     button.colors = colors;
    // }
}
