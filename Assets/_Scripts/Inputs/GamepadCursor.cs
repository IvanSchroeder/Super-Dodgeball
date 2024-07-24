using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using ExtensionMethods;

public class GamepadCursor : MonoBehaviour {
    [SerializeField] public PlayerInput playerInput;
    [SerializeField] public RectTransform cursorTransform;
    [SerializeField] public Canvas canvas;
    [SerializeField] public RectTransform canvasRectTransform;
    [SerializeField] private float cursorSpeed = 1000f;

    private Mouse virtualMouse;
    private Camera mainCamera;
    private bool previousMouseState;

    private void OnEnable() {
        mainCamera = this.GetMainCamera();

        if (virtualMouse == null) {
            virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
        }
        else if (!virtualMouse.added) {
            InputSystem.AddDevice(virtualMouse);
        }

        InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);

        if (cursorTransform != null) {
            Vector2 position = cursorTransform.anchoredPosition;
            InputState.Change(virtualMouse.position, position);
        }
        
        InputSystem.onAfterUpdate += UpdateMotion;
    }

    private void OnDisable() {
        playerInput.user.UnpairDevice(virtualMouse);
        if (virtualMouse != null && virtualMouse.added) InputSystem.RemoveDevice(virtualMouse);
        InputSystem.onAfterUpdate -= UpdateMotion;
    }

    private void UpdateMotion() {
        if (virtualMouse == null || Gamepad.current == null) {
            return;
        }

        Vector2 deltaValue = Gamepad.current.rightStick.ReadValue();
        deltaValue *= cursorSpeed * Time.deltaTime;

        Vector2 currentPosition = virtualMouse.position.ReadValue();
        Vector2 newPosition = currentPosition + deltaValue;

        newPosition.x = newPosition.x.Clamp(0f, Screen.width); // TODO - add padding
        newPosition.y = newPosition.y.Clamp(0f, Screen.height);

        InputState.Change(virtualMouse.position, newPosition);
        InputState.Change(virtualMouse.delta, deltaValue);

        bool aButtonsIsPressed = Gamepad.current.aButton.IsPressed();
        if (previousMouseState != aButtonsIsPressed) {
            virtualMouse.CopyState<MouseState>(out var mouseState);
            mouseState.WithButton(MouseButton.Left, aButtonsIsPressed);
            InputState.Change(virtualMouse, mouseState);
            previousMouseState = aButtonsIsPressed;
        }

        AnchorCursor(newPosition);
    }

    private void AnchorCursor(Vector2 position) {
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out anchoredPosition);
        cursorTransform.anchoredPosition = anchoredPosition;
    }
}
