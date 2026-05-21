using UnityEngine;
using DinoAlkkagi.Data;

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

        private float[,] heightfield;
        private ProceduralBoardSurface boardSurface;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        public IBoardSurface BoardSurface => boardSurface;

        private void Awake()
        {
            settings ??= FindFirstObjectByType<GameSettings>();
            featureFlags ??= FindFirstObjectByType<FeatureFlags>();

            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            if (featureFlags != null && featureFlags.enableProceduralMap)
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
            physMat.bounciness = settings.defaultBounciness;
            physMat.dynamicFriction = settings.defaultFriction;
            physMat.staticFriction = settings.defaultFriction;
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

            Debug.Log($"[ProceduralBoardGenerator] Generated board with seed={seed}, res={settings.mapResolution}");
        }
    }
}
