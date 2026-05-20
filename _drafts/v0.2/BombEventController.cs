// ============================================================
// v0.2 폭발물 모듈 (상상코딩 — 참고용)
// ============================================================
// 기획서 6-6 폭발물 알고리즘 기반.
// Physics.OverlapSphere로 주변 알 탐색 → 거리 비례 AddForce.
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class BombEventController : MonoBehaviour, IEnvironmentModule
{
    [Header("--- 설정 (임시) ---")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private LayerMask eggLayer;

    [Header("--- FeatureFlags 참조 (임시) ---")]
    [SerializeField] private bool isEnabled = false;

    public string ModuleName => "BombEvent";
    public bool IsEnabled => isEnabled;

    // ─── IEnvironmentModule ──────────────────────────────────

    public void Enable()
    {
        // TODO: 폭발물 오브젝트 생성 / 배치
        Debug.Log("[Bomb] Module enabled.");
    }

    public void Disable()
    {
        // TODO: 모든 폭발물 제거
        Debug.Log("[Bomb] Module disabled.");
    }

    public void Tick(float deltaTime)
    {
        // TODO: 폭발물 카운트다운 감소
        // TODO: 타이머 0 → Detonate()
    }

    // ─── 핵심 로직 (미구현) ──────────────────────────────────

    private void Detonate(Vector3 center)
    {
        // [기획 6-6]
        // 1. Physics.OverlapSphere(center, explosionRadius, eggLayer)
        // 2. 각 hit collider → GetComponent<EggController>
        // 3. 거리 비례 force 계산
        // 4. egg.Rigidbody.AddForce(direction * force, Impulse)
        // 5. 파티클 / 사운드 / 카메라 셰이크 트리거

        Debug.Log($"[Bomb] Detonate at {center} (radius: {explosionRadius})");
    }

    // ─── 생성 / 배치 (미구현) ────────────────────────────────

    private void SpawnBomb(Vector3 position)
    {
        // TODO: 폭발물 프리팹 Instantiate
        // TODO: 카운트다운 UI 연결
    }
}
