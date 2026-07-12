using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuBootstrap : MonoBehaviour
{
    private const string SceneName = "StartMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;
        Build();
    }

    private static void Build()
    {
        EnsureEventSystem();
        GameObject canvasObject = new GameObject("StartMenuCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreatePanel(canvas.transform, "Background", Vector2.zero, new Vector2(1920f, 1080f), new Color(0.035f, 0.03f, 0.045f, 1f));
        GameObject mainMenu = new GameObject("MainMenuContent");
        mainMenu.transform.SetParent(canvas.transform, false);
        mainMenu.AddComponent<RectTransform>();
        CreateTitle(mainMenu.transform);

        GameObject settingsPanel = CreateSettingsPanel(canvas.transform, mainMenu);
        CreateButton(mainMenu.transform, "StartGameButton", "НАЧАТЬ ИГРУ", Position("StartGameButtonPosition", new Vector2(-560f, -80f)),
            () => SceneManager.LoadScene(GameDayState.PhoneSceneName));
        CreateButton(mainMenu.transform, "SettingsButton", "НАСТРОЙКИ", Position("SettingsButtonPosition", new Vector2(-560f, -170f)),
            () => { mainMenu.SetActive(false); settingsPanel.SetActive(true); });
        CreateButton(mainMenu.transform, "QuitGameButton", "ВЫЙТИ ИЗ ИГРЫ", Position("QuitGameButtonPosition", new Vector2(-560f, -260f)),
            Application.Quit);
    }

    private static GameObject CreateSettingsPanel(Transform parent, GameObject mainMenu)
    {
        GameObject panel = CreatePanel(parent, "SettingsPanel", Vector2.zero, new Vector2(760f, 520f), new Color(0.055f, 0.05f, 0.07f, 0.98f));
        AddText(panel.transform, "НАСТРОЙКИ", new Vector2(0f, 190f), new Vector2(650f, 60f), 35f);
        CreateSlider(panel.transform, "GameVolumeSlider", "ГРОМКОСТЬ ИГРЫ", new Vector2(0f, 75f), GameAudioSettings.GameVolume, GameAudioSettings.SetGameVolume);
        CreateSlider(panel.transform, "VoiceVolumeSlider", "ГРОМКОСТЬ ГОЛОСА", new Vector2(0f, -55f), GameAudioSettings.VoiceVolume, GameAudioSettings.SetVoiceVolume);
        CreateButton(panel.transform, "CloseSettingsButton", "НАЗАД", new Vector2(0f, -190f),
            () => { panel.SetActive(false); mainMenu.SetActive(true); });
        panel.SetActive(false);
        return panel;
    }

    private static void CreateSlider(Transform parent, string name, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> action)
    {
        AddText(parent, label, position + new Vector2(0f, 45f), new Vector2(580f, 40f), 24f);
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(560f, 34f);
        Slider slider = sliderObject.AddComponent<Slider>();
        GameObject background = CreatePanel(sliderObject.transform, "Background", Vector2.zero, rect.sizeDelta, new Color(0.18f, 0.18f, 0.22f, 1f));
        Stretch(background.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        Stretch(fillAreaRect, 15f, 15f, 0f, 0f);
        GameObject fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.7f, 0.18f, 0.2f, 1f));
        Stretch(fill.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        Stretch(handleAreaRect, 15f, 15f, 0f, 0f);
        GameObject handle = CreatePanel(handleArea.transform, "Handle", Vector2.zero, new Vector2(30f, 46f), Color.white);
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = value;
        slider.onValueChanged.AddListener(action);
    }

    private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void CreateTitle(Transform parent)
    {
        AddText(parent, "CULT CALL", new Vector2(-480f, 280f), new Vector2(800f, 150f), 78f);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = CreatePanel(parent, name, position, new Vector2(430f, 68f), new Color(0.12f, 0.105f, 0.15f, 0.96f));
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        AddText(obj.transform, label, Vector2.zero, new Vector2(410f, 60f), 27f);
        return button;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private static void AddText(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static Vector2 Position(string markerName, Vector2 fallback)
    {
        GameObject marker = GameObject.Find(markerName);
        return marker == null ? fallback : (Vector2)marker.transform.localPosition;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<InputSystemUIInputModule>();
    }
}
