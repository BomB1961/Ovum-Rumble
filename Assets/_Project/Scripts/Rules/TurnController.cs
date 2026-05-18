using UnityEngine;
using DinoAlkkagi.Core;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 턴 순서와 입력 잠금을 관리한다.
    /// 기획서 6-2 턴 진행 규칙을 구현한다.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [SerializeField] private int totalPlayers = 2;
        [SerializeField] private int startingPlayerId = 1;

        private int currentPlayerId;
        private bool isInputLocked = false;

        public int CurrentPlayerId => currentPlayerId;
        public bool IsInputLocked => isInputLocked;
        public int TotalPlayers => totalPlayers;

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleOnGameStarted;
            GameEvents.OnEggLaunched += HandleOnEggLaunched;
            GameEvents.OnAllEggsStopped += HandleOnAllEggsStopped;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleOnGameStarted;
            GameEvents.OnEggLaunched -= HandleOnEggLaunched;
            GameEvents.OnAllEggsStopped -= HandleOnAllEggsStopped;
        }

        private void HandleOnGameStarted()
        {
            currentPlayerId = startingPlayerId;
            isInputLocked = false;
            Debug.Log($"[TurnController] Game started. First turn: Player {currentPlayerId}");
            GameEvents.TriggerTurnStarted(currentPlayerId);
        }

        private void HandleOnEggLaunched(EggController egg)
        {
            // 발사 즉시 입력 잠금 — Resolving 상태로 전환
            isInputLocked = true;
            Debug.Log($"[TurnController] Input locked. Player {currentPlayerId} launched.");
        }

        private void HandleOnAllEggsStopped()
        {
            AdvanceTurn();
        }

        /// <summary>
        /// 다음 플레이어로 턴을 넘긴다.
        /// </summary>
        public void AdvanceTurn()
        {
            currentPlayerId = (currentPlayerId % totalPlayers) + 1;
            isInputLocked = false;
            Debug.Log($"[TurnController] Turn advanced -> Player {currentPlayerId}");
            GameEvents.TriggerTurnStarted(currentPlayerId);
        }

        /// <summary>
        /// 특정 플레이어의 턴인지 확인한다.
        /// </summary>
        public bool IsPlayerTurn(int playerId)
        {
            return currentPlayerId == playerId && !isInputLocked;
        }

        public void LockInput()
        {
            isInputLocked = true;
        }

        public void UnlockInput()
        {
            isInputLocked = false;
        }
    }
}
