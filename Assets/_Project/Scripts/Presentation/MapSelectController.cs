using DinoAlkkagi.Core;
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
    [SerializeField] private TMP_Text serverAddressText;

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
        UpdateServerAddressText();
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
        serverAddressText ??= FindText("Text_ServerAddress");
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

    private void UpdateServerAddressText()
    {
        serverAddressText ??= CreateServerAddressText();
        if (serverAddressText == null)
        {
            Debug.LogWarning("[MapSelectController] Text_ServerAddress not found and could not be created.");
            return;
        }

        serverAddressText.gameObject.SetActive(true);
        string serverAddress = string.IsNullOrWhiteSpace(GameLaunchContext.ServerAddress) ? "-" : GameLaunchContext.ServerAddress;
        serverAddressText.text = $"서버 주소 : {serverAddress}";
    }

    private TMP_Text CreateServerAddressText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        GameObject textObject = new GameObject("Text_ServerAddress", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 115f);
        rectTransform.sizeDelta = new Vector2(900f, 80f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text template = canvas.GetComponentInChildren<TMP_Text>(true);
        if (template != null)
        {
            text.font = template.font;
        }

        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static TMP_Text FindText(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }
}
}
