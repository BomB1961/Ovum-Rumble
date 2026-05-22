using DinoAlkkagi.Core;
using DinoAlkkagi.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoAlkkagi.Utility
{
    public sealed class VsComputerTestBootstrap : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "01_Game";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private Vector3 temporaryBoardCenter = new Vector3(0f, -0.25f, 0f);
        [SerializeField] private Vector3 temporaryBoardSize = new Vector3(14f, 0.5f, 14f);
        [SerializeField] private bool disableFallZonesForTest = true;
        [SerializeField] private bool createTemporaryFallZoneForTest = true;
        [SerializeField] private Vector3 temporaryFallZoneCenter = new Vector3(0f, -3f, 0f);
        [SerializeField] private Vector3 temporaryFallZoneSize = new Vector3(24f, 1f, 24f);

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            GameLaunchContext.SetMode(GameMode.VsComputer);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            if (loadOnStart && SceneManager.GetActiveScene().name != gameSceneName)
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != gameSceneName)
            {
                return;
            }

            GameLaunchContext.SetMode(GameMode.VsComputer);
            EnsureTemporaryBoard();

            if (disableFallZonesForTest)
            {
                DisableFallZones();
            }

            if (createTemporaryFallZoneForTest)
            {
                EnsureTemporaryFallZone();
            }
        }

        private void EnsureTemporaryBoard()
        {
            if (GameObject.Find("TemporaryVsComputerTestBoard") != null)
            {
                return;
            }

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "TemporaryVsComputerTestBoard";
            board.transform.position = temporaryBoardCenter;
            board.transform.localScale = temporaryBoardSize;

            Renderer renderer = board.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                renderer.material = new Material(shader)
                {
                    color = new Color(0.34f, 0.35f, 0.32f)
                };
            }
        }

        private static void DisableFallZones()
        {
            BoardFallZone[] fallZones = FindObjectsByType<BoardFallZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (BoardFallZone fallZone in fallZones)
            {
                fallZone.gameObject.SetActive(false);
            }
        }

        private void EnsureTemporaryFallZone()
        {
            if (GameObject.Find("TemporaryVsComputerFallZone") != null)
            {
                return;
            }

            GameObject fallZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallZone.name = "TemporaryVsComputerFallZone";
            fallZone.transform.position = temporaryFallZoneCenter;
            fallZone.transform.localScale = temporaryFallZoneSize;

            Renderer renderer = fallZone.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            Collider collider = fallZone.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            fallZone.AddComponent<BoardFallZone>();
        }
    }
}
