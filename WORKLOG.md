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