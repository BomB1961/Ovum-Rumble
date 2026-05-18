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
    }
}
