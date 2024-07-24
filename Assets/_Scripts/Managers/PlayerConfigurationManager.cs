using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System;
using ExtensionMethods;
using UnityEngine.InputSystem.Users;

public enum JoinMode {
    FreeForAll,
    TeamBased
}

public class PlayerConfigurationManager : MonoBehaviour {
    public static PlayerConfigurationManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] public List<PlayerInputs> PlayerInputsList = new List<PlayerInputs>();
    [SerializeField] private GameObject selectionPanelPrefab;
    [SerializeField] public List<Team> TeamsList = new List<Team>();
    [SerializeField] public List<GameObject> SelectionPanelsList = new List<GameObject>();
    [SerializeField] public List<GameObject> JoinPromptsPanelsList = new List<GameObject>();
    [SerializeField, Range(2, 8)] private int maxPlayers;
    [SerializeField, Range(1, 7)] private int minPlayers;
    [SerializeField] private int currentJoinedPlayers = 0;
    [SerializeField] private int currentReadyPlayers = 0;
    [SerializeField] private JoinMode joinMode = JoinMode.FreeForAll;

    public static event Action OnPlayerUnready;
    public static event Action OnAllPlayersReady;
    public static event Action OnAllPlayersLeft;
    public static event Action OnStartGame;

    public static event Action OnPlayerJoined;
    public static event Action OnPlayerLeft;

    private void OnEnable() {
        GameManager.OnLoadScreenEnd += ClearBindsAndLists;
    }

    private void OnDisable() {
        GameManager.OnLoadScreenEnd -= ClearBindsAndLists;
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
        }
    }

    public void Init() {
        SelectionPanelsList = new List<GameObject>();
        JoinPromptsPanelsList = new List<GameObject>();

        for (int i = 0; i < maxPlayers; i++) {
            var selectionPanel = Instantiate(selectionPanelPrefab, GameManager.Instance.joinMenuUIManager.currentSelectionPanelsGroup.transform);
            selectionPanel.name = $"{selectionPanelPrefab.name}_P{i + 1}";
            SelectionPanelsList.Add(selectionPanel);
            JoinPromptsPanelsList.Add(selectionPanel.transform.GetChild(0).gameObject);
        }
    }

    private void BindJoinButton() {
        foreach (PlayerInputs player in PlayerInputsList) {
            player.joinUI.started += StartGame;
        }
    }

    private void UnbindJoinButton() {
        foreach (PlayerInputs player in PlayerInputsList) {
            player.joinUI.started -= StartGame;
        }
    }

    private void ClearBindsAndLists() {
        SelectionPanelsList.Clear();
        JoinPromptsPanelsList.Clear();
        this.gameObject.Destroy();
    }

    public void JoinPlayer(PlayerInputs playerInputs) {
        int index = playerInputs.playerId;
        PlayerInputsList.Add(playerInputs);
        PlayerInputsList = new List<PlayerInputs>(PlayerInputsList.OrderBy(p => p.playerId));

        JoinPromptsPanelsList[index]?.SetActive(false);

        currentJoinedPlayers++;

        OnPlayerJoined?.Invoke();
    }

    public void RemovePlayer(PlayerInputs playerInputs) {
        int index = playerInputs.playerId;
        PlayerInputsList.Remove(playerInputs);
        PlayerInputsList = new List<PlayerInputs>(PlayerInputsList.OrderBy(p => p.playerId));

        JoinPromptsPanelsList[index]?.SetActive(true);

        Debug.Log($"Removed Player{index + 1}");

        currentJoinedPlayers--;

        if (currentJoinedPlayers <= 0) {
            currentJoinedPlayers = 0;
            Debug.Log($"All players left!");
            OnAllPlayersLeft?.Invoke();
        }

        CheckIfAllReady();
    }

    public void SetPlayerReady(PlayerInputs playerInputs) {
        // if (PlayerInputsList.Any(p => p == playerInputs)) {
        //     playerInputs.isReady = true;
        //     currentReadyPlayers++;
        // }

        if (PlayerInputsList.Contains(playerInputs)) {
            playerInputs.isReady = true;
            currentReadyPlayers++;
        }

        CheckIfAllReady();
    }

    public void CheckIfAllReady() {
        if (currentReadyPlayers >= minPlayers && currentReadyPlayers <= maxPlayers && PlayerInputsList.All(p => p.isReady == true)) {
            foreach (var player in PlayerInputsList) {
                player.playerConfigurationObject.transform.SetParent(GameManager.Instance.gameObject.transform);
            }

            Debug.Log("All players are ready");
            BindJoinButton();
            OnAllPlayersReady?.Invoke();
        }
    }

    public void SetPlayerUnready(PlayerInputs playerInputs) {
        // if (PlayerInputsList.Any(p => p == playerInputs)) {
        //     PlayerInputsList.Find(p => p == playerInputs).isReady = false;
        //     playerInputs.playerInputComponent.gameObject.transform.SetParent(null);
        //     currentReadyPlayers--;
        // }

        if (PlayerInputsList.Contains(playerInputs)) {
            playerInputs.isReady = false;
            playerInputs.playerInputComponent.gameObject.transform.SetParent(null);
            currentReadyPlayers--;
        }

        UnbindJoinButton();
        OnPlayerUnready?.Invoke();
    }

    public void StartGame(InputAction.CallbackContext context) {
        UnbindJoinButton();

        OnStartGame?.Invoke();
    }
}