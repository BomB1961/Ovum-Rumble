using UnityEngine;
using System.Collections.Generic;
using DinoAlkkagi.Data;
using DinoAlkkagi.Rules;

namespace DinoAlkkagi.Core
{
    /// <summary>
    /// 게임 전체 흐름을 관리하는 최상위 컨트롤러.
    /// Person C가 확장할 메인 진입점. Person B의 룰 시스템을 오케스트레이션한다.
    /// 기획서 4. 게임 상태 머신과 6-2 턴 진행을 구현한다.
    /// </summary>
    public class GameSessionController : MonoBehaviour
    {
        [Header("--- 설정 ---")]
        [SerializeField] private GameSettings settings;

        [Header("--- 시스템 참조 ---")]
        [SerializeField] private TurnController turnController;
        [SerializeField] private MotionResolver motionResolver;
        [SerializeField] private WinConditionChecker winConditionChecker;

        [Header("--- 스폰 설정 ---")]
        [SerializeField] private Transform player1SpawnRoot;
        [SerializeField] private Transform player2SpawnRoot;
        [SerializeField] private GameObject eggPrefab;

        private List<EggController> allEggs = new List<EggController>();
        private GameState currentState = GameState.Setup;

        public GameState CurrentState => currentState;
        public IReadOnlyList<EggController> AllEggs => allEggs.AsReadOnly();

        private void OnEnable()
        {
            GameEvents.OnGameEnded += HandleOnGameEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEnded -= HandleOnGameEnded;
        }

        private void Awake()
        {
            // 자동 탐색 — 다른 팀원이 인스펙터 연결을 빼먹어도 null 방지
            settings ??= FindFirstObjectByType<GameSettings>();
            turnController ??= FindFirstObjectByType<TurnController>();
            motionResolver ??= FindFirstObjectByType<MotionResolver>();
            winConditionChecker ??= FindFirstObjectByType<WinConditionChecker>();

            // 누락 체크 — 명확한 에러 로그로 즉시 발견 가능
            if (settings == null)
                Debug.LogError("[GameSessionController] GameSettings not found! Create a GameSettings asset and assign it.");
            if (turnController == null)
                Debug.LogError("[GameSessionController] TurnController not found! Add it to the scene.");
            if (motionResolver == null)
                Debug.LogError("[GameSessionController] MotionResolver not found! Add it to the scene.");
            if (winConditionChecker == null)
                Debug.LogError("[GameSessionController] WinConditionChecker not found! Add it to the scene.");
            if (eggPrefab == null)
                Debug.LogWarning("[GameSessionController] EggPrefab not assigned! Set it in the inspector before playing.");
            if (player1SpawnRoot == null)
                Debug.LogWarning("[GameSessionController] Player1SpawnRoot not assigned!");
            if (player2SpawnRoot == null)
                Debug.LogWarning("[GameSessionController] Player2SpawnRoot not assigned!");
        }

        private void Start()
        {
            BeginGame();
        }

        /// <summary>
        /// 게임을 처음부터 시작한다.
        /// Setup -> Aiming 상태 전환 + GameStarted 이벤트.
        /// </summary>
        public void BeginGame()
        {
            SetState(GameState.Setup);
            ClearAllEggs();
            SpawnAllEggs();
            SetState(GameState.Aiming);
            Debug.Log("[GameSessionController] Game setup complete. Starting match.");
            GameEvents.TriggerGameStarted();
        }

        /// <summary>
        /// 게임을 재시작한다 (결과 화면에서 호출).
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameSessionController] Restarting game...");
            BeginGame();
        }

        private void SpawnAllEggs()
        {
            if (eggPrefab == null)
            {
                Debug.LogError("[GameSessionController] EggPrefab is not assigned!");
                return;
            }

            for (int i = 0; i < settings.eggsPerPlayer; i++)
            {
                SpawnEggForPlayer(1, player1SpawnRoot, i);
                SpawnEggForPlayer(2, player2SpawnRoot, i);
            }

            Debug.Log($"[GameSessionController] Spawned {settings.eggsPerPlayer * 2} eggs ({settings.eggsPerPlayer} per player).");
        }

        private void SpawnEggForPlayer(int playerId, Transform spawnRoot, int index)
        {
            if (spawnRoot == null)
            {
                Debug.LogError($"[GameSessionController] SpawnRoot for Player {playerId} is not assigned!");
                return;
            }

            if (index >= spawnRoot.childCount)
            {
                Debug.LogWarning($"[GameSessionController] SpawnRoot for Player {playerId} has only {spawnRoot.childCount} children, but index {index} requested.");
                return;
            }

            Transform spawnPoint = spawnRoot.GetChild(index);
            GameObject eggObj = Instantiate(eggPrefab, spawnPoint.position, spawnPoint.rotation);
            eggObj.name = $"Egg_P{playerId}_{index + 1}";

            EggController egg = eggObj.GetComponent<EggController>();
            if (egg != null)
            {
                egg.Initialize(playerId);
                egg.transform.position = spawnPoint.position;
                egg.transform.rotation = spawnPoint.rotation;

                // Person A EggController의 인스턴스 이벤트 → GameEvents 브릿지
                egg.Launched += (e) => GameEvents.TriggerEggLaunched(e);
                egg.Fallen += (e) => GameEvents.TriggerEggFell(e);

                allEggs.Add(egg);
                motionResolver?.RegisterEgg(egg);
                winConditionChecker?.RegisterEgg(egg);
            }
            else
            {
                Debug.LogError($"[GameSessionController] EggPrefab missing EggController component!");
                Destroy(eggObj);
            }
        }

        private void ClearAllEggs()
        {
            for (int i = allEggs.Count - 1; i >= 0; i--)
            {
                if (allEggs[i] != null)
                    Destroy(allEggs[i].gameObject);
            }
            allEggs.Clear();
            motionResolver?.ClearEggs();
            winConditionChecker?.ClearEggs();
        }

        private void HandleOnGameEnded(GameResult result)
        {
            SetState(GameState.Result);
            Debug.Log($"[GameSessionController] === Game Over: {result} ===");
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            // Person C: 여기서 UI 업데이트 이벤트를 추가할 수 있음
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Restart Game")]
        private void DebugRestart()
        {
            RestartGame();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUILayout.Label($"State: {currentState}");
            GUILayout.Label($"Turn: Player {turnController?.CurrentPlayerId}");
            GUILayout.Label($"Input Locked: {turnController?.IsInputLocked}");
            GUILayout.Label($"Resolving: {motionResolver?.IsResolving} ({motionResolver?.ResolveTime:F1}s)");

            if (winConditionChecker != null)
            {
                GUILayout.Label($"P1 Alive: {winConditionChecker.GetAliveCount(1)}");
                GUILayout.Label($"P2 Alive: {winConditionChecker.GetAliveCount(2)}");
            }

            if (currentState == GameState.Result && GUILayout.Button("Restart"))
            {
                RestartGame();
            }
        }
#endif
    }
}
