using UnityEngine;

namespace DinoAlkkagi.Data
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "DinoAlkkagi/AudioSettings")]
    public class AudioSettings : ScriptableObject
    {
        [Header("--- BGM ---")]
        [Range(0f, 1f)]
        public float bgmVolume = 0.4f;

        [Header("--- SFX ---")]
        [Range(0f, 1f)]
        public float sfxVolume = 0.7f;
        [Tooltip("충돌 impact가 이 값일 때 최대 볼륨")]
        public float maxImpactForVolume = 10f;
        [Range(0f, 1f)]
        public float launchVolume = 0.6f;
        [Range(0f, 1f)]
        public float fallVolume = 0.8f;
        [Range(0f, 1f)]
        public float winVolume = 0.7f;
        [Range(0f, 1f)]
        public float loseVolume = 0.5f;
        [Range(0f, 1f)]
        public float turnStartVolume = 0.4f;

        [Header("--- Pooling ---")]
        [Tooltip("동시 재생 가능한 최대 SFX AudioSource 수")]
        public int sfxPoolSize = 8;
    }
}
