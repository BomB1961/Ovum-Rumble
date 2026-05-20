using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// AI 상대 입력 컨트롤러.
    /// FlickInputController와 동일한 GameEvents.TriggerEggLaunched()를 사용하므로
    /// TurnController/MotionResolver/WinConditionChecker 등 기존 시스템과 완전 호환됨.
    /// 
    /// 멀티 대비: 나중에 NetworkInputController로 교체할 때
    /// FeatureFlags.enableAI = false, enableLanMultiplayer = true만 바꾸면 됨.
    /// </summary>
    public class AIInputController : MonoBehaviour
    {
        [Header("--- AI 설정 ---")]
        [SerializeField] private int aiPlayerId = 2;
        [SerializeField] private float decisionDelay = 0.5f;
        [SerializeField] private float aimNoise = 0.15f;
        [SerializeField] private float minForce = 2f;
        [SerializeField] private float maxForce = 12f;

        [Header("--- FeatureFlags ---")]
        [SerializeField] private FeatureFlags featureFlags;

        private List<EggController> trackedEggs = new List<EggController>();
        private bool isAiTurn = false;
        private float timer = 0f;
        private bool decisionMade = false;

        private void OnEnable()
        {
            GameEvents.OnTurnStarted += HandleOnTurnStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted -= HandleOnTurnStarted;
        }

        private void HandleOnTurnStarted(int playerId)
        {
            // AI 모드 체크
            bool aiEnabled = featureFlags != null && featureFlags.enableAI;
            isAiTurn = aiEnabled && playerId == aiPlayerId;

            if (isAiTurn)
            {
                timer = decisionDelay;
                decisionMade = false;
            }
        }

        private void Update()
        {
            if (!isAiTurn || decisionMade) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                ExecuteAITurn();
                decisionMade = true;
                isAiTurn = false;
            }
        }

        private void ExecuteAITurn()
        {
            // 살아있는 내 알
            var myEggs = trackedEggs.Where(e => e != null && e.IsAlive && e.OwnerPlayerId == aiPlayerId).ToList();
            if (myEggs.Count == 0) return;

            // 살아있는 적 알
            var targetEggs = trackedEggs.Where(e => e != null && e.IsAlive && e.OwnerPlayerId != aiPlayerId).ToList();

            // 1. 알 선택: 중앙에서 가장 먼 알 우선 (공격적으로)
            EggController selectedEgg = myEggs
                .OrderByDescending(e => e.transform.position.magnitude)
                .First();

            // 2. 타겟: 가장 가까운 적 알
            Vector3 targetPos;
            if (targetEggs.Count > 0)
            {
                targetPos = targetEggs
                    .OrderBy(e => Vector3.Distance(selectedEgg.transform.position, e.transform.position))
                    .First().transform.position;
            }
            else
            {
                // 적이 없으면 중앙으로 (밀어내기)
                targetPos = Vector3.zero;
            }

            // 3. 발사 방향 (드래그 반대 방향 모방)
            Vector3 direction = selectedEgg.transform.position - targetPos;
            direction.y = 0f;
            if (direction.magnitude < 0.01f)
                direction = Random.insideUnitSphere;
            direction.Normalize();

            // 4. 노이즈 (완벽 조준 방지)
            direction += Random.insideUnitSphere * aimNoise;
            direction.y = 0f;
            direction.Normalize();

            // 5. 힘 (거리 비례 + 랜덤)
            float dist = Vector3.Distance(selectedEgg.transform.position, targetPos);
            float force = Mathf.Clamp(dist * 1.5f, minForce, maxForce);

            // 6. 발사 — FlickInputController와 동일한 이벤트 사용
            selectedEgg.Launch(direction * force);
            GameEvents.TriggerEggLaunched(selectedEgg);

            Debug.Log($"[AI] P{aiPlayerId} fired at {targetPos} (force: {force:F1})");
        }

        public void RegisterEggs(IEnumerable<EggController> eggs)
        {
            trackedEggs.Clear();
            trackedEggs.AddRange(eggs);
        }

        public void ClearEggs()
        {
            trackedEggs.Clear();
        }
    }
}
