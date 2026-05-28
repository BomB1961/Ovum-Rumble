## Context

현재 턴제 알까기 게임은 `MotionResolver.EndResolve()` → `OnAllEggsStopped` → `TurnController.AdvanceTurn()` → `OnTurnStarted`의 단방향 흐름으로 턴이 전환된다. 턴 사이에 아무런 이벤트가 없어 플레이어는 항상 다음 턴을 예측 가능하다. `FeatureFlags.enableDinosaurNpc` 필드만 존재할 뿐 실제 구현은 전무하다.

기존 유사 패턴으로 `BombEventController`가 턴 시작 시 폭탄을 스폰하지만, 이는 턴 안에서 발생하며 턴 흐름 자체를 차단하지 않는다.

## Goals / Non-Goals

**Goals:**
- 턴 사이 페이즈(`OnBetweenTurns` / `OnBetweenTurnsEnded`)를 만들어 확장 가능한 게이트 제공
- 20% 확률로 공룡이 랜덤 스폰되어 보드를 직선으로 가로지름
- 공룡과 알 사이의 자연스러운 Rigidbody 물리 충돌
- T-Rex와 Velociraptor가 충돌 시 T-Rex가 Velociraptor를 삭제 (크기 차이 반영)
- `FeatureFlags.enableDinosaurNpc`로 On/Off 제어
- Feature OFF 시에도 게이트 페이즈가 정상 동작 (패스스루)

**Non-Goals:**
- 애니메이션 연출 고도화 (파티클, 사운드 등 — 추후 작업)
- 공룡 충돌 시 squash/stretch 애니메이션
- 경로 다양화 (직선 외 커브/랜덤 경로)
- 2마리 이상 동시 출현

## Decisions

### 1. 이벤트 흐름: Option C — BetweenTurns 게이트

**채택**: `OnBetweenTurns` / `OnBetweenTurnsEnded` 이벤트를 GameEvents에 추가.

```
OnAllEggsStopped
  → DinosaurEventController → StartCoroutine → yield return null (1프레임)
  → TriggerBetweenTurns()
  → 20% 랜덤 → 공룡 or 패스
  → TriggerBetweenTurnsEnded()
  → TurnController.AdvanceTurn()
```

**대안 검토**:
- Option A (TurnController가 직접 DinosaurEventController 호출): 결합도 증가, SOLID 위반 → 기각
- Option B (DinosaurEventController가 TurnController 직접 호출): BombEventController와 유사하나 턴 흐름 개입이 과도 → 기각
- Option D (새 GameState 추가): 상태 머신 과도 확장, 단일 기능에 불필요 → 기각

**선택 이유**: Option C는 미래 확장(바람, 지진 등)도 `OnBetweenTurns`에 구독만 하면 되는 가장 느슨한 결합도를 제공한다.

### 2. 1프레임 패스스루 지연

**채택**: Coroutine `yield return null`으로 1프레임 지연.

`OnAllEggsStopped` 구독자들의 실행 순서는 C# 이벤트 등록 순서에 의존한다. `WinConditionChecker`가 `DinosaurEventController`보다 늦게 등록되면, 동기식 패스스루에서 승리 판정이 완료되기 전에 `AdvanceTurn()`이 호출될 수 있다. 1프레임 지연은 모든 OnAllEggsStopped 구독자(특히 WinConditionChecker)의 실행 완료를 보장한다.

60fps에서 16ms로 인간이 인지 불가능하다.

### 3. 공룡 이동: AddForce + PD 속도 보정

**채택**: `Rigidbody.AddForce` 기반 이동 + 속도 오차 보정.

```csharp
Vector3 desiredVelXZ = direction * walkSpeed;
Vector3 currentVelXZ = new Vector3(rb.velocity.x, 0, rb.velocity.z);
Vector3 force = (desiredVelXZ - currentVelXZ) * correctionGain;
rb.AddForce(force, ForceMode.Acceleration);
```

**대안 검토**:
- `rb.velocity` 직접 설정: 충돌 시 부자연스러움, 관성 무시 → 기각

**선택 이유**: AddForce는 Unity 물리엔진과 자연스럽게 상호작용하며, 알과 충돌 시 공룡이 약간 감속 후 복구되는 자연스러운 느낌을 준다. PD 보정으로 목표 속도에 수렴한다.

### 4. 공룡 간 충돌 해소: 타입 기반 계층 구조

**채택**: `DinosaurWalker.OnCollisionEnter`에서 타입 비교. T-Rex vs Velociraptor 충돌 시 Velociraptor를 `Destroy()`.

| 충돌 조합 | 결과 |
|---|---|
| T-Rex ↔ T-Rex | 물리 충돌 (서로 밀림) |
| Velociraptor ↔ Velociraptor | 물리 충돌 (서로 밀림) |
| T-Rex ↔ Velociraptor | Velociraptor 즉시 삭제 |

**대안 검토**:
- Layer Collision Matrix로 무조건 통과: 사용자가 몰입도 저하를 이유로 거부
- 별도 경로로 충돌 회피: Z 간격 강제 → 코드 복잡도 대비 이득 없음

**선택 이유**: 크기 차이를 게임 메커닉으로 반영. 같은 타입끼리는 자연 물리, 다른 타입은 계층적 해소.

### 5. 보드 경계 조회: IBoardSurface.GetPlayableBounds()

**채택**: `IBoardSurface`에 `GetPlayableBounds()` 메서드 추가. `StaticBoardSurface`는 이미 계산된 `bounds` 필드를 반환.

`GetCameraBounds()`가 동일한 `bounds`를 반환하지만 의미적 분리를 위해 별도 메서드로 추가한다.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| 정면충돌로 두 공룡 데드락 | `maxWalkDuration`(보드폭/속도 + 버퍼) 후 강제 Destroy |
| 공룡이 알을 보드 밖으로 밀어냄 | `BoardFallZone`이 감지, `MarkFallen()` → 승리 조건 체크 정상 작동 |
| Feature OFF 시 패스스루 누락 | DinosaurEventController는 Feature OFF에도 OnEnable에서 구독, 패스스루 모드로 동작 |
| Coroutine이 파괴된 GameObject 참조 | `null` 체크로 중복 Destroy 방지 |
| Animator Controller가 빈 상태 | Unity Editor에서 Walk 애니메이션 클립 추출 및 할당 필요 (코드 외 작업) |
| 동일 프레임 내 OnAllEggsStopped 구독 순서 문제 | 1프레임 지연으로 해결 — 모든 구독자 완료 보장 |
