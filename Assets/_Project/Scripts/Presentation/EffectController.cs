using System.Collections;
using System.Collections.Generic;
using DinoAlkkagi.Core;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public class EffectController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EggSpawner eggSpawner;

        [Header("Particle Prefabs")]
        [SerializeField] private ParticleSystem collisionDustPrefab;
        [SerializeField] private ParticleSystem fallEffectPrefab;

        [Header("Collision Settings")]
        [SerializeField] private float minImpactForEffect = 1f;
        [SerializeField] private float maxImpactForScale = 12f;
        [SerializeField] private float minParticleScale = 0.5f;
        [SerializeField] private float maxParticleScale = 1.5f;
        [SerializeField] private int maxPoolSize = 16;

        [Header("Effect Lifespan")]
        [SerializeField] private float effectLifetime = 3f;

        private readonly List<ParticleSystem> collisionPool = new List<ParticleSystem>();
        private readonly List<ParticleSystem> fallPool = new List<ParticleSystem>();
        private readonly Dictionary<ParticleSystem, Coroutine> deactivationCoroutines =
            new Dictionary<ParticleSystem, Coroutine>();
        private int collisionPoolIndex;
        private int fallPoolIndex;

        private readonly List<EggController> subscribedEggs = new List<EggController>();

        private void Awake()
        {
            InitializePool(collisionPool, collisionDustPrefab, maxPoolSize);
            InitializePool(fallPool, fallEffectPrefab, Mathf.Max(1, maxPoolSize / 2));
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleGameStarted;
            GameEvents.OnEggFell += HandleEggFell;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleGameStarted;
            GameEvents.OnEggFell -= HandleEggFell;
            UnsubscribeAllEggs();
            StopAllCoroutines();
            deactivationCoroutines.Clear();
        }

        private void HandleGameStarted()
        {
            SubscribeToEggs();
        }

        private void SubscribeToEggs()
        {
            UnsubscribeAllEggs();

            if (eggSpawner == null) return;

            foreach (EggController egg in eggSpawner.SpawnedEggs)
            {
                if (egg == null) continue;
                egg.CollisionOccurred += HandleEggCollisionWithPosition;
                subscribedEggs.Add(egg);
            }
        }

        private void UnsubscribeAllEggs()
        {
            foreach (EggController egg in subscribedEggs)
            {
                if (egg != null)
                {
                    egg.CollisionOccurred -= HandleEggCollisionWithPosition;
                }
            }
            subscribedEggs.Clear();
        }

        private void InitializePool(List<ParticleSystem> pool, ParticleSystem prefab, int count)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                ParticleSystem instance = Instantiate(prefab, transform);
                instance.gameObject.SetActive(false);
                pool.Add(instance);
            }
        }

        private void HandleEggCollisionWithPosition(EggController egg, float impact)
        {
            if (collisionDustPrefab == null) return;
            if (impact < minImpactForEffect) return;

            float t = Mathf.Clamp01((impact - minImpactForEffect) / (maxImpactForScale - minImpactForEffect));
            float scale = Mathf.Lerp(minParticleScale, maxParticleScale, t);

            ParticleSystem effect = GetNextFromPool(collisionPool, ref collisionPoolIndex);
            if (effect == null) return;

            effect.transform.position = egg.transform.position;
            effect.transform.localScale = Vector3.one * scale;
            effect.gameObject.SetActive(true);
            effect.Play();

            ScheduleDeactivation(effect);
        }

        private void HandleEggFell(EggController egg)
        {
            if (fallEffectPrefab == null || egg == null) return;

            ParticleSystem effect = GetNextFromPool(fallPool, ref fallPoolIndex);
            if (effect == null) return;

            effect.transform.position = egg.transform.position;
            effect.transform.localScale = Vector3.one;
            effect.gameObject.SetActive(true);
            effect.Play();

            ScheduleDeactivation(effect);
        }

        private ParticleSystem GetNextFromPool(List<ParticleSystem> pool, ref int index)
        {
            if (pool.Count == 0) return null;

            ParticleSystem effect = pool[index];
            index = (index + 1) % pool.Count;
            return effect;
        }

        private void ScheduleDeactivation(ParticleSystem effect)
        {
            if (deactivationCoroutines.TryGetValue(effect, out Coroutine pendingCoroutine))
            {
                StopCoroutine(pendingCoroutine);
            }

            deactivationCoroutines[effect] = StartCoroutine(DeactivateAfterDelay(effect, effectLifetime));
        }

        private IEnumerator DeactivateAfterDelay(ParticleSystem effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                effect.gameObject.SetActive(false);
                deactivationCoroutines.Remove(effect);
            }
        }
    }
}
