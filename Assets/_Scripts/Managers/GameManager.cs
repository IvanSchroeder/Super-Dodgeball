using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System;
using ExtensionMethods;
using UnityEngine.UI;
using TMPro;

public enum GameState {
    Paused,
    Playing
}

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public GameState gameState = GameState.Playing;

    [Header("--- Prefabs ---")]
    [Header("UI")]
    [SerializeField] private GameObject playerConfigurationManagerPrefab;
    [SerializeField] private GameObject targetPrefab;
    [Header("Gameplay")]
    [SerializeField] private GameObject ballPrefab;

    [Header("--- References ---")]
    [Header("UI")]
    [SerializeField] public Camera mainCamera;
    [SerializeField] public Canvas mainCanvas;
    [SerializeField] public Canvas loadingScreenCanvas;
    [SerializeField] public CanvasGroup canvasGroup;
    [SerializeField] public MainMenuUIManager mainMenuUIManager;
    [SerializeField] public GameplayUIManager gameplayUIManager;
    [SerializeField] public JoinScreenUIManager joinMenuUIManager;
    [SerializeField] public GameObject mainMenuUI;
    [SerializeField] public GameObject gameplayUI;
    [SerializeField] public GameObject currentScreenUI;
    [SerializeField] public GameObject currentCanvas;
    [SerializeField] public GameObject playerConfigurationManager;
    [SerializeField] public GameObject currentSelectionPanelsGroup;
    [SerializeField] public InputSystemUIInputModule mainMenuUIInputModule;
    [SerializeField] public MultiplayerEventSystem eventSystem;
    [Header("Gameplay")]
    [SerializeField] public CameraController mainCameraController;
    [SerializeField] public GameObject currentBall;
    [SerializeField] private GameObject levelCenter;

    private static int MainMenuScene;
    private static int GameplayScene;

    [Header("--- Player Info ---")]
    [SerializeField] private List<PlayerController> PlayerControllersList;
    [SerializeField] private List<PlayerInputs> PlayerInputsList;
    [SerializeField] private GameObject playerPrefab;

    [Header("--- Scene Management Parameters ---")]
    [SerializeField] private FloatSO countdownTimer;
    [SerializeField] private float secondsToWaitButtonPress;
    [SerializeField] private float secondsToStart;
    [SerializeField] private float secondsToWaitAfterCountdown;
    [SerializeField] private float secondsForInputModuleOff;
    [SerializeField] private float secondsForWinScreen;
    [SerializeField] private float fadeInSeconds;
    [SerializeField] private float fadeOutSeconds;
    [SerializeField] private float secondsToWaitInLoadingScreen;

    [Header("--- Gameplay Parameters ---")]
    [SerializeField] private float timerLimit;
    [SerializeField] private float gameplayTimer;
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] public int maxStacksAmount = 5;

    [Header("--- Spawn Parameters ---")]
    [SerializeField] private float radiusAroundCenter;
    [Range(0f, 360f)]
    [SerializeField] private float rotationOffset;
    [SerializeField] private float remainingPlayers;
    [SerializeField] private float remainingPlayersThisRound;
    [SerializeField] private float secondsToRespawnPlayers;

    [Header("--- Slowdown Parameters ---")]
    [SerializeField] private Optional<bool> hasSlowDown = new Optional<bool>(true);
    [SerializeField] private SlowdownParameters freezeFrameParameters = new SlowdownParameters(0f, 0f, 0f, 0.5f);
    [SerializeField] private SlowdownParameters slowdownParameters = new SlowdownParameters(0.1f, 0.1f, 0.1f, 0.1f);
    [SerializeField] private SlowdownParameters deathSlowdownParameters = new SlowdownParameters(0.5f, 0.5f, 0.25f, 1.5f);

    [SerializeField] public Material colorSwapMat;

    public static event Action OnGameStarted;
    public static event Action OnCountdownEnd;
    public static event Action OnLoadScreenEnd;
    public static event Action<List<PlayerController>> OnPlayersCreated;
    public static event Action<PlayerInputs> OnPlayerCreated;
    public static event Action OnAllPlayersRespawn;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnReturnToMainMenu;

    private WaitForSecondsRealtime secondsAfterButtonPress;
    private WaitForSecondsRealtime secondsAfterSlowdown;
    private WaitForSecondsRealtime secondsAfterCountdown;
    private WaitForSecondsRealtime secondsInLoadingScreen;
    private WaitForSeconds secondsForRespawnPlayers;
    private WaitForSeconds secondsAfterWin;
    private WaitForSeconds secondsInputModuleOff;
    // private WaitForEndOfFrame waitForEndOfFrame;
    private Coroutine SlowdownCoroutine;
    private Coroutine CountdownCoroutine;

    private void OnEnable() {
        PlayerConfigurationManager.OnStartGame += StartCountdown;
        PlayerConfigurationManager.OnPlayerUnready += SuspendCountdown;
        PlayerConfigurationManager.OnPlayerJoined += SuspendCountdown;
        PlayerConfigurationManager.OnPlayerJoined += UnbindCancel;
        PlayerConfigurationManager.OnAllPlayersLeft += RebindCancel;

        BallController.OnBallHit += FreezeFrame;

        PlayerHealth.OnPlayerHit += FreezeFrame;
        PlayerHealth.OnPlayerDeath += PlayerDeathSlowdown;
        PlayerHealth.OnPlayerDeath += CheckRemainingPlayersThisRound;
    }

    private void OnDisable() {
        PlayerConfigurationManager.OnStartGame -= StartCountdown;
        PlayerConfigurationManager.OnPlayerUnready -= SuspendCountdown;
        PlayerConfigurationManager.OnPlayerJoined -= SuspendCountdown;
        PlayerConfigurationManager.OnPlayerJoined -= UnbindCancel;
        PlayerConfigurationManager.OnAllPlayersLeft -= RebindCancel;

        BallController.OnBallHit -= FreezeFrame;

        PlayerHealth.OnPlayerHit -= FreezeFrame;
        PlayerHealth.OnPlayerDeath -= PlayerDeathSlowdown;
        PlayerHealth.OnPlayerDeath -= CheckRemainingPlayersThisRound;
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            print($"Destroyed instance of GameManager");
            Destroy(gameObject);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(Instance);
            print($"Assigned current instance of GameManager");
        }

        MainMenuScene = SceneManager.GetSceneByBuildIndex(0).buildIndex;
        GameplayScene = SceneManager.GetSceneByBuildIndex(1).buildIndex;

        secondsAfterSlowdown = new WaitForSecondsRealtime(1f);
        secondsInputModuleOff = new WaitForSeconds(secondsForInputModuleOff);
        secondsAfterButtonPress = new WaitForSecondsRealtime(secondsToWaitButtonPress);
        secondsAfterCountdown = new WaitForSecondsRealtime(secondsToWaitAfterCountdown);
        secondsInLoadingScreen = new WaitForSecondsRealtime(secondsToWaitInLoadingScreen);
        secondsForRespawnPlayers = new WaitForSeconds(secondsToRespawnPlayers);
        secondsAfterWin = new WaitForSeconds(secondsForWinScreen);
    }

    private void Start() {
        #if UNITY_EDITOR
            secondsToStart = 1f;
        #endif

        if (mainCamera == null) mainCamera = this.GetMainCamera();
        mainCamera.transform.SetParent(this.transform);
        if (mainCameraController == null) mainCameraController = mainCamera?.GetComponentInHierarchy<CameraController>();

        mainCanvas.gameObject.transform.SetParent(this.transform, false);
        mainCanvas.worldCamera = mainCamera;
        loadingScreenCanvas.worldCamera = mainCamera;

        Init();
    }

    private void Init() {
        SwitchCanvas(currentCanvas, mainMenuUI);
        gameplayUI.SetActive(false);
        SwitchScreens(currentScreenUI, mainMenuUIManager.mainMenuScreen);
        mainMenuUIManager.controlsMenuScreen.SetActive(false);
        mainMenuUIManager.joinMenuScreen.SetActive(false);
        gameplayUIManager.pauseMenuScreen.SetActive(false);
        gameplayUIManager.winScreen.SetActive(false);

        mainMenuUIManager.playGameButton.onClick.AddListener(PlayGame);
        mainMenuUIManager.quitGameButton.onClick.AddListener(QuitGame);
        mainMenuUIManager.showControlsButton.onClick.AddListener(ShowControlsScreen);

        eventSystem.firstSelectedGameObject = mainMenuUIManager.playGameButton.gameObject;
        eventSystem.SetSelectedGameObject(mainMenuUIManager.playGameButton.gameObject);
    }

    private void BindCancel() {
        mainMenuUIInputModule.cancel.action.started += BackToMainMenu;
    }

    private void RebindCancel() {
        mainMenuUIInputModule.cancel.action.started += BackToMainMenu;
        StartCoroutine(ToggleInputModule());
    }

    private void UnbindCancel() {
        mainMenuUIInputModule.cancel.action.started -= BackToMainMenu;
        mainMenuUIInputModule.enabled = false;
    }

    private IEnumerator ToggleInputModule() {
        mainMenuUIInputModule.enabled = false;
        yield return secondsInputModuleOff;
        mainMenuUIInputModule.enabled = true;
        yield return null;
    }

    private void BindUIControls() {
        foreach (PlayerInputs input in PlayerInputsList) {
            input.EnableUIActions();
            input.pauseUI.started += TogglePauseState;
        }
    }

    private void UnbindUIControls() {
        foreach (PlayerInputs input in PlayerInputsList) {
            input.DisableUIActions();
            input.pauseUI.started -= TogglePauseState;
        }
    }

    private void CreatePlayerConfigurationManager(bool startActive = false) {
        if (playerConfigurationManager == null) {
            playerConfigurationManager = Instantiate(playerConfigurationManagerPrefab);
            playerConfigurationManager.GetComponentInHierarchy<PlayerConfigurationManager>().Init();
            playerConfigurationManager.name = playerConfigurationManagerPrefab.name;
            playerConfigurationManager.SetActive(startActive);
        }
    }

    public void PlayGame() {
        StartCoroutine(LoadPlayGameScreen());
    }

    public void BackToMainMenu(InputAction.CallbackContext context) {
        StartCoroutine(LoadMainMenuScreen());
    }

    public void ShowControlsScreen() {
        StartCoroutine(LoadControlsScreen());
    }

    public void QuitGame() {
        #if UNITY_EDITOR
         // Application.Quit() does not work in the editor so
         // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private IEnumerator LoadPlayGameScreen() {
        loadingScreenCanvas.gameObject.SetActive(true);
        yield return secondsAfterButtonPress;
        yield return StartCoroutine(ScreenFade(1f, fadeInSeconds));
        SwitchScreens(currentScreenUI, mainMenuUIManager.joinMenuScreen);
        joinMenuUIManager.ClearSelectionPanels();
        CreatePlayerConfigurationManager(true);
        yield return StartCoroutine(ScreenFade(0f, fadeOutSeconds));
        loadingScreenCanvas.gameObject.SetActive(false);
        yield return StartCoroutine(ToggleInputModule());
        BindCancel();
    }

    private IEnumerator LoadMainMenuScreen() {
        UnbindCancel();
        loadingScreenCanvas.gameObject.SetActive(true);
        yield return secondsAfterButtonPress;
        yield return StartCoroutine(ScreenFade(1f, fadeInSeconds));
        SwitchScreens(currentScreenUI, mainMenuUIManager.mainMenuScreen);
        eventSystem.SetSelectedGameObject(mainMenuUIManager.playGameButton.gameObject);
        playerConfigurationManager?.Destroy();
        playerConfigurationManager = null;
        joinMenuUIManager.ClearSelectionPanels();
        yield return StartCoroutine(ScreenFade(0f, fadeOutSeconds));
        loadingScreenCanvas.gameObject.SetActive(false);
        yield return StartCoroutine(ToggleInputModule());
    }

    private IEnumerator LoadControlsScreen() {
        loadingScreenCanvas.gameObject.SetActive(true);
        yield return secondsAfterButtonPress;
        yield return StartCoroutine(ScreenFade(1f, fadeInSeconds));
        SwitchScreens(currentScreenUI, mainMenuUIManager.controlsMenuScreen);
        yield return StartCoroutine(ScreenFade(0f, fadeOutSeconds));
        loadingScreenCanvas.gameObject.SetActive(false);
        yield return StartCoroutine(ToggleInputModule());
        BindCancel();
    }
    
    private void SwitchCanvas(GameObject canvasToHide, GameObject canvasToShow) {
        if (canvasToHide != null) canvasToHide.SetActive(false);
        if (canvasToShow != null) canvasToShow.SetActive(true);
        currentCanvas = canvasToShow;
    }

    private void SwitchScreens(GameObject screenToHide, GameObject screenToShow) {
        if (screenToHide != null) screenToHide?.SetActive(false);
        if (screenToShow != null) screenToShow?.SetActive(true);
        currentScreenUI = screenToShow;
    }

    private void StartCountdown() {
        if (CountdownCoroutine != null) StopCoroutine(CountdownCoroutine);
        CountdownCoroutine = StartCoroutine(GameStartCountdown());
    }

    private IEnumerator GameStartCountdown() {
        countdownTimer.Value = secondsToStart;

        while (countdownTimer.Value > 0.0f) {
            countdownTimer.Value -= Time.unscaledDeltaTime;

            yield return Utils.waitForEndOfFrame;
        }

        countdownTimer.Value = 0.0f;
        OnCountdownEnd?.Invoke();
        yield return secondsAfterCountdown;
        StartGame();
    }

    private void SuspendCountdown() {
        if (CountdownCoroutine != null) StopCoroutine(CountdownCoroutine);
    }

    private void StartGame() {
        StartCoroutine(LoadScene(1, InitializeGameplayScene()));
    }

    private IEnumerator LoadScene(int _sceneToLoad, IEnumerator midLoadRoutine = null, IEnumerator endLoadRoutine = null) {
        loadingScreenCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(ScreenFade(1f, fadeInSeconds * 2));
        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(_sceneToLoad);

        while (!loadingOperation.isDone) {
            yield return null;
        }

        yield return secondsInLoadingScreen;

        Time.timeScale = 1f;
        if (midLoadRoutine != null) yield return StartCoroutine(midLoadRoutine);
        
        yield return StartCoroutine(ScreenFade(0f, fadeOutSeconds * 2));
        if (endLoadRoutine != null) yield return StartCoroutine(endLoadRoutine);

        OnLoadScreenEnd?.Invoke();
        loadingScreenCanvas.gameObject.SetActive(false);
    }

    private IEnumerator ScreenFade(float targetAlpha, float duration = 1f) {
        float elapsedTime = 0f;
        float percentageComplete = elapsedTime / duration;
        float startValue = canvasGroup.alpha;

        while (elapsedTime < duration) {
            canvasGroup.alpha = Mathf.Lerp(startValue, targetAlpha, percentageComplete);
            elapsedTime += Time.unscaledDeltaTime;
            percentageComplete = elapsedTime / duration;

            yield return Utils.waitForEndOfFrame;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator InitializeGameplayScene() {
        OnGameStarted?.Invoke();
        var cameraTarget = Instantiate(targetPrefab);
        var cameraPosition = Instantiate(targetPrefab);
        cameraTarget.GetComponent<SpriteRenderer>().color = Color.red;
        cameraPosition.GetComponent<SpriteRenderer>().color = Color.cyan;
        cameraTarget.name = $"CameraTarget";
        cameraPosition.name = $"CameraPosition";
        mainCameraController.cameraTarget = cameraTarget.transform;
        mainCameraController.cameraPosition = cameraPosition.transform;

        SwitchCanvas(currentCanvas, gameplayUI);
        mainMenuUIManager.mainMenuScreen.SetActive(true);
        mainMenuUIManager.controlsMenuScreen.SetActive(false);
        mainMenuUIManager.joinMenuScreen.SetActive(false);
        SwitchScreens(currentScreenUI, gameplayUIManager.gameplayScreen);
        gameplayUIManager.pauseMenuScreen.SetActive(false);
        gameplayUIManager.winScreen.SetActive(false);

        levelCenter = GameObject.FindWithTag("LevelCenter");
        currentBall = Instantiate(ballPrefab, levelCenter.transform.position, Quaternion.identity);
        currentBall.name = ballPrefab.name;

        gameplayUIManager.resumeGameButton.onClick.AddListener(ResumeGame);
        gameplayUIManager.mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        eventSystem.firstSelectedGameObject = gameplayUIManager.resumeGameButton.gameObject;
        eventSystem.SetSelectedGameObject(gameplayUIManager.resumeGameButton.gameObject);

        CreatePlayerControllers();
        BindUIControls();
        UnbindCancel();
        yield return StartCoroutine(ToggleInputModule());
        yield return null;
    }

    private void ReturnToMainMenu() {
        foreach(PlayerInputs input in PlayerInputsList) {
            input.playerConfigurationObject.Destroy();
        }

        OnReturnToMainMenu?.Invoke();
        PlayerInputsList.Clear();
        PlayerControllersList.Clear();
        StartCoroutine(LoadScene(0, InitializeMainMenuScene(), ToggleInputModule()));
    }

    private IEnumerator InitializeMainMenuScene() {
        SwitchCanvas(currentCanvas, mainMenuUI);
        gameplayUI.SetActive(false);
        SwitchScreens(currentScreenUI, mainMenuUIManager.mainMenuScreen);
        mainMenuUIManager.controlsMenuScreen.SetActive(false);
        mainMenuUIManager.joinMenuScreen.SetActive(false);
        gameplayUIManager.pauseMenuScreen.SetActive(false);
        gameplayUIManager.winScreen.SetActive(false);

        eventSystem.firstSelectedGameObject = mainMenuUIManager.playGameButton.gameObject;
        eventSystem.SetSelectedGameObject(mainMenuUIManager.playGameButton.gameObject);
        yield return null;
    }

    private void ResumeGame() {
        UnpauseGame();
    }

    private void TogglePauseState(InputAction.CallbackContext context = default) {
        if (gameState == GameState.Playing) {
            PauseGame();
        }
        else if (gameState == GameState.Paused) {
            UnpauseGame();
        }
    }

    private void PauseGame() {
        SwitchScreens(currentScreenUI, gameplayUIManager.pauseMenuScreen);
        eventSystem.SetSelectedGameObject(gameplayUIManager.resumeGameButton.gameObject);
        OnGamePaused?.Invoke();
        gameState = GameState.Paused;
        Time.timeScale = 0f;
    }

    private void UnpauseGame() {
        SwitchScreens(currentScreenUI, gameplayUIManager.gameplayScreen);
        OnGameResumed?.Invoke();
        gameState = GameState.Playing;
        Time.timeScale = 1f;
    }

    private void CreatePlayerControllers() {
        PlayerInputsList = new List<PlayerInputs>(PlayerConfigurationManager.Instance.PlayerInputsList);

        List<Vector3> SpawnPositionsList = new List<Vector3>();

        int maxCount = PlayerInputsList.Count;
        float angleStep = 360f / maxCount;
        Vector3 centerPos = Vector3.zero;

        for (int i = 0; i < PlayerInputsList.Count; i++) {
            // forward or back for rotation direction. Forward is counter clockwise, Back is clockwise.
            Quaternion rotation = Quaternion.AngleAxis((i * angleStep) + rotationOffset, Vector3.back);
            // left or right for first angle position. Left for leftmost, Right for rightmost
            Vector3 direction = rotation * Vector3.left;
            Vector3 spawnPosition = centerPos + (direction * radiusAroundCenter);

            SpawnPositionsList.Add(spawnPosition);
        }

        int count = 0;

        foreach (PlayerInputs inputs in PlayerInputsList) {
            PlayerConfiguration playerConfig = inputs.playerConfiguration;

            GameObject playerObject = Instantiate(playerPrefab, SpawnPositionsList[count], Quaternion.identity);
            playerObject.name = $"PlayerCharacter_P{inputs.playerId + 1}";

            TeamAssigner teamAssigner = playerObject.GetComponentInHierarchy<TeamAssigner>();
            teamAssigner.team = playerConfig.Team;
            teamAssigner.ReloadTeamConfiguration();

            PlayerController playerController = playerObject.GetComponentInHierarchy<PlayerController>();
            PlayerHealth playerHP = playerObject.GetComponentInHierarchy<PlayerHealth>();
            
            playerConfig.Controller = playerController;
            playerConfig.Health = playerHP;
            playerConfig.TeamAssigner = teamAssigner;

            Vector2 lookDirection = centerPos - playerController.transform.position;

            FloatSO playerHealthSO = ScriptableObject.CreateInstance<FloatSO>();
            FloatSO playerMaxHealthSO = ScriptableObject.CreateInstance<FloatSO>();
            IntSO playerStacksSO = ScriptableObject.CreateInstance<IntSO>();
            playerHealthSO.name = $"{inputs.playerNameTag}_Health";
            playerMaxHealthSO.name = $"{inputs.playerNameTag}_MaxHealth";
            playerStacksSO.name = $"{inputs.playerNameTag}_StacksAmount";
            playerConfig.HealthAsset = playerHealthSO;
            playerConfig.MaxHealthAsset = playerMaxHealthSO;
            playerConfig.StacksAmountAsset = playerStacksSO;
            playerHealthSO.Value = startingHealth;
            playerMaxHealthSO.Value = startingHealth;
            playerStacksSO.Value = maxStacksAmount;

            PlayerControllersList.Add(playerController);

            playerController.Init(inputs, lookDirection);
            playerHP.Init(inputs);
            OnPlayerCreated?.Invoke(inputs);

            count++;
        }

        remainingPlayers = count;
        remainingPlayersThisRound = count;

        OnPlayersCreated?.Invoke(PlayerControllersList);
    }

    private void CheckRemainingPlayersThisRound() {
        remainingPlayersThisRound--;

        if (remainingPlayersThisRound <= 1) {
            currentBall.Destroy();
            int count = 0;

            foreach (PlayerInputs input in PlayerInputsList) {
                if (input.isTerminated && !input.wasChecked) {
                    remainingPlayers--;
                    input.wasChecked = true;
                }
                else count++;
            }

            if (remainingPlayers <= 1) {
                StartCoroutine(PlayerWinScreen(PlayerInputsList.Find(p => !p.isDead)));
            }
            else {
                StartCoroutine(RespawnRemainingPlayers());
            }
        }
    }

    private IEnumerator RespawnRemainingPlayers() {
        yield return secondsForRespawnPlayers;
        currentBall = Instantiate(ballPrefab, levelCenter.transform.position, Quaternion.identity);
        currentBall.name = ballPrefab.name;
        remainingPlayersThisRound = remainingPlayers;
        OnAllPlayersRespawn?.Invoke();
        yield return null;
    }

    private IEnumerator PlayerWinScreen(PlayerInputs inputs) {
        UnbindUIControls();
        yield return secondsForRespawnPlayers;
        SwitchScreens(currentScreenUI, gameplayUIManager.winScreen);
        gameplayUIManager.winText.text = $"{inputs.playerNameTag} has won this match!";
        gameplayUIManager.winText.color = inputs.playerConfiguration.TeamAssigner.currentTeamConfiguration.Color;
        yield return secondsAfterWin;
        ReturnToMainMenu();
        yield return null;
    }

    private void FreezeFrame() {
        if (hasSlowDown.Enabled && hasSlowDown.Value) {
            if (SlowdownCoroutine != null) StopCoroutine(SlowdownCoroutine);
            Time.timeScale = 1f;
            SlowdownCoroutine = StartCoroutine(Slowdown(freezeFrameParameters));
        }
    }

    // private void Slowdown() {
    //     if (hasSlowDown.Enabled && hasSlowDown.Value) {
    //         if (SlowdownCoroutine != null) StopCoroutine(SlowdownCoroutine);
    //         Time.timeScale = 1f;
    //         SlowdownCoroutine = StartCoroutine(Slowdown(slowdownParameters));
    //     }
    // }

    private void PlayerDeathSlowdown() {
        if (hasSlowDown.Enabled && hasSlowDown.Value) {
            if (SlowdownCoroutine != null) StopCoroutine(SlowdownCoroutine);
            Time.timeScale = 1f;
            SlowdownCoroutine = StartCoroutine(Slowdown(deathSlowdownParameters));
        }
    }

    private IEnumerator Slowdown(SlowdownParameters parameters, float defaultTimeScale = 1f) {
        secondsAfterSlowdown = new WaitForSecondsRealtime(parameters.secondsToRestore);

        yield return StartCoroutine(SlowTime(parameters.slowTimeTarget, defaultTimeScale, parameters.targetTimeScale));
        yield return secondsAfterSlowdown;
        yield return StartCoroutine(SpeedTime(parameters.speedTimeTarget, parameters.targetTimeScale, defaultTimeScale));
    }

    private IEnumerator SlowTime(float slowTimeTarget, float defaultTimeScale, float targetTimeScale) {
        float elapsedTime = 0f;
        float percentageComplete = 0f;
        float slowedTimeScale = defaultTimeScale;
        Time.timeScale = slowedTimeScale;

        if (slowTimeTarget > 0f) {
            while (elapsedTime < slowTimeTarget) {
                percentageComplete = elapsedTime / slowTimeTarget;
                slowedTimeScale = Mathf.Lerp(slowedTimeScale, targetTimeScale, percentageComplete);
                Time.timeScale = slowedTimeScale;
                elapsedTime += Time.unscaledDeltaTime;

                if (elapsedTime >= slowTimeTarget) {
                    slowedTimeScale = targetTimeScale;
                    elapsedTime = 0f;
                    Time.timeScale = slowedTimeScale;
                    yield break;
                }

                yield return Utils.waitForEndOfFrame;
            }
        }
        else {
            slowTimeTarget = 0f;
            slowedTimeScale = targetTimeScale;
            Time.timeScale = slowedTimeScale;
            yield return null;
        }
    }

    private IEnumerator SpeedTime(float speedTimeTarget, float defaultTimeScale, float targetTimeScale) {
        float elapsedTime = 0f;
        float percentageComplete = 0f;
        float slowedTimeScale = defaultTimeScale;
        Time.timeScale = slowedTimeScale;
        
        if (speedTimeTarget > 0f) {
            while (elapsedTime < speedTimeTarget) {
                percentageComplete = elapsedTime / speedTimeTarget;
                slowedTimeScale = Mathf.Lerp(slowedTimeScale, targetTimeScale, percentageComplete);
                Time.timeScale = slowedTimeScale;
                elapsedTime += Time.unscaledDeltaTime;

                if (elapsedTime >= speedTimeTarget) {
                    elapsedTime = 0f;
                    slowedTimeScale = targetTimeScale;
                    Time.timeScale = slowedTimeScale;
                    yield break;
                }

                yield return Utils.waitForEndOfFrame;
            }
        }
        else {
            speedTimeTarget = 0f;
            slowedTimeScale = targetTimeScale;
            Time.timeScale = slowedTimeScale;
            yield return null;
        }
    }
}

[Serializable]
public struct SlowdownParameters {
    public float slowTimeTarget;
    public float speedTimeTarget;
    public float targetTimeScale;
    public float secondsToRestore;

    public SlowdownParameters(float _slowTimeTarget, float _speedTimeTarget, float _targetTimeScale, float _secondsToRestore) {
        slowTimeTarget = _slowTimeTarget;
        speedTimeTarget = _speedTimeTarget;
        targetTimeScale = _targetTimeScale;
        secondsToRestore = _secondsToRestore;
    }
}
