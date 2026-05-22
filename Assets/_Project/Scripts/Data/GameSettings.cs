using UnityEngine;

namespace DinoAlkkagi.Data
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "DinoAlkkagi/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Players")]
        public int totalPlayers = 2;
        public int eggsPerPlayer = 6;

        [Header("Motion")]
        public float stopVelocity = 0.08f;
        public float stopHoldTime = 1.0f;
        public float maxResolveTime = 8f;

        [Header("Launch")]
        public float launchPowerMultiplier = 1.5f;
        public float minLaunchForce = 2f;
        public float maxLaunchForce = 20f;

        [Header("Physics Materials")]
        public float defaultBounciness = 0.3f;
        public float defaultFriction = 0.4f;

        [Header("Resolve Timing")]
        public float resolveTimeScale = 2.5f;

        [Header("Board Reference")]
        public float boardSize = 10f;
        public float fallZoneDepth = 2f;
    }
}
