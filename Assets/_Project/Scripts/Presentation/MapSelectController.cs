using DinoAlkkagi.Core;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoAlkkagi.Presentation
{
public class MapSelectController : MonoBehaviour
{
    private const string MapSelectSceneName = "02_MapSelect";
    private const string GameSceneName = "01_Game";

    [SerializeField] private Button terrianButton;
    [SerializeField] private Button iceButton;
    [SerializeField] private Button desertButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForMapSelectScene()
    {
        if (SceneManager.GetActiveScene().name != MapSelectSceneName
            || FindFirstObjectByType<MapSelectController>() != null)
        {
            return;
        }

        new GameObject(nameof(MapSelectController)).AddComponent<MapSelectController>();
    }

    private void Awake()
    {
        ResolveMissingReferences();
        RegisterButtonListeners();
    }

    private void Start()
    {
        // 네트워크 호스트 모드: 씬 로드 후 서버 시작
        if (GameLaunchContext.IsNetworkHost)
        {
            DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
            if (netMan != null && !NetworkServer.active && !NetworkClient.active)
            {
                netMan.StartNetworkHost();
                Debug.Log("[MapSelectController] Network host started.");
            }
        }
    }

    public void OnClickTerrian()
    {
        SelectMapAndStart(MapId.Terrian);
    }

    public void OnClickIce()
    {
        SelectMapAndStart(MapId.Ice);
    }

    public void OnClickDesert()
    {
        SelectMapAndStart(MapId.Desert);
    }

    private void SelectMapAndStart(MapId mapId)
    {
        GameLaunchContext.SelectMap(mapId);

        if (GameLaunchContext.IsNetworkHost && NetworkServer.active)
        {
            DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
            if (netMan != null)
            {
                netMan.ServerChangeScene(GameSceneName);
                return;
            }
        }

        SceneManager.LoadScene(GameSceneName);
    }

    private void RegisterButtonListeners()
    {
        AddClickListener(terrianButton, OnClickTerrian);
        AddClickListener(iceButton, OnClickIce);
        AddClickListener(desertButton, OnClickDesert);
    }

    private void ResolveMissingReferences()
    {
        terrianButton ??= FindButton("Button_Terrian", "button_terrian");
        iceButton ??= FindButton("Button_Ice", "button_ice");
        desertButton ??= FindButton("Button_Desert", "button_Desert");
    }

    private static void AddClickListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButton(params string[] names)
    {
        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                return found.GetComponent<Button>();
            }
        }

        return null;
    }
}
}
