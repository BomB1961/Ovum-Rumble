## Why

턴제 알까기 게임에서 플레이어 간 턴 전환 시 예측 불가능한 방해 요소를 추가하여 긴장감과 재미를 높인다. 현재는 턴과 턴 사이에 아무 일도 일어나지 않아 단조롭다. 공룡이 보드를 가로질러 알과 충돌하는 랜덤 이벤트로 승부의 흐름을 뒤흔든다.

## What Changes

- **신규**: `GameEvents`에 `OnBetweenTurns` / `OnBetweenTurnsEnded` 이벤트 추가 — 턴 사이 게이트 페이즈
- **신규**: `DinosaurEventController` — 20% 확률로 턴 사이 공룡 스폰 및 보행 관리
- **신규**: `DinosaurWalker` — 공룡 개별 Prefab에 부착, T-Rex가 Velociraptor를 충돌 시 삭제
- **신규**: `IBoardSurface.GetPlayableBounds()` — 보드 영역 경계 조회
- **수정**: `TurnController` — 턴 진행 구독을 `OnAllEggsStopped` → `OnBetweenTurnsEnded`로 변경
- **수정**: `StaticBoardSurface` — `GetPlayableBounds()` 구현
- **Unity Editor**: Dinosaur 레이어, Animator Controller, Prefab 생성

## Capabilities

### New Capabilities

- `dinosaur-event`: 턴 사이 랜덤 공룡 방해 이벤트 — 공룡 스폰, 보드 가로지르기, 알 밀기, 공룡 간 충돌 해소

### Modified Capabilities

<!-- None — existing capabilities unchanged -->

## Impact

| 영역 | 영향 |
|---|---|
| `GameEvents` | 이벤트 2개 추가 (`OnBetweenTurns`, `OnBetweenTurnsEnded`) |
| `TurnController` | 구독 변경 (기존 `HandleOnAllEggsStopped` 제거, `HandleOnBetweenTurnsEnded` 추가) |
| `IBoardSurface` | 메서드 1개 추가 시그니처 |
| `StaticBoardSurface` | 메서드 1개 구현 |
| `Environment/` | `DinosaurEventController.cs`, `DinosaurWalker.cs` 신규 |
| Physics | Dinosaur 레이어 추가 (Collision Matrix: Default✓ Egg✓ Dinosaur✓) |
| `Anim/` | Animator Controller에 Walk 스테이트 추가 |
| `FeatureFlags` | `enableDinosaurNpc = true` |
