using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private const string StartMenuScene = "StartMenu";
    private GameObject root;
    private GameObject mainPanel;
    private GameObject settingsPanel;
    private Slider gameVolumeSlider;
    private Slider voiceVolumeSlider;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<PauseMenuController>() != null) return;
        GameObject obj = new GameObject("GlobalPauseMenu");
        DontDestroyOnLoad(obj);
        obj.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        BuildInterface();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (paused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == StartMenuScene || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf) ShowMainPanel();
            else SetPaused(!paused);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == StartMenuScene) SetPaused(false);
    }

    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        root.SetActive(paused);
        if (paused) ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        gameVolumeSlider.SetValueWithoutNotify(GameAudioSettings.GameVolume);
        voiceVolumeSlider.SetValueWithoutNotify(GameAudioSettings.VoiceVolume);
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void BuildInterface()
    {
        root = new GameObject("PauseCanvas");
        root.transform.SetParent(transform, false);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        GameObject dimmer = Panel(root.transform, "Dimmer", Vector2.zero, new Vector2(1920f, 1080f), new Color(0.015f, 0.012f, 0.02f, 0.92f));
        mainPanel = new GameObject("PauseMainPanel");
        mainPanel.transform.SetParent(dimmer.transform, false);
        mainPanel.AddComponent<RectTransform>();
        Label(mainPanel.transform, "ПАУЗА", new Vector2(-500f, 230f), new Vector2(600f, 100f), 64f);
        Button(mainPanel.transform, "ResumeButton", "ПРОДОЛЖИТЬ", new Vector2(-520f, 80f), () => SetPaused(false));
        Button(mainPanel.transform, "SettingsButton", "НАСТРОЙКИ", new Vector2(-520f, -10f), ShowSettingsPanel);
        Button(mainPanel.transform, "MainMenuButton", "В ГЛАВНОЕ МЕНЮ", new Vector2(-520f, -100f), () => { SetPaused(false); SceneManager.LoadScene(StartMenuScene); });
        Button(mainPanel.transform, "QuitButton", "ВЫЙТИ ИЗ ИГРЫ", new Vector2(-520f, -190f), Application.Quit);

        settingsPanel = Panel(dimmer.transform, "PauseSettingsPanel", Vector2.zero, new Vector2(760f, 520f), new Color(0.055f, 0.05f, 0.07f, 1f));
        Label(settingsPanel.transform, "НАСТРОЙКИ", new Vector2(0f, 190f), new Vector2(650f, 60f), 35f);
        gameVolumeSlider = CreateSlider(settingsPanel.transform, "GameVolumeSlider", "ГРОМКОСТЬ ИГРЫ", new Vector2(0f, 75f), GameAudioSettings.GameVolume, GameAudioSettings.SetGameVolume);
        voiceVolumeSlider = CreateSlider(settingsPanel.transform, "VoiceVolumeSlider", "ГРОМКОСТЬ ГОЛОСА", new Vector2(0f, -55f), GameAudioSettings.VoiceVolume, GameAudioSettings.SetVoiceVolume);
        Button(settingsPanel.transform, "BackButton", "НАЗАД", new Vector2(0f, -190f), ShowMainPanel);
        root.SetActive(false);
    }

    private static Slider CreateSlider(Transform parent, string name, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> action)
    {
        Label(parent, label, position + Vector2.up * 45f, new Vector2(580f, 40f), 24f);
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(560f, 34f);
        UnityEngine.UI.Slider slider = obj.AddComponent<UnityEngine.UI.Slider>();
        GameObject background = Panel(obj.transform, "Background", Vector2.zero, rect.sizeDelta, new Color(0.18f, 0.18f, 0.22f, 1f));
        Stretch(background.GetComponent<RectTransform>(), 0f, 0f);
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        Stretch(fillAreaRect, 15f, 15f);
        GameObject fill = Panel(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.7f, 0.18f, 0.2f, 1f));
        Stretch(fill.GetComponent<RectTransform>(), 0f, 0f);
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(obj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        Stretch(handleAreaRect, 15f, 15f);
        GameObject handle = Panel(handleArea.transform, "Handle", Vector2.zero, new Vector2(30f, 46f), Color.white);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        slider.onValueChanged.AddListener(action);
        return slider;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = Panel(parent, name, position, new Vector2(430f, 68f), new Color(0.12f, 0.105f, 0.15f, 0.98f));
        UnityEngine.UI.Button button = obj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        Label(obj.transform, text, Vector2.zero, new Vector2(410f, 60f), 27f);
        return button;
    }

    private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
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

    private static void Label(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
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

    private static void Stretch(RectTransform rect, float horizontalInset, float verticalInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }
}
