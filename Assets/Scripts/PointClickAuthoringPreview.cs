using UnityEngine;

[ExecuteAlways]
public class PointClickAuthoringPreview : MonoBehaviour
{
    public PointClickSceneLayout layout;
    public RectTransform television;
    public RectTransform bed;
    public RectTransform book;
    public RectTransform garbagePile;
    public RectTransform[] garbageItems;
    public RectTransform garbageBasket;
    public RectTransform sleepZone;

    private void Awake()
    {
        // Permanent scene hierarchy: it remains visible in Play Mode.
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying || layout == null) return;
        bool changed = false;
        changed |= Sync(television, ref layout.televisionPosition, ref layout.televisionSize, ref layout.televisionRotation);
        changed |= Sync(bed, ref layout.bedPosition, ref layout.bedSize, ref layout.bedRotation);
        changed |= Sync(book, ref layout.bookPosition, ref layout.bookSize, ref layout.bookRotation);
        changed |= Sync(garbagePile, ref layout.squarePilePosition, ref layout.squarePileSize);
        changed |= Sync(garbageBasket, ref layout.dropZonePosition, ref layout.dropZoneSize);
        changed |= Sync(sleepZone, ref layout.sleepZonePosition, ref layout.sleepZoneSize);
        if (bed != null && sleepZone != null)
        {
            sleepZone.anchorMin = bed.anchorMin;
            sleepZone.anchorMax = bed.anchorMax;
            sleepZone.pivot = bed.pivot;
            sleepZone.anchoredPosition = bed.anchoredPosition;
            sleepZone.sizeDelta = bed.sizeDelta;
            sleepZone.localEulerAngles = bed.localEulerAngles;
            sleepZone.localScale = bed.localScale;
            layout.sleepZonePosition = layout.bedPosition;
            layout.sleepZoneSize = layout.bedSize;
        }
        if (changed) UnityEditor.EditorUtility.SetDirty(layout);
    }

    private static bool Sync(RectTransform rect, ref Vector2 position, ref Vector2 size)
    {
        if (rect == null) return false;
        bool changed = position != rect.anchoredPosition || size != rect.sizeDelta;
        position = rect.anchoredPosition;
        size = rect.sizeDelta;
        return changed;
    }

    private static bool Sync(RectTransform rect, ref Vector2 position, ref Vector2 size, ref float rotation)
    {
        bool changed = Sync(rect, ref position, ref size);
        if (rect != null)
        {
            float newRotation = Mathf.DeltaAngle(0f, rect.localEulerAngles.z);
            changed |= !Mathf.Approximately(rotation, newRotation);
            rotation = newRotation;
        }
        return changed;
    }
#endif
}
