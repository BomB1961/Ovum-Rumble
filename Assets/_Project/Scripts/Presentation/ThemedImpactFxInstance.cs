using System.Collections.Generic;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public class ThemedImpactFxInstance : MonoBehaviour
    {
        [SerializeField] private float duration = 1.1f;
        [SerializeField] private float shardTravel = 0.8f;
        [SerializeField] private float sparkTravel = 1.15f;
        [SerializeField] private bool destroyOnComplete = true;

        private readonly List<FxPart> parts = new List<FxPart>();
        private float elapsed;
        private float impactScale = 1f;

        public void Play(float scale)
        {
            impactScale = Mathf.Max(0.1f, scale);
            elapsed = 0f;
            CacheParts();
            gameObject.SetActive(true);
            Animate(0f);
        }

        private void Awake()
        {
            CacheParts();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            Animate(t);

            if (elapsed >= duration)
            {
                if (destroyOnComplete)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private void CacheParts()
        {
            parts.Clear();

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform)
                {
                    continue;
                }

                parts.Add(new FxPart(child));
            }
        }

        private void Animate(float t)
        {
            float flash = 1f - Mathf.Clamp01(t / 0.22f);
            float sparks = 1f - Mathf.Clamp01((t - 0.08f) / 0.45f);
            float shards = Mathf.Clamp01(t / 0.9f);
            float cracks = 1f - Mathf.Clamp01((t - 0.18f) / 0.62f);

            foreach (FxPart part in parts)
            {
                if (part.Transform == null)
                {
                    continue;
                }

                string objectName = part.Transform.name;
                Vector3 direction = part.OriginalLocalPosition.sqrMagnitude > 0.0001f
                    ? part.OriginalLocalPosition.normalized
                    : Vector3.forward;

                if (objectName.Contains("ImpactFlash") || objectName.Contains("HeatHaze") || objectName.Contains("OptionalCrackGlow"))
                {
                    bool visible = flash > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition;
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.5f, 1.5f * impactScale, 1f - flash);
                    continue;
                }

                if (objectName.Contains("Spark"))
                {
                    bool visible = sparks > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition + direction * (sparkTravel * impactScale * (1f - sparks));
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.1f, impactScale, sparks);
                    continue;
                }

                if (objectName.Contains("BasaltShard"))
                {
                    part.Transform.gameObject.SetActive(true);
                    Vector3 arc = direction * (shardTravel * impactScale * shards) + Vector3.up * Mathf.Sin(shards * Mathf.PI) * 0.24f * impactScale;
                    part.Transform.localPosition = part.OriginalLocalPosition + arc;
                    part.Transform.localRotation = part.OriginalLocalRotation * Quaternion.Euler(190f * shards, 270f * shards, 110f * shards);
                    continue;
                }

                if (objectName.Contains("LavaCrack"))
                {
                    bool visible = cracks > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition;
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.75f, 1.1f * impactScale, Mathf.Sin(t * Mathf.PI));
                }
            }
        }

        private readonly struct FxPart
        {
            public readonly Transform Transform;
            public readonly Vector3 OriginalLocalPosition;
            public readonly Vector3 OriginalLocalScale;
            public readonly Quaternion OriginalLocalRotation;

            public FxPart(Transform transform)
            {
                Transform = transform;
                OriginalLocalPosition = transform.localPosition;
                OriginalLocalScale = transform.localScale;
                OriginalLocalRotation = transform.localRotation;
            }
        }
    }
}
