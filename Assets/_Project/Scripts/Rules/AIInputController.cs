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

        public int AiPlayerId => aiPlayerId;
        public bool IsAiEnabled => (featureFlags != null && featureFlags.enableAI) || GameLaunchContext.IsVsComputer;

        public bool IsAiPlayer(int playerId)
        {
            return IsAiEnabled && playerId == aiPlayerId;
        }

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
            isAiTurn = IsAiPlayer(playerId);

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

        // ─── AI 코어 ─────────────────────────────────────

        private void ExecuteAITurn()
        {
            var myEggs = GetAliveEggs(aiPlayerId);
            var enemyEggs = GetAliveEggs(GetOpponentId(aiPlayerId));
            if (myEggs.Count == 0 || enemyEggs.Count == 0) return;

            // 모든 (내 알, 적) 쌍을 평가해서 최적의 샷 선택
            ShotEvaluation best = FindBestShot(myEggs, enemyEggs);
            if (best.egg == null) return;

            Vector3 direction = ComputeDirection(best.egg, best.targetPos);
            best.egg.Launch(direction * best.force);
            Debug.Log($"[AI][{strategy}] P{aiPlayerId} score={best.score:F2} force={best.force:F1}");
        }

        /// <summary>하나의 샷(발사할 알 + 타겟 + 힘) 평가 결과</summary>
        private struct ShotEvaluation
        {
            public EggController egg;
            public Vector3 targetPos;
            public float force;
            public float score;
        }

        /// <summary>모든 가능한 샷을 평가하여 최고 점수 샷 반환</summary>
        private ShotEvaluation FindBestShot(List<EggController> myEggs, List<EggController> enemyEggs)
        {
            ShotEvaluation best = new ShotEvaluation { score = float.MinValue };

            foreach (var egg in myEggs)
            {
                foreach (var enemy in enemyEggs)
                {
                    float score = EvaluateShot(egg, enemy, myEggs, enemyEggs);
                    if (score <= best.score) continue;

                    best.egg = egg;
                    best.targetPos = enemy.transform.position;
                    best.force = ComputeForce(egg, enemy, myEggs, enemyEggs);
                    best.score = score;
                }
            }

            return best;
        }

        /// <summary>특정 (내 알 → 적 타겟) 샷의 점수 계산 (0~1)</summary>
        private float EvaluateShot(EggController egg, EggController target,
            List<EggController> myEggs, List<EggController> enemyEggs)
        {
            Vector3 eggPos = egg.transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 forwardDir = (aiPlayerId == 1) ? Vector3.forward : Vector3.back;
            Vector3 shotDir = (targetPos - eggPos).normalized;
            shotDir.y = 0f;

            // ──── 공격 가치 (0~0.40) ────
            // 타겟이 가장자리에 가깝고(=밀어내기 쉬움), 주변에 다른 적이 없어야(=산란 방지)
            float targetVuln = EdgeDanger(targetPos);
            int nearbyEnemies = CountNearbyAllies(target, enemyEggs);
            float targetIsolation = 1f - Mathf.Clamp01(nearbyEnemies * 0.2f);
            float attackScore = (targetVuln * 0.7f + targetIsolation * 0.3f) * 0.40f;

            // ──── 안전성 (0~0.35) ────
            // 내 알이 가장자리에서 멀고, 발사 경로에 아군이 없을수록 높음
            float eggSafety = 1f - EdgeDanger(eggPos);
            int friendliesInLine = CountFriendliesInLineOfFire(egg, targetPos, myEggs);
            float friendlyPenalty = Mathf.Clamp01(friendliesInLine * 0.35f);
            float safetyScore = (eggSafety * 0.6f + (1f - friendlyPenalty) * 0.4f) * 0.35f;

            // ──── 사질 (0~0.25) ────
            // 정면(적진 방향)을 향하고, 너무 멀지도 가깝지도 않아야 높음
            float aimQuality = Mathf.Clamp01(Vector3.Dot(forwardDir, shotDir));
            float distFactor = Mathf.Clamp01(
                Vector3.Distance(eggPos, targetPos) / (boardHalfSize * 2f));
            float qualityScore = (aimQuality * 0.6f + distFactor * 0.4f) * 0.25f;

            return attackScore + safetyScore + qualityScore;
        }

        /// <summary>발사 경로상에 있는 아군 알 수 (자해 방지)</summary>
        private int CountFriendliesInLineOfFire(EggController from, Vector3 targetPos, List<EggController> friendlies)
        {
            Vector3 fireDir = (targetPos - from.transform.position).normalized;
            fireDir.y = 0f;
            float threshold = 0.85f; // 좁은 각도로 정밀 검사

            return friendlies.Count(e =>
            {
                if (e == from) return false;
                Vector3 toFriendly = (e.transform.position - from.transform.position).normalized;
                toFriendly.y = 0f;
                return Vector3.Dot(fireDir, toFriendly) > threshold;
            });
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

        /// <summary>목표에 맞는 최적의 발사 힘 계산</summary>
        private float ComputeForce(EggController egg, EggController target,
            List<EggController> myEggs, List<EggController> enemyEggs)
        {
            float dist = Vector3.Distance(egg.transform.position, target.transform.position);
            float baseForce = dist * 1.2f;

            // 타겟이 가장자리에 가까우면 약하게 쳐도 넘어감
            float targetEdge = EdgeDanger(target.transform.position);
            baseForce *= Mathf.Lerp(1.3f, 0.6f, targetEdge);

            // 내 알이 가장자리에 가까우면 약하게 (자살 방지)
            float myEdge = EdgeDanger(egg.transform.position);
            baseForce *= Mathf.Lerp(1.0f, 0.5f, myEdge);

            // 발사 경로에 아군이 있으면 약하게 (자해 방지)
            int friendliesInLine = CountFriendliesInLineOfFire(egg, target.transform.position, myEggs);
            baseForce *= Mathf.Lerp(1.0f, 0.35f, Mathf.Clamp01(friendliesInLine * 0.4f));

            // [Power 알 호환] EggController.Launch()에서 powerMultiplier를 곱하므로
            // AI는 나누기로 보정하여 의도한 force 유지 (기본 1.0, 없으면 무시)
            float powerMult = egg.PowerMultiplier;
            if (powerMult > 1.01f)
                baseForce /= powerMult;

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

        /// <summary>특정 위치 주변에 있는 아군/적군 알 수</summary>
        private int CountNearbyAllies(EggController center, List<EggController> allies)
        {
            float radius = boardHalfSize * 0.4f;
            return allies.Count(e => e != center
                                  && Vector3.Distance(center.transform.position, e.transform.position) < radius);
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
