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

    private void Awake()
    {
        ResolveMissingReferences();
        RegisterButtonListeners();
    }

    private void Start()
    {
        if (GameLaunchContext.IsNetworkHost)
        {
            netMan = FindFirstObjectByType<DinoNetworkManager>();
            if (netMan == null) return;

            netMan.OnRemotePlayerConnected += HandleRemotePlayerConnected;
            netMan.OnRemotePlayerDisconnected += HandleRemotePlayerDisconnected;
            netMan.OnRoomCreated += HandleRoomCreated;

            if (!NetworkServer.active && !NetworkClient.active)
            {
                GameLaunchContext.SetMode(GameMode.NetworkHost);
                try
                {
                    netMan.StartNetworkHost();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MapSelectController] Host start failed: {ex.Message}");
                    FallbackToLocalMode();
                    return;
                }
            }
            else
            {
                if (NetworkServer.connections.Count >= 2)
                {
                    HandleRemotePlayerConnected();
                    return;
                }
            }

            SetMapButtonsEnabled(false);
            SetStatusText("VPS 릴레이에 연결 중...");
        }
        else if (GameLaunchContext.IsNetworkClient)
        {
            SetMapButtonsEnabled(false);
            SetStatusText("호스트가 맵을 선택 중입니다...");
            Debug.Log("[MapSelectController] Client waiting for host map selection.");
        }
    }

    private void HandleRoomCreated(string code)
    {
        string msg = $"🏠 방 코드: {code}\n상대방이 이 코드를 입력하면 연결됩니다.";
        if (connectionStatusText != null)
        {
            connectionStatusText.text = msg;
            connectionStatusText.fontSize = 48;
            connectionStatusText.gameObject.SetActive(true);
            connectionStatusText.transform.SetAsLastSibling();
        }
        else
        {
            // 백업: 동적으로 생성
            connectionStatusText = CreateStatusText();
            connectionStatusText.text = msg;
            connectionStatusText.fontSize = 48;
        }
        Debug.Log($"[MapSelectController] ★★★ 방 코드: {code} ★★★");
    }

    private string GetLocalIp()
    {
        System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    private void FallbackToLocalMode()
    {
        Debug.LogWarning("[MapSelectController] Host 실패 → LocalHotseat 모드로 전환합니다.");
        GameLaunchContext.SetMode(GameMode.LocalHotseat);
        GameLaunchContext.ResetToDefault();
        SetMapButtonsEnabled(true);
        SetStatusText("[안내] 네트워크 호스트를 시작할 수 없습니다.\n포트 7777이 이미 사용 중입니다. 로컬 핫시트 모드로 전환합니다.");
    }

    private void OnDestroy()
    {
        if (netMan != null)
        {
            netMan.OnRemotePlayerConnected -= HandleRemotePlayerConnected;
            netMan.OnRemotePlayerDisconnected -= HandleRemotePlayerDisconnected;
            netMan.OnRoomCreated -= HandleRoomCreated;
        }
    }

    private void HandleRemotePlayerConnected()
    {
        SetMapButtonsEnabled(true);
        SetStatusText("");
        connectionStatusText?.gameObject.SetActive(false);
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
                MapSelectMessage mapMsg = new MapSelectMessage { mapId = (int)mapId };
                NetworkServer.SendToAll(mapMsg);
                Debug.Log($"[MapSelectController] Sent MapSelectMessage: {mapId}");

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
        connectionStatusText ??= FindText("Text_ConnectionStatus", "Text");
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
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, 50);
        rt.sizeDelta = new Vector2(600, 50);

        return text;
    }

    private void SetMapButtonsEnabled(bool enabled)
    {
        SetButtonInteractable(terrianButton, enabled);
        SetButtonInteractable(iceButton, enabled);
        SetButtonInteractable(desertButton, enabled);
    }

    private static void SetButtonInteractable(Button button, bool enabled)
    {
        if (button == null) return;
        button.interactable = enabled;

        var text = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (text != null)
        {
            Color c = text.color;
            c.a = enabled ? 1f : 0.5f;
            text.color = c;
        }
    }

    public void SetStatusText(string message)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.gameObject.SetActive(true);
            connectionStatusText.transform.SetAsLastSibling();
        }
        Debug.Log($"[MapSelect] {message}");
    }

    private static void AddClickListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButton(params string[] names)
    {
        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null) return found.GetComponent<Button>();
        }
        return null;
    }

    private static TMP_Text FindText(params string[] names)
    {
        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null) return found.GetComponent<TMP_Text>();
        }
        return null;
    }
}
}
