# LAN 멀티플레이어 구현 계획 (Mirror)

> **프로젝트**: 공룡 알까기 (Dino Alkkagi)
> **기반**: Unity 6.3 LTS, C#, URP
> **네트워크 라이브러리**: Mirror (MIT License)
> **버전**: v0.4 (계획)
> **작성일**: 2026-05-26

---

## 1. 개요

### 목표
- 2인 LAN 멀티플레이어 지원 (1호스트 + 1클라이언트)
- 기존 핫시트/AI 모드는 정상 작동 유지
- FeatureFlags로 멀티 기능 On/Off 가능
- v0.4 단계로, 기본 알까기 코어는 영향을 받지 않음

### 현재 상태
- 핫시트 2인, VsComputer(AI) 모드 정상 동작
- `GameEvents` 정적 클래스 기반 이벤트 통신
- 모든 물리/판정은 Unity Physics (비결정적)
- `MainMenuController`에 Host/Join 버튼 UI 존재 (기능 미연결)
- `FeatureFlags.enableLanMultiplayer`, `enableLobby` 플래그 존재

---

## 2. 아키텍처

### 네트워크 모델: 서버 권한 (Server Authority)

```
원격 클라이언트 (Player 2)         호스트 (Player 1 + Server)
        │                                      │
        │────── ConnectRequest ───────────────>│
        │<───── JoinAccepted (PlayerId=2) ─────│
        │                                      │
        │  [턴开始时]                            │── GameEvents 정상 동작
        │                                      │
        │  [P2의 턴]                            │
        │────── LaunchInput(eggId,dir,force) ──>│
        │                                      │── egg.Launch() 호출
        │                                      │── Physics 시뮬레이션 (서버 전용)
        │<───── StateSnapshot (egg transforms) ─│  (주기적 전송)
        │                                      │
        │  [모든 알 정지]                        │
        │<───── TurnChange (nextPlayerId) ──────│
        │                                      │
        │  [게임 종료]                           │
        │<───── GameResult (winner) ────────────│
```

### 핵심 원칙
1. **서버가 모든 물리와 판정을 실행** — Unity Physics 비결정성 문제 회피
2. **클라이언트는 입력만 전송** — `FlickInputController`가 네트워크 모드에서 입력 메시지 전송
3. **호스트는 Server + Player 1을 동시에 실행** — Mirror Host 모드
4. **GameEvents는 서버에서만 발행** — 클라이언트는 네트워크 메시지로 상태 수신

### 데이터 흐름

```
클라이언트                            서버 (호스트)
  FlickInputController               GameSessionController
       │                                    │
       │  (P2 turn)                         │
       │  drag → release                    │
       │    │                               │
       │    ▼                               │
       │  NetworkInputRelay                 │
       │  .Send(LaunchInput)                │
       │    │                               │
       │    └────────Message───────────>     │
       │                         NetworkPlayer.OnLaunchInput()
       │                              │
       │                              ▼
       │                         GameSessionController.OnRemoteLaunch()
       │                              │
       │                              ▼
       │                         EggController.Launch()
       │                              │
       │                              ▼
       │                         MotionResolver (Physics)
       │                              │
       │    <────StateSnapshot────────┘
       │         (egg pos/rot/vel)    │
       │                              │
       │  [주기적 수신]                │
       │  NetworkReceiver             │
       │  → 각 EggController에        │
       │    transform 적용            │
```

---

## 3. 네트워크 메시지 프로토콜

모든 메시지는 Mirror의 `NetworkMessage`를 상속받아 struct로 정의.

| 메시지 | 방향 | 내용 |
|--------|------|------|
| `JoinGameMessage` | C→S | 클라이언트가 호스트에 접속 |
| `JoinAcceptedMessage` | S→C | 서버가 할당한 PlayerId (1 또는 2) |
| `LaunchInputMessage` | C→S | 발사할 알의 eggId, direction, force |
| `StateSnapshotMessage` | S→C | 전체 알의 위치/회전/속도/생존 상태 + 턴 정보 |
| `TurnChangeMessage` | S→C | 현재 턴 플레이어 ID |
| `GameResultMessage` | S→C | 게임 결과 (Player1Win/Player2Win/Draw) |
| `RestartMessage` | C→S | 재시작 요청 |
| `RestartConfirmedMessage` | S→C | 재시작 승인 + 새 게임 시작 |
| `MapSelectMessage` | C→S | 클라이언트가 선택한 맵 전송 |
| `ChatMessage` | 양방향 | 채팅 메시지 (선택) |

---

## 4. 파일 구조 (신규 및 변경)

```
Assets/_Project/Scripts/
├── Core/
│   ├── GameLaunchContext.cs          ← [변경] NetworkMode 추가
│   └── GameSessionController.cs      ← [변경] 네트워크 모드 분기
├── Input/
│   └── FlickInputController.cs       ← [변경] 네트워크 모드 입력 릴레이
├── Rules/
│   ├── TurnController.cs             ← [변경] 서버 전용, 네트워크 분기
│   └── WinConditionChecker.cs        ← [변경] 서버 전용
├── Network/                          ★ [신규] 네트워크 모듈
│   ├── DinoNetworkManager.cs         — NetworkManager 상속, 접속 관리
│   ├── DinoNetworkPlayer.cs          — 각 플레이어 메시지 처리
│   ├── NetworkMessages.cs            — 모든 메시지 struct 정의
│   ├── NetworkGameStateSync.cs       — 서버→클라이언트 상태 동기화
│   └── NetworkInputRelay.cs          — 클라이언트 입력→서버 릴레이
├── Presentation/
│   ├── MainMenuController.cs         ← [변경] 네트워크 로비 UI 연결
│   ├── MapSelectController.cs        ← [변경] 맵 선택 동기화
│   └── GameUIController.cs           ← [변경] 연결 상태 표시
└── Data/
    └── FeatureFlags.cs               ← [변경] enableLanMultiplayer 연동
```

---

## 5. 단계별 구현

### Phase 0: Mirror 패키지 설치

**작업**:
1. Unity Asset Store에서 "Mirror Networking" (무료) 설치
2. 또는 OpenUPM으로 설치: `openupm add com.mirror-networking.mirror`
3. KcpTransport 기본 설정 확인
4. NetworkManager 프리팹 생성
5. Scene 등록 (Build Settings)

**변경 파일**:
- `Packages/manifest.json` — Mirror 패키지 의존성 추가
- 신규: `Assets/_Project/Prefabs/Network/DinoNetworkManager.prefab`
- `ProjectSettings/EditorBuildSettings.asset` — 씬 등록

**체크포인트**: Unity 실행 후 Mirror가 정상 로드되는지 확인.

---

### Phase 1: 네트워크 기반 (Foundation)

**작업**:
1. `DinoNetworkManager` — NetworkManager 상속
   - 플레이어 연결/해제 처리
   - 게임 씬 관리
   - 최대 2명 접속 제한
   - 호스트/클라이언트/서버 모드 진입점
2. `NetworkMessages` — 메시지 struct 정의
3. `DinoNetworkPlayer` — NetworkBehaviour, 연결된 플레이어
   - PlayerId 할당 (호스트=1, 클라이언트=2)
   - 메시지 핸들러 등록
4. 네트워크 Discovery (선택, LAN 자동 탐색)

**신규 파일**:
- `Assets/_Project/Scripts/Network/DinoNetworkManager.cs`
- `Assets/_Project/Scripts/Network/DinoNetworkPlayer.cs`
- `Assets/_Project/Scripts/Network/NetworkMessages.cs`

**체크포인트**: 두 Unity 인스턴스가 접속/해제 가능.

---

### Phase 2: 게임 상태 동기화

**작업**:
1. `NetworkGameStateSync` — NetworkBehaviour
   - 서버에서 모든 알 수집
   - 주기적으로 `StateSnapshotMessage` 전송 (해석 중 100ms 간격)
   - 알 위치/회전/속도/생존 여부 직렬화
   - 클라이언트에서 수신 → 알 transform 적용 (보간 처리)
2. `EggController`에 NetworkIdentity 추가 (Mirror가 생성 관리)
   - 알의 네트워크 ID 부여 (netId 활용)
   - 서버에서만 물리 활성화, 클라이언트에서는 kinematic
3. `TurnChangeMessage` / `GameResultMessage` 처리

**신규 파일**:
- `Assets/_Project/Scripts/Network/NetworkGameStateSync.cs`

**변경 파일**:
- `EggController.cs` — NetworkIdentity 연동, 네트워크 모드 분기

**체크포인트**: 클라이언트에서 서버의 게임 상태를 실시간으로 볼 수 있음.

---

### Phase 3: 입력 릴레이

**작업**:
1. `NetworkInputRelay`
   - 클라이언트 측: `FlickInputController.EggLaunched` 이벤트 구독
   - 발사 시 `LaunchInputMessage`를 서버로 전송
   - 서버 측: 메시지 수신 → 해당 알의 `egg.Launch()` 호출
2. `FlickInputController` 네트워크 모드 분기
   - 로컬 모드: 현재 동작 유지 (`egg.Launch()` 직접 호출)
   - 네트워크 모드: 이벤트 발생만 하고 실제 발사는 서버에 위임
3. 알 식별: `egg.NetId`를 사용하여 클라이언트가 선택한 알 식별

**신규 파일**:
- `Assets/_Project/Scripts/Network/NetworkInputRelay.cs`

**변경 파일**:
- `FlickInputController.cs` — 네트워크 모드 분기 (isNetworkMode 플래그)

**체크포인트**: 원격 클라이언트가 알을 선택/발사하면 서버에서 물리가 실행됨.

---

### Phase 4: 게임 흐름 통합

**작업**:
1. `GameSessionController` 네트워크 분기
   - 서버 모드: 현재와 동일하게 모든 로직 실행
   - 클라이언트 모드: 게임 로직 실행 안 함, 네트워크 메시지로 상태 수신만
2. `TurnController` 서버 전용화
   - 턴 진행 로직은 서버에서만 실행
   - `NetworkGameStateSync`가 턴 변경 메시지 클라이언트 전송
3. `WinConditionChecker` 서버 전용화
   - 승패 판정은 서버에서만
   - 결과는 `GameResultMessage`로 클라이언트 전송
4. 재시작 흐름
   - 양쪽이 재시작 요청 시 서버가 `BeginGame()` 호출
   - 알 재생성 후 NetworkServer.Spawn 동기화

**변경 파일**:
- `GameSessionController.cs` — 네트워크 분기 처리
- `TurnController.cs` — 네트워크 모드에서 서버 전용
- `WinConditionChecker.cs` — 네트워크 모드에서 서버 전용
- `NetworkGameStateSync.cs` — turn/result 메시지 확장

**체크포인트**: 전체 게임 사이클 (턴 → 발사 → 해석 → 승패 → 재시작)이 네트워크에서 동작.

---

### Phase 5: UI/로비

**작업**:
1. `MainMenuController` 네트워크 UI 연결
   - "Host Game" → `DinoNetworkManager.StartHost()` + MapSelectScene
   - "Join" → IP 입력 UI → `DinoNetworkManager.StartClient(ip)`
   - 접속 상태 표시 (Connecting, Connected, Disconnected)
2. `MapSelectController` 맵 선택 동기화
   - 호스트가 맵 선택 → 씬 로드 전 클라이언트에 맵 정보 전송
   - 클라이언트는 동일한 맵 로드
3. `GameUIController` 연결 상태 표시
   - 원격 플레이어 접속 상태 HUD 표시
   - 연결 끊김 시 메시지 표시

**변경 파일**:
- `MainMenuController.cs` — 네트워크 로직 연결
- `MapSelectController.cs` — 맵 선택 동기화
- `GameUIController.cs` — 연결 상태 UI
- `GameLaunchContext.cs` — 네트워크 모드 추가

**체크포인트**: 메뉴에서 Host/Join → 맵 선택 → 게임 시작 전체 플로우 동작.

---

### Phase 6: 폴리싱

**작업**:
1. 연결 끊김 처리
   - 클라이언트 연결 끊김: 호스트에 알림, "플레이어 연결이 끊어졌습니다" 메시지
   - 호스트 연결 끊김: 클라이언트에 "호스트 연결이 끊어졌습니다" → 메인 메뉴 복귀
2. 연결 상태 표시 (HUD에 ping 또는 연결 표시기)
3. `FeatureFlags.enableLanMultiplayer` 연동
   - 플래그가 false면 네트워크 메뉴/기능 비활성화
4. 예외 처리
   - 접속 실패 시 에러 메시지
   - 타임아웃 처리
5. Credits.md 업데이트 (Mirror, MIT)

**변경 파일**:
- `FeatureFlags.cs` — 네트워크 플래그 실제 연동
- 각종 UI/메시지 관련 파일
- `Docs/Credits.md` — 에셋 크레딧 추가

---

## 6. GameLaunchContext 변경

```csharp
public enum GameMode
{
    LocalHotseat,
    VsComputer,
    NetworkHost,      // ★ 추가
    NetworkClient     // ★ 추가
}

public static class GameLaunchContext
{
    // 기존 필드 유지
    public static GameMode CurrentMode { get; private set; }
    public static bool IsNetwork => CurrentMode == GameMode.NetworkHost 
                                 || CurrentMode == GameMode.NetworkClient;
    public static bool IsNetworkHost => CurrentMode == GameMode.NetworkHost;
    public static bool IsNetworkClient => CurrentMode == GameMode.NetworkClient;
    
    // ★ 추가: 네트워크 접속 정보
    public static string ServerIP { get; set; } = "127.0.0.1";
    public static int ServerPort { get; set; } = 7777;
}
```

---

## 7. FlickInputController 네트워크 분기

```csharp
// FlickInputController.cs — 변경 예시
public class FlickInputController : MonoBehaviour
{
    // ...기존 필드...
    [SerializeField] private bool useNetworkRelay; // ★ 추가
    
    private void TryLaunchSelectedEgg()
    {
        // ...기존 검증 로직...
        
        if (useNetworkRelay)
        {
            // ★ 네트워크 모드: 입력을 서버로 전송 (직접 발사 안 함)
            NetworkInputRelay.SendLaunchInput(selectedEgg.netId, direction, force);
            EggLaunched?.Invoke(selectedEgg);
        }
        else
        {
            // 로컬 모드: 기존대로 직접 발사
            selectedEgg.Launch(direction * force);
            EggLaunched?.Invoke(selectedEgg);
        }
        ClearSelection();
    }
}
```

---

## 8. GameSessionController 네트워크 분기

```csharp
// GameSessionController — 변경 예시
public class GameSessionController : MonoBehaviour
{
    private bool isServerMode => GameLaunchContext.IsNetworkHost 
                              || !GameLaunchContext.IsNetwork;
    private bool isClientOnly => GameLaunchContext.IsNetworkClient;
    
    private void Start()
    {
        if (isClientOnly)
        {
            // 클라이언트: 게임 로직 실행 안 함
            // 네트워크 메시지로 상태만 수신
            return;
        }
        
        // 서버/로컬: 기존 Start() 로직
        if (flickInputController != null)
            flickInputController.EggLaunched += OnFlickEggLaunched;
        DistributeBoardSurface();
        StartCoroutine(InitializePhysicsNextFrame());
        BeginGame();
    }
    
    // 네트워크로부터 원격 입력 수신
    public void OnRemoteLaunch(uint eggNetId, Vector3 direction, float force)
    {
        if (!isServerMode) return;
        EggController egg = FindEggByNetId(eggNetId);
        if (egg != null)
            egg.Launch(direction * force);
    }
    
    // Update에서 네트워크 모드일 때 동기화
    private void Update()
    {
        if (isClientOnly) return; // 클라이언트는 동기화 안 함
        // ...기존 SyncInputAvailability...
    }
}
```

---

## 9. 리스크 및 고려사항

### 물리 동기화
- **문제**: Unity Physics는 비결정적 → 같은 입력이라도 다른 결과 가능
- **해결**: 서버 권한(Server Authority). 서버만 물리 실행, 클라이언트는 결과만 수신
- **대안**: 호스트 화면을 스트리밍 (Parsec 방식), 단점: 호스트 유리

### 입력 지연
- 턴제 게임이므로 실시간 지연에 민감하지 않음
- 해석 중 Time.timeScale 2.5x는 서버에서만 적용
- 클라이언트는 정상 시간으로 렌더링 (보간 처리)

### 재시작 동기화
- 양쪽의 재시작 요청을 동기화해야 함
- 호스트가 재시작 결정권, 클라이언트는 요청만 가능

### NAT/방화벽
- LAN 환경이므로 NAT 문제 없음
- 추후 온라인 확장 시 Steamworks Transport 또는 Relay 필요

### 기존 모드 영향
- 핫시트/AI 모드는 `GameLaunchContext.IsNetwork`로 완전히 분기
- FeatureFlags로 네트워크 기능 비활성화 가능

---

## 10. 체크포인트 요약

| Phase | 체크포인트 | 예상 시간 |
|-------|-----------|-----------|
| 0 | Mirror 설치, Manager 프리팹, 씬 등록 | 30분 |
| 1 | 두 인스턴스 접속/해제 확인 | 2시간 |
| 2 | 클라이언트에서 서버 게임 상태 렌더링 | 3시간 |
| 3 | 원격 클라이언트 입력 → 서버 발사 확인 | 2시간 |
| 4 | 전체 게임 사이클 네트워크 동작 | 3시간 |
| 5 | 메뉴→게임 전체 플로우 동작 | 2시간 |
| 6 | 폴리싱, 예외 처리, 크레딧 | 1시간 |
| **합계** | | **13.5시간** |

---

## 11. 참고

- Mirror 공식 문서: https://mirror-networking.gitbook.io/docs
- Mirror GitHub: https://github.com/MirrorNetworking/Mirror
- 라이선스: MIT — `Credits.md` 기록 필수
- 기존 게임플랜(`Docs/gameplan.md`) 9번 항목: Person B가 LAN 실험 담당
- `FeatureFlags.enableLanMultiplayer`를 통해 모듈 On/Off
