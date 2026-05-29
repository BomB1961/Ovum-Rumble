using System.Collections;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using DinoAlkkagi.Rules;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public class DinosaurEventController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FeatureFlags featureFlags;
        [SerializeField] private TurnController turnController;
        [SerializeField] private StaticBoardLoader boardLoader;

        [Header("Dinosaur Prefabs")]
        [SerializeField] private GameObject[] dinoPrefabs;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnChance = 0.2f;
        [SerializeField] private float spawnHeight = 1.5f;
        [SerializeField] private float spawnInset = 3f;

        [Header("Walk Settings")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float maxWalkDuration = 8f;

        [Header("Layer")]
        [SerializeField] private string dinoLayerName = "Dinosaur";

        private IBoardSurface boardSurface;
        private bool isBetweenTurnsActive;

        private void Awake()
        {
            turnController ??= FindFirstObjectByType<TurnController>();
            boardLoader ??= FindFirstObjectByType<StaticBoardLoader>();
            if (boardLoader != null)
            {
                boardSurface = boardLoader.BoardSurface;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnAllEggsStopped += HandleAllEggsStopped;
        }

        private void OnDisable()
        {
            GameEvents.OnAllEggsStopped -= HandleAllEggsStopped;
        }

        private void HandleAllEggsStopped()
        {
            if (isBetweenTurnsActive) return;
            StartCoroutine(HandleBetweenTurns());
        }

        private IEnumerator HandleBetweenTurns()
        {
            isBetweenTurnsActive = true;

            try
            {
                yield return null;

                GameEvents.TriggerBetweenTurns();

                if (featureFlags != null && featureFlags.enableDinosaurNpc && Random.value < spawnChance)
                {
                    turnController?.LockInput();
                    try
                    {
                        yield return StartCoroutine(SpawnAndWalk());
                    }
                    finally
                    {
                        turnController?.UnlockInput();
                    }

                    yield return StartCoroutine(WaitForEggsToSettle());
                }

                GameEvents.TriggerBetweenTurnsEnded();
            }
            finally
            {
                isBetweenTurnsActive = false;
            }
        }

        private IEnumerator SpawnAndWalk()
        {
            if (dinoPrefabs == null || dinoPrefabs.Length == 0 || boardSurface == null)
            {
                yield break;
            }

            GameObject prefab = dinoPrefabs[Random.Range(0, dinoPrefabs.Length)];
            int dinoLayer = LayerMask.NameToLayer(dinoLayerName);
            Bounds bounds = boardSurface.GetPlayableBounds();

            DinosaurWalker walker = prefab.GetComponent<DinosaurWalker>();
            float forwardAngle = walker != null ? walker.ForwardAngle : 0f;
            float dinoWalkSpeed = walker != null ? walker.WalkSpeed : walkSpeed;
            float dinoAnimSpeed = walker != null ? walker.AnimatorSpeed : 1f;

            bool useXAxis = Random.value > 0.5f;
            bool goingPositive = Random.value > 0.5f;
            Vector3 direction;
            Vector3 spawnPos;

            if (useXAxis)
            {
                float spawnX = goingPositive ? bounds.min.x + spawnInset : bounds.max.x - spawnInset;
                float safeZ = Mathf.Max(bounds.extents.z - spawnInset, 0f);
                float spawnZ = Random.Range(bounds.center.z - safeZ, bounds.center.z + safeZ);
                float groundY = boardSurface.GetHeight(new Vector3(spawnX, 0f, spawnZ));
                spawnPos = new Vector3(spawnX, groundY + spawnHeight, spawnZ);
                direction = goingPositive ? Vector3.right : Vector3.left;
            }
            else
            {
                float spawnZ = goingPositive ? bounds.min.z + spawnInset : bounds.max.z - spawnInset;
                float safeX = Mathf.Max(bounds.extents.x - spawnInset, 0f);
                float spawnX = Random.Range(bounds.center.x - safeX, bounds.center.x + safeX);
                float groundY = boardSurface.GetHeight(new Vector3(spawnX, 0f, spawnZ));
                spawnPos = new Vector3(spawnX, groundY + spawnHeight, spawnZ);
                direction = goingPositive ? Vector3.forward : Vector3.back;
            }

            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, forwardAngle, 0f);
            GameObject dino = Instantiate(prefab, spawnPos, rotation, transform);

            if (dinoLayer >= 0) dino.layer = dinoLayer;

            Animator animator = dino.GetComponent<Animator>();
            if (animator != null) animator.speed = dinoAnimSpeed;

            Rigidbody rb = dino.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("[DinosaurEventController] Dinosaur prefab missing Rigidbody.");
                Destroy(dino);
                yield break;
            }

            // 물리 안착 대기
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float elapsed = 0f;
            while (dino != null && elapsed < maxWalkDuration)
            {
                Vector3 targetVelXZ = direction * dinoWalkSpeed;
                Vector3 currentVelXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(targetVelXZ - currentVelXZ, ForceMode.VelocityChange);

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            if (dino != null) Destroy(dino);
        }

        private IEnumerator WaitForEggsToSettle()
        {
            bool settled = false;
            float timeout = 5f;
            float elapsed = 0f;

            System.Action handler = () => settled = true;
            GameEvents.OnAllEggsStopped += handler;

            try
            {
                while (!settled && elapsed < timeout)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }
            finally
            {
                GameEvents.OnAllEggsStopped -= handler;
            }
        }
    }
}
