using System.Collections.Generic;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public class EggDesignImpactFxPreview : MonoBehaviour
    {
        [SerializeField] private Transform eggRoot;
        [SerializeField] private Transform impactFxRoot;
        [SerializeField] private float cycleDuration = 2.4f;
        [SerializeField] private float activeDuration = 1.1f;
        [SerializeField] private float shardTravel = 0.8f;
        [SerializeField] private float sparkTravel = 1.15f;
        [SerializeField] private float eggSpinSpeed = 18f;

        private readonly List<FxPart> parts = new List<FxPart>();

        private void Awake()
        {
            CacheParts();
        }

        private void OnEnable()
        {
            CacheParts();
        }

        private void Update()
        {
            if (eggRoot != null)
            {
                eggRoot.Rotate(Vector3.up, eggSpinSpeed * Time.deltaTime, Space.World);
            }

            if (impactFxRoot == null)
            {
                return;
            }

            float cycle = Mathf.Repeat(Time.time, cycleDuration);
            bool active = cycle <= activeDuration;
            impactFxRoot.gameObject.SetActive(active);

            if (!active)
            {
                return;
            }

            float t = Mathf.Clamp01(cycle / activeDuration);
            AnimateParts(t);
        }

        private void CacheParts()
        {
            parts.Clear();

            if (impactFxRoot == null)
            {
                return;
            }

            foreach (Transform child in impactFxRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == impactFxRoot)
                {
                    continue;
                }

                parts.Add(new FxPart(child));
            }
        }

        private void AnimateParts(float t)
        {
            float flash = 1f - Mathf.Clamp01(t / 0.24f);
            float sparks = 1f - Mathf.Clamp01((t - 0.12f) / 0.48f);
            float shards = Mathf.Clamp01(t / 0.9f);
            float cracks = 1f - Mathf.Clamp01((t - 0.2f) / 0.65f);

            foreach (FxPart part in parts)
            {
                if (part.Transform == null)
                {
                    continue;
                }

                string name = part.Transform.name;
                Vector3 direction = part.OriginalLocalPosition.sqrMagnitude > 0.0001f
                    ? part.OriginalLocalPosition.normalized
                    : Vector3.forward;

                if (name.Contains("ImpactFlash") || name.Contains("HeatHaze") || name.Contains("OptionalCrackGlow"))
                {
                    bool visible = flash > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition;
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.55f, 1.55f, 1f - flash);
                    continue;
                }

                if (name.Contains("Spark"))
                {
                    bool visible = sparks > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition + direction * (sparkTravel * (1f - sparks));
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.15f, 1f, sparks);
                    continue;
                }

                if (name.Contains("BasaltShard"))
                {
                    part.Transform.gameObject.SetActive(true);
                    Vector3 arc = direction * (shardTravel * shards) + Vector3.up * Mathf.Sin(shards * Mathf.PI) * 0.22f;
                    part.Transform.localPosition = part.OriginalLocalPosition + arc;
                    part.Transform.localRotation = part.OriginalLocalRotation * Quaternion.Euler(180f * shards, 260f * shards, 95f * shards);
                    continue;
                }

                if (name.Contains("LavaCrack"))
                {
                    bool visible = cracks > 0f;
                    part.Transform.gameObject.SetActive(visible);
                    part.Transform.localPosition = part.OriginalLocalPosition;
                    part.Transform.localScale = part.OriginalLocalScale * Mathf.Lerp(0.75f, 1.12f, Mathf.Sin(t * Mathf.PI));
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
