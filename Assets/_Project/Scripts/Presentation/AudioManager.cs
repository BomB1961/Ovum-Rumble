using System.Collections.Generic;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public class AudioManager : MonoBehaviour
    {
        public const string BgmVolumePrefsKey = "BGMVolume";
        public const string SfxVolumePrefsKey = "SFXVolume";

        [Header("Settings")]
        [SerializeField] private Data.AudioSettings settings;

        [Header("Audio Mixer Groups")]
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup masterGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup bgmGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;
        [SerializeField] private string bgmVolumeParameter = "BGMVolume";
        [SerializeField] private string sfxVolumeParameter = "SFXVolume";

        [Header("BGM")]
        [SerializeField] private AudioClip bgmClip;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip launchClip;
        [SerializeField] private AudioClip collisionClip;
        [SerializeField] private AudioClip fallClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] private AudioClip turnStartClip;

        private AudioSource bgmSource;
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private int sfxPoolIndex;
        private float bgmBaseVolume;
        private float sfxFallbackVolumeMultiplier = 1f;

        private void Awake()
        {
            SetupBgmSource();
            SetupSfxPool();
            ApplySavedVolumes();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleGameStarted;
            GameEvents.OnTurnStarted += HandleTurnStarted;
            GameEvents.OnEggLaunched += HandleEggLaunched;
            GameEvents.OnEggCollision += HandleEggCollision;
            GameEvents.OnEggFell += HandleEggFell;
            GameEvents.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleGameStarted;
            GameEvents.OnTurnStarted -= HandleTurnStarted;
            GameEvents.OnEggLaunched -= HandleEggLaunched;
            GameEvents.OnEggCollision -= HandleEggCollision;
            GameEvents.OnEggFell -= HandleEggFell;
            GameEvents.OnGameEnded -= HandleGameEnded;
        }

        private void SetupBgmSource()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.outputAudioMixerGroup = bgmGroup;
            bgmBaseVolume = settings != null ? settings.bgmVolume : 0.4f;
            bgmSource.volume = bgmBaseVolume;
        }

        private void SetupSfxPool()
        {
            int poolSize = settings != null ? settings.sfxPoolSize : 8;
            for (int i = 0; i < poolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = sfxGroup;
                sfxPool.Add(source);
            }
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null) return;

            AudioSource source = GetNextSfxSource();
            source.clip = clip;
            source.volume = GetSfxSourceVolume(volume);
            source.pitch = 1f;
            source.Play();
        }

        private void PlaySfxWithRandomPitch(AudioClip clip, float volume, float pitchMin, float pitchMax)
        {
            if (clip == null) return;

            AudioSource source = GetNextSfxSource();
            source.clip = clip;
            source.volume = GetSfxSourceVolume(volume);
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.Play();
        }

        private AudioSource GetNextSfxSource()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                int index = (sfxPoolIndex + i) % sfxPool.Count;
                AudioSource availableSource = sfxPool[index];
                if (!availableSource.isPlaying)
                {
                    sfxPoolIndex = (index + 1) % sfxPool.Count;
                    return availableSource;
                }
            }

            AudioSource sourceToReplace = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Count;
            return sourceToReplace;
        }

        private float GetSfxSourceVolume(float clipVolume)
        {
            return UsesSfxMixer() ? clipVolume : clipVolume * sfxFallbackVolumeMultiplier;
        }

        private bool UsesSfxMixer()
        {
            return sfxGroup != null && sfxGroup.audioMixer != null;
        }

        private void HandleGameStarted()
        {
            if (bgmClip != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        private void HandleTurnStarted(int playerId)
        {
            float volume = settings != null ? settings.turnStartVolume : 0.4f;
            PlaySfxWithRandomPitch(turnStartClip, volume, 0.9f, 1.1f);
        }

        private void HandleEggLaunched(EggController egg)
        {
            float volume = settings != null ? settings.launchVolume : 0.6f;
            PlaySfxWithRandomPitch(launchClip, volume, 0.85f, 1.15f);
        }

        private void HandleEggCollision(float impact)
        {
            if (settings != null)
            {
                float t = Mathf.Clamp01(impact / settings.maxImpactForVolume);
                float volume = Mathf.Lerp(0.1f, settings.sfxVolume, t);
                PlaySfxWithRandomPitch(collisionClip, volume, 0.8f, 1.2f);
            }
            else
            {
                PlaySfxWithRandomPitch(collisionClip, 0.5f, 0.8f, 1.2f);
            }
        }

        private void HandleEggFell(EggController egg)
        {
            float volume = settings != null ? settings.fallVolume : 0.8f;
            PlaySfx(fallClip, volume);
        }

        private void HandleGameEnded(GameResult result)
        {
            if (settings == null)
            {
                PlaySfx(winClip, 0.7f);
                return;
            }

            switch (result)
            {
                case GameResult.Player1Win:
                case GameResult.Player2Win:
                    PlaySfx(winClip, settings.winVolume);
                    break;
                case GameResult.Draw:
                    PlaySfx(loseClip, settings.loseVolume);
                    break;
            }
        }

        public void SetBGMVolume(float normalizedVolume)
        {
            normalizedVolume = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(BgmVolumePrefsKey, normalizedVolume);
            PlayerPrefs.Save();

            if (bgmGroup == null || bgmGroup.audioMixer == null)
            {
                if (bgmSource != null)
                {
                    bgmSource.volume = bgmBaseVolume * normalizedVolume;
                }
                return;
            }

            float db = normalizedVolume > 0.0001f
                ? 20f * Mathf.Log10(normalizedVolume)
                : -80f;
            bgmGroup.audioMixer.SetFloat(bgmVolumeParameter, db);
        }

        public void SetSFXVolume(float normalizedVolume)
        {
            normalizedVolume = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(SfxVolumePrefsKey, normalizedVolume);
            PlayerPrefs.Save();

            sfxFallbackVolumeMultiplier = normalizedVolume;

            if (!UsesSfxMixer())
            {
                return;
            }

            float db = normalizedVolume > 0.0001f
                ? 20f * Mathf.Log10(normalizedVolume)
                : -80f;
            sfxGroup.audioMixer.SetFloat(sfxVolumeParameter, db);
        }

        private void ApplySavedVolumes()
        {
            SetBGMVolume(PlayerPrefs.GetFloat(BgmVolumePrefsKey, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(SfxVolumePrefsKey, 1f));
        }
    }
}
