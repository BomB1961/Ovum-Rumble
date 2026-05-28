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
    private const string MapSelectSceneName = "02_MapSelect";

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
    [SerializeField] private TMP_Text controlsGuideText;
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
        ShowControlsPanel();
    }

    public void OnClickJoinRoom()
    {
        ShowControlsPanel();
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
        ShowMainMenu();
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
        SetPanelState(mainMenuPanel, true);
        SetPanelState(joinPanel, true);
        SetPanelState(settingsPanel, false);
    }

    private void ShowControlsPanel()
    {
        SetPanelState(hostIpInput != null ? hostIpInput.gameObject : null, false);
        SetPanelState(joinGameButton != null ? joinGameButton.gameObject : null, false);
        SetPanelState(controlsGuideText != null ? controlsGuideText.gameObject : null, true);

        if (controlsGuideText != null)
        {
            controlsGuideText.text = "1. \uB0B4 \uC54C\uC744 \uC120\uD0DD\uD569\uB2C8\uB2E4.\n"
                + "2. \uB9C8\uC6B0\uC2A4\uB85C \uB4DC\uB798\uADF8\uD574 \uBC29\uD5A5\uACFC \uD798\uC744 \uC815\uD569\uB2C8\uB2E4.\n"
                + "3. \uB9C8\uC6B0\uC2A4\uB97C \uB193\uC73C\uBA74 \uC54C\uC774 \uBC1C\uC0AC\uB429\uB2C8\uB2E4.\n"
                + "4. \uC54C\uC774 \uC6C0\uC9C1\uC774\uB294 \uB3D9\uC548\uC5D0\uB294 \uAE30\uB2E4\uB9BD\uB2C8\uB2E4.\n"
                + "5. \uC0C1\uB300 \uC54C\uC744 \uBAA8\uB450 \uB5A8\uC5B4\uB728\uB9AC\uBA74 \uC2B9\uB9AC\uD569\uB2C8\uB2E4.";
        }

        ShowJoinPanel();
    }

    private void ShowSettingsPanel()
    {
        SetPanelState(mainMenuPanel, true);
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
            tmpLabel.text = "AI와 플레이";
            return;
        }

        Text legacyLabel = buttonObject.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.text = "AI와 플레이";
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
