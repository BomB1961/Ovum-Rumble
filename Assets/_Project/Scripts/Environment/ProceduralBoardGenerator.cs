using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DinoAlkkagi.Environment
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralBoardGenerator : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField] private FeatureFlags featureFlags;
        [SerializeField] private int manualSeed = -1;
        [SerializeField] private Material boardMaterial;
        [SerializeField] private GameObject terrianMapPrefab;
        [SerializeField] private GameObject iceMapPrefab;
        [SerializeField] private GameObject desertMapPrefab;

        private float[,] heightfield;
        private ProceduralBoardSurface boardSurface;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        private MapId selectedMap = MapId.Terrian;
        private GameObject spawnedMapVisual;
        private float selectedBounciness = -1f;
        private float selectedFriction = -1f;

        public IBoardSurface BoardSurface => boardSurface;

        public void ConfigureForMap(MapId mapId)
        {
            selectedMap = mapId;
            manualSeed = 240101;
            selectedBounciness = 0.3f;
            selectedFriction = 0.4f;
        }

        private void Awake()
        {
            settings ??= FindFirstObjectByType<GameSettings>();
            featureFlags ??= FindFirstObjectByType<FeatureFlags>();

            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (GameLaunchContext.HasSelectedMap)
            {
                ConfigureForMap(GameLaunchContext.SelectedMap);
                GenerateBoard();
            }
            else if (featureFlags != null && featureFlags.enableProceduralMap)
            {
                GenerateBoard();
            }
        }

        public void GenerateBoard()
        {
            int seed = manualSeed >= 0 ? manualSeed : Random.Range(0, 999999);

            for (int attempt = 0; attempt <= settings.maxRetryCount; attempt++)
            {
                int currentSeed = attempt == 0 ? seed : Random.Range(0, 999999);

                heightfield = HeightfieldGenerator.Generate(
                    settings.mapResolution,
                    settings.noiseScale,
                    settings.heightMultiplier,
                    settings.noiseOctaves,
                    currentSeed
                );

                SlopeClamper.Clamp(heightfield, settings.boardSize, settings.maxSlopeGradient);
                SpawnZoneFlattener.Flatten(heightfield, settings.boardSize, settings.spawnFlattenRadius);

                if (MapValidator.Validate(heightfield, settings.boardSize, settings.spawnFlattenRadius, settings.maxSlopeGradient))
                {
                    ApplyMesh(currentSeed);
                    return;
                }

                Debug.Log($"[ProceduralBoardGenerator] Attempt {attempt + 1} failed validation, retrying...");
            }

            Debug.LogWarning("[ProceduralBoardGenerator] Max retries reached, using last generated map.");
            ApplyMesh(seed);
        }

        private void ApplyMesh(int seed)
        {
            Mesh mesh = MeshFromHeightfield.Create(heightfield, settings.boardSize);
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            PhysicsMaterial physMat = new PhysicsMaterial("BoardPhysics");
            float bounciness = selectedBounciness >= 0f ? selectedBounciness : settings.defaultBounciness;
            float friction = selectedFriction >= 0f ? selectedFriction : settings.defaultFriction;
            physMat.bounciness = bounciness;
            physMat.dynamicFriction = friction;
            physMat.staticFriction = friction;
            meshCollider.sharedMaterial = physMat;

            var renderer = GetComponent<MeshRenderer>();
            if (boardMaterial != null)
            {
                renderer.material = boardMaterial;
            }
            else if (renderer.sharedMaterial == null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }

            boardSurface = new ProceduralBoardSurface(heightfield, settings.boardSize);
            ApplySelectedMapVisual();

            Debug.Log($"[ProceduralBoardGenerator] Generated board with seed={seed}, res={settings.mapResolution}");
        }

        private void ApplySelectedMapVisual()
        {
            GameObject prefab = GetSelectedMapPrefab();
            if (prefab == null)
            {
                return;
            }

            if (spawnedMapVisual != null)
            {
                Destroy(spawnedMapVisual);
            }

            spawnedMapVisual = Instantiate(prefab, transform.position, transform.rotation);
            spawnedMapVisual.name = $"{selectedMap}_Visual";
            MatchBoardTransform(spawnedMapVisual.transform);
            DisableVisualColliders(spawnedMapVisual);

            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        private void MatchBoardTransform(Transform mapVisual)
        {
            mapVisual.SetPositionAndRotation(transform.position, transform.rotation);
            mapVisual.localScale = transform.lossyScale;
        }

        private GameObject GetSelectedMapPrefab()
        {
            switch (selectedMap)
            {
                case MapId.Ice:
                    return iceMapPrefab != null ? iceMapPrefab : LoadMapPrefab("Assets/_Project/Prefabs/Board/Board_Ice_Wrapper.prefab");
                case MapId.Desert:
                    return desertMapPrefab != null ? desertMapPrefab : LoadMapPrefab("Assets/_Project/Prefabs/Board/Board_Desert_Wrapper.prefab");
                default:
                    return terrianMapPrefab != null ? terrianMapPrefab : LoadMapPrefab("Assets/_Project/Prefabs/Board/Terrian.prefab");
            }
        }

        private static void DisableVisualColliders(GameObject mapVisual)
        {
            foreach (Collider collider in mapVisual.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        private static GameObject LoadMapPrefab(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            return null;
#endif
        }
    }
}
