using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.InputSystem.Users;

public enum ControllerType {
    PC,
    PlayStation,
    Xbox
}

public enum PlayerState {
    Idle,
    Moving,
    Dodging,
    ChargingSwing,
    Swinging,
    Hurt,
    Dead,
    Jumping
}

public enum RollType {
    TowardsMovement,
    TowardsAim
}

[CreateAssetMenu(fileName = "NewPlayerInputs", menuName = "Assets/Player Inputs/Player Inputs")]
public class PlayerInputs : ScriptableObject {
    [Header("--- References ---")]
    public PlayerInput playerInputComponent;
    public PlayerInputActions playerControls;
    public InputActionAsset inputActionAsset;
    // [HideInInspector] public InputActionMap gameplay;
    // [HideInInspector] public InputActionMap ui;

    [Header("--- Player Info ---")]
    public string playerNameTag;
    public int playerId;
    public InputUser user;
    public InputDevice device;
    public string currentControllerScheme;
    public ControllerType controllerType;
    public RollType rollType;
    public GameObject playerConfigurationObject;
    public Vector2 MoveDirection = Vector3.zero;
    public Vector2 LastMoveDirection = Vector3.right;
    public Vector2 MousePositionInput = Vector3.zero;
    public Vector2 AimDirection;
    public Vector2 LastAimDirection;
    public bool isReady;
    public bool isDead;
    public bool isTerminated;
    public bool wasChecked = false;
    public PlayerConfiguration playerConfiguration = new PlayerConfiguration();
    public PlayerState playerState = PlayerState.Idle;

    public int currentAnimationState;
    public readonly int Idle = Animator.StringToHash("Idle");
    public readonly int Move = Animator.StringToHash("Movement");
    public readonly int ChargeStart = Animator.StringToHash("ChargeStart");
    public readonly int ChargeLoop = Animator.StringToHash("ChargeLoop");
    public readonly int Attack = Animator.StringToHash("Attack");
    public readonly int Hurt = Animator.StringToHash("Hurt");
    public readonly int Death = Animator.StringToHash("Death");

    [Header("--- Actions ---")]
    [Header("Gameplay Actions")]
    [HideInInspector] public InputAction move;
    [HideInInspector] public InputAction aim;
    [HideInInspector] public InputAction swing;
    [HideInInspector] public InputAction bunt;
    [HideInInspector] public InputAction dodge;
    [HideInInspector] public InputAction jump;
    [HideInInspector] public InputAction rollTypeToggle;

    [Header("UI Actions")]
    [HideInInspector] public InputAction moveUI;
    [HideInInspector] public InputAction submitUI;
    [HideInInspector] public InputAction cancelUI;
    [HideInInspector] public InputAction joinUI;
    [HideInInspector] public InputAction pauseUI;

    public void Init(PlayerInputActions controls, PlayerInput inputComponent, string name, int id, InputDevice device, string controllerScheme,
    ControllerType controllerType, GameObject configuration) {
        playerInputComponent = inputComponent;
        inputActionAsset = inputComponent.actions;
        user = playerInputComponent.user;
        playerNameTag = name;
        playerId = id;
        this.device = device;
        currentControllerScheme = controllerScheme;
        this.controllerType = controllerType;
        playerConfigurationObject = configuration;

        //FindActions();
        PlayerInputActions inputsForThisUser = new PlayerInputActions();
        playerControls = inputsForThisUser;
        playerControls.Enable();
        user.AssociateActionsWithUser(playerControls);

        GetActions();
    }

    // public void FindActions() {
    //     gameplay = inputActionAsset.FindActionMap("Gameplay");
    //     ui = inputActionAsset.FindActionMap("UI");
    //     //gameplay = playerControls.asset.FindActionMap("Gameplay");
    //     //ui = playerControls.asset.FindActionMap("UI");

    //     move = gameplay.FindAction("Movement");
    //     aim = gameplay.FindAction("Aim");
    //     swing = gameplay.FindAction("Swing");
    //     bunt = gameplay.FindAction("Bunt");
    //     dodge = gameplay.FindAction("Dodge");
    //     jump = gameplay.FindAction("Jump");
    //     toggle = gameplay.FindAction("ActivateHelpers");

    //     moveUI = ui.FindAction("Move");
    //     submitUI = ui.FindAction("Select");
    //     cancelUI = ui.FindAction("Cancel");
    //     joinUI = ui.FindAction("Join");
    //     pauseUI = ui.FindAction("Pause");
    // }

    public void GetActions() {
        move = playerControls.Gameplay.Movement;
        aim = playerControls.Gameplay.Aim;
        swing = playerControls.Gameplay.Swing;
        bunt = playerControls.Gameplay.Bunt;
        dodge = playerControls.Gameplay.Dodge;
        jump = playerControls.Gameplay.Jump;
        rollTypeToggle = playerControls.Gameplay.ActivateHelpers;

        moveUI = playerControls.UI.Move;
        submitUI = playerControls.UI.Submit;
        cancelUI = playerControls.UI.Cancel;
        joinUI = playerControls.UI.Join;
        pauseUI = playerControls.UI.Pause;
    }

    public void EnableGameplayActions() {
        move.Enable();
        aim.Enable();
        swing.Enable();
        bunt.Enable();
        dodge.Enable();
        jump.Enable();
        rollTypeToggle.Enable();
    }

    public void DisableGameplayActions() {
        move.Disable();
        aim.Disable();
        swing.Disable();
        bunt.Disable();
        dodge.Disable();
        jump.Disable();
        rollTypeToggle.Disable();
    }

    public void EnableUIActions() {
        moveUI.Enable();
        submitUI.Enable();
        cancelUI.Enable();
        joinUI.Enable();
        pauseUI.Enable();
    }

    public void DisableUIActions() {
        moveUI.Disable();
        submitUI.Disable();
        cancelUI.Disable();
        joinUI.Disable();
        pauseUI.Disable();
    }
}

[Serializable]
public class PlayerConfiguration {
    // [SerializeField] private PlayerInputs playerInputs;
    // [SerializeField] private int playerIndex;
    // [SerializeField] private ControllerType controllerType;
    // [SerializeField] private bool playerIsReady;
    [SerializeField] private Team playerTeam;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TeamAssigner playerTeamAssigner;
    [SerializeField] private FloatSO playerHealthAsset;
    [SerializeField] private FloatSO playerMaxHealthAsset;
    [SerializeField] private IntSO playerStacksAmountAsset;
    [SerializeField] private ShaderPropertyIDs colorSwapShaderProperties;
    [SerializeField] private Material colorSwapMaterial;
    [SerializeField] private Material progressBarMaterial;
    [SerializeField] private List<Material> MaterialsList;
    [SerializeField] private List<Material> ActiveMaterialsList;
    [SerializeField] private List<Material> DisableMaterialsList;

    public PlayerConfiguration() {
    }

    // public PlayerInputs Inputs { get => playerInputs; set { playerInputs = value; } }
    // public int PlayerIndex { get => playerIndex; set { playerIndex = value; } }
    // public ControllerType ControllerType { get => controllerType; set { controllerType = value; } }
    // public bool IsReady { get => playerIsReady; set { playerIsReady = value; } }
    public Team Team { get => playerTeam; set { playerTeam = value; } }
    public PlayerController Controller { get => playerController; set { playerController = value; } }
    public PlayerHealth Health { get => playerHealth; set { playerHealth = value; } }
    public TeamAssigner TeamAssigner { get => playerTeamAssigner; set { playerTeamAssigner = value; } }
    public FloatSO HealthAsset { get => playerHealthAsset; set { playerHealthAsset = value; } }
    public FloatSO MaxHealthAsset { get => playerMaxHealthAsset; set { playerMaxHealthAsset = value; } }
    public IntSO StacksAmountAsset { get => playerStacksAmountAsset; set { playerStacksAmountAsset = value; } }
    public ShaderPropertyIDs ColorSwapShaderProperties { get => colorSwapShaderProperties; set { colorSwapShaderProperties = value; } }
    public Material ColorSwapMaterial { get => colorSwapMaterial; set { colorSwapMaterial = value; } }
}
