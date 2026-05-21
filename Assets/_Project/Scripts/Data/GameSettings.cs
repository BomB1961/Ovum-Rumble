using UnityEngine;

namespace DinoAlkkagi.Data
{
    /// <summary>
    /// 게임 전체 설정값을 Inspector에서 튜닝할 수 있는 ScriptableObject.
    /// Person B의 판정 알고리즘 수치와 Person A의 발사 수치를 포함한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "DinoAlkkagi/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("--- 플레이어 설정 ---")]
        public int totalPlayers = 2;
        public int eggsPerPlayer = 6;

        [Header("--- 정지 판정 ---")]
        [Tooltip("이 속도(m/s) 이하면 '정지'로 간주")]
        public float stopVelocity = 0.08f;
        [Tooltip("모든 알이 stopVelocity 이하 상태를 유지해야 하는 시간(초)")]
        public float stopHoldTime = 1.0f;
        [Tooltip("발사 후 최대 해석 시간(초). 초과 시 강제 정지")]
        public float maxResolveTime = 8f;

        [Header("--- 발사 힘 ---")]
        [Tooltip("드래그 거리에 곱할 계수")]
        public float launchPowerMultiplier = 1.5f;
        public float minLaunchForce = 2f;
        public float maxLaunchForce = 20f;

        [Header("--- 물리 재질 (런타임 오버라이드) ---")]
        public float defaultBounciness = 0.3f;
        public float defaultFriction = 0.4f;

        [Header("--- 보드 크기 (참조용) ---")]
        public float boardSize = 10f;
        public float fallZoneDepth = 2f;

        [Header("--- 절차 맵 생성 ---")]
        [Tooltip("heightfield 해상도 (N x N)")]
        public int mapResolution = 64;
        [Tooltip("펄린 노이즈 스케일 — 작을수록 넓고 완만한 언덕")]
        public float noiseScale = 2f;
        [Tooltip("높이 배율 — PS1 숲 스타일 (1.0~2.0 추천)")]
        public float heightMultiplier = 1.2f;
        [Tooltip("노이즈 옥타브 수 — 낮을수록 매끄러움")]
        public int noiseOctaves = 2;
        [Tooltip("스폰 존 평탄화 반경")]
        public float spawnFlattenRadius = 2.5f;
        [Tooltip("최대 허용 경사 기울기")]
        public float maxSlopeGradient = 0.5f;
        [Tooltip("맵 검증 실패 시 최대 재시도 횟수")]
        public int maxRetryCount = 10;
        [Tooltip("보드 두께 (옆면/밑면 생성용)")]
        public float boardThickness = 1f;
    }
}
