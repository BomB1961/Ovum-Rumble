using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DinoAlkkagi.Data;
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

        private List<EggController> allEggs = new List<EggController>();
        private GameState currentState = GameState.Setup;

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
        }

        private void Start()
        {
            // FlickInputController 직접 구독 (GameEvents 체인 우회, 확실한 발사 감지)
            if (flickInputController != null)
                flickInputController.EggLaunched += OnFlickEggLaunched;

            BeginGame();
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
            }

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

        /// <summary>
        /// 턴 시작 시 FlickInputController와 동기화한다.
        /// </summary>
        private void HandleOnTurnStarted(int playerId)
        {
            if (flickInputController == null) return;

            flickInputController.SetActivePlayer(playerId);
            flickInputController.SetInputEnabled(true);
        }

        /// <summary>
        /// 발사 즉시 입력을 잠근다 (GameEvents 체인).
        /// </summary>
        private void HandleOnEggLaunched(EggController egg)
        {
            LockAllInput();
        }

        /// <summary>
        /// FlickInputController 직접 구독 — 발사 감지 확실하게.
        /// </summary>
        private void OnFlickEggLaunched(EggController egg)
        {
            LockAllInput();
        }

        /// <summary>
        /// 안전장치: isInputLocked 상태와 FlickInputController를 강제 동기화.
        /// </summary>
        private void Update()
        {
            if (turnController == null || flickInputController == null) return;

            flickInputController.SetInputEnabled(!turnController.IsInputLocked);
        }

        /// <summary>
        /// 입력을 잠근다 (발사 후 MotionResolver가 해제할 때까지).
        /// TurnController가 이벤트를 받아 IsInputLocked를 설정하고,
        /// 여기서 FlickInputController를 비활성화한다.
        /// </summary>
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
