using System.Collections.Generic;
using UnityEngine;

// [AIM_TRAJECTORY_ADDED] begin
public class AimTrajectoryVisual : MonoBehaviour
{
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private Camera mainCamera;

    [Header("Dots")]
    [SerializeField] private int maxDotCount = 14;
    [SerializeField] private float startOffset = 0.45f;
    [SerializeField] private float maxDistance = 5.5f;
    [SerializeField] private float heightOffset = 0.15f;
    [SerializeField] private float dotScale = 0.25f;

    private readonly List<Transform> dots = new();

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (dotPrefab == null)
        {
            return;
        }

        for (int i = 0; i < maxDotCount; i++)
        {
            Transform dot = Instantiate(dotPrefab, transform);
            dot.gameObject.SetActive(false);
            dots.Add(dot);
        }
    }

    public void Show(Vector3 origin, Vector3 direction, float power01)
    {
        if (dotPrefab == null || dots.Count == 0)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            Hide();
            return;
        }

        direction.Normalize();
        power01 = Mathf.Clamp01(power01);

        int activeCount = Mathf.RoundToInt(Mathf.Lerp(3, maxDotCount, power01));
        float distance = Mathf.Lerp(1.0f, maxDistance, power01);

        for (int i = 0; i < dots.Count; i++)
        {
            bool active = i < activeCount;
            dots[i].gameObject.SetActive(active);

            if (!active)
            {
                continue;
            }

            float t = (i + 1f) / activeCount;

            Vector3 pos = origin + direction * (startOffset + distance * t);
            pos.y += heightOffset;

            dots[i].position = pos;

            float pulse = 1f + Mathf.Sin(Time.time * 8f + i * 0.6f) * 0.12f;
            float scale = dotScale * Mathf.Lerp(1.2f, 0.6f, t) * pulse;

            dots[i].localScale = Vector3.one * scale;

            if (mainCamera != null)
            {
                dots[i].rotation = mainCamera.transform.rotation;
            }
        }
    }

    public void Hide()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].gameObject.SetActive(false);
        }
    }
}
// [AIM_TRAJECTORY_ADDED] end
