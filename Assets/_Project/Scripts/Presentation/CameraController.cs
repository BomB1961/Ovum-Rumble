using System;
using System.Collections;
using UnityEngine;
using DinoAlkkagi.Core;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CameraMode
{
    Free,
    TopDown,
    Transitioning
}

[Serializable]
public struct PlayerCameraState
{
    public Vector3 pivot;
    public float yaw;
    public float pitch;
    public float distance;
}

public class CameraController : MonoBehaviour
{
    [Header("Camera Ref")]
    [SerializeField] private Camera inputCamera;

    [Header("Free Mode")]
    [SerializeField] private Vector3 pivotPoint = Vector3.zero;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float orbitSensitivity = 200f;
    [SerializeField] private float zoomSensitivity = 5f;
    [SerializeField] private LayerMask orbitPivotLayerMask = 384;
    [SerializeField] private float pitchMin = -89f;
    [SerializeField] private float pitchMax = 89f;

    [Header("TopDown Mode")]
    [SerializeField] private float topDownDistance = 15f;
    [SerializeField] private Vector3 topDownPivot = Vector3.zero;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Shake (Cinemachine Impulse)")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float minImpactForShake = 2f;
    [SerializeField] private float maxImpactForShake = 15f;
    [SerializeField] private float minShakeVelocity = 0.1f;
    [SerializeField] private float maxShakeVelocity = 1.0f;

    private const float TopDownPitch = 90f;
    private const float TopDownYaw = 0f;

    private float yaw;
    private float pitch;
    private Vector2 lastPanScreenPos;
    private bool panJustStarted;
    private bool isOrbiting;
    private bool isPanning;

    private CameraMode cameraMode = CameraMode.Free;
    private readonly PlayerCameraState[] savedStates = new PlayerCameraState[3];
    private int currentViewerId;

    private void Awake()
    {
        if (inputCamera == null)
            inputCamera = Camera.main;

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        Vector3 offset = inputCamera.transform.position - pivotPoint;
        float initDist = offset.magnitude;
        Vector3 initDir = offset.normalized;
        float initPitch = Mathf.Asin(initDir.y) * Mathf.Rad2Deg;
        float initYaw = Mathf.Atan2(initDir.x, initDir.z) * Mathf.Rad2Deg;

        var defaultState = new PlayerCameraState
        {
            pivot = pivotPoint,
            yaw = initYaw,
            pitch = initPitch,
            distance = initDist
        };

        for (int i = 1; i <= 2; i++)
            savedStates[i] = defaultState;
    }

    private void OnEnable()
    {
        GameEvents.OnTurnStarted += HandleTurnStarted;
        GameEvents.OnEggCollision += HandleEggCollision;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnStarted -= HandleTurnStarted;
        GameEvents.OnEggCollision -= HandleEggCollision;
    }

    private void Start()
    {
        Vector3 offset = inputCamera.transform.position - pivotPoint;
        distance = offset.magnitude;
        Vector3 direction = offset.normalized;
        pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        if (DebugToggleTopDownPressed())
        {
            SaveCurrentState(currentViewerId);
            StopAllCoroutines();
            StartCoroutine(TransitionToTopDownCoroutine());
            return;
        }

        if (DebugToggleTopDownReleased())
        {
            StopAllCoroutines();
            StartCoroutine(TransitionToFreeCoroutine(currentViewerId));
            return;
        }

        if (DebugToggleTopDownHeld())
            return;

        if (cameraMode == CameraMode.Free)
        {
            HandleOrbitInput();
            HandlePanInput();
            HandleDollyInput();
        }
    }

    private void LateUpdate()
    {
        ApplyCameraTransform();
    }

    private void HandleTurnStarted(int playerId)
    {
        if (currentViewerId > 0)
            SaveCurrentState(currentViewerId);

        currentViewerId = playerId;
        StopAllCoroutines();
        StartCoroutine(TransitionToFreeCoroutine(playerId));
    }

    private void HandleEggCollision(float impact)
    {
        if (impulseSource == null) return;
        if (impact < minImpactForShake) return;

        float t = Mathf.Clamp01((impact - minImpactForShake) / (maxImpactForShake - minImpactForShake));
        float velocity = Mathf.Lerp(minShakeVelocity, maxShakeVelocity, t);

        impulseSource.GenerateImpulseWithVelocity(UnityEngine.Random.insideUnitSphere.normalized * velocity);
    }

    private void SaveCurrentState(int playerId)
    {
        if (playerId <= 0) return;

        savedStates[playerId] = new PlayerCameraState
        {
            pivot = pivotPoint,
            yaw = yaw,
            pitch = pitch,
            distance = distance
        };
    }

    private IEnumerator TransitionToTopDownCoroutine()
    {
        cameraMode = CameraMode.Transitioning;

        Vector3 startPivot = pivotPoint;
        float startYaw = yaw;
        float startPitch = pitch;
        float startDist = distance;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            pivotPoint = Vector3.Lerp(startPivot, topDownPivot, t);
            yaw = Mathf.LerpAngle(startYaw, TopDownYaw, t);
            pitch = Mathf.LerpAngle(startPitch, TopDownPitch, t);
            distance = Mathf.Lerp(startDist, topDownDistance, t);

            yield return null;
        }

        pivotPoint = topDownPivot;
        yaw = TopDownYaw;
        pitch = TopDownPitch;
        distance = topDownDistance;

        cameraMode = CameraMode.TopDown;
    }

    private IEnumerator TransitionToFreeCoroutine(int playerId)
    {
        cameraMode = CameraMode.Transitioning;
        PlayerCameraState target = savedStates[playerId];

        Vector3 startPivot = pivotPoint;
        float startYaw = yaw;
        float startPitch = pitch;
        float startDist = distance;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            pivotPoint = Vector3.Lerp(startPivot, target.pivot, t);
            yaw = Mathf.LerpAngle(startYaw, target.yaw, t);
            pitch = Mathf.LerpAngle(startPitch, target.pitch, t);
            distance = Mathf.Lerp(startDist, target.distance, t);

            yield return null;
        }

        pivotPoint = target.pivot;
        yaw = target.yaw;
        pitch = target.pitch;
        distance = target.distance;

        cameraMode = CameraMode.Free;
    }

    private void HandleOrbitInput()
    {
        bool rightHeld = IsSecondaryButtonHeld();

        if (WasSecondaryButtonPressedThisFrame())
        {
            Ray ray = inputCamera.ScreenPointToRay(GetPointerPosition());
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, orbitPivotLayerMask, QueryTriggerInteraction.Ignore))
            {
                isOrbiting = true;
                pivotPoint = hit.point;
            }
        }

        if (!rightHeld)
            isOrbiting = false;

        if (isOrbiting)
        {
            Vector2 mouseDelta = GetMouseDelta();
            yaw += mouseDelta.x * orbitSensitivity * 0.01f;
            pitch -= mouseDelta.y * orbitSensitivity * 0.01f;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }
    }

    private void HandlePanInput()
    {
        if (WasMiddleButtonPressedThisFrame())
        {
            isPanning = true;
            panJustStarted = true;
        }

        if (WasMiddleButtonReleasedThisFrame())
            isPanning = false;

        if (isPanning)
        {
            Vector2 currentScreenPos = GetPointerPosition();

            if (panJustStarted)
            {
                panJustStarted = false;
                lastPanScreenPos = currentScreenPos;
                return;
            }

            Vector2 screenDelta = currentScreenPos - lastPanScreenPos;
            lastPanScreenPos = currentScreenPos;

            if (screenDelta.sqrMagnitude < 0.001f) return;

            Vector3 pivotScreenBefore = inputCamera.WorldToScreenPoint(pivotPoint);
            Vector3 pivotScreenAfter = pivotScreenBefore + new Vector3(screenDelta.x, screenDelta.y, 0f);

            Ray rayBefore = inputCamera.ScreenPointToRay(pivotScreenBefore);
            Ray rayAfter = inputCamera.ScreenPointToRay(pivotScreenAfter);

            Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
            if (boardPlane.Raycast(rayBefore, out float enterBefore) && boardPlane.Raycast(rayAfter, out float enterAfter))
            {
                Vector3 worldBefore = rayBefore.GetPoint(enterBefore);
                Vector3 worldAfter = rayAfter.GetPoint(enterAfter);
                pivotPoint += worldBefore - worldAfter;
            }
        }
    }

    private void HandleDollyInput()
    {
        float scroll = GetScrollDelta();
        if (Mathf.Approximately(scroll, 0f)) return;

        distance -= scroll * zoomSensitivity;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void ApplyCameraTransform()
    {
        float pitchRad = pitch * Mathf.Deg2Rad;
        float yawRad = yaw * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            -Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );

        inputCamera.transform.position = pivotPoint + direction * distance;
        inputCamera.transform.LookAt(pivotPoint);
    }

    private bool WasSecondaryButtonPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    private bool WasSecondaryButtonReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(1);
#endif
    }

    private bool IsSecondaryButtonHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private bool WasMiddleButtonPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(2);
#endif
    }

    private bool WasMiddleButtonReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(2);
#endif
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool DebugToggleTopDownPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    private bool DebugToggleTopDownReleased()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.T);
#endif
    }

    private bool DebugToggleTopDownHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.isPressed;
#else
        return Input.GetKey(KeyCode.T);
#endif
    }

    private float GetScrollDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return 0f;
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        return scroll.y;
#else
        return Input.GetAxis("Mouse ScrollWheel");
#endif
    }
}
