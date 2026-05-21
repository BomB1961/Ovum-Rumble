using System.Collections;
using System.Collections.Generic;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public class BombEventController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FeatureFlags featureFlags;
        [SerializeField] private EggSpawner eggSpawner;

        [Header("Bomb Settings")]
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionForce = 15f;
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private int spawnIntervalTurns = 4;
        [SerializeField] private Vector3 bombSpawnCenter = Vector3.zero;
        [SerializeField] private float bombSpawnRadius = 2f;

        [Header("Explosion Effect")]
        [SerializeField] private ParticleSystem explosionPrefab;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private float explosionVolume = 0.9f;

        private int turnCounter;
        private readonly List<GameObject> activeBombs = new List<GameObject>();

        private void OnEnable()
        {
            if (featureFlags == null || !featureFlags.enableBomb) return;

            GameEvents.OnTurnStarted += HandleTurnStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted -= HandleTurnStarted;
        }

        private void HandleTurnStarted(int playerId)
        {
            turnCounter++;
            if (turnCounter < spawnIntervalTurns) return;

            turnCounter = 0;
            SpawnBomb();
        }

        private void SpawnBomb()
        {
            if (bombPrefab == null) return;

            Vector2 randomCircle = Random.insideUnitCircle * bombSpawnRadius;
            Vector3 spawnPos = bombSpawnCenter + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity, transform);
            activeBombs.Add(bomb);

            StartCoroutine(CountdownAndExplode(bomb));
        }

        private IEnumerator CountdownAndExplode(GameObject bomb)
        {
            yield return new WaitForSeconds(countdownDuration);

            if (bomb == null) yield break;

            Vector3 bombPos = bomb.transform.position;
            activeBombs.Remove(bomb);
            Destroy(bomb);

            ExplodeAt(bombPos);
        }

        private void ExplodeAt(Vector3 position)
        {
            Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);
            HashSet<Rigidbody> processedBodies = new HashSet<Rigidbody>();

            foreach (Collider hit in hitColliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb == null || processedBodies.Contains(rb)) continue;

                processedBodies.Add(rb);

                Vector3 direction = rb.position - position;
                float distance = direction.magnitude;
                if (distance < 0.01f) distance = 0.01f;

                float forceFactor = 1f - (distance / explosionRadius);
                forceFactor = Mathf.Clamp01(forceFactor);

                Vector3 force = direction.normalized * (explosionForce * forceFactor);
                force.y = Mathf.Max(force.y, 2f * forceFactor);
                rb.AddForce(force, ForceMode.Impulse);
            }

            if (explosionPrefab != null)
            {
                ParticleSystem effect = Instantiate(explosionPrefab, position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
            }

            if (explosionClip != null)
            {
                GameObject tempAudio = new GameObject("ExplosionSFX");
                tempAudio.transform.position = position;
                AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                audioSource.clip = explosionClip;
                audioSource.volume = explosionVolume;
                audioSource.Play();
                Destroy(tempAudio, explosionClip.length);
            }
        }
    }
}
