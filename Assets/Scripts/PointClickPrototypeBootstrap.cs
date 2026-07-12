using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PointClickPrototypeBootstrap : MonoBehaviour
{
    private const string SceneName = "PointClickPrototype";
    private const string SleepBackgroundResourceName = "SleepScreenBackground";
    private const string WordRewardScheduleResourceName = "WordRewardSchedule";
    private const string MiniGameSquareResourcePrefix = "MiniGameSquare";
    private static readonly string[][] DefaultRewardWordsByDay =
    {
        new[] { "тихо" },
        new[] { "адрес" },
        new[] { "слушай" },
        new[] { "жди" },
        new[] { "помогу" }
    };
    private static readonly Vector2 MiniGameSourceZonePosition = new Vector2(-250f, -128f);
    private static readonly Vector2 MiniGameDropZonePosition = new Vector2(250f, -128f);
    private static readonly Vector2 MiniGameZoneSize = new Vector2(300f, 150f);
    private static readonly Vector2 MiniGameSquareSize = new Vector2(58f, 58f);
    private static readonly Vector2[] MiniGameSquarePositions =
    {
        new Vector2(-82f, 0f),
        new Vector2(0f, 0f),
        new Vector2(82f, 0f)
    };
    private static PointClickSceneLayout sceneLayout;
    private static PointClickAuthoringPreview authoringPreview;
    public static Vector2 MiniGameSquareSizeForDrop => sceneLayout != null ? sceneLayout.squareSize : MiniGameSquareSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallSceneLoadHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildPrototypeIfNeeded(scene.name);
    }

    private static void BuildPrototypeIfNeeded(string activeSceneName)
    {
        if (activeSceneName != SceneName)
        {
            return;
        }

        sceneLayout = Object.FindFirstObjectByType<PointClickSceneLayout>();
        authoringPreview = Object.FindFirstObjectByType<PointClickAuthoringPreview>();
        EnsureEventSystem();

        Canvas canvas = authoringPreview != null ? authoringPreview.GetComponent<Canvas>() : CreateCanvas();
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (authoringPreview == null) CreateSleepBackground(canvas.transform);
        CreateWordRewardMiniGame(canvas.transform);
        CreateDayLabel(canvas.transform);
        CreateTelevisionMiniGame(canvas.transform);
        if (authoringPreview == null) CreateBedVisual(canvas.transform);
        if (authoringPreview == null) CreateBookVisual(canvas.transform);
        CreateSleepZone(canvas.transform);
    }

    private static void CreateBookVisual(Transform parent)
    {
        if (sceneLayout == null || sceneLayout.bookSprite == null) return;
        GameObject book = CreatePanelObject(parent, "RoomBook", sceneLayout.bookPosition, sceneLayout.bookSize, Color.white);
        book.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, sceneLayout.bookRotation);
        Image image = book.GetComponent<Image>();
        image.sprite = sceneLayout.bookSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void CreateBedVisual(Transform parent)
    {
        if (sceneLayout == null || sceneLayout.bedSprite == null)
        {
            return;
        }

        GameObject bed = CreatePanelObject(parent, "BedVisual", sceneLayout.bedPosition, sceneLayout.bedSize, Color.white);
        RectTransform rect = bed.GetComponent<RectTransform>();
        rect.localEulerAngles = new Vector3(0f, 0f, sceneLayout.bedRotation);
        Image image = bed.GetComponent<Image>();
        image.sprite = sceneLayout.bedSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void CreateTelevisionMiniGame(Transform parent)
    {
        TelevisionCreditsSchedule schedule = Resources.Load<TelevisionCreditsSchedule>("TelevisionCreditsSchedule");
        TelevisionCreditsDay day = schedule != null ? schedule.GetDay(GameDayState.CurrentDay) : null;
        if (day == null || string.IsNullOrWhiteSpace(day.creditsText) || string.IsNullOrWhiteSpace(day.rewardWord)) return;

        GameObject television = authoringPreview != null && authoringPreview.television != null
            ? authoringPreview.television.gameObject
            : CreatePanelObject(parent, "Television", sceneLayout != null ? sceneLayout.televisionPosition : GetEditablePosition("TelevisionPosition", new Vector2(600f, -190f)), sceneLayout != null ? sceneLayout.televisionSize : new Vector2(260f, 170f), new Color(0.07f, 0.08f, 0.1f, 1f));
        television.GetComponent<Image>().raycastTarget = true;
        if (authoringPreview == null && sceneLayout != null) television.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, sceneLayout.televisionRotation);
        if (authoringPreview == null && sceneLayout != null && sceneLayout.televisionSprite != null)
        {
            television.GetComponent<Image>().sprite = sceneLayout.televisionSprite;
            television.GetComponent<Image>().color = Color.white;
            television.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
        }
        AddCenteredLabel(television.transform, "ТЕЛЕВИЗОР", 25f);

        GameObject window = CreatePanelObject(parent, "TelevisionMiniGame",
            sceneLayout != null ? sceneLayout.televisionInterfacePosition : GetEditablePosition("TelevisionMiniGamePosition", Vector2.zero),
            sceneLayout != null ? sceneLayout.televisionInterfaceSize : new Vector2(1050f, 360f), new Color(0.025f, 0.03f, 0.035f, 0.98f));
        window.SetActive(false);

        GameObject viewport = CreatePanelObject(window.transform, "CreditsViewport", Vector2.zero,
            new Vector2(930f, 170f), new Color(0.02f, 0.08f, 0.07f, 1f));
        RectMask2D mask = viewport.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;

        GameObject credits = new GameObject("MovingCredits");
        credits.transform.SetParent(viewport.transform, false);
        RectTransform creditsRect = credits.AddComponent<RectTransform>();
        creditsRect.anchorMin = creditsRect.anchorMax = new Vector2(0f, 0.5f);
        creditsRect.pivot = new Vector2(0f, 0.5f);
        creditsRect.sizeDelta = new Vector2(10f, 100f);
        HorizontalLayoutGroup layout = credits.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = credits.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject rewardLabelObject = new GameObject("TelevisionRewardLabel");
        rewardLabelObject.transform.SetParent(window.transform, false);
        RectTransform rewardLabelRect = rewardLabelObject.AddComponent<RectTransform>();
        rewardLabelRect.anchorMin = rewardLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        rewardLabelRect.anchoredPosition = new Vector2(0f, -125f);
        rewardLabelRect.sizeDelta = new Vector2(900f, 52f);
        TextMeshProUGUI rewardLabel = rewardLabelObject.AddComponent<TextMeshProUGUI>();
        rewardLabel.text = string.Empty;
        rewardLabel.fontSize = 25f;
        rewardLabel.alignment = TextAlignmentOptions.Center;
        rewardLabel.color = new Color(0.35f, 1f, 0.48f, 1f);
        rewardLabel.raycastTarget = false;

        TelevisionCreditsController controller = window.AddComponent<TelevisionCreditsController>();
        controller.Initialize(viewport.GetComponent<RectTransform>(), creditsRect, rewardLabel, day);
        controller.BuildWords(credits.transform);

        GameObject closeButtonObject = CreatePanelObject(window.transform, "CloseTelevisionMiniGame",
            new Vector2(470f, -150f), new Vector2(38f, 38f), new Color(0.72f, 0.18f, 0.18f, 0.96f));
        closeButtonObject.GetComponent<Image>().sprite = CreateCircleSprite();
        TelevisionCloseButton closeButton = closeButtonObject.AddComponent<TelevisionCloseButton>();
        closeButton.Initialize(window);

        TelevisionButton button = television.GetComponent<TelevisionButton>();
        if (button == null) button = television.AddComponent<TelevisionButton>();
        button.Initialize(television.GetComponent<Image>(), window, controller);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("PointClickCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateSleepBackground(Transform parent)
    {
        GameObject backgroundObject = new GameObject("SleepScreenBackground");
        backgroundObject.transform.SetParent(parent, false);

        RectTransform rect = backgroundObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = backgroundObject.AddComponent<Image>();
        image.color = new Color(0.06f, 0.055f, 0.07f, 1f);
        image.sprite = sceneLayout != null && sceneLayout.roomBackgroundSprite != null ? sceneLayout.roomBackgroundSprite : Resources.Load<Sprite>(SleepBackgroundResourceName) ?? CreateSolidSprite();
        if (sceneLayout != null && sceneLayout.roomBackgroundSprite != null) image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static void CreateSleepZone(Transform parent)
    {
        Vector2 sleepZonePosition = sceneLayout != null ? sceneLayout.sleepZonePosition : GetEditablePosition("SleepZonePosition", new Vector2(-120f, 92f));
        GameObject zoneObject = authoringPreview != null && authoringPreview.sleepZone != null
            ? authoringPreview.sleepZone.gameObject
            : sceneLayout != null
            ? CreatePanelObject(parent, "SleepZone", sleepZonePosition, sceneLayout.sleepZoneSize, new Color(0.9f, 0.76f, 0.42f, 0.18f))
            : CreateAnchoredPanelObject(parent, "SleepZone", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), sleepZonePosition, new Vector2(420f, 170f), new Color(0.9f, 0.76f, 0.42f, 0.18f));
        zoneObject.GetComponent<Image>().color = Color.clear;
        zoneObject.GetComponent<Image>().raycastTarget = true;

        GameObject popupObject = CreateAnchoredPanelObject(
            zoneObject.transform,
            "SleepPopup",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 22f),
            new Vector2(390f, 74f),
            Color.clear);

        AddCenteredLabel(popupObject.transform, "Отправиться спать", 28f);
        popupObject.SetActive(false);

        SleepZoneButton sleepZone = zoneObject.GetComponent<SleepZoneButton>();
        if (sleepZone == null) sleepZone = zoneObject.AddComponent<SleepZoneButton>();
        sleepZone.Initialize(zoneObject.GetComponent<Image>(), popupObject);
    }

    private static void CreateWordRewardMiniGame(Transform parent)
    {
        if (GameDayState.HasCompletedWordRewardForDay(GameDayState.CurrentDay))
        {
            return;
        }

        IReadOnlyList<string> rewardWords = GetRewardWordsForCurrentDay();
        if (rewardWords == null || rewardWords.Count == 0)
        {
            return;
        }

        GameObject miniGameRoot = new GameObject("WordRewardMiniGame");
        miniGameRoot.transform.SetParent(parent, false);

        RectTransform rootRect = miniGameRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        MiniGameDropZone dropZone = CreateMiniGameDropZone(miniGameRoot.transform, rewardWords);
        dropZone.SetCompletionLabel(CreateMiniGameRewardLabel(miniGameRoot.transform));
        CreateMiniGameSourceZone(miniGameRoot.transform, dropZone);
    }

    private static void CreateMiniGameSourceZone(Transform parent, MiniGameDropZone dropZone)
    {
        RectTransform[] authoredGarbage = GetAuthoredGarbageItems();
        if (authoredGarbage.Length > 0)
        {
            foreach (RectTransform garbageItem in authoredGarbage)
            {
                if (garbageItem == null) continue;
                GameObject garbage = garbageItem.gameObject;
                Image image = garbage.GetComponent<Image>();
                if (image != null) image.raycastTarget = true;
                MiniGameSquare sceneSquare = garbage.GetComponent<MiniGameSquare>();
                if (sceneSquare == null) sceneSquare = garbage.AddComponent<MiniGameSquare>();
                sceneSquare.Initialize(dropZone);
            }
            return;
        }

        GameObject sourceObject = CreatePanelObject(
            parent,
            "MiniGameSourceZone",
            sceneLayout != null ? sceneLayout.squarePilePosition : GetEditablePosition("SquarePilePosition", MiniGameSourceZonePosition),
            sceneLayout != null ? sceneLayout.squarePileSize : MiniGameZoneSize,
            new Color(0.12f, 0.16f, 0.18f, 0.78f));

        for (int i = 0; i < MiniGameSquarePositions.Length; i++)
        {
            GameObject squareObject = CreatePanelObject(
                sourceObject.transform,
                "MiniGameSquare" + (i + 1),
                MiniGameSquarePositions[i],
                sceneLayout != null ? sceneLayout.squareSize : MiniGameSquareSize,
                GetMiniGameSquareColor(i));

            Image squareImage = squareObject.GetComponent<Image>();
            squareImage.sprite = sceneLayout != null && sceneLayout.garbageSprite != null
                ? sceneLayout.garbageSprite
                : Resources.Load<Sprite>(MiniGameSquareResourcePrefix + (i + 1)) ?? CreateSolidSprite();
            if (sceneLayout != null && sceneLayout.garbageSprite != null) squareImage.color = Color.white;
            squareImage.type = Image.Type.Simple;
            squareImage.preserveAspect = false;

            MiniGameSquare square = squareObject.AddComponent<MiniGameSquare>();
            square.Initialize(dropZone);
        }
    }

    private static RectTransform[] GetAuthoredGarbageItems()
    {
        if (authoringPreview == null) return System.Array.Empty<RectTransform>();
        if (authoringPreview.garbageItems != null && authoringPreview.garbageItems.Length > 0)
            return authoringPreview.garbageItems;
        if (authoringPreview.garbagePile == null) return System.Array.Empty<RectTransform>();

        var items = new List<RectTransform>();
        for (int i = 1; i <= MiniGameSquarePositions.Length; i++)
        {
            Transform child = authoringPreview.garbagePile.Find("Garbage " + i);
            if (child is RectTransform rect) items.Add(rect);
        }
        return items.ToArray();
    }

    private static MiniGameDropZone CreateMiniGameDropZone(Transform parent, IReadOnlyList<string> rewardWords)
    {
        GameObject dropZoneObject = authoringPreview != null && authoringPreview.garbageBasket != null
            ? authoringPreview.garbageBasket.gameObject
            : CreatePanelObject(parent, "MiniGameDropZone", sceneLayout != null ? sceneLayout.dropZonePosition : GetEditablePosition("SquareDropZonePosition", MiniGameDropZonePosition), sceneLayout != null ? sceneLayout.dropZoneSize : MiniGameZoneSize, new Color(0.24f, 0.2f, 0.12f, 0.82f));
        dropZoneObject.GetComponent<Image>().raycastTarget = true;

        MiniGameDropZone dropZone = dropZoneObject.GetComponent<MiniGameDropZone>();
        if (dropZone == null) dropZone = dropZoneObject.AddComponent<MiniGameDropZone>();
        if (authoringPreview == null && sceneLayout != null && sceneLayout.dropZoneSprite != null)
        {
            dropZoneObject.GetComponent<Image>().sprite = sceneLayout.dropZoneSprite;
            dropZoneObject.GetComponent<Image>().color = Color.white;
            dropZoneObject.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
        }
        RectTransform[] authoredItems = GetAuthoredGarbageItems();
        int requiredCount = authoredItems.Length > 0
            ? authoredItems.Length
            : MiniGameSquarePositions.Length;
        dropZone.Initialize(requiredCount, GameDayState.CurrentDay, rewardWords);
        return dropZone;
    }

    private static Vector2 GetEditablePosition(string markerName, Vector2 fallback)
    {
        GameObject marker = GameObject.Find(markerName);
        if (marker == null)
        {
            return fallback;
        }

        Vector3 position = marker.transform.localPosition;
        return new Vector2(position.x, position.y);
    }

    private static IReadOnlyList<string> GetRewardWordsForCurrentDay()
    {
        DailyWordRewardSchedule schedule = Resources.Load<DailyWordRewardSchedule>(WordRewardScheduleResourceName);
        if (schedule != null)
        {
            return schedule.GetWordsForDay(GameDayState.CurrentDay);
        }

        int dayIndex = GameDayState.CurrentDay - 1;
        if (dayIndex < 0 || dayIndex >= DefaultRewardWordsByDay.Length)
        {
            return System.Array.Empty<string>();
        }

        return DefaultRewardWordsByDay[dayIndex];
    }

    private static TMP_Text CreateMiniGameRewardLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("MiniGameRewardLabel");
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -222f);
        rect.sizeDelta = new Vector2(520f, 46f);

        TMP_Text text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.gameObject.SetActive(false);
        return text;
    }

    private static void CreateDayLabel(Transform parent)
    {
        GameObject labelObject = CreateAnchoredPanelObject(
            parent,
            "CurrentDayLabel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(32f, -32f),
            new Vector2(280f, 58f),
            new Color(0.08f, 0.09f, 0.11f, 0.72f));

        AddCenteredLabel(labelObject.transform, "День " + GameDayState.CurrentDay, 26f);
    }

    private static GameObject CreateAnchoredPanelObject(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;

        return panelObject;
    }

    private static GameObject CreatePanelObject(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;

        return panelObject;
    }

    private static void AddCenteredLabel(Transform parent, string label, float fontSize)
    {
        if (authoringPreview != null && parent != null && parent.name == "Television") return;
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Circle";
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(distance - radius + 1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Color GetMiniGameSquareColor(int index)
    {
        switch (index)
        {
            case 0:
                return new Color(0.82f, 0.22f, 0.25f, 1f);
            case 1:
                return new Color(0.2f, 0.48f, 0.86f, 1f);
            default:
                return new Color(0.88f, 0.7f, 0.18f, 1f);
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }
}

public class TelevisionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image image;
    private GameObject miniGame;
    private TelevisionCreditsController controller;
    private Color idleColor;

    public void Initialize(Image targetImage, GameObject miniGameObject, TelevisionCreditsController creditsController)
    {
        image = targetImage;
        miniGame = miniGameObject;
        controller = creditsController;
        idleColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = Color.Lerp(idleColor, new Color(1f, 0.9f, 0.55f, idleColor.a), 0.35f);
    }
    public void OnPointerExit(PointerEventData eventData) { image.color = idleColor; }
    public void OnPointerClick(PointerEventData eventData)
    {
        miniGame.SetActive(true);
        miniGame.transform.SetAsLastSibling();
        controller.Begin();
    }
}

public class TelevisionCloseButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject miniGame;
    private Image image;
    private Color idleColor;

    public void Initialize(GameObject miniGameObject)
    {
        miniGame = miniGameObject;
        image = GetComponent<Image>();
        idleColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData) { image.color = idleColor * 1.3f; }
    public void OnPointerExit(PointerEventData eventData) { image.color = idleColor; }
    public void OnPointerClick(PointerEventData eventData)
    {
        miniGame.SetActive(false);
    }
}

public class TelevisionCreditsController : MonoBehaviour
{
    private RectTransform viewport;
    private RectTransform credits;
    private TMP_Text rewardLabel;
    private TelevisionCreditsDay data;
    private bool moving;
    private bool collected;
    private bool hasFinished;

    public void Initialize(RectTransform viewportRect, RectTransform creditsRect, TMP_Text resultLabel, TelevisionCreditsDay day)
    {
        viewport = viewportRect;
        credits = creditsRect;
        rewardLabel = resultLabel;
        data = day;
    }

    public void BuildWords(Transform parent)
    {
        string[] words = data.creditsText.Split(new[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        bool targetCreated = false;
        foreach (string word in words)
        {
            GameObject wordObject = new GameObject("Credit_" + word);
            wordObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = wordObject.AddComponent<TextMeshProUGUI>();
            text.text = word;
            text.fontSize = 32f;
            text.color = new Color(0.72f, 0.82f, 0.76f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            LayoutElement element = wordObject.AddComponent<LayoutElement>();
            element.preferredWidth = Mathf.Max(75f, word.Length * 21f);
            element.preferredHeight = 70f;

            if (!targetCreated && string.Equals(TrimWord(word), TrimWord(data.rewardWord), System.StringComparison.OrdinalIgnoreCase))
            {
                targetCreated = true;
                text.raycastTarget = true;
                TelevisionRewardWord reward = wordObject.AddComponent<TelevisionRewardWord>();
                reward.Initialize(text, data.rewardWord, this);
            }
        }
    }

    public void Begin()
    {
        if (hasFinished) return;
        Canvas.ForceUpdateCanvases();
        credits.anchoredPosition = new Vector2(viewport.rect.width, 0f);
        moving = true;
    }

    private void Update()
    {
        if (!moving) return;
        credits.anchoredPosition += Vector2.left * data.scrollSpeed * Time.deltaTime;
        if (credits.anchoredPosition.x < -credits.rect.width)
        {
            moving = false;
            hasFinished = true;
        }
    }

    public void Collect(string word, TMP_Text label)
    {
        if (collected) return;
        collected = true;
        GameDayState.AddEarnedWord(word);
        label.color = new Color(0.35f, 1f, 0.48f, 1f);
        if (rewardLabel != null)
        {
            rewardLabel.text = "Новое слово «" + word + "» добавлено в банк слов";
        }
    }

    private static string TrimWord(string word) { return word.Trim(' ', '.', ',', '!', '?', ':', ';', '—', '-', '«', '»'); }
}

public class TelevisionRewardWord : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private TMP_Text label;
    private string rewardWord;
    private TelevisionCreditsController controller;
    private float highlight;
    private bool hovered;

    public void Initialize(TMP_Text targetLabel, string word, TelevisionCreditsController owner)
    {
        label = targetLabel;
        rewardWord = word;
        controller = owner;
    }

    private void Update()
    {
        highlight = Mathf.MoveTowards(highlight, hovered ? 1f : 0.55f, Time.deltaTime * 0.8f);
        label.color = Color.Lerp(new Color(0.72f, 0.82f, 0.76f), new Color(1f, 0.82f, 0.18f), highlight);
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    public void OnPointerClick(PointerEventData eventData) { controller.Collect(rewardWord, label); enabled = false; }
}

public class SleepZoneButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image zoneImage;
    private GameObject popup;

    public void Initialize(Image image, GameObject popupObject)
    {
        zoneImage = image;
        popup = popupObject;
        SetHovered(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameDayState.AdvanceDay();
        SceneManager.LoadScene(GameDayState.PhoneSceneName);
    }

    private void SetHovered(bool hovered)
    {
        if (zoneImage != null)
        {
            zoneImage.color = Color.clear;
        }

        if (popup != null)
        {
            popup.SetActive(hovered);
        }
    }
}

public class MiniGameSquare : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private MiniGameDropZone dropZone;
    private Transform startParent;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Initialize(MiniGameDropZone targetDropZone)
    {
        dropZone = targetDropZone;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startParent = transform.parent;
        startPosition = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform)
        {
            transform.SetParent(startParent, false);
            rectTransform.anchoredPosition = startPosition;
        }
    }

    public void DropIntoZone(PointerEventData eventData, RectTransform dropRect)
    {
        if (dropZone == null)
        {
            return;
        }

        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dropRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPosition);

        transform.SetParent(dropRect, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = PointClickPrototypeBootstrap.MiniGameSquareSizeForDrop;
        rectTransform.anchoredPosition = localPosition;

        dropZone.RegisterSquare(this);
    }
}

public class MiniGameDropZone : MonoBehaviour, IDropHandler
{
    private readonly HashSet<MiniGameSquare> droppedSquares = new HashSet<MiniGameSquare>();
    private readonly List<string> rewardWords = new List<string>();
    private int requiredSquares;
    private int rewardDay;
    private bool completed;
    private TMP_Text completionLabel;
    private Image background;

    public void Initialize(int requiredSquareCount, int day, IReadOnlyList<string> wordsToReward)
    {
        requiredSquares = requiredSquareCount;
        rewardDay = day;
        background = GetComponent<Image>();

        rewardWords.Clear();
        if (wordsToReward == null)
        {
            return;
        }

        foreach (string word in wordsToReward)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                rewardWords.Add(word.Trim());
            }
        }
    }

    public void SetCompletionLabel(TMP_Text label)
    {
        completionLabel = label;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        MiniGameSquare square = eventData.pointerDrag.GetComponent<MiniGameSquare>();
        if (square == null)
        {
            return;
        }

        square.DropIntoZone(eventData, transform as RectTransform);
    }

    public void RegisterSquare(MiniGameSquare square)
    {
        if (completed || square == null)
        {
            return;
        }

        droppedSquares.Add(square);

        if (droppedSquares.Count < requiredSquares)
        {
            return;
        }

        completed = true;
        GameDayState.AddEarnedWords(rewardWords);
        GameDayState.MarkWordRewardCompletedForDay(rewardDay);

        if (background != null)
        {
            background.color = new Color(0.22f, 0.42f, 0.22f, 0.86f);
        }

        if (completionLabel != null)
        {
            completionLabel.text = rewardWords.Count == 1
                ? "Новое слово: " + rewardWords[0]
                : "Новые слова: " + string.Join(", ", rewardWords);
            completionLabel.gameObject.SetActive(true);
        }
    }
}

public class PointClickDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        UIDraggableSquare square = eventData.pointerDrag.GetComponent<UIDraggableSquare>();
        if (square == null)
        {
            return;
        }

        RectTransform rectTransform = square.GetComponent<RectTransform>();
        square.transform.SetParent(transform, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
