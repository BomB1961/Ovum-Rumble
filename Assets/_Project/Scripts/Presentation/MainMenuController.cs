using TMPro;
using DinoAlkkagi.Core;
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
    [SerializeField] private Button vsComputerButton;
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
    [SerializeField] private AudioManager audioManager;

    private void Awake()
    {
        GameLaunchContext.ResetToDefault();
        ResolveMissingReferences();
        EnsureVsComputerButton();
        audioManager ??= FindFirstObjectByType<AudioManager>();
        InitializeVolumeSliders();
        RegisterButtonListeners();
        ShowMainMenu();
    }

    public void OnClickHostGame()
    {
        GameLaunchContext.SetMode(GameMode.LocalHotseat);
        SceneManager.LoadScene(GameSceneName);
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
        GameLaunchContext.SetMode(GameMode.LocalHotseat);
        SceneManager.LoadScene(GameSceneName);
    }

    public void OnClickVsComputer()
    {
        GameLaunchContext.SetMode(GameMode.VsComputer);
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
