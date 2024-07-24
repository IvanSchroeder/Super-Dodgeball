using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using NaughtyAttributes;

public class CameraController : MonoBehaviour {
    public enum PanMode {
        Lerp,
        MoveTowards,
        SmoothDamp
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private List<PlayerController> PlayerControllersList;
    [SerializeField] private List<Transform> PlayerControllersTransformsList;
    [SerializeField] public Transform cameraTarget;
    [SerializeField] public Transform cameraPosition;
    [SerializeField] private IntSO pixelsPerUnit;

    [Header("Movement parameters")]
    [SerializeField] private bool isStatic;
    [SerializeField] private bool snapToPixelGrid = true;
    [MinValue(1)]
    [SerializeField] private int snapValueMultiplier = 1;
    [SerializeField] private PanMode panMode = PanMode.Lerp;
    [SerializeField] private Vector3 startingPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private Vector3 currentPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float cameraDistanceZ = -10f;
    [SerializeField] private float cameraSpeed;
    [SerializeField] private float smoothTime = 0.5f;

    private Vector3 velocity;

    public Coroutine followCoroutine;
    public Coroutine resetCoroutine;

    private void OnEnable() {
        GameManager.OnPlayersCreated += Initiliaze;
        GameManager.OnReturnToMainMenu += ResetController;

        GameManager.OnAllPlayersRespawn += AddTargets;
        PlayerHealth.OnPlayerDeath += RemoveTargets;

        if (mainCamera == null) mainCamera = this.GetComponentInHierarchy<Camera>();
    }

    private void OnDisable() {
        GameManager.OnPlayersCreated -= Initiliaze;
        GameManager.OnReturnToMainMenu -= ResetController;

        GameManager.OnAllPlayersRespawn -= AddTargets;
        PlayerHealth.OnPlayerDeath -= RemoveTargets;
    }

    private void Initiliaze(List<PlayerController> Players) {
        PlayerControllersList = new List<PlayerController>(Players);

        PlayerControllersTransformsList = new List<Transform>();
        foreach (var controller in PlayerControllersList) {
            PlayerControllersTransformsList.Add(controller.transform);
        }

        if (followCoroutine != null) StopCoroutine(followCoroutine);
        startingPosition = new Vector3(0f, 0f, cameraDistanceZ);
        mainCamera.transform.position = startingPosition;
        followCoroutine = StartCoroutine(FollowTarget(mainCamera, cameraTarget));
    }

    private void FixedUpdate() {
        if (!snapToPixelGrid) return;

        Vector3 snappedPosition = GetSnappedPosition(currentPosition, pixelsPerUnit.Value);
        mainCamera.transform.position = snappedPosition;

        if (cameraPosition != null) cameraPosition.position = GetSnappedPosition(cameraPosition.position, pixelsPerUnit.Value).FlattenZ();
        if (cameraTarget != null) cameraTarget.position = GetSnappedPosition(cameraTarget.position, pixelsPerUnit.Value).FlattenZ();
    }

    private void ResetController() {
        PlayerControllersList = new List<PlayerController>();
        PlayerControllersTransformsList = new List<Transform>();
        targetPosition = targetPosition.Flatten();
        cameraTarget.gameObject.Destroy();
        cameraPosition.gameObject.Destroy();
        cameraTarget = null;
        cameraPosition = null;

        if (followCoroutine != null) StopCoroutine(followCoroutine);
        if (resetCoroutine != null) StopCoroutine(resetCoroutine);
        resetCoroutine = StartCoroutine(ResetCameraPosition(mainCamera, startingPosition));
    }

    private IEnumerator ResetCameraPosition(Camera camera, Vector3 startingPosition) {
        targetPosition = startingPosition;
        Vector3 currentPosition = camera.transform.position;

        while (Vector3.Distance(currentPosition, targetPosition) > 0f) {
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * cameraSpeed);
            camera.transform.position = currentPosition;

            yield return Utils.waitForEndOfFrame;
        }

        camera.transform.position = targetPosition;
    }

    private void AddTargets() {
        PlayerControllersTransformsList = new List<Transform>();

        foreach (var player in PlayerControllersList) {
            if (!(player.playerInputs.isTerminated)) {
                PlayerControllersTransformsList.Add(player.transform);
            }
        }
    }

    private void RemoveTargets() {
        foreach (var player in PlayerControllersList) {
            if (PlayerControllersTransformsList.Contains(player.transform) && player.playerInputs.isDead) {
                PlayerControllersTransformsList.Remove(player.transform);
            }
        }
    }

    private IEnumerator FollowTarget(Camera camera, Transform targetToFollow) {
        targetPosition = new Vector3(targetToFollow.position.x, targetToFollow.position.y, cameraDistanceZ);

        yield return StartCoroutine(ResetCameraPosition(camera, startingPosition));

        while (true) {
            targetToFollow.transform.position = Utils.Average(PlayerControllersTransformsList);

            if (isStatic) targetPosition = new Vector3(0f, 0f, cameraDistanceZ);
            else targetPosition = new Vector3(targetToFollow.position.x, targetToFollow.position.y, cameraDistanceZ);

            currentPosition = camera.transform.position;

            switch (panMode) {
                case PanMode.Lerp:
                    currentPosition = Vector3.Lerp(currentPosition, targetPosition, cameraSpeed * Time.deltaTime);
                break;
                case PanMode.MoveTowards:
                    currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, cameraSpeed * Time.deltaTime);
                break;
                case PanMode.SmoothDamp:
                    currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref velocity, smoothTime * Time.deltaTime);
                break;
            }

            camera.transform.position = currentPosition;
            cameraPosition.position = currentPosition.FlattenZ();

            yield return Utils.waitForEndOfFrame;
        }
    }

    private Vector3 GetSnappedPosition(Vector3 position, float snapPPU) {
        float snapValue = snapPPU * snapValueMultiplier;
        float pixelGridSize = 1f / snapValue;
        // float x = ((position.x * snapValue).Round() / snapValue);
        // float y = ((position.y * snapValue).Round() / snapValue);
        float x = ((position.x / pixelGridSize).Round() * pixelGridSize);
        float y = ((position.y / pixelGridSize).Round() * pixelGridSize);
        return new Vector3(x, y, position.z);
    }
}
