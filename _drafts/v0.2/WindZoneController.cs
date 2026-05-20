// ============================================================
// v0.2 바람 모듈 (상상코딩 — 참고용)
// ============================================================
// 보드 위에 일정 영역에 지속적인 힘을 가한다.
// 알이 바람 영역에 들어가면 추가 AddForce.
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class WindZoneController : MonoBehaviour, IEnvironmentModule
{
    [Header("--- 설정 (임시) ---")]
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0f);
    [SerializeField] private float windStrength = 2f;
    [SerializeField] private Vector3 zoneCenter;
    [SerializeField] private Vector3 zoneSize = new Vector3(3f, 1f, 3f);

    [Header("--- FeatureFlags 참조 (임시) ---")]
    [SerializeField] private bool isEnabled = false;

    public string ModuleName => "WindZone";
    public bool IsEnabled => isEnabled;

    // ─── IEnvironmentModule ──────────────────────────────────

    public void Enable()
    {
        Debug.Log("[Wind] Module enabled.");
    }

    public void Disable()
    {
        Debug.Log("[Wind] Module disabled.");
    }

    public void Tick(float deltaTime)
    {
        // TODO: 
        // 1. Physics.OverlapBox(zoneCenter, zoneSize/2)로 영역 내 알 탐색
        // 2. 각 알에 windDirection * windStrength 만큼 AddForce
        // 3. 바람 시각 효과 (파티클)
    }

    // ─── 기즈모 (시각화) ─────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(zoneCenter, zoneSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(zoneCenter, windDirection * windStrength);
    }
}
