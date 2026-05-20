// ============================================================
// v0.2 환경 모듈 인터페이스 (상상코딩 — 참고용)
// ============================================================
// 모든 환경 모듈은 이 인터페이스를 구현한다.
// 꺼도 Basic Core는 정상 작동해야 함.
// ============================================================

public interface IEnvironmentModule
{
    /// <summary>모듈 이름 (디버그용)</summary>
    string ModuleName { get; }

    /// <summary>FeatureFlags에서 이 모듈이 켜져 있는지</summary>
    bool IsEnabled { get; }

    /// <summary>모듈 초기화 (Enable 시 호출)</summary>
    void Enable();

    /// <summary>모듈 정리 (Disable 시 호출)</summary>
    void Disable();

    /// <summary>매 프레임 업데이트 (IsEnabled=true일 때만 호출됨)</summary>
    void Tick(float deltaTime);
}
