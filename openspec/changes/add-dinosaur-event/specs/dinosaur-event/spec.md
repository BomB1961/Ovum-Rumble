## ADDED Requirements

### Requirement: Between-Turns Gate Phase
시스템은 `OnAllEggsStopped` 이벤트 발생 후, 다음 턴으로 넘어가기 전에 `OnBetweenTurns` → `OnBetweenTurnsEnded` 게이트 페이즈를 SHALL 제공한다. TurnController는 `OnBetweenTurnsEnded`에서만 `AdvanceTurn()`을 호출한다.

#### Scenario: Normal pass-through with no events
- **WHEN** `OnAllEggsStopped`가 발생하고 공룡 이벤트가 트리거되지 않음
- **THEN** `OnBetweenTurns`가 발행된 후 1프레임 이내에 `OnBetweenTurnsEnded`가 발행된다
- **THEN** TurnController가 `AdvanceTurn()`을 호출하여 다음 플레이어 턴으로 전환된다

#### Scenario: Feature flag is disabled
- **WHEN** `enableDinosaurNpc` FeatureFlag가 false임
- **THEN** DinosaurEventController는 `OnBetweenTurns` → `OnBetweenTurnsEnded`를 즉시 패스스루한다
- **THEN** TurnController가 정상적으로 `AdvanceTurn()`을 호출한다

#### Scenario: Game ended during previous phase
- **WHEN** `OnAllEggsStopped`에서 WinConditionChecker가 `OnGameEnded`를 발행함
- **THEN** TurnController는 `OnBetweenTurnsEnded`를 수신해도 `AdvanceTurn()`을 호출하지 않는다

### Requirement: Random Dinosaur Spawn
시스템은 `OnBetweenTurns` 페이즈에서 20% 확률로 단일 공룡을 보드 가장자리에 스폰 SHALL 한다. 스폰 시 T-Rex 또는 Velociraptor 중 하나를 랜덤으로 선택한다.

#### Scenario: 20% chance triggers dinosaur spawn
- **WHEN** 랜덤 값이 `spawnChance`(기본 0.2) 이하임
- **THEN** 공룡 한 마리가 보드 가장자리에 스폰된다
- **THEN** `TurnController.LockInput()`이 호출되어 플레이어 입력이 차단된다
- **THEN** 공룡 타입은 T-Rex 또는 Velociraptor 중 랜덤으로 선택된다

#### Scenario: 80% chance skips dinosaur spawn
- **WHEN** 랜덤 값이 `spawnChance`를 초과함
- **THEN** 공룡이 스폰되지 않고 `OnBetweenTurnsEnded`가 즉시 발행된다
- **THEN** 플레이어 입력이 차단되지 않는다

### Requirement: Dinosaur Walk Across Board
스폰된 공룡은 보드를 직선으로 가로질러 반대쪽 가장자리까지 걸어가야 한다. 이동은 Rigidbody.AddForce 기반으로, 목표 속도로 수렴하도록 PD 보정이 적용된다.

#### Scenario: Dinosaur walks straight line
- **WHEN** 공룡이 스폰됨
- **THEN** 공룡은 X축 방향(좌→우 또는 우→좌 랜덤)으로 `walkSpeed`로 이동한다
- **THEN** Z좌표는 보드 범위 내에서 랜덤으로 선택된다
- **THEN** 고도(Y)는 보드 표면 위 `spawnHeight`로 유지된다

#### Scenario: Dinosaur reaches opposite edge
- **WHEN** 공룡이 보드 반대쪽 가장자리에 도달
- **THEN** 공룡 GameObject가 Destroy된다
- **THEN** `OnBetweenTurnsEnded`가 발행되어 턴이 진행된다

#### Scenario: Dinosaur walk times out
- **WHEN** 공룡이 `maxWalkDuration`(기본 8초) 이내에 반대쪽에 도달하지 못함
- **THEN** 공룡 GameObject가 Destroy된다
- **THEN** `OnBetweenTurnsEnded`가 발행되어 턴이 진행된다

### Requirement: Dinosaur-Egg Physics Collision
공룡은 알과의 충돌 시 Unity Rigidbody 물리 엔진을 통해 알을 밀어낸다. 공룡의 질량은 알보다 충분히 커서(기본 10배) 공룡이 알에 의해 크게 방해받지 않는다.

#### Scenario: Dinosaur bumps into eggs
- **WHEN** 공룡이 알과 충돌함
- **THEN** 알이 공룡의 진행 방향으로 밀려난다
- **THEN** 공룡은 일시적으로 감속 후 PD 보정에 의해 목표 속도로 복구된다

#### Scenario: Dinosaur pushes egg off board
- **WHEN** 공룡이 민 알이 보드 밖으로 떨어짐
- **THEN** `BoardFallZone`이 알을 감지하고 `MarkFallen()`을 호출한다
- **THEN** 승리 조건 체크가 정상적으로 진행된다

### Requirement: Dinosaur-vs-Dinosaur Collision Resolution
두 공룡이 충돌 시, T-Rex와 Velociraptor는 크기 차이에 따라 계층적으로 해소된다. T-Rex가 Velociraptor와 충돌하면 Velociraptor는 즉시 삭제된다. 동종 간 충돌은 일반 Rigidbody 물리 충돌로 처리된다.

#### Scenario: T-Rex collides with Velociraptor
- **WHEN** T-Rex와 Velociraptor가 충돌함
- **THEN** Velociraptor GameObject가 즉시 Destroy된다
- **THEN** T-Rex는 충돌 반동을 받은 후 PD 보정으로 계속 진행한다

#### Scenario: Same-type dinosaurs collide
- **WHEN** 같은 타입의 공룡 두 마리가 충돌함 (T-Rex↔T-Rex 또는 Velociraptor↔Velociraptor)
- **THEN** 두 공룡 모두 충돌 반동을 받는다
- **THEN** 두 공룡 모두 PD 보정으로 목표 속도로 복구를 시도한다

#### Scenario: Dinosaur destroyed mid-walk
- **WHEN** Velociraptor가 T-Rex에 의해 Destroy됨
- **THEN** 해당 공룡의 Walk 코루틴이 null 체크로 안전하게 종료된다
- **THEN** 중복 Destroy가 발생하지 않는다

### Requirement: Board Boundary Query
`IBoardSurface`는 보드의 플레이 가능한 영역의 Bounds를 반환하는 `GetPlayableBounds()` 메서드를 제공한다. `StaticBoardSurface`는 모든 보드 Collider를 감싼 결합 Bounds를 반환한다.

#### Scenario: Query board boundaries
- **WHEN** `GetPlayableBounds()`가 호출됨
- **THEN** 보드의 모든 Collider를 포함하는 Bounds가 반환된다
- **THEN** bounds.min/max에서 X, Z 축 경계 값을 얻을 수 있다
