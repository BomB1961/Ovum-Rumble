using System;
using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Environment;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlickInputController : MonoBehaviour
{
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask eggLayerMask = ~0;
    [SerializeField] private float minForce = 1.5f;
    [SerializeField] private float maxForce = 12f;
    [SerializeField] private float forceMultiplier = 8f;
    [SerializeField] private float maxDragDistance = 2f;
    [SerializeField] private int activePlayerId = 1;
    [SerializeField] private bool inputEnabled = true;
    [SerializeField] private bool useNetworkRelay;

    private EggController selectedEgg;
    private Vector3 dragStartWorld;
    private bool isDragging;
    private IBoardSurface boardSurface;

    public event Action<EggController> EggSelected;
    public event Action<EggController> EggLaunched;

    public bool UseNetworkRelay
    {
        get => useNetworkRelay;
        set => useNetworkRelay = value;
    }

    public void SetBoardSurface(IBoardSurface surface)
    {
        boardSurface = surface;
    }

    private void Awake()
    {
        if (inputCamera == null)
        {
            inputCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            return;
        }

        if (WasPrimaryButtonPressedThisFrame())
        {
            TryBeginDrag();
        }

        if (WasPrimaryButtonReleasedThisFrame())
        {
            TryLaunchSelectedEgg();
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            ClearSelection();
        }
    }

    public void SetActivePlayer(int playerId)
    {
        activePlayerId = playerId;
        ClearSelection();
    }

    public void ClearSelection()
    {
        selectedEgg = null;
        isDragging = false;
        dragStartWorld = Vector3.zero;
    }

    private void TryBeginDrag()
    {
        if (inputCamera == null)
        {
            Debug.LogError($"{nameof(FlickInputController)} requires an input camera.", this);
            return;
        }

        Ray ray = inputCamera.ScreenPointToRay(GetPointerPosition());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, eggLayerMask))
        {
            ClearSelection();
            return;
        }

        EggController egg = hit.collider.GetComponentInParent<EggController>();
        if (!CanSelect(egg))
        {
            ClearSelection();
            return;
        }

        selectedEgg = egg;
        dragStartWorld = GetBoardPoint(ray, hit.point);
        isDragging = true;
        EggSelected?.Invoke(selectedEgg);
    }

    private void TryLaunchSelectedEgg()
    {
        if (!isDragging || selectedEgg == null)
        {
            ClearSelection();
            return;
        }

        if (inputCamera == null)
        {
            ClearSelection();
            return;
        }

        if (!CanSelect(selectedEgg))
        {
            ClearSelection();
            return;
        }

        Ray ray = inputCamera.ScreenPointToRay(GetPointerPosition());
        Vector3 dragEndWorld = GetBoardPoint(ray, dragStartWorld);
        Vector3 dragVector = dragStartWorld - dragEndWorld;
        dragVector.y = 0f;

        float dragDistance = Mathf.Min(dragVector.magnitude, maxDragDistance);
        if (dragDistance <= Mathf.Epsilon)
        {
            ClearSelection();
            return;
        }

        Vector3 direction = dragVector.normalized;
        float force = Mathf.Clamp(dragDistance * forceMultiplier, minForce, maxForce);

        if (useNetworkRelay)
        {
            uint eggNetId = (uint)selectedEgg.NetworkEggId;
            NetworkInputRelay relay = NetworkInputRelay.Instance;
            if (relay != null)
            {
                relay.SendLaunchInput(eggNetId, direction, force);
            }

            // 서버가 처리할 때까지 self-lock: 재발사 방지
            // HandleOnTurnStarted에서 서버의 다음 턴 신호를 받으면 재개됨
            inputEnabled = false;
        }
        else
        {
            selectedEgg.Launch(direction * force);
        }

        EggLaunched?.Invoke(selectedEgg);
        ClearSelection();
    }

    private bool CanSelect(EggController egg)
    {
        return egg != null
            && egg.IsAlive
            && egg.CanLaunch
            && egg.OwnerPlayerId == activePlayerId;
    }

    private Vector3 GetBoardPoint(Ray ray, Vector3 fallback)
    {
        if (boardSurface != null)
        {
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float planeEnter))
            {
                Vector3 planePoint = ray.GetPoint(planeEnter);
                float height = boardSurface.GetHeight(planePoint);
                return new Vector3(planePoint.x, height, planePoint.z);
            }
            return fallback;
        }

        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
        if (boardPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return fallback;
    }

    private bool WasPrimaryButtonPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool WasPrimaryButtonReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
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
}
