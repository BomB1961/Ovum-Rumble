using DinoAlkkagi.Core;
using Mirror;
using TMPro;
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
    [SerializeField] private TMP_Text connectionStatusText;

    private DinoNetworkManager netMan;

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
            netMan = FindFirstObjectByType<DinoNetworkManager>();
            if (netMan != null && !NetworkServer.active && !NetworkClient.active)
            {
                netMan.OnRemotePlayerConnected += HandleRemotePlayerConnected;
                netMan.OnRemotePlayerDisconnected += HandleRemotePlayerDisconnected;
                netMan.StartNetworkHost();
                SetMapButtonsEnabled(false);
                SetStatusText("상대방 연결을 기다리는 중...");
                Debug.Log("[MapSelectController] Network host started. Waiting for player 2.");
            }
        }
    }

    private void OnDestroy()
    {
        if (netMan != null)
        {
            netMan.OnRemotePlayerConnected -= HandleRemotePlayerConnected;
            netMan.OnRemotePlayerDisconnected -= HandleRemotePlayerDisconnected;
        }
    }

    private void HandleRemotePlayerConnected()
    {
        SetMapButtonsEnabled(true);
        SetStatusText("P2 연결됨! 맵을 선택하세요.");
        Debug.Log("[MapSelectController] Remote player connected. Map selection enabled.");
    }

    private void HandleRemotePlayerDisconnected()
    {
        SetMapButtonsEnabled(false);
        SetStatusText("상대방 연결이 끊어졌습니다. 다시 기다리는 중...");
        Debug.Log("[MapSelectController] Remote player disconnected. Waiting again.");
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
            DinoNetworkManager nMan = netMan ?? FindFirstObjectByType<DinoNetworkManager>();
            if (nMan != null)
            {
                nMan.ServerChangeScene(GameSceneName);
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
        connectionStatusText ??= FindText("Text_ConnectionStatus");
        if (connectionStatusText == null)
        {
            connectionStatusText = CreateStatusText();
        }
    }

    private TMP_Text CreateStatusText()
    {
        GameObject go = new GameObject("Text_ConnectionStatus", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        go.transform.SetParent(FindFirstObjectByType<Canvas>()?.transform ?? null, false);

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = "";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -100);
        rt.sizeDelta = new Vector2(600, 50);

        return text;
    }

    private void SetMapButtonsEnabled(bool enabled)
    {
        if (terrianButton != null) terrianButton.interactable = enabled;
        if (iceButton != null) iceButton.interactable = enabled;
        if (desertButton != null) desertButton.interactable = enabled;
    }

    private void SetStatusText(string message)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.gameObject.SetActive(true);
        }
        Debug.Log($"[MapSelect] {message}");
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

    private static TMP_Text FindText(params string[] names)
    {
        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                return found.GetComponent<TMP_Text>();
            }
        }

        return null;
    }
}
}
