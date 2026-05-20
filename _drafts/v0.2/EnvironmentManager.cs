// ============================================================
// v0.2 환경 모듈 매니저 (상상코딩 — 참고용)
// ============================================================
// FeatureFlags를 읽어서 각 환경 모듈을 Enable/Disable.
// Basic Core 위에 붙는 레이어.
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using DinoAlkkagi.Data;

public class EnvironmentManager : MonoBehaviour
{
    [Header("--- FeatureFlags 참조 ---")]
    [SerializeField] private FeatureFlags featureFlags;

    private List<IEnvironmentModule> modules = new List<IEnvironmentModule>();

    private void Start()
    {
        if (featureFlags == null)
        {
            featureFlags = FindFirstObjectByType<FeatureFlags>();
        }

        // 모든 IEnvironmentModule 수집
        GetComponentsInChildren(modules);

        // FeatureFlags 기반 Enable/Disable
        foreach (var module in modules)
        {
            bool shouldEnable = GetFlagState(module.ModuleName);
            if (shouldEnable) module.Enable();
            else module.Disable();
        }

        Debug.Log($"[EnvironmentManager] {modules.Count} modules registered.");
    }

    private void Update()
    {
        if (featureFlags == null) return;

        foreach (var module in modules)
        {
            if (module.IsEnabled)
            {
                module.Tick(Time.deltaTime);
            }
        }
    }

    private bool GetFlagState(string moduleName)
    {
        // moduleName → FeatureFlags 필드 매핑 (임시)
        switch (moduleName)
        {
            case "BombEvent":    return featureFlags.enableBomb;
            case "WindZone":     return featureFlags.enableWind;
            case "Earthquake":   return featureFlags.enableEarthquake;
            case "DinosaurNpc":  return featureFlags.enableDinosaurNpc;
            default: return false;
        }
    }

    // ─── 수동 제어 (디버그 / 개발용) ─────────────────────────

    [ContextMenu("Enable All Modules")]
    private void DebugEnableAll()
    {
        foreach (var module in modules) module.Enable();
    }

    [ContextMenu("Disable All Modules")]
    private void DebugDisableAll()
    {
        foreach (var module in modules) module.Disable();
    }
}
