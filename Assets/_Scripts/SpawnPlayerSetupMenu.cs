using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class SpawnPlayerSetupMenu : MonoBehaviour {
    [SerializeField] private GameObject playerSetupMenuPrefab;
    [SerializeField] private PlayerInput input;

    private void Awake() {
        int index = input.playerIndex;
        this.name = $"PlayerConfiguration_P{index + 1}";
        var rootMenu = GameManager.Instance.joinMenuUIManager.currentSelectionPanelsGroup;

        if (rootMenu != null) {
            Transform panelTransform = PlayerConfigurationManager.Instance.SelectionPanelsList[index].transform;
            var menu = Instantiate(playerSetupMenuPrefab, panelTransform);

            menu.name = $"PlayerSetupMenuPanel_P{index + 1}";
            input.uiInputModule = menu.GetComponentInChildren<InputSystemUIInputModule>();

            //maybe just use playerInput to join and create a ScriptableInputs PlayerInputs with PlayerInputActions and use that as controls, in UI and in Gamepñay
            PlayerInputs playerInputs = ScriptableObject.CreateInstance<PlayerInputs>();
            GameObject playerConfigurationGameObject = this.gameObject;

            input.uiInputModule.actionsAsset = input.actions;
            input.uiInputModule.deselectOnBackgroundClick = false;
            input.uiInputModule.move = InputActionReference.Create(playerInputs.moveUI);
            input.uiInputModule.submit = InputActionReference.Create(playerInputs.submitUI);
            input.uiInputModule.cancel = InputActionReference.Create(playerInputs.cancelUI);

            SetupMenuController setupMenuController = menu.GetComponent<SetupMenuController>();
            setupMenuController.SetPlayerInput(input, playerInputs, playerConfigurationGameObject);
        }

        Destroy(this);
    }
}
