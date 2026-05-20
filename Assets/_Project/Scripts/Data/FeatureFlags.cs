using UnityEngine;

namespace DinoAlkkagi.Data
{
    /// <summary>
    /// 확장 모듈의 켜고 끔을 관리하는 ScriptableObject.
    /// 모든 확장 기능은 이 플래그가 false여도 기본 게임이 정상 작동해야 한다.
    /// 기획서 10. 확장 모듈 설계 참조.
    /// </summary>
    [CreateAssetMenu(fileName = "FeatureFlags", menuName = "DinoAlkkagi/FeatureFlags")]
    public class FeatureFlags : ScriptableObject
    {
        [Header("--- v0.2 환경 모듈 ---")]
        public bool enableBomb = false;
        public bool enableWind = false;
        public bool enableEarthquake = false;
        public bool enableDinosaurNpc = false;

        [Header("--- v0.3 룰 확장 ---")]
        public bool enableHpSystem = false;
        public bool enableAbilityEggs = false;
        public bool enableHatching = false;
        public bool enable4PlayerMode = false;

        [Header("--- AI ---")]
        public bool enableAI = false;

        [Header("--- v0.4 네트워크 ---")]
        public bool enableLanMultiplayer = false;
        public bool enableLobby = false;

        /// <summary>
        /// 모든 확장 기능을 비활성화한다 (Basic Core 전용 테스트).
        /// </summary>
        public void DisableAllExtensions()
        {
            enableBomb = false;
            enableWind = false;
            enableEarthquake = false;
            enableDinosaurNpc = false;
            enableHpSystem = false;
            enableAbilityEggs = false;
            enableHatching = false;
            enable4PlayerMode = false;
            enableLanMultiplayer = false;
            enableLobby = false;
        }
    }
}
