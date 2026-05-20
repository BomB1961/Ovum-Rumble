// ============================================================
// v0.2 지진 모듈 (상상코딩 — 참고용)
// ============================================================
// 일정 시간마다 보드 전체에 진동 → 모든 알에 무작위 힘.
// 카메라 셰이크 + 사운드 연동.
// ============================================================

using UnityEngine;

public class EarthquakeController : MonoBehaviour, IEnvironmentModule
{
    [Header("--- 설정 (임시) ---")]
    [SerializeField] private float interval = 15f;
    [SerializeField] private float shakeForceMin = 1f;
    [SerializeField] private float shakeForceMax = 5f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("--- FeatureFlags 참조 (임시) ---")]
    [SerializeField] private bool isEnabled = false;

    private float timer = 0f;

    public string ModuleName => "Earthquake";
    public bool IsEnabled => isEnabled;

    // ─── IEnvironmentModule ──────────────────────────────────

    public void Enable()
    {
        timer = interval;
        Debug.Log("[Earthquake] Module enabled.");
    }

    public void Disable()
    {
        Debug.Log("[Earthquake] Module disabled.");
    }

    public void Tick(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0f)
        {
            TriggerEarthquake();
            timer = interval;
        }
    }

    // ─── 핵심 로직 (미구현) ──────────────────────────────────

    private void TriggerEarthquake()
    {
        // TODO:
        // 1. 모든 활성 알 탐색 (EggController.FindObjectsByType?)
        // 2. 각 알에 무작위 방향 * Random.Range(shakeForceMin, shakeForceMax) AddForce
        // 3. 카메라 셰이크 트리거
        // 4. 지진 사운드 재생

        Debug.Log("[Earthquake] Shake!");
    }
}
