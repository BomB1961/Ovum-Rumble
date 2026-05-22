using System.Collections.Generic;
using DinoAlkkagi.Core;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public class ThemedImpactFxController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private bool enableThemedImpactFx = true;
        [SerializeField] private EggSpawner eggSpawner;
        [SerializeField] private GameObject embercoreImpactFxPrefab;
        [SerializeField] private GameObject prismhornImpactFxPrefab;
        [SerializeField] private GameObject tidecrestImpactFxPrefab;

        [Header("Impact Thresholds")]
        [SerializeField] private float minImpactForEffect = 1.25f;
        [SerializeField] private float maxImpactForScale = 12f;
        [SerializeField] private float minEffectScale = 0.7f;
        [SerializeField] private float maxEffectScale = 1.35f;

        [Header("Placement")]
        [SerializeField] private float surfaceOffset = 0.05f;

        private readonly List<EggController> subscribedEggs = new List<EggController>();

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleGameStarted;
            SubscribeToEggs();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleGameStarted;
            UnsubscribeAllEggs();
        }

        private void HandleGameStarted()
        {
            SubscribeToEggs();
        }

        private void SubscribeToEggs()
        {
            UnsubscribeAllEggs();

            if (eggSpawner == null)
            {
                return;
            }

            foreach (EggController egg in eggSpawner.SpawnedEggs)
            {
                if (egg == null)
                {
                    continue;
                }

                egg.CollisionDetailedOccurred += HandleEggCollision;
                subscribedEggs.Add(egg);
            }
        }

        private void UnsubscribeAllEggs()
        {
            foreach (EggController egg in subscribedEggs)
            {
                if (egg != null)
                {
                    egg.CollisionDetailedOccurred -= HandleEggCollision;
                }
            }

            subscribedEggs.Clear();
        }

        private void HandleEggCollision(EggController egg, float impact, Vector3 contactPoint, Vector3 contactNormal)
        {
            if (!enableThemedImpactFx || egg == null)
            {
                return;
            }

            if (impact < minImpactForEffect)
            {
                return;
            }

            GameObject impactFxPrefab = GetImpactFxPrefab(egg, out EggSkinFxTheme fxTheme);
            if (impactFxPrefab == null)
            {
                return;
            }

            float impactT = Mathf.Clamp01((impact - minImpactForEffect) / Mathf.Max(0.01f, maxImpactForScale - minImpactForEffect));
            float effectScale = Mathf.Lerp(minEffectScale, maxEffectScale, impactT);
            Vector3 normal = contactNormal.sqrMagnitude > 0.0001f ? contactNormal.normalized : egg.transform.forward;
            Vector3 spawnPosition = contactPoint + normal * surfaceOffset;
            Quaternion spawnRotation = GetRotationForNormal(normal) * GetRotationOffset(fxTheme);

            GameObject instance = Instantiate(impactFxPrefab, spawnPosition, spawnRotation, transform);

            ThemedImpactFxInstance fxInstance = instance.GetComponent<ThemedImpactFxInstance>();
            if (fxInstance == null)
            {
                fxInstance = instance.AddComponent<ThemedImpactFxInstance>();
            }

            fxInstance.Play(effectScale);
        }

        private GameObject GetImpactFxPrefab(EggController egg, out EggSkinFxTheme fxTheme)
        {
            EggSkinTheme skinTheme = egg.GetComponentInChildren<EggSkinTheme>();
            if (skinTheme == null)
            {
                fxTheme = EggSkinFxTheme.Embercore;
                return embercoreImpactFxPrefab;
            }

            fxTheme = skinTheme.Theme;

            switch (skinTheme.Theme)
            {
                case EggSkinFxTheme.Prismhorn:
                    return prismhornImpactFxPrefab != null ? prismhornImpactFxPrefab : embercoreImpactFxPrefab;
                case EggSkinFxTheme.Tidecrest:
                    return tidecrestImpactFxPrefab != null ? tidecrestImpactFxPrefab : embercoreImpactFxPrefab;
                case EggSkinFxTheme.Embercore:
                default:
                    return embercoreImpactFxPrefab;
            }
        }

        private static Quaternion GetRotationForNormal(Vector3 normal)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(normal, up);
        }

        private static Quaternion GetRotationOffset(EggSkinFxTheme fxTheme)
        {
            switch (fxTheme)
            {
                case EggSkinFxTheme.Prismhorn:
                    return Quaternion.Euler(90f, 0f, 0f);
                case EggSkinFxTheme.Tidecrest:
                case EggSkinFxTheme.Embercore:
                default:
                    return Quaternion.identity;
            }
        }
    }
}
