using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 각 플레이어의 생존 알 수를 추적하고 승패를 판정한다.
    /// 기획서 6-5 승패 판정 알고리즘을 구현한다.
    /// </summary>
    public class WinConditionChecker : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;

        private List<EggController> trackedEggs = new List<EggController>();
        private bool gameEnded = false;

        public bool GameEnded => gameEnded;

        private void OnEnable()
        {
            GameEvents.OnAllEggsStopped += HandleOnAllEggsStopped;
            GameEvents.OnGameStarted += HandleOnGameStarted;
            GameEvents.OnEggFell += HandleOnEggFell;
        }

        private void OnDisable()
        {
            GameEvents.OnAllEggsStopped -= HandleOnAllEggsStopped;
            GameEvents.OnGameStarted -= HandleOnGameStarted;
            GameEvents.OnEggFell -= HandleOnEggFell;
        }

        private void HandleOnGameStarted()
        {
            gameEnded = false;
        }

        private void HandleOnEggFell(EggController egg)
        {
            // 알이 떨어질 때마다 승패 체크 (바로 반응)
            CheckWinCondition();
        }

        private void HandleOnAllEggsStopped()
        {
            // 모든 알이 멈췄을 때도 한 번 더 체크 (안전장치)
            if (!gameEnded)
                CheckWinCondition();
        }

        /// <summary>
        /// [6-5] 승패 판정 알고리즘:
        /// 각 플레이어의 생존 알 수를 계산하고 0이면 승리 처리한다.
        /// </summary>
        public void CheckWinCondition()
        {
            if (gameEnded) return;

            int p1Alive = GetAliveCount(1);
            int p2Alive = GetAliveCount(2);

            // 아직 시작 전이거나 알이 아직 등록되지 않은 경우 체크 스킵
            if (p1Alive == 0 && p2Alive == 0)
            {
                int totalEggs = trackedEggs.Count;
                if (totalEggs < settings.totalPlayers * settings.eggsPerPlayer)
                    return; // 아직 알이 다 등록되지 않음
            }

            if (p1Alive == 0 && p2Alive > 0)
            {
                gameEnded = true;
                Debug.Log($"[WinConditionChecker] Player 2 wins! (P1 eggs: {p1Alive}, P2 eggs: {p2Alive})");
                GameEvents.TriggerGameEnded(GameResult.Player2Win);
            }
            else if (p2Alive == 0 && p1Alive > 0)
            {
                gameEnded = true;
                Debug.Log($"[WinConditionChecker] Player 1 wins! (P1 eggs: {p1Alive}, P2 eggs: {p2Alive})");
                GameEvents.TriggerGameEnded(GameResult.Player1Win);
            }
            else if (p1Alive == 0 && p2Alive == 0)
            {
                // 동시에 전멸 (매우 드문 경우)
                gameEnded = true;
                Debug.Log("[WinConditionChecker] Draw! Both players eliminated.");
                GameEvents.TriggerGameEnded(GameResult.Draw);
            }
            // else: 아직 승패 안 남
        }

        public int GetAliveCount(int playerId)
        {
            return trackedEggs.Count(e => e != null && e.OwnerPlayerId == playerId && !e.IsFallen);
        }

        public void RegisterEgg(EggController egg)
        {
            if (egg != null && !trackedEggs.Contains(egg))
                trackedEggs.Add(egg);
        }

        public void UnregisterEgg(EggController egg)
        {
            trackedEggs.Remove(egg);
        }

        public void ClearEggs()
        {
            trackedEggs.Clear();
        }
    }
}
