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
public class GameUIController : MonoBehaviour
{
    private const string GameSceneName = "01_Game";
    private const string MainMenuSceneName = "00_MainMenu";

    private static readonly string[] GuideMessages =
    {
        "\uc54c\uc744 \uc870\uc900\ud558\uc138\uc694.",
        "\uc0c1\ub300 \uc54c\uc744 \ubc16\uc73c\ub85c \ubc00\uc5b4\ub0b4\uc138\uc694.",
        "\ucc28\ub840\uac00 \ub05d\ub098\uba74 \ub2e4\uc74c \ud50c\ub808\uc774\uc5b4\ub85c \ub118\uc5b4\uac11\ub2c8\ub2e4.",
        "\ub0a8\uc740 \uc54c \uac1c\uc218\ub97c \ud655\uc778\ud558\uc138\uc694."
    };

    [Header("HUD")]
    [SerializeField] private GameObject gameHudPanel;
    [SerializeField] private TMP_Text currentTurnText;
    [SerializeField] private TMP_Text p1EggCountText;
    [SerializeField] private TMP_Text p2EggCountText;
    [SerializeField] private TMP_Text turnTimeText;
    [SerializeField] private TMP_Text gameTimeText;
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private Button settingsButton;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDetailText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button settingsMainMenuButton;
    [SerializeField] private Button settingsExitButton;

    [Header("Gameplay")]
    [SerializeField] private GameSessionController gameSessionController;
    [SerializeField] private AudioManager audioManager;

    private void Awake()
    {
        EnsureUiReferences();
        gameSessionController ??= FindFirstObjectByType<GameSessionController>();
        audioManager ??= FindFirstObjectByType<AudioManager>();
        InitializeVolumeSliders();
        RegisterListeners();
        ResetGameUI();
    }

    public void UpdateHUD(string currentTurn, int p1EggCount, int p2EggCount)
    {
        SetCurrentTurnText(currentTurn);
        SetP1EggCountText(p1EggCount);
        SetP2EggCountText(p2EggCount);
    }

    public void UpdateHUD(string currentTurn, int p1EggCount, int p2EggCount, float turnRemainingSeconds, float gameElapsedSeconds)
    {
        UpdateHUD(currentTurn, p1EggCount, p2EggCount);
        SetTurnTime(turnRemainingSeconds);
        SetGameTime(gameElapsedSeconds);
    }

    public void SetCurrentTurnText(string playerName)
    {
        SetText(currentTurnText, $"\ud604\uc7ac \ucc28\ub840: {playerName}");
    }

    public void SetP1EggCountText(int eggCount)
    {
        SetText(p1EggCountText, $"P1 \ub0a8\uc740 \uc54c: {eggCount}\uac1c");
    }

    public void SetP2EggCountText(int eggCount)
    {
        SetText(p2EggCountText, $"P2 \ub0a8\uc740 \uc54c: {eggCount}\uac1c");
    }

    public void SetTurnTime(float remainingSeconds)
    {
        SetText(turnTimeText, FormatTime(remainingSeconds));
    }

    public void SetGameTime(float elapsedSeconds)
    {
        SetText(gameTimeText, FormatTime(elapsedSeconds));
    }

    public void SetGuideText(string message)
    {
        SetText(guideText, message);
    }

    public void SetRandomGuideText()
    {
        SetGuideText(GuideMessages[Random.Range(0, GuideMessages.Length)]);
    }

    public void ShowResult(string winnerName, int p1EggCount, int p2EggCount, int p1WinCount, int p2WinCount)
    {
        SetPanelState(resultPanel, true);
        PrepareResultButtons();
        SetText(resultTitleText, $"{winnerName} \uc2b9\ub9ac!");
        SetResultDetail(winnerName, p1EggCount, p2EggCount, p1WinCount, p2WinCount);
    }

    public void ShowDrawResult(int p1EggCount, int p2EggCount, int p1WinCount, int p2WinCount)
    {
        SetPanelState(resultPanel, true);
        PrepareResultButtons();
        SetText(resultTitleText, "\ubb34\uc2b9\ubd80!");
        SetText(resultDetailText, "\uac8c\uc784 \uc885\ub8cc");
    }

    public void HideResult()
    {
        SetPanelState(resultPanel, false);
    }

    public void OnClickRetry()
    {
        Debug.Log("Result Retry button clicked.");
        ResetGameUI();
        gameSessionController ??= FindFirstObjectByType<GameSessionController>();
        gameSessionController?.RestartGame();
    }

    public void OnClickMainMenu()
    {
        Debug.Log("Main Menu button clicked.");
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void OnClickExit()
    {
        Debug.Log("Exit button clicked.");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickSettings()
    {
        Debug.Log("Settings button clicked.");
        SetPanelState(settingsPanel, true);
    }

    public void OnClickSettingsBack()
    {
        Debug.Log("Settings Back button clicked.");
        SetPanelState(settingsPanel, false);
    }

    public void OnBgmVolumeChanged(float value)
    {
        audioManager ??= FindFirstObjectByType<AudioManager>();
        audioManager?.SetBGMVolume(value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        audioManager ??= FindFirstObjectByType<AudioManager>();
        audioManager?.SetSFXVolume(value);
    }

    public void ResetGameUI()
    {
        SetPanelState(gameHudPanel, true);
        SetPanelState(settingsPanel, false);
        HideResult();
        UpdateHUD("P1", 0, 0, 25f, 0f);
        SetGuideText("\uc54c\uc744 \uc870\uc900\ud558\uc138\uc694.");
    }

    private void RegisterListeners()
    {
        AddClickListener(settingsButton, OnClickSettings);
        AddClickListener(settingsBackButton, OnClickSettingsBack);
        AddClickListener(settingsMainMenuButton, OnClickMainMenu);
        AddClickListener(retryButton, OnClickRetry);
        AddClickListener(mainMenuButton, OnClickMainMenu);
        AddClickListener(exitButton, OnClickExit);
        AddClickListener(settingsExitButton, OnClickExit);

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

    private void EnsureUiReferences()
    {
        gameHudPanel ??= FindGameObject("UI_GameHUDPanel", "UI_HUDPanel");
        resultPanel ??= FindGameObject("UI_ResultPanel", " UI_ResultPanel");
        settingsPanel ??= FindGameObject("UI_SettingsPanel");

        currentTurnText ??= FindText("Text_CurrentTurn");
        p1EggCountText ??= FindText("Text_P1EggCount");
        p2EggCountText ??= FindText("Text_P2EggCount");
        turnTimeText ??= FindText("Text_TurnTime");
        gameTimeText ??= FindText("Text_GameTime");
        guideText ??= FindText("Text_Guide");
        resultTitleText ??= FindText("Text_ResultTitle");
        resultDetailText ??= FindText("Text_ResultDetail");

        settingsButton ??= FindButton("Button_Settings");
        retryButton ??= FindButton("Button_Retry", " Button_Retry");
        mainMenuButton ??= FindButton("Button_MainMenu");
        exitButton ??= FindButton("Button_Exit");
        settingsBackButton ??= FindButton("Button_SettingsBack", "Button_CloseSettings");
        settingsMainMenuButton ??= FindButton("Button_SettingsMainMenu");
        settingsExitButton ??= FindButton("Button_SettingsExit");

        bgmSlider ??= FindSlider("Slider_BGM");
        sfxSlider ??= FindSlider("Slider_SFX");

        turnTimeText ??= CreateHudText("Text_TurnTime", "00:30", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(430f, 50f), TextAlignmentOptions.Center);
        gameTimeText ??= CreateHudText("Text_GameTime", "00:00", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(520f, 50f), TextAlignmentOptions.Center);
        exitButton ??= CreateResultExitButton();
        settingsExitButton ??= CreateSettingsExitButton();
    }

    private void SetResultDetail(string winnerName, int p1EggCount, int p2EggCount, int p1WinCount, int p2WinCount)
    {
        bool isP1Winner = winnerName == "P1";
        int winnerEggCount = isP1Winner ? p1EggCount : p2EggCount;
        int winnerWinCount = isP1Winner ? p1WinCount : p2WinCount;
        SetText(resultDetailText, $"\ub0a8\uc740 \uc54c {winnerEggCount}\n{winnerWinCount}\uc2b9");
    }

    private void PrepareResultButtons()
    {
        SetPanelState(retryButton != null ? retryButton.gameObject : null, true);
        SetPanelState(mainMenuButton != null ? mainMenuButton.gameObject : null, true);
        SetPanelState(exitButton != null ? exitButton.gameObject : null, true);
        PrepareButton(retryButton);
        PrepareButton(mainMenuButton);
        PrepareButton(exitButton);
    }

    private void PrepareButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = true;
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Color imageColor = image.color;
            imageColor.a = 1f;
            image.color = imageColor;
        }

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            tmpLabel.gameObject.SetActive(true);
            tmpLabel.alpha = 1f;
            return;
        }

        Text legacyLabel = button.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.gameObject.SetActive(true);
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

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null)
        {
            target.text = text;
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private TMP_Text CreateHudText(
        string objectName,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        TextAlignmentOptions alignment)
    {
        if (gameHudPanel == null)
        {
            return null;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(gameHudPanel.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.pivot = new Vector2(0.5f, 1f);

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = 42f;
        textComponent.color = Color.white;
        textComponent.alignment = alignment;
        textComponent.raycastTarget = false;

        if (currentTurnText != null)
        {
            textComponent.font = currentTurnText.font;
            textComponent.fontSharedMaterial = currentTurnText.fontSharedMaterial;
        }

        return textComponent;
    }

    private Button CreateResultExitButton()
    {
        if (resultPanel == null || mainMenuButton == null)
        {
            return mainMenuButton;
        }

        GameObject exitObject = Instantiate(mainMenuButton.gameObject, resultPanel.transform);
        exitObject.name = "Button_Exit";

        RectTransform rectTransform = exitObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0f, -352f);

        Button button = exitObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        TMP_Text tmpLabel = exitObject.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "\uc885\ub8cc";
        }
        else
        {
            Text legacyLabel = exitObject.GetComponentInChildren<Text>();
            if (legacyLabel != null)
            {
                legacyLabel.text = "\uc885\ub8cc";
            }
        }

        return button;
    }

    private Button CreateSettingsExitButton()
    {
        if (settingsPanel == null || settingsMainMenuButton == null)
        {
            return null;
        }

        GameObject exitObject = Instantiate(settingsMainMenuButton.gameObject, settingsPanel.transform);
        exitObject.name = "Button_SettingsExit";

        RectTransform rectTransform = exitObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0f, -340f);

        Button button = exitObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        TMP_Text tmpLabel = exitObject.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "\uac8c\uc784 \uc885\ub8cc";
        }
        else
        {
            Text legacyLabel = exitObject.GetComponentInChildren<Text>();
            if (legacyLabel != null)
            {
                legacyLabel.text = "\uac8c\uc784 \uc885\ub8cc";
            }
        }

        return button;
    }

    private static void AddClickListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetPanelState(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static GameObject FindGameObject(params string[] names)
    {
        foreach (string objectName in names)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static TMP_Text FindText(params string[] names)
    {
        GameObject found = FindGameObject(names);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButton(params string[] names)
    {
        GameObject found = FindGameObject(names);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Slider FindSlider(params string[] names)
    {
        GameObject found = FindGameObject(names);
        return found != null ? found.GetComponent<Slider>() : null;
    }
}
}
