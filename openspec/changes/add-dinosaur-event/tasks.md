## 1. Core Events

- [x] 1.1 `GameEvents.cs`에 `OnBetweenTurns` / `OnBetweenTurnsEnded` 이벤트 및 Trigger 메서드 추가
- [x] 1.2 `TurnController.cs` — 구독 변경: `OnAllEggsStopped` 관련 등록/해제 제거, `HandleOnAllEggsStopped` 메서드 삭제, `OnBetweenTurnsEnded` 구독 및 `HandleOnBetweenTurnsEnded` 핸들러 추가 (기존 로직 이식)

## 2. Board Boundary

- [x] 2.1 `IBoardSurface.cs`에 `Bounds GetPlayableBounds();` 시그니처 추가
- [x] 2.2 `StaticBoardSurface.cs`에 `GetPlayableBounds()` 구현 (기존 `bounds` 필드 반환)

## 3. Data Types

- [x] 3.1 `DinosaurType.cs` 열거형 생성 — `TRex`, `Velociraptor`

## 4. Dinosaur Walker

- [x] 4.1 `DinosaurWalker.cs` 생성 — `DinosaurType Type` 프로퍼티, `OnCollisionEnter`에서 타입 비교 및 T-Rex vs Velociraptor 시 Velociraptor `Destroy()` 처리

## 5. Dinosaur Event Controller

- [x] 5.1 `DinosaurEventController.cs` 생성 — SerializeField 필드 (`FeatureFlags`, `TurnController`, `boardSurface`, `dinoPrefabs`, `spawnChance`, `walkSpeed`, `walkForce`, `correctionGain`, `spawnHeight`, `maxWalkDuration`, `dinoLayerName`)
- [x] 5.2 OnEnable/OnDisable — `GameEvents.OnAllEggsStopped` 구독 및 해제
- [x] 5.3 `HandleBetweenTurnsCoroutine` — 1프레임 yield return null 후 `TriggerBetweenTurns()`, 20% 랜덤 체크, 공룡 스폰 또는 패스, `TriggerBetweenTurnsEnded()`
- [x] 5.4 `SpawnDinosaur` — 랜덤 타입/Z/방향, 보드 가장자리 스폰 위치 계산, Instantiate + Rigidbody/레이어 설정, `DinosaurWalker.Type` 할당
- [x] 5.5 `WalkDinosaur` 코루틴 — 매 `FixedUpdate`마다 AddForce + PD 보정, `maxWalkDuration` 타임아웃, null 체크로 조기 파괴 대응
- [x] 5.6 `GetSpawnPosition` — `GetPlayableBounds()`로 보드 경계 획득, 랜덤 Z, 방향에 따른 X 설정, raycast로 Y 보정

## 6. Unity Scene & Assets

- [x] 6.1 Project Settings → Tags and Layers에 "Dinosaur" 레이어 추가, Physics Collision Matrix에서 Dinosaur-Dinosaur 충돌 활성화
- [x] 6.2 .glb 모델의 Animation 탭에서 Walk 클립 추출 및 Animator Controller에 Walk State 할당
- [x] 6.3 T-Rex.prefab, Velociraptor.prefab 생성 (Animator + Rigidbody(질량10, Y회전 Freeze) + Collider + DinosaurWalker + Dinosaur 레이어 할당)
- [x] 6.4 01_Game.unity에 DinosaurEventController GameObject 추가, SerializeField 바인딩 (FeatureFlags, TurnController, boardLoader, dinoPrefabs[])
- [x] 6.5 `FeatureFlags.asset`에서 `enableDinosaurNpc = true`로 설정
