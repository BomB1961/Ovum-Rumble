using TMPro;
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

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button showJoinPanelButton;
    [SerializeField] private Button testJoinButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitGameButton;

    [Header("Join")]
    [SerializeField] private TMP_InputField hostIpInput;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button cancelJoinButton;

    [Header("Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeSettingsButton;

    private void Awake()
    {
        ResolveMissingReferences();
        RegisterButtonListeners();
        ShowMainMenu();
    }

    public void OnClickHostGame()
    {
        OnClickCreateRoom();
    }

    public void OnClickStartGame()
    {
        OnClickHostGame();
    }

    public void OnClickCreateRoom()
    {
        Debug.Log("TODO: Mirror 설치 후 StartHost 연결");
    }

    public void OnClickShowJoinPanel()
    {
        OnClickJoinRoom();
    }

    public void OnClickJoinRoom()
    {
        ShowJoinPanel();
    }

    public void OnClickTestJoin()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void OnClickJoinGame()
    {
        string hostIp = hostIpInput != null ? hostIpInput.text.Trim() : string.Empty;
        Debug.Log($"TODO: Mirror 설치 후 StartClient 연결. Host IP: {hostIp}");
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
        Debug.Log($"TODO: AudioMixer BGM 볼륨 연결. Value: {value:0.00}");
    }

    public void OnSfxVolumeChanged(float value)
    {
        Debug.Log($"TODO: AudioMixer SFX 볼륨 연결. Value: {value:0.00}");
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
    }

    private void ShowJoinPanel()
    {
        SetPanelState(mainMenuPanel, false);
        SetPanelState(joinPanel, true);
        SetPanelState(settingsPanel, false);
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
        AddClickListener(showJoinPanelButton, OnClickShowJoinPanel);
        AddClickListener(testJoinButton, OnClickTestJoin);
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
        settingsButton ??= FindInactiveComponent<Button>("Button_Settings");
        quitGameButton ??= FindInactiveComponent<Button>("Button_QuitGame");

        hostIpInput ??= FindInactiveComponent<TMP_InputField>("Input Field_HostIP");
        joinGameButton ??= FindInactiveComponent<Button>("Button_JoinGame", "Button_JoinRoom (1)");
        cancelJoinButton ??= FindInactiveComponent<Button>("Button_CancelJoin", "Button_JoinRoom (2)");

        bgmSlider ??= FindInactiveComponent<Slider>("Slider_BGM");
        sfxSlider ??= FindInactiveComponent<Slider>("Slider_SFX");
        closeSettingsButton ??= FindInactiveComponent<Button>("Button_CloseSettings");
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
