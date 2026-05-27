using TMPro;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;
using DinoAlkkagi.Network;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DinoAlkkagi.Presentation
{
public class MainMenuController : MonoBehaviour
{
    private const string GameSceneName = "01_Game";
    private const string MapSelectSceneName = "02_MapSelect";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject connectionStatusPanel;

    [Header("Main Menu")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button showJoinPanelButton;
    [SerializeField] private Button testJoinButton;
    [SerializeField] private Button vsComputerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitGameButton;

    [Header("Join")]
    [SerializeField] private TMP_InputField hostIpInput;
    [SerializeField] private TMP_Text controlsGuideText;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button cancelJoinButton;

    [Header("Connection Status")]
    [SerializeField] private TMP_Text connectionStatusText;

    [Header("Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private AudioManager audioManager;

    [Header("Feature Flags")]
    [SerializeField] private FeatureFlags featureFlags;

    private string pendingAutoJoinIp;
    private RoomCodeDiscovery roomDiscovery;

    private void Awake()
    {
        GameLaunchContext.ResetToDefault();
        ResolveMissingReferences();
        EnsureVsComputerButton();
        EnsureJoinButton();
        audioManager ??= FindFirstObjectByType<AudioManager>();
        featureFlags ??= Resources.Load<FeatureFlags>("FeatureFlags");
        InitializeVolumeSliders();
        RegisterButtonListeners();
        ShowMainMenu();
        UpdateNetworkButtons();

        roomDiscovery = gameObject.AddComponent<RoomCodeDiscovery>();
        roomDiscovery.OnRoomFound += HandleRoomFound;
        roomDiscovery.OnListenError += (err) =>
            ShowConnectionStatus($"방 검색 실패: {err}");
        roomDiscovery.OnDiscoveryTimeout += () =>
        {
            if (gameObject != null)
                ShowMainMenu();
        };

        // 커맨드라인 자동 접속: --auto-join 127.0.0.1
        string[] args = System.Environment.GetCommandLineArgs();
        bool hasAutoHost = false;
        bool hasAutoJoin = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == "--auto-host" && !hasAutoJoin)
            {
                hasAutoHost = true;
                Debug.Log($"[MainMenu] Auto-host requested");
            }
            else if (args[i].ToLower() == "--auto-join" && i + 1 < args.Length && !hasAutoHost)
            {
                pendingAutoJoinIp = args[i + 1];
                hasAutoJoin = true;
                Debug.Log($"[MainMenu] Auto-join requested: {pendingAutoJoinIp}");
            }
        }
        if (hasAutoHost)
            Invoke(nameof(DoAutoHost), 1f);
        else if (hasAutoJoin)
            Invoke(nameof(DoAutoJoin), 1f);
    }

    private void DoAutoHost()
    {
        DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
        if (netMan != null)
        {
            Debug.Log("[MainMenu] Auto-hosting...");
            GameLaunchContext.SetMode(GameMode.NetworkHost);
            SceneManager.LoadScene(MapSelectSceneName);
        }
    }

    private void DoAutoJoin()
    {
        string ip = string.IsNullOrEmpty(pendingAutoJoinIp) ? "127.0.0.1" : pendingAutoJoinIp;
        DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
        if (netMan != null)
        {
            ShowConnectionStatus($"자동 접속 중: {ip}");
            netMan.StartNetworkClient(ip);
        }
    }

    private void UpdateNetworkButtons()
    {
        if (featureFlags == null) return;
        bool lanEnabled = featureFlags.enableLanMultiplayer;
        if (hostGameButton != null) hostGameButton.interactable = lanEnabled;
        if (showJoinPanelButton != null) showJoinPanelButton.interactable = lanEnabled;
        if (testJoinButton != null) testJoinButton.interactable = lanEnabled;
    }

    private void HandleRoomFound(string ip, string code)
    {
        Debug.Log($"[MainMenu] Room found! Code: {code}, IP: {ip}");
        ShowConnectionStatus($"방 {code} 찾음! {ip}에 연결 중...");
        DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
        if (netMan != null)
        {
            netMan.StartNetworkClient(ip);
        }
    }

    public void OnClickHostGame()
    {
        DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
        if (netMan != null)
        {
            GameLaunchContext.SetMode(GameMode.NetworkHost);
            ShowConnectionStatus("호스트 시작 중...");
            SceneManager.LoadScene(MapSelectSceneName);
            return;
        }

        GameLaunchContext.SetMode(GameMode.LocalHotseat);
        SceneManager.LoadScene(MapSelectSceneName);
    }

    public void OnClickStartGame()
    {
        OnClickHostGame();
    }

    public void OnClickCreateRoom()
    {
        OnClickHostGame();
    }

    public void OnClickCreateAiRoom()
    {
        GameLaunchContext.SetMode(GameMode.VsComputer);
        SceneManager.LoadScene(MapSelectSceneName);
    }

    public void OnClickShowJoinPanel()
    {
        ShowJoinPanel();
    }

    public void OnClickJoinRoom()
    {
        ShowJoinPanel();
    }

    public void OnClickTestJoin()
    {
#if UNITY_EDITOR
        Debug.LogWarning("[MainMenu] TestJoin: 서버 없이 클라이언트 모드 진입 (Editor 디버깅 전용)");
        GameLaunchContext.SetMode(GameMode.NetworkClient);
        GameLaunchContext.SetNetworkClientInfo(2);
        SceneManager.LoadScene(GameSceneName);
#else
        Debug.Log("[MainMenu] TestJoin is only available in Editor.");
        ShowMainMenu();
#endif
    }

    public void OnClickVsComputer()
    {
        GameLaunchContext.SetMode(GameMode.VsComputer);
        SceneManager.LoadScene(GameSceneName);
    }

    public void OnClickJoinGame()
    {
        string input = hostIpInput != null ? hostIpInput.text.Trim() : "";
        if (string.IsNullOrEmpty(input))
        {
            ShowConnectionStatus("방 코드 또는 IP를 입력하세요.");
            return;
        }

        // IP 주소 감지 (숫자.숫자.숫자.숫자 패턴)
        if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
        {
            ShowConnectionStatus($"서버 {input}에 직접 연결 중...");
            DinoNetworkManager netMan = FindFirstObjectByType<DinoNetworkManager>();
            if (netMan != null)
                netMan.StartNetworkClient(input);
            return;
        }

        // 방 코드 (4자리 숫자)
        if (input.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{4}$"))
        {
            ShowConnectionStatus("방 코드는 4자리 숫자, 또는 IP 주소를 입력하세요.");
            return;
        }

        ShowConnectionStatus($"방 {input} 검색 중...");
        roomDiscovery?.StartClientLookup(input);
    }

    public void OnClickCancelJoin()
    {
        ShowMainMenu();
    }

    public void OnClickSettings()
    {
        ShowSettingsPanel();
    }

    public void OnClickCloseSettings()
    {
        ShowMainMenu();
    }

    public void OnBgmVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(AudioManager.BgmVolumePrefsKey, value);
        PlayerPrefs.Save();
        audioManager ??= FindFirstObjectByType<AudioManager>();
        audioManager?.SetBGMVolume(value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(AudioManager.SfxVolumePrefsKey, value);
        PlayerPrefs.Save();
        audioManager ??= FindFirstObjectByType<AudioManager>();
        audioManager?.SetSFXVolume(value);
    }

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMainMenu()
    {
        SetPanelState(mainMenuPanel, true);
        SetPanelState(joinPanel, false);
        SetPanelState(settingsPanel, false);
        SetPanelState(connectionStatusPanel, false);
    }

    private void ShowJoinPanel()
    {
        SetPanelState(mainMenuPanel, false);
        SetPanelState(joinPanel, true);
        SetPanelState(settingsPanel, false);
        SetPanelState(connectionStatusPanel, false);

        if (controlsGuideText != null)
            controlsGuideText.gameObject.SetActive(false);
        if (hostIpInput != null)
            hostIpInput.gameObject.SetActive(true);
        if (joinGameButton != null)
            joinGameButton.gameObject.SetActive(true);
    }

    public void OnClickManual()
    {
        ShowControlsPanel();
    }

    private void ShowControlsPanel()
    {
        SetPanelState(mainMenuPanel, false);
        SetPanelState(joinPanel, true);
        SetPanelState(settingsPanel, false);
        SetPanelState(connectionStatusPanel, false);
        SetPanelState(hostIpInput != null ? hostIpInput.gameObject : null, false);
        SetPanelState(joinGameButton != null ? joinGameButton.gameObject : null, false);

        if (controlsGuideText != null)
        {
            controlsGuideText.gameObject.SetActive(true);
            controlsGuideText.text = "1. 내 알을 선택합니다.\n"
                + "2. 마우스로 드래그해 방향과 힘을 정합니다.\n"
                + "3. 마우스를 놓으면 알이 발사됩니다.\n"
                + "4. 알이 움직이는 동안에는 기다립니다.\n"
                + "5. 상대 알을 모두 떨어뜨리면 승리합니다.\n\n"
                + "[되돌아가기] 화면 아무 곳이나 클릭";
        }

        Invoke(nameof(ShowMainMenu), 8f);
    }

    public void ShowConnectionStatus(string message)
    {
        if (connectionStatusPanel != null)
            connectionStatusPanel.SetActive(true);
        if (connectionStatusText != null)
            connectionStatusText.text = message;
        SetPanelState(mainMenuPanel, false);
        SetPanelState(joinPanel, false);
        SetPanelState(settingsPanel, false);

        // 연결 상태에서 빠져나올 '취소' 버튼 보장
        EnsureConnectionCancelButton();
    }

    public void OnClientDisconnected()
    {
        ShowConnectionStatus("연결이 끊어졌습니다.");
        Invoke(nameof(ShowMainMenu), 2f);
    }

    private void EnsureConnectionCancelButton()
    {
        if (connectionStatusPanel == null) return;
        // 이미 취소 버튼이 있으면 스킵
        if (cancelJoinButton != null && cancelJoinButton.gameObject.activeInHierarchy
            && cancelJoinButton.transform.IsChildOf(connectionStatusPanel.transform))
            return;

        Button template = cancelJoinButton ?? joinGameButton ?? hostGameButton;
        if (template == null) return;

        GameObject cancelObj = Instantiate(template.gameObject, connectionStatusPanel.transform);
        cancelObj.name = "Button_CancelConnection";
        Button cancelBtn = cancelObj.GetComponent<Button>();
        if (cancelBtn != null)
        {
            cancelBtn.onClick = new Button.ButtonClickedEvent();
            cancelBtn.onClick.AddListener(() =>
            {
                roomDiscovery?.Cancel();
                var netMan = FindFirstObjectByType<DinoNetworkManager>();
                if (netMan != null && NetworkClient.active)
                    netMan.StopClient();
                ShowMainMenu();
            });
        }

        RectTransform rt = cancelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 60);
        rt.sizeDelta = new Vector2(200, 50);

        SetButtonText(cancelObj, "취소");
    }

    private void ShowSettingsPanel()
    {
        SetPanelState(mainMenuPanel, false);
        SetPanelState(joinPanel, false);
        SetPanelState(settingsPanel, true);
    }

    private static void SetPanelState(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void RegisterButtonListeners()
    {
        AddClickListener(hostGameButton, OnClickHostGame);
        AddClickListener(showJoinPanelButton, OnClickJoinRoom);
        AddClickListener(testJoinButton, OnClickTestJoin);
        AddClickListener(vsComputerButton, OnClickVsComputer);
        AddClickListener(settingsButton, OnClickSettings);
        AddClickListener(quitGameButton, OnClickQuitGame);
        AddClickListener(joinGameButton, OnClickJoinGame);
        AddClickListener(cancelJoinButton, OnClickCancelJoin);
        AddClickListener(closeSettingsButton, OnClickCloseSettings);

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        // Button_Menual이 씬에 있으면 자동 연결
        var manualBtn = FindInactiveComponent<Button>("Button_Menual");
        if (manualBtn != null)
        {
            manualBtn.onClick = new Button.ButtonClickedEvent();
            manualBtn.onClick.AddListener(OnClickManual);
        }
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

    private void ResolveMissingReferences()
    {
        mainMenuPanel ??= FindInactiveGameObject("UI_MainMenuPanel");
        joinPanel ??= FindInactiveGameObject("UI_JoinRoomPanel", "Panel_join", "Panel_Join");
        settingsPanel ??= FindInactiveGameObject("UI_SettingsPanel");

        hostGameButton ??= FindInactiveComponent<Button>("Button_HostGame", "Button_CreateRoom");
        showJoinPanelButton ??= FindInactiveComponent<Button>("Button_ShowJoinPanel", "Button_JoinRoom");
        testJoinButton ??= FindInactiveComponent<Button>("Button_TestJoin");
        vsComputerButton ??= FindInactiveComponent<Button>("Button_VsComputer");
        settingsButton ??= FindInactiveComponent<Button>("Button_Settings");
        quitGameButton ??= FindInactiveComponent<Button>("Button_QuitGame");

        hostIpInput ??= FindInactiveComponent<TMP_InputField>("Input Field_HostIP");
        controlsGuideText ??= FindInactiveComponent<TMP_Text>("Text_InputField_IP");
        joinGameButton ??= FindInactiveComponent<Button>("Button_JoinGame", "Button_JoinRoom (1)");
        cancelJoinButton ??= FindInactiveComponent<Button>("Button_CancelJoin", "Button_JoinRoom (2)");

        bgmSlider ??= FindInactiveComponent<Slider>("Slider_BGM");
        sfxSlider ??= FindInactiveComponent<Slider>("Slider_SFX");
        closeSettingsButton ??= FindInactiveComponent<Button>("Button_CloseSettings");
    }

    private void EnsureVsComputerButton()
    {
        if (vsComputerButton != null)
        {
            return;
        }

        Button templateButton = testJoinButton != null ? testJoinButton : hostGameButton;
        if (templateButton == null || templateButton.transform.parent == null)
        {
            return;
        }

        GameObject buttonObject = Instantiate(templateButton.gameObject, templateButton.transform.parent);
        buttonObject.name = "Button_VsComputer";
        buttonObject.transform.SetSiblingIndex(templateButton.transform.GetSiblingIndex());

        vsComputerButton = buttonObject.GetComponent<Button>();
        if (vsComputerButton != null)
        {
            vsComputerButton.onClick = new Button.ButtonClickedEvent();
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        RectTransform templateRect = templateButton.GetComponent<RectTransform>();
        if (buttonRect != null && templateRect != null)
        {
            buttonRect.anchorMin = templateRect.anchorMin;
            buttonRect.anchorMax = templateRect.anchorMax;
            buttonRect.pivot = templateRect.pivot;
            buttonRect.sizeDelta = templateRect.sizeDelta;
            buttonRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, 66f);
        }

        TMP_Text tmpLabel = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            tmpLabel.text = "컴퓨터와 대결";
            return;
        }

        Text legacyLabel = buttonObject.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.text = "컴퓨터와 대결";
        }
    }

    private void EnsureJoinButton()
    {
        if (showJoinPanelButton != null)
            return;

        Button templateButton = vsComputerButton ?? hostGameButton;
        if (templateButton == null || templateButton.transform.parent == null)
            return;

        // Join 버튼 생성
        GameObject buttonObj = Instantiate(templateButton.gameObject, templateButton.transform.parent);
        buttonObj.name = "Button_ShowJoinPanel";
        buttonObj.transform.SetSiblingIndex(templateButton.transform.GetSiblingIndex());

        showJoinPanelButton = buttonObj.GetComponent<Button>();
        if (showJoinPanelButton != null)
        {
            showJoinPanelButton.onClick = new Button.ButtonClickedEvent();
            showJoinPanelButton.onClick.AddListener(OnClickShowJoinPanel);
        }

        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        RectTransform tmpRect = templateButton.GetComponent<RectTransform>();
        if (btnRect != null && tmpRect != null)
        {
            btnRect.anchorMin = tmpRect.anchorMin;
            btnRect.anchorMax = tmpRect.anchorMax;
            btnRect.pivot = tmpRect.pivot;
            btnRect.sizeDelta = tmpRect.sizeDelta;
            btnRect.anchoredPosition = tmpRect.anchoredPosition + new Vector2(0f, 132f);
        }

        SetButtonText(buttonObj, "IP로 접속");

        // Join 패널도 없으면 동적 생성
        if (joinPanel == null)
        {
            CreateJoinPanel();
        }
    }

    private static void SetButtonText(GameObject buttonObj, string text)
    {
        TMP_Text tmp = buttonObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }
        Text legacy = buttonObj.GetComponentInChildren<Text>(true);
        if (legacy != null) { legacy.text = text; }
    }

    private void CreateJoinPanel()
    {
        if (mainMenuPanel == null) return;

        // Join 패널 컨테이너
        GameObject panel = new GameObject("UI_JoinRoomPanel", typeof(RectTransform), typeof(CanvasRenderer));
        panel.transform.SetParent(mainMenuPanel.transform.parent, false);
        panel.SetActive(false);
        joinPanel = panel;

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // IP 입력 필드
        GameObject inputObj = new GameObject("InputField_HostIP", typeof(RectTransform));
        inputObj.transform.SetParent(panel.transform, false);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        hostIpInput = inputField;

        RectTransform inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputRt.sizeDelta = new Vector2(300, 40);
        inputRt.anchoredPosition = new Vector2(0, 20);

        // 접속 버튼
        GameObject joinBtnObj = Instantiate(hostGameButton.gameObject, panel.transform);
        joinBtnObj.name = "Button_JoinGame";
        Button joinBtn = joinBtnObj.GetComponent<Button>();
        if (joinBtn != null)
        {
            joinBtn.onClick = new Button.ButtonClickedEvent();
            joinBtn.onClick.AddListener(OnClickJoinGame);
        }
        joinGameButton = joinBtn;

        RectTransform joinBtnRt = joinBtnObj.GetComponent<RectTransform>();
        joinBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
        joinBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        joinBtnRt.anchoredPosition = new Vector2(0, -40);
        joinBtnRt.sizeDelta = new Vector2(200, 50);
        SetButtonText(joinBtnObj, "접속");

        // 취소 버튼
        GameObject cancelBtnObj = Instantiate(hostGameButton.gameObject, panel.transform);
        cancelBtnObj.name = "Button_CancelJoin";
        Button cancelBtn = cancelBtnObj.GetComponent<Button>();
        if (cancelBtn != null)
        {
            cancelBtn.onClick = new Button.ButtonClickedEvent();
            cancelBtn.onClick.AddListener(OnClickCancelJoin);
        }
        cancelJoinButton = cancelBtn;

        RectTransform cancelBtnRt = cancelBtnObj.GetComponent<RectTransform>();
        cancelBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
        cancelBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        cancelBtnRt.anchoredPosition = new Vector2(0, -100);
        cancelBtnRt.sizeDelta = new Vector2(200, 50);
        SetButtonText(cancelBtnObj, "취소");

        // Placeholder text for input (간략 처리)
        GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
        placeholder.transform.SetParent(inputObj.transform, false);
        TMP_Text placeText = placeholder.AddComponent<TextMeshProUGUI>();
        placeText.text = "방 코드 4자리 (예: 2847)";
        placeText.fontSize = 18;
        placeText.color = Color.gray;
        placeText.alignment = TextAlignmentOptions.Center;

        // 실제 입력 텍스트
        GameObject textArea = new GameObject("Text", typeof(RectTransform));
        textArea.transform.SetParent(inputObj.transform, false);
        TMP_Text textComp = textArea.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 24;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;

        // TextMeshPro InputField 설정
        inputField.textViewport = inputRt;
        inputField.textComponent = textComp;
        inputField.placeholder = placeText;
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.characterLimit = 4;
    }

    private void InitializeVolumeSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(AudioManager.BgmVolumePrefsKey, 1f));
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(AudioManager.SfxVolumePrefsKey, 1f));
        }
    }

    private static GameObject FindInactiveGameObject(params string[] names)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            foreach (string objectName in names)
            {
                if (transform.name == objectName && transform.hideFlags == HideFlags.None)
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private static T FindInactiveComponent<T>(params string[] names) where T : Component
    {
        GameObject gameObject = FindInactiveGameObject(names);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }
}
}
