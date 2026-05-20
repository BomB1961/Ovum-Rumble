using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    public enum AIStrategy
    {
        Basic,       // Level 1: 중앙 알 → 가까운 적 (지금)
        Defensive,   // Level 2: 위험한 내 알 보호 우선
        Aggressive,  // Level 2: 위험한 적 밀어내기 우선
        Smart        // Level 2: 상황 판단 혼합
    }

    /// <summary>
    /// AI 상대 입력 컨트롤러 (Level 2).
    /// FlickInputController와 동일한 이벤트 인터페이스 사용.
    /// 멀티 대비: FeatureFlags.enableAI → enableLanMultiplayer 전환 가능.
    /// </summary>
    public class AIInputController : MonoBehaviour
    {
        [Header("--- AI 전략 ---")]
        [SerializeField] private AIStrategy strategy = AIStrategy.Smart;

        [Header("--- AI 설정 ---")]
        [SerializeField] private int aiPlayerId = 2;
        [SerializeField] private float decisionDelay = 0.5f;
        [SerializeField] private float aimNoise = 0.12f;
        [SerializeField] private float minForce = 2f;
        [SerializeField] private float maxForce = 14f;
        [SerializeField] private float boardHalfSize = 4f; // 보드 절반 크기 (Prototype Board Scale 8 기준)

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

        // ─── Level 2 AI 코어 ─────────────────────────────────

        private void ExecuteAITurn()
        {
            var myEggs   = GetAliveEggs(aiPlayerId);
            var enemyEggs = GetAliveEggs(GetOpponentId(aiPlayerId));
            if (myEggs.Count == 0) return;

            // 1. 알 선택
            EggController selectedEgg = PickEgg(myEggs, enemyEggs);

            // 2. 타겟 위치 결정
            Vector3 targetPos = PickTarget(selectedEgg, enemyEggs);

            // 3. 발사 방향
            Vector3 direction = ComputeDirection(selectedEgg, targetPos);

            // 4. 발사 힘
            float force = ComputeForce(selectedEgg, targetPos, enemyEggs);

            // 5. 발사
            selectedEgg.Launch(direction * force);
            Debug.Log($"[AI][{strategy}] P{aiPlayerId} → {targetPos} (force: {force:F1})");
        }

        // ─── 알 선택 ──────────────────────────────────────────

        private EggController PickEgg(List<EggController> myEggs, List<EggController> enemyEggs)
        {
            return strategy switch
            {
                AIStrategy.Basic => PickBasicEgg(myEggs),
                AIStrategy.Defensive => PickDefensiveEgg(myEggs, enemyEggs),
                AIStrategy.Aggressive => PickAggressiveEgg(myEggs, enemyEggs),
                AIStrategy.Smart => PickSmartEgg(myEggs, enemyEggs),
                _ => PickBasicEgg(myEggs)
            };
        }

        private EggController PickBasicEgg(List<EggController> myEggs)
        {
            // 중앙에 가장 가까운 알 (안전)
            return myEggs.OrderBy(e => e.transform.position.magnitude).First();
        }

        private EggController PickDefensiveEdgeEgg(List<EggController> myEggs)
        {
            // 가장자리 위험도가 높은 알 우선 (위험한 알 먼저 보호)
            return myEggs.OrderByDescending(e => EdgeDanger(e.transform.position)).First();
        }

        private EggController PickDefensiveEgg(List<EggController> myEggs, List<EggController> enemyEggs)
        {
            if (enemyEggs.Count == 0) return PickBasicEgg(myEggs);

            // 가장자리에 있으면서 적과 가까운 알 우선
            return myEggs
                .OrderByDescending(e => EdgeDanger(e.transform.position) * 0.7f
                                      + ClosestEnemyDist(e, enemyEggs) * 0.3f)
                .First();
        }

        private EggController PickAggressiveEgg(List<EggController> myEggs, List<EggController> enemyEggs)
        {
            if (enemyEggs.Count == 0) return PickBasicEgg(myEggs);

            // 중앙에 있으면서 적과 일직선상에 있는 알
            return myEggs
                .OrderBy(e => e.transform.position.magnitude) // 중앙 우선
                .ThenByDescending(e => CountEnemiesInLine(e, enemyEggs)) // 일직선 적 많을수록
                .First();
        }

        private EggController PickSmartEgg(List<EggController> myEggs, List<EggController> enemyEggs)
        {
            if (enemyEggs.Count == 0) return PickBasicEgg(myEggs);

            // 점수 기반: 안전함 + 공격 기회
            return myEggs
                .OrderByDescending(e =>
                {
                    float edgeScore = 1f - EdgeDanger(e.transform.position); // 0~1, 1=안전
                    float attackScore = CountEnemiesInLine(e, enemyEggs) * 0.2f; // 공격 기회
                    float dangerScore = EdgeDanger(e.transform.position) * 0.5f; // 위험할수록 먼저

                    return edgeScore + attackScore + dangerScore;
                })
                .First();
        }

        // ─── 타겟팅 ──────────────────────────────────────────

        private Vector3 PickTarget(EggController selectedEgg, List<EggController> enemyEggs)
        {
            if (enemyEggs.Count == 0) return Vector3.zero; // 중앙

            return strategy switch
            {
                AIStrategy.Basic => TargetNearest(selectedEgg, enemyEggs),
                AIStrategy.Defensive => TargetNearest(selectedEgg, enemyEggs),
                AIStrategy.Aggressive => TargetMostVulnerable(selectedEgg, enemyEggs),
                AIStrategy.Smart => TargetSmart(selectedEgg, enemyEggs),
                _ => TargetNearest(selectedEgg, enemyEggs)
            };
        }

        private Vector3 TargetNearest(EggController from, List<EggController> enemies)
        {
            return enemies
                .OrderBy(e => Vector3.Distance(from.transform.position, e.transform.position))
                .First().transform.position;
        }

        private Vector3 TargetMostVulnerable(EggController from, List<EggController> enemies)
        {
            // 가장자리에 가깝고, 밀어내기 쉬운 적 우선
            return enemies
                .OrderByDescending(e => EdgeDanger(e.transform.position))
                .First().transform.position;
        }

        private Vector3 TargetSmart(EggController from, List<EggController> enemies)
        {
            // 밀집 지역 우선 + 가장자리 적 우선
            return enemies
                .OrderByDescending(e => EdgeDanger(e.transform.position) * 0.6f
                                       + CountNearbyAllies(e, enemies) * 0.1f
                                       + CountNearbyAllies(e, GetAliveEggs(aiPlayerId)) * 0.1f) // 내 알도 많으면 위험
                .First().transform.position;
        }

        // ─── 방향 계산 ────────────────────────────────────────

        private Vector3 ComputeDirection(EggController from, Vector3 targetPos)
        {
            Vector3 dir = targetPos - from.transform.position;
            dir.y = 0f;

            if (dir.magnitude < 0.01f)
                dir = Random.insideUnitSphere;

            dir.Normalize();

            // 노이즈 (완벽 조준 방지)
            dir += (Vector3)Random.insideUnitCircle * aimNoise;
            dir.y = 0f;
            dir.Normalize();

            return dir;
        }

        // ─── 힘 계산 ──────────────────────────────────────────

        private float ComputeForce(EggController selectedEgg, Vector3 targetPos, List<EggController> enemyEggs)
        {
            float dist = Vector3.Distance(selectedEgg.transform.position, targetPos);
            float myEdgeDanger = EdgeDanger(selectedEgg.transform.position);
            float baseForce = dist * 1.5f;

            // 전략별 힘 조정
            switch (strategy)
            {
                case AIStrategy.Defensive:
                    // 내가 위험하면 약하게 (자살 방지)
                    baseForce *= Mathf.Lerp(1.2f, 0.6f, myEdgeDanger);
                    break;

                case AIStrategy.Aggressive:
                    // 무조건 강하게
                    baseForce *= 1.3f;
                    break;

                case AIStrategy.Smart:
                    // 적이 위험하면 강하게, 내가 위험하면 약하게
                    float enemyEdge = enemyEggs.Count > 0
                        ? enemyEggs.Max(e => EdgeDanger(e.transform.position))
                        : 0f;
                    float powerRatio = Mathf.Lerp(0.8f, 1.4f, enemyEdge) * Mathf.Lerp(1.0f, 0.7f, myEdgeDanger);
                    baseForce *= powerRatio;
                    break;
            }

            return Mathf.Clamp(baseForce * Random.Range(0.85f, 1.15f), minForce, maxForce);
        }

        // ─── 유틸리티 ─────────────────────────────────────────

        private int GetOpponentId(int playerId) => playerId == 1 ? 2 : 1;

        private List<EggController> GetAliveEggs(int playerId)
        {
            return trackedEggs.Where(e => e != null && e.IsAlive && e.OwnerPlayerId == playerId).ToList();
        }

        /// <summary>가장자리 위험도 (0~1, 1=바로 떨어질 위험)</summary>
        private float EdgeDanger(Vector3 pos)
        {
            float dangerX = 1f - Mathf.Abs(pos.x) / boardHalfSize;
            float dangerZ = 1f - Mathf.Abs(pos.z) / boardHalfSize;
            return 1f - Mathf.Min(dangerX, dangerZ);
        }

        /// <summary>특정 알에서 가장 가까운 적까지의 거리 (0~1 정규화)</summary>
        private float ClosestEnemyDist(EggController egg, List<EggController> enemies)
        {
            if (enemies.Count == 0) return 1f;
            float minDist = enemies.Min(e => Vector3.Distance(egg.transform.position, e.transform.position));
            return 1f - Mathf.Clamp01(minDist / (boardHalfSize * 2f));
        }

        /// <summary>특정 위치 주변에 있는 아군/적군 알 수</summary>
        private int CountNearbyAllies(EggController center, List<EggController> allies)
        {
            float radius = boardHalfSize * 0.4f;
            return allies.Count(e => e != center
                                  && Vector3.Distance(center.transform.position, e.transform.position) < radius);
        }

        /// <summary>선택한 알의 발사 방향에 있는 적 수 (일직선 판단)</summary>
        private int CountEnemiesInLine(EggController egg, List<EggController> enemies)
        {
            Vector3 toCenter = (Vector3.zero - egg.transform.position).normalized;
            float threshold = 0.7f; // 방향 일치 임계값

            return enemies.Count(e =>
            {
                Vector3 toEnemy = (e.transform.position - egg.transform.position).normalized;
                return Vector3.Dot(toCenter, toEnemy) > threshold;
            });
        }

        // ─── 외부 등록 ────────────────────────────────────────

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
