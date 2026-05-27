using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DinoAlkkagi.Data;
using DinoAlkkagi.Environment;
using DinoAlkkagi.Rules;

namespace DinoAlkkagi.Core
{
    /// <summary>
    /// 게임 전체 흐름을 관리하는 컨트롤러.
    /// - EggSpawner: 알 생성 위임
    /// - FlickInputController: 입력 제어 연동
    /// - TurnController / MotionResolver / WinConditionChecker: Rules 시스템 오케스트레이션
    /// - BoardFallZone: 낙하 감지
    /// </summary>
    public class GameSessionController : MonoBehaviour
    {
        [Header("--- 설정 ---")]
        [SerializeField] private GameSettings settings;

        [Header("--- Rules 시스템 ---")]
        [SerializeField] private TurnController turnController;
        [SerializeField] private MotionResolver motionResolver;
        [SerializeField] private WinConditionChecker winConditionChecker;

        [Header("--- Person A 시스템 (자동 연결) ---")]
        [SerializeField] private EggSpawner eggSpawner;
        [SerializeField] private FlickInputController flickInputController;

        [Header("--- Person B 추가 (씬에 배치 필요) ---")]
        [SerializeField] private BoardFallZone boardFallZone;
        [SerializeField] private AIInputController aiInputController;

        [Header("--- 정적 맵 ---")]
        [SerializeField] private StaticBoardLoader staticBoardLoader;

        [Header("--- 네트워크 ---")]
        [SerializeField] private DinoNetworkManager networkManager;

        private List<EggController> allEggs = new List<EggController>();
        private GameState currentState = GameState.Setup;

        private bool isServerMode;
        private bool isClientOnly;

        public GameState CurrentState => currentState;
        public IReadOnlyList<EggController> AllEggs => allEggs.AsReadOnly();

        private void OnEnable()
        {
            GameEvents.OnGameEnded += HandleOnGameEnded;
            GameEvents.OnTurnStarted += HandleOnTurnStarted;
            GameEvents.OnEggLaunched += HandleOnEggLaunched;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEnded -= HandleOnGameEnded;
            GameEvents.OnTurnStarted -= HandleOnTurnStarted;
            GameEvents.OnEggLaunched -= HandleOnEggLaunched;

            if (flickInputController != null)
                flickInputController.EggLaunched -= OnFlickEggLaunched;
        }

        private void Awake()
        {
            // 자동 탐색
            settings ??= FindFirstObjectByType<GameSettings>();
            turnController ??= FindFirstObjectByType<TurnController>();
            motionResolver ??= FindFirstObjectByType<MotionResolver>();
            winConditionChecker ??= FindFirstObjectByType<WinConditionChecker>();
            eggSpawner ??= FindFirstObjectByType<EggSpawner>();
            flickInputController ??= FindFirstObjectByType<FlickInputController>();
            boardFallZone ??= FindFirstObjectByType<BoardFallZone>();
            aiInputController ??= FindFirstObjectByType<AIInputController>();
            staticBoardLoader ??= FindFirstObjectByType<StaticBoardLoader>();
            networkManager ??= FindFirstObjectByType<DinoNetworkManager>();

            isServerMode = GameLaunchContext.IsNetworkHost || !GameLaunchContext.IsNetwork;
            // 호스트(서버+클라이언트)는 isClientOnly=false. 순수 클라이언트만 true.
            isClientOnly = GameLaunchContext.IsNetworkClient && !GameLaunchContext.IsNetworkHost;

            if (flickInputController != null)
                flickInputController.UseNetworkRelay = isClientOnly;

            // 누락 체크
            if (settings == null)
                Debug.LogError("[GameSessionController] GameSettings not found!");
            if (turnController == null)
                Debug.LogError("[GameSessionController] TurnController not found!");
            if (motionResolver == null)
                Debug.LogError("[GameSessionController] MotionResolver not found!");
            if (winConditionChecker == null)
                Debug.LogError("[GameSessionController] WinConditionChecker not found!");
            if (eggSpawner == null)
                Debug.LogError("[GameSessionController] EggSpawner not found!");
            if (flickInputController == null)
                Debug.LogError("[GameSessionController] FlickInputController not found!");
            if (boardFallZone == null)
                Debug.LogWarning("[GameSessionController] BoardFallZone not found! Add it below the board.");
            if (aiInputController == null)
                Debug.Log("[GameSessionController] AIInputController not found. AI opponent disabled.");
        }

        private void Start()
        {
            if (isClientOnly)
            {
                Debug.Log("[GameSessionController] Client-only mode. Spawning eggs for display.");
                DistributeBoardSurface();

                eggSpawner?.ClearSpawnedEggs();
                eggSpawner?.SpawnAll();
                CollectAndRegisterEggs();
                SetState(GameState.Aiming);

                if (flickInputController != null)
                {
                    flickInputController.UseNetworkRelay = true;
                    flickInputController.SetActivePlayer(GameLaunchContext.LocalPlayerId);
                    flickInputController.SetInputEnabled(false);
                    flickInputController.EggLaunched += OnFlickEggLaunched;
                }

                MakeClientEggsKinematic();
                GameEvents.TriggerGameStarted();
                return;
            }

            if (flickInputController != null)
                flickInputController.EggLaunched += OnFlickEggLaunched;

            DistributeBoardSurface();
            StartCoroutine(InitializePhysicsNextFrame());
            BeginGame();
        }

        private void MakeClientEggsKinematic()
        {
            foreach (var egg in allEggs)
            {
                if (egg == null || egg.Rigidbody == null) continue;
                egg.Rigidbody.isKinematic = true;
                egg.Rigidbody.useGravity = false;
            }
        }

        private IEnumerator InitializePhysicsNextFrame()
        {
            yield return null;
            Physics.SyncTransforms();
            Debug.Log("[GameSessionController] Physics synced after scene load");
        }

        private void DistributeBoardSurface()
        {
            if (staticBoardLoader == null) return;

            IBoardSurface surface = staticBoardLoader.BoardSurface;
            if (surface == null) return;

            eggSpawner?.SetBoardSurface(surface);
            flickInputController?.SetBoardSurface(surface);
            var cam = FindFirstObjectByType<CameraController>();
            if (cam != null) cam.SetBoardSurface(surface);
            boardFallZone?.SetBoardSurface(surface);

            Debug.Log("[GameSessionController] Distributed IBoardSurface to subsystems.");
        }

        /// <summary>
        /// 게임 시작: EggSpawner가 생성한 알을 Rules 시스템에 등록하고 게임을 시작한다.
        /// </summary>
        public void BeginGame()
        {
            SetState(GameState.Setup);

            // 이전 알 제거 후 새로 생성 (Restart 대응)
            eggSpawner?.ClearSpawnedEggs();
            CollectAndRegisterEggs();
            SetState(GameState.Aiming);
            Debug.Log("[GameSessionController] Game setup complete. Starting match.");

            if (isServerMode && networkManager != null)
                networkManager.NotifyGameStarted();

            GameEvents.TriggerGameStarted();
        }

        /// <summary>
        /// EggSpawner가 생성한 알들을 수집하여 Rules 시스템에 등록한다.
        /// </summary>
        private void CollectAndRegisterEggs()
        {
            // 기존 알 목록 초기화
            allEggs.Clear();
            motionResolver?.ClearEggs();
            winConditionChecker?.ClearEggs();

            if (eggSpawner == null) return;

            // EggSpawner가 아직 알을 생성하지 않았다면 직접 요청
            if (eggSpawner.SpawnedEggs.Count == 0)
            {
                eggSpawner.SpawnAll();
            }

            // 생성된 알 수집 및 등록
            foreach (var egg in eggSpawner.SpawnedEggs)
            {
                if (egg == null) continue;
                allEggs.Add(egg);
                motionResolver?.RegisterEgg(egg);
                winConditionChecker?.RegisterEgg(egg);

                // 이벤트 브릿지: Person A EggController → GameEvents
                egg.Launched -= OnEggLaunchedBridge; // 중복 방지
                egg.Launched += OnEggLaunchedBridge;
                egg.Fallen -= OnEggFellBridge;
                egg.Fallen += OnEggFellBridge;
                egg.CollisionOccurred -= OnEggCollisionBridge;
                egg.CollisionOccurred += OnEggCollisionBridge;
            }

            // AIInputController에 알 등록 (AI 발사용)
            aiInputController?.RegisterEggs(allEggs);

            Debug.Log($"[GameSessionController] Registered {allEggs.Count} eggs.");
        }

        private void OnEggLaunchedBridge(EggController egg)
        {
            GameEvents.TriggerEggLaunched(egg);
        }

        private void OnEggFellBridge(EggController egg)
        {
            GameEvents.TriggerEggFell(egg);
        }

        private void OnEggCollisionBridge(EggController egg, float impact)
        {
            GameEvents.TriggerEggCollision(impact);
        }

        /// <summary>
        /// 턴 시작 시 FlickInputController와 동기화한다.
        /// </summary>
        private void HandleOnTurnStarted(int playerId)
        {
            if (flickInputController == null) return;

            if (isClientOnly)
            {
                // 클라이언트: 항상 P2 제어, P1 턴엔 입력 차단
                // 게임 종료 후 늦게 도착한 TurnStarted에서 입력 재개되지 않도록 currentState 체크
                flickInputController.SetActivePlayer(GameLaunchContext.LocalPlayerId);
                flickInputController.SetInputEnabled(playerId == GameLaunchContext.LocalPlayerId
                    && !turnController.IsInputLocked
                    && currentState != GameState.Result);
            }
            else if (GameLaunchContext.IsNetworkHost)
            {
                // 호스트(네트워크): P1 턴만 직접 조종, P2 턴은 클라이언트 입력 대기
                if (playerId == 1)
                {
                    flickInputController.SetActivePlayer(1);
                    SyncInputAvailability();
                }
                else
                {
                    flickInputController.SetInputEnabled(false);
                }
            }
            else
            {
                // 로컬 핫시트: 턴에 따라 자유롭게 조종
                flickInputController.SetActivePlayer(playerId);
                SyncInputAvailability();
            }
        }

        /// <summary>
        /// 발사 즉시 입력을 잠근다 (GameEvents 체인).
        /// </summary>
        private void HandleOnEggLaunched(EggController egg)
        {
            SyncInputAvailability();
        }

        /// <summary>
        /// FlickInputController 직접 구독 — 발사 감지 확실하게.
        /// </summary>
        private void OnFlickEggLaunched(EggController egg)
        {
            SyncInputAvailability();
        }

        /// <summary>
        /// 안전장치: isInputLocked 상태와 FlickInputController를 강제 동기화.
        /// </summary>
        private void Update()
        {
            if (isClientOnly) return;
            if (turnController == null || flickInputController == null) return;

            SyncInputAvailability();
        }

        private void SyncInputAvailability()
        {
            if (turnController == null || flickInputController == null)
            {
                return;
            }

            // 네트워크 호스트: P1 턴만 직접 조종
            if (GameLaunchContext.IsNetworkHost && turnController.CurrentPlayerId != 1)
            {
                flickInputController.SetInputEnabled(false);
                return;
            }

            bool isAiTurn = aiInputController != null && aiInputController.IsAiPlayer(turnController.CurrentPlayerId);
            flickInputController.SetInputEnabled(!turnController.IsInputLocked && !isAiTurn);
        }

        /// <summary>
        /// 입력을 잠근다 (발사 후 MotionResolver가 해제할 때까지).
        /// TurnController가 이벤트를 받아 IsInputLocked를 설정하고,
        /// 여기서 FlickInputController를 비활성화한다.
        /// </summary>
        /// <summary>
        /// 네트워크로부터 원격 클라이언트의 발사 입력을 받아 처리한다.
        /// </summary>
    public void OnRemoteLaunch(uint eggNetId, Vector3 direction, float force)
    {
        if (!isServerMode)
        {
            Debug.LogWarning("[GameSessionController] Ignored remote launch: not server mode.");
            return;
        }

        // 게임 종료 후 발사 차단
        if (currentState == GameState.Result)
        {
            Debug.LogWarning("[GameSessionController] Ignored remote launch: game is over.");
            return;
        }

        // 아직 이전 발사 해결 중이면 차단
        if (turnController != null && turnController.IsInputLocked)
        {
            Debug.LogWarning("[GameSessionController] Ignored remote launch: input locked (resolving).");
            return;
        }

        EggController egg = FindEggByNetId(eggNetId);
        if (egg == null)
        {
            Debug.LogWarning($"[GameSessionController] Remote launch: egg with netId {eggNetId} not found.");
            return;
        }

        // 턴 확인: 현재 턴인 플레이어의 알만 발사 가능
        int currentTurnId = turnController != null ? turnController.CurrentPlayerId : 0;
        if (egg.OwnerPlayerId != currentTurnId)
        {
            Debug.LogWarning($"[GameSessionController] Ignored remote launch: P{egg.OwnerPlayerId} egg not P{currentTurnId}'s turn.");
            return;
        }

        Debug.Log($"[GameSessionController] Remote launch: P{egg.OwnerPlayerId} egg {eggNetId}, force={force}");
        egg.Launch(direction * force);
    }

        private EggController FindEggByNetId(uint netId)
        {
            int eggId = (int)netId;
            foreach (var egg in allEggs)
            {
                if (egg == null) continue;
                if (egg.NetworkEggId == eggId)
                    return egg;
            }
            return null;
        }

        public void LockAllInput()
        {
            turnController?.LockInput();
            flickInputController?.SetInputEnabled(false);
        }

        /// <summary>
        /// 입력을 해제한다.
        /// </summary>
        public void UnlockAllInput()
        {
            turnController?.UnlockInput();
        }

        public void RestartGame()
        {
            Debug.Log("[GameSessionController] Restarting game...");
            // BeginGame가 모든 정리 + 재생성을 처리함
            BeginGame();
        }

        private void HandleOnGameEnded(GameResult result)
        {
            SetState(GameState.Result);
            flickInputController?.SetInputEnabled(false);
            Debug.Log($"[GameSessionController] === Game Over: {result} ===");
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Restart Game")]
        private void DebugRestart()
        {
            RestartGame();
        }

        private GUIStyle debugLabelStyle;
        private GUIStyle debugButtonStyle;

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // 스타일 캐싱 (매 프레임 new 안 하게)
            if (debugLabelStyle == null)
            {
                debugLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 40,
                    normal = { textColor = Color.white }
                };
                debugButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 40,
                    fixedHeight = 80,
                    fixedWidth = 300
                };
            }

            GUILayout.BeginArea(new Rect(20, 20, 500, 500));
            GUILayout.Label($"State: {currentState}", debugLabelStyle);
            GUILayout.Label($"Turn: Player {turnController?.CurrentPlayerId}", debugLabelStyle);
            GUILayout.Label($"Input Locked: {turnController?.IsInputLocked}", debugLabelStyle);
            GUILayout.Label($"Resolving: {motionResolver?.IsResolving} ({motionResolver?.ResolveTime:F1}s)", debugLabelStyle);

            if (winConditionChecker != null)
            {
                GUILayout.Label($"P1 Alive: {winConditionChecker.GetAliveCount(1)}", debugLabelStyle);
                GUILayout.Label($"P2 Alive: {winConditionChecker.GetAliveCount(2)}", debugLabelStyle);
            }

            if (currentState == GameState.Result)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("Restart", debugButtonStyle))
                {
                    RestartGame();
                }
            }
            GUILayout.EndArea();
        }
#endif
    }
}
