using System;
using System.Collections;
using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Environment;
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

    [Header("Pan Limit")]
    [SerializeField] private float panLimitRadius = 30f;
    [SerializeField] private float panLimitSoftMargin = 10f;
    [SerializeField] private float panReturnSmoothTime = 0.15f;

    [Header("Shake")]
    [SerializeField] private float minImpactForShake = 2f;
    [SerializeField] private float maxImpactForShake = 15f;
    [SerializeField] private float minShakeIntensity = 0.05f;
    [SerializeField] private float maxShakeIntensity = 0.4f;
    [SerializeField] private float shakeDuration = 0.3f;

    private const float TopDownPitch = 89f;
    private const float TopDownYaw = 0f;
    private static readonly Plane BoardPlane = new Plane(Vector3.up, Vector3.zero);

    private float yaw;
    private float pitch;
    private Vector2 lastPanScreenPos;
    private bool panJustStarted;
    private bool isOrbiting;
    private bool isPanning;

    private CameraMode cameraMode = CameraMode.Free;
    private readonly PlayerCameraState[] savedStates = new PlayerCameraState[3];
    private int currentViewerId;

    private Vector2 shakeOffset;
    private float shakeTimer;
    private float currentShakeIntensity;
    private IBoardSurface boardSurface;
    private Vector3 boardCenter;
    private Vector3 panReturnVelocity;

    public void SetBoardSurface(IBoardSurface surface)
    {
        boardSurface = surface;
        ApplyBoardCameraBounds();
    }

    private void Awake()
    {
        if (inputCamera == null)
            inputCamera = Camera.main;

        InitializeFromCurrentCamera();
        ApplyBoardCameraBounds();
        SaveStateForAllPlayers();
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
        ClampCameraState();
        SaveStateForAllPlayers();
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
        UpdateShake();
        ApplyCameraTransform();
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / shakeDuration);
            shakeOffset = UnityEngine.Random.insideUnitCircle * currentShakeIntensity * decay;
        }
        else
        {
            shakeOffset = Vector2.zero;
        }
    }

    private void HandleTurnStarted(int playerId)
    {
        // Host: TurnController.AdvanceTurn() + OnClientTurnChange 중복 방지
        if (playerId == currentViewerId) return;

        if (currentViewerId > 0)
            SaveCurrentState(currentViewerId);

        currentViewerId = playerId;
        StopAllCoroutines();
        StartCoroutine(TransitionToFreeCoroutine(playerId));
    }

    private void HandleEggCollision(float impact)
    {
        if (impact < minImpactForShake) return;

        float t = Mathf.Clamp01((impact - minImpactForShake) / (maxImpactForShake - minImpactForShake));
        currentShakeIntensity = Mathf.Lerp(minShakeIntensity, maxShakeIntensity, t);
        shakeTimer = shakeDuration;
    }

    private void SaveCurrentState(int playerId)
    {
        if (!IsValidPlayerId(playerId)) return;

        savedStates[playerId] = new PlayerCameraState
        {
            pivot = ConstrainPanPivot(pivotPoint, panLimitRadius),
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
        PlayerCameraState target = IsValidPlayerId(playerId) ? savedStates[playerId] : GetCurrentState();

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
            panReturnVelocity = Vector3.zero;
        }

        if (WasMiddleButtonReleasedThisFrame())
            isPanning = false;

        if (!isPanning)
        {
            ReturnPanPivotToBounds();
            return;
        }

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

            if (BoardPlane.Raycast(rayBefore, out float enterBefore) && BoardPlane.Raycast(rayAfter, out float enterAfter))
            {
                Vector3 worldBefore = rayBefore.GetPoint(enterBefore);
                Vector3 worldAfter = rayAfter.GetPoint(enterAfter);
                Vector3 delta = worldBefore - worldAfter;

                Vector3 nextPivot = pivotPoint + delta;
                float xzDist = HorizontalDistance(nextPivot, boardCenter);
                float maxElasticRadius = panLimitRadius + Mathf.Max(0f, panLimitSoftMargin);

                if (xzDist > panLimitRadius && panLimitSoftMargin > 0f)
                {
                    float t = Mathf.Clamp01((xzDist - panLimitRadius) / panLimitSoftMargin);
                    delta *= Mathf.Lerp(1f, 0.1f, t);
                }

                pivotPoint += delta;
                pivotPoint = ConstrainPanPivot(pivotPoint, maxElasticRadius);
            }
        }
    }

    private void ReturnPanPivotToBounds()
    {
        if (HorizontalDistance(pivotPoint, boardCenter) <= panLimitRadius)
        {
            panReturnVelocity = Vector3.zero;
            return;
        }

        Vector3 targetPivot = ConstrainPanPivot(pivotPoint, panLimitRadius);
        pivotPoint = Vector3.SmoothDamp(
            pivotPoint,
            targetPivot,
            ref panReturnVelocity,
            Mathf.Max(0.01f, panReturnSmoothTime));

        if ((pivotPoint - targetPivot).sqrMagnitude < 0.0001f)
        {
            pivotPoint = targetPivot;
            panReturnVelocity = Vector3.zero;
        }
    }

    private void HandleDollyInput()
    {
        float scroll = GetScrollDelta();
        if (Mathf.Approximately(scroll, 0f)) return;

        distance -= scroll * zoomSensitivity;
        ClampCameraState();
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

        Vector3 stablePosition = pivotPoint + direction * distance;
        Vector3 lookDirection = pivotPoint - stablePosition;
        if (lookDirection.sqrMagnitude < 0.0001f)
            lookDirection = Vector3.down;

        Quaternion stableRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        Vector3 positionShakeOffset = stableRotation * new Vector3(shakeOffset.x, shakeOffset.y, 0f);

        inputCamera.transform.SetPositionAndRotation(
            stablePosition + positionShakeOffset,
            stableRotation);
    }

    private void InitializeFromCurrentCamera()
    {
        Vector3 offset = inputCamera.transform.position - pivotPoint;
        if (offset.sqrMagnitude < 0.0001f)
        {
            yaw = TopDownYaw;
            pitch = pitchMax;
        }
        else
        {
            Vector3 direction = offset.normalized;
            pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            yaw = Mathf.Atan2(direction.x, -direction.z) * Mathf.Rad2Deg;
            distance = offset.magnitude;
        }

        ClampCameraState();
    }

    private void ApplyBoardCameraBounds()
    {
        if (boardSurface == null) return;

        Bounds bounds = boardSurface.GetCameraBounds();
        boardCenter = bounds.center;
        pivotPoint = bounds.center;

        float fitDistance = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.5f;
        distance = Mathf.Clamp(fitDistance, minDistance, maxDistance);
        topDownPivot = bounds.center;
        topDownDistance = Mathf.Max(topDownDistance, fitDistance);

        SaveStateForAllPlayers();
    }

    private void ClampCameraState()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        pivotPoint = ConstrainPanPivot(pivotPoint, panLimitRadius);
    }

    private PlayerCameraState GetCurrentState()
    {
        return new PlayerCameraState
        {
            pivot = pivotPoint,
            yaw = yaw,
            pitch = pitch,
            distance = distance
        };
    }

    private void SaveStateForAllPlayers()
    {
        PlayerCameraState state = GetCurrentState();
        for (int i = 1; i < savedStates.Length; i++)
            savedStates[i] = state;
    }

    private bool IsValidPlayerId(int playerId)
    {
        return playerId > 0 && playerId < savedStates.Length;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private Vector3 ConstrainPanPivot(Vector3 pivot, float radius)
    {
        if (HorizontalDistance(pivot, boardCenter) <= radius)
            return pivot;

        float originalY = pivot.y;
        Vector3 offset = pivot - boardCenter;
        offset.y = 0f;
        pivot = boardCenter + offset.normalized * radius;
        pivot.y = originalY;
        return pivot;
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
