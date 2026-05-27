# WORKLOG

## 2026-05-27 — LAN 멀티플레이어 기반 구축 + 안정화 (feature/lan-multiplayer)

### 네트워크 인프라
- Mirror 패키지 설치 및 네트워크 프리팹/컴포넌트 배치 (`DinoNetworkManager`, `NetworkInputRelay`, `NetworkGameStateSync`, `RoomCodeDiscovery`)
- `GameLaunchContext`에 `GameMode.NetworkHost`/`NetworkClient` 및 관련 프로퍼티 추가
- `KcpTransport` 포트 7777, `RoomCodeDiscovery` 포트 7778 설정
- 방 코드(UDP broadcast) + IP 직접 입력(127.0.0.1 fallback) 이중 탐색 지원
- `RoomCodeDiscovery` UDP 리스너를 `Thread.Abort()` 대신 `ReceiveTimeout` + `Close`로 안전 종료

### 게임 플로우 연동
- `DinoNetworkManager` — Host 시작, Client 접속, 플레이어 접속/해제, JoinGame/JoinAccepted 메시지 처리
- `NetworkInputRelay` — 클라이언트 발사 입력을 서버로 중계 (LaunchInputMessage), 재시작 요청 (RestartRequestMessage)
- `MapSelectController` — P2 접속 전까지 맵 버튼 비활성화, 접속 시 활성화, 호스트가 맵 선택 후 게임 시작
- `MainMenuController` — Host/Client 버튼, IP 입력, 방 코드 표시, CleanupPreviousSession()으로 이전 세션 정리
- 게임 종료 후 `ServerChangeScene("01_Game")`으로 완전 리로드 (RestartConfirmedMessage 제거)

### 클라이언트 동기화
- `NetworkGameStateSync` — 20fps(50ms) 스냅샷 송신, 클라이언트 측 Queue 기반 보간(100ms 버퍼, 250ms extrapolation)
- `StateSnapshotMessage`에 게임 시간/턴 시간 포함 → 클라이언트 HUD 타이머 동기화
- `isResolving` 플래그를 스냅샷에 포함 → 클라이언트도 "알이 움직이는중..." 가이드 표시
- `TurnChangeMessage`로 서버→클라이언트 턴 변경 동기화

### 입력 제어
- 클라이언트: `FlickInputController.useNetworkRelay = true` → 발사 입력을 서버로 전송 (로컬 물리 미실행)
- 호스트: P1 턴만 직접 조종, P2 턴은 입력 차단 (`SyncInputAvailability`에 `CurrentPlayerId != 1` 가드)
- 클라이언트: 발사 즉시 `inputEnabled = false` self-lock, 서버의 TurnChangeMessage 도착 시 재개
- 서버: `OnRemoteLaunch()`에 턴/잠금/게임오버 3단 검증 추가

### 버그 수정
| 문제 | 원인 | 해결 |
|------|------|------|
| "Couldn't create a Convex Mesh" | MeshCollider 사용 | → SphereCollider로 교체 (YoshiEgg, razorbill_egg) |
| ScriptableObject 로딩 실패 | FindObjectOfType으로 SO 탐색 | → Resources.Load로 변경, FeatureFlags.asset 이동 |
| 포트 충돌 | Host/Client 모두 7778 바인딩 | → Client는 ephemeral 포트(0) 사용 |
| Host Mode 덮어쓰기 | OnClientJoinAccepted가 무조건 NetworkClient 설정 | → `!NetworkServer.active` 가드 |
| isClientOnly 잘못 판별 | `IsNetworkClient`만 확인 | → `IsNetworkClient && !IsNetworkHost` |
| Host 게임 종료 프리즈 | GameResultMessage 무한 루프 | → `gameEndSent` guard 추가 |
| Host 자기 스냅샷 오염 | 서버가 자신의 스냅샷을 보간 | → `ApplySnapshot`에서 `NetworkServer.active` early return |
| 클라이언트 재시작 안 됨 | `OnClickRetry()`가 로컬 `RestartGame()` 호출 | → 네트워크 모드면 `RestartRequestMessage` 전송 |
| 클라이언트 무한 공격 | 서버 `OnRemoteLaunch()`에 검증 없음 | → 턴/잠금/게임오버 검증 + 클라이언트 self-lock |
| Host P2 턴에도 P1 알 조종 가능 | `Update()` 루프가 `SyncInputAvailability()`로 입력 재개 | → `IsNetworkHost && CurrentPlayerId != 1` 가드 |
| 카메라 전환 안 됨 (Host) | `TriggerTurnStarted` 중복 발사로 전환 중단 | → `playerId == currentViewerId` early return |
| 가이드 텍스트 안 바뀜 (A) | `HandleTurnStarted()`가 가이드 리셋 안 함 | → `ShowGuide("알을 조준하세요.")` 추가 |
| 가이드 텍스트 안 바뀜 (B) | 클라이언트가 `OnEggLaunched` 이벤트 못 받음 | → 스냅샷 `isResolving` 플래그로 트리거 |
| 스냅샷 멈춤 | `isResolving` 조건으로 스냅샷 전송 제한 | → 항상 전송으로 변경 (턴 전환 후 B 화면 멈춤 해결) |
| Host 재시작 불가 | `RestartGame()` 인플레이스 리셋 한계 | → `ServerChangeScene("01_Game")` 전체 리로드 |

### 충돌 없이 main 병합 완료

### 2026-05-19

- Person A MVP의 기본 폴더 구조와 `EggController` 구현을 완료했다. Unity 컴파일, 콘솔 에러/워닝, 타입 로드 검증을 통과했다.
- `EggSpawner` 구현을 완료했다. P1/P2 알 생성, 2x3 배치, 중복 생성 정리, 생성 목록 제공을 추가하고 Unity 컴파일 및 콘솔 검증을 통과했다.
- Person A 작업 운영 워크플로우를 `Docs/PersonA_MVP_Implementation_Plan.md`에 보강했다. 기능 단위 구현, Codex Unity 검증, 사용자 테스트 승인, 커밋/푸시, main 병합, 작업일지 갱신 루프를 명시했다.
- `FlickInputController`를 구현했다. 현재 플레이어의 살아있는 알 선택, 드래그 반대 방향 발사, 힘 제한, 입력 활성화/플레이어 전환 API, 선택/발사 이벤트를 추가했다.
- `Egg.prefab`과 `Egg_PhysicMaterial.physicMaterial`을 구성했다. Rigidbody, SphereCollider, EggController, 물리 마찰/탄성 기본값 연결을 Unity에서 검증했다.
- `A_EggPhysics_Prototype.unity` 테스트 Scene을 조립했다. Camera, Light, 임시 보드, EggSpawner, FlickInputController, Player Root를 배치하고 Play Mode에서 P1/P2 알 6개씩 총 12개 생성을 확인했다.
- Unity 컴파일, Console Error 확인, Play Mode 생성 검증을 통과했다. 사용자 Unity Editor 시각 테스트에서 선택, 드래그 발사, 충돌 동작에 문제가 없음을 승인받았다.
- 마감 품질 보강으로 `Egg` Layer를 추가하고 `Egg.prefab`과 `FlickInputController.eggLayerMask`를 해당 Layer로 제한했다. Unity 컴파일, Console Error 확인, Play Mode 알 12개 생성 및 Layer 적용 검증을 통과했다.

## 2026-05-19 (Person D)

- `AudioSettings` ScriptableObject를 구현했다. BGM/SFX 볼륨, 충돌 임팩트 스케일링, 풀 사이즈 등 오디오 설정을 Inspector에서 튜닝 가능하게 했다.
- `AudioManager`를 구현했다. GameEvents 구독으로 발사/충돌/낙하/승패/턴시작 SFX 재생, 충돌 강도에 따른 볼륨 스케일링, AudioSource 풀링, BGM 루프 재생, 랜덤 피치 변화를 추가했다. AudioClips은 Inspector 연결, 실제 오디오 파일은 나중에 추가 예정.
- `EffectController`를 구현했다. EggSpawner 참조로 각 EggController의 CollisionOccurred 이벤트를 구독하여 충돌 위치에 먼지 파티클 생성, GameEvents.OnEggFell로 낙하 파티클 생성, ParticleSystem 풀링, 충돌 강도에 따른 파티클 스케일링을 추가했다.
- `CameraController`에 Cinemachine Impulse 셰이크를 추가했다. GameEvents.OnEggCollision 구독으로 충돌 강도에 비례하는 카메라 흔들림, CinemachineImpulseSource 연결, 패키지 매니페스트에 Cinemachine 3.1.3 추가.
- `BombEventController` 스캐폴드를 작성했다. FeatureFlags.enableBomb으로 제어, 턴 카운트 기반 폭발물 스폰, 카운트다운 후 OverlapSphere 폭발, 거리 비례 힘 적용, 폭발 파티클/SFX 재생.
- [확인필요] Unity Editor에서 Cinemachine 패키지 resolve 후 컴파일 확인 필요. AudioMixer 에셋, CinemachineImpulseSource 컴포넌트, 파티클 프리팹은 Unity Editor에서 수동 연결 필요.
