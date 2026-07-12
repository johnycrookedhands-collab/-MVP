using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PointClickHierarchyAuthoring
{
    static PointClickHierarchyAuthoring()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += () => TryBuild(SceneManager.GetActiveScene());
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) { TryBuild(scene); }

    [MenuItem("Tools/Cult Call/Rebuild Home Scene Objects")]
    public static void RebuildAndSaveHomeScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/PointClickPrototype.unity", OpenSceneMode.Single);
        TryBuild(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void TryBuild(Scene scene)
    {
        if (!scene.IsValid() || scene.name != "PointClickPrototype" || Application.isPlaying) return;
        GameObject existing = GameObject.Find("HOME VISUALS (EDIT OBJECTS HERE)");
        if (existing != null)
        {
            UpdatePngObjects(existing.transform);
            return;
        }
        PointClickSceneLayout layout = Object.FindFirstObjectByType<PointClickSceneLayout>();
        if (layout == null) return;

        GameObject root = new GameObject("HOME VISUALS (EDIT OBJECTS HERE)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(PointClickAuthoringPreview));
        Undo.RegisterCreatedObjectUndo(root, "Create editable home hierarchy");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateStretchImage(root.transform, "Room Background", layout.roomBackgroundSprite);
        RectTransform tv = CreateImage(root.transform, "Television", layout.televisionSprite, layout.televisionPosition, layout.televisionSize, layout.televisionRotation);
        RectTransform bed = CreateImage(root.transform, "Bed", layout.bedSprite, layout.bedPosition, layout.bedSize, layout.bedRotation);
        RectTransform book = CreateImage(root.transform, "Book", layout.bookSprite, layout.bookPosition, layout.bookSize, layout.bookRotation);
        RectTransform garbage = CreateImage(root.transform, "Garbage Icons", layout.garbageSprite, layout.squarePilePosition, layout.squarePileSize, 0f);
        RectTransform basket = CreateImage(root.transform, "Garbage Basket", layout.dropZoneSprite, layout.dropZonePosition, layout.dropZoneSize, 0f);
        RectTransform sleep = CreateImage(root.transform, "Sleep Interaction Zone", null, layout.sleepZonePosition, layout.sleepZoneSize, 0f);
        sleep.GetComponent<Image>().color = new Color(1f, 0.65f, 0.15f, 0.18f);

        PointClickAuthoringPreview preview = root.GetComponent<PointClickAuthoringPreview>();
        preview.layout = layout;
        preview.television = tv;
        preview.bed = bed;
        preview.book = book;
        preview.garbagePile = garbage;
        preview.garbageBasket = basket;
        preview.sleepZone = sleep;
        EditorSceneManager.MarkSceneDirty(scene);
        UpdatePngObjects(root.transform);
    }

    private static void UpdatePngObjects(Transform root)
    {
        SetPair(root.Find("Bed"), "Room_Bed-2.png", "Room_Bed-1.png");
        SetPair(root.Find("Book"), "Room_Book-2.png", "Room_Book-1.png");
        SetPair(root.Find("Television"), "Room_Tv_-2.png", "Room_Tv_-1.png");
        SetPair(root.Find("Garbage Basket"), "Room_Korzina_-2.png", "Room_Korzina_-1.png");
        CreateGarbageItems(root);
        Transform sleepZone = root.Find("Sleep Interaction Zone");
        Transform bedOutline = root.Find("Bed/Hover Outline");
        if (sleepZone != null && bedOutline != null)
        {
            HoverOutline hover = sleepZone.GetComponent<HoverOutline>();
            if (hover == null) hover = sleepZone.gameObject.AddComponent<HoverOutline>();
            hover.SetOutline(bedOutline.gameObject);
            sleepZone.GetComponent<Image>().raycastTarget = true;
        }
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
    }

    private static void CreateGarbageItems(Transform root)
    {
        Transform container = root.Find("Garbage Icons");
        if (container == null) return;
        Image containerImage = container.GetComponent<Image>();
        containerImage.color = Color.clear;
        containerImage.raycastTarget = false;
        RectTransform[] items = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            string name = "Garbage " + (i + 1);
            Transform existing = container.Find(name);
            GameObject item = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(container, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = item.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/HomePNG/Room_Garbage-{i + 1}.png");
            image.color = Color.white;
            image.raycastTarget = true;
            image.alphaHitTestMinimumThreshold = 0.1f;

            Transform oldOutline = item.transform.Find("Hover Outline");
            GameObject outline = oldOutline != null ? oldOutline.gameObject : new GameObject("Hover Outline", typeof(RectTransform), typeof(Image));
            outline.transform.SetParent(item.transform, false);
            RectTransform outlineRect = outline.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero; outlineRect.anchorMax = Vector2.one; outlineRect.offsetMin = Vector2.zero; outlineRect.offsetMax = Vector2.zero;
            Image outlineImage = outline.GetComponent<Image>();
            outlineImage.sprite = image.sprite;
            outlineImage.color = new Color(1f, 0.45f, 0.55f, 0.75f);
            outlineImage.raycastTarget = false;
            HoverOutline hover = item.GetComponent<HoverOutline>();
            if (hover == null) hover = item.AddComponent<HoverOutline>();
            hover.SetOutline(outline);
            items[i] = rect;
        }
        PointClickAuthoringPreview preview = root.GetComponent<PointClickAuthoringPreview>();
        if (preview != null) preview.garbageItems = items;
    }

    private static void SetPair(Transform target, string normalName, string outlineName)
    {
        if (target == null) return;
        Sprite normal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/HomePNG/" + normalName);
        Sprite outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/HomePNG/" + outlineName);
        if (normal == null || outlineSprite == null) return;
        Image image = target.GetComponent<Image>();
        image.sprite = normal;
        image.color = Color.white;
        image.raycastTarget = true;
        image.alphaHitTestMinimumThreshold = 0.1f;
        Transform oldOutline = target.Find("Hover Outline");
        GameObject outline = oldOutline != null ? oldOutline.gameObject : new GameObject("Hover Outline", typeof(RectTransform), typeof(Image));
        outline.transform.SetParent(target, false);
        RectTransform rect = outline.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image outlineImage = outline.GetComponent<Image>();
        outlineImage.sprite = outlineSprite;
        outlineImage.color = Color.white;
        outlineImage.raycastTarget = false;
        HoverOutline hover = target.GetComponent<HoverOutline>();
        if (hover == null) hover = target.gameObject.AddComponent<HoverOutline>();
        hover.SetOutline(outline);
        EditorUtility.SetDirty(target.gameObject);
    }

    private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, float rotation)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
        image.raycastTarget = false;
        return rect;
    }

    private static void CreateStretchImage(Transform parent, string name, Sprite sprite)
    {
        RectTransform rect = CreateImage(parent, name, sprite, Vector2.zero, Vector2.zero, 0f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
    }
}
