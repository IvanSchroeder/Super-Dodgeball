using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.UI;
using ExtensionMethods;
using System;

public class MainMenuUIManager : MonoBehaviour {
    [SerializeField] public Button playGameButton;
    [SerializeField] public Button quitGameButton;
    [SerializeField] public Button showControlsButton;

    [SerializeField] public GameObject mainMenuScreen;
    [SerializeField] public GameObject controlsMenuScreen;
    [SerializeField] public GameObject joinMenuScreen;
}
