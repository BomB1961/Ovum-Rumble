using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DinoAlkkagi.Environment
{
    public class StaticBoardLoader : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField] private GameObject terrianMapPrefab;
        [SerializeField] private GameObject iceMapPrefab;
        [SerializeField] private GameObject desertMapPrefab;
        [SerializeField] private SkyboxManager skyboxManager;

        private StaticBoardSurface boardSurface;
        private MapId selectedMap = MapId.Terrian;
        private GameObject spawnedMap;

        public IBoardSurface BoardSurface => boardSurface;

        public void ConfigureForMap(MapId mapId)
        {
            selectedMap = mapId;
        }

        private void Awake()
        {
            settings ??= FindFirstObjectByType<GameSettings>();

            if (GameLaunchContext.HasSelectedMap)
            {
                ConfigureForMap(GameLaunchContext.SelectedMap);
            }

            LoadSelectedMap();
        }

        public void LoadSelectedMap()
        {
            GameObject prefab = GetSelectedMapPrefab();
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(StaticBoardLoader)}] No prefab assigned for {selectedMap}.", this);
                return;
            }

            if (spawnedMap != null)
            {
                Destroy(spawnedMap);
            }

            spawnedMap = Instantiate(prefab, transform.position, transform.rotation, transform);
            spawnedMap.name = $"{selectedMap}_Board";
            spawnedMap.transform.localScale = Vector3.one;

            Collider[] colliders = spawnedMap.GetComponentsInChildren<Collider>();
            boardSurface = new StaticBoardSurface(colliders, settings != null ? settings.boardSize : 10f);

            float bounciness = settings != null ? settings.defaultBounciness : 0.3f;
            float friction = settings != null ? settings.defaultFriction : 0.4f;
            PhysicsMaterial physMat = new PhysicsMaterial("BoardPhysics")
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction
            };

            int activeColliderCount = 0;
            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                collider.enabled = true;
                collider.sharedMaterial = physMat;
                activeColliderCount++;
            }

            Physics.SyncTransforms();
            Debug.Log($"[{nameof(StaticBoardLoader)}] Loaded {selectedMap} board with {activeColliderCount} colliders.");

            if (skyboxManager != null)
                skyboxManager.ConfigureForMap(selectedMap);
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
