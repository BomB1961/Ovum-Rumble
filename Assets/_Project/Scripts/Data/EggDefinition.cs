using UnityEngine;

namespace DinoAlkkagi.Data
{
    [CreateAssetMenu(fileName = "EggDefinition", menuName = "DinoAlkkagi/EggDefinition")]
    public class EggDefinition : ScriptableObject
    {
        [Header("Identity")]
        public EggType eggType = EggType.Default;

        [Header("Launch")]
        [Min(0f)]
        public float launchImpulseMultiplier = 1f;

        [Header("Physics Expansion")]
        [Min(0f)]
        public float massMultiplier = 1f;
        [Min(0f)]
        public float linearDampingMultiplier = 1f;
        [Min(0f)]
        public float angularDampingMultiplier = 1f;
    }
}
