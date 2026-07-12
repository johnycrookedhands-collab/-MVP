using UnityEngine;

public class PointClickSceneLayout : MonoBehaviour
{
    [Header("Room background")]
    public Sprite roomBackgroundSprite;

    [Header("Square pile")]
    public Vector2 squarePilePosition = new Vector2(-250f, -128f);
    public Vector2 squarePileSize = new Vector2(300f, 150f);
    public Vector2 squareSize = new Vector2(58f, 58f);
    public Sprite garbageSprite;

    [Header("Square drop zone")]
    public Sprite dropZoneSprite;
    public Vector2 dropZonePosition = new Vector2(250f, -128f);
    public Vector2 dropZoneSize = new Vector2(300f, 150f);

    [Header("Television")]
    public Sprite televisionSprite;
    public Vector2 televisionPosition = new Vector2(600f, -190f);
    public Vector2 televisionSize = new Vector2(260f, 170f);
    [Range(-180f, 180f)] public float televisionRotation;

    [Header("Television mini-game window")]
    public Vector2 televisionInterfacePosition = Vector2.zero;
    public Vector2 televisionInterfaceSize = new Vector2(1050f, 360f);

    [Header("Bed visual")]
    public Sprite bedSprite;
    public Vector2 bedPosition = new Vector2(-120f, 92f);
    public Vector2 bedSize = new Vector2(420f, 170f);
    [Range(-180f, 180f)] public float bedRotation;

    [Header("Sleep interaction zone")]
    public Vector2 sleepZonePosition = new Vector2(-120f, 92f);
    public Vector2 sleepZoneSize = new Vector2(420f, 170f);

    [Header("Book on shelf")]
    public Sprite bookSprite;
    public Vector2 bookPosition = new Vector2(250f, 40f);
    public Vector2 bookSize = new Vector2(90f, 120f);
    [Range(-180f, 180f)] public float bookRotation;

    [ContextMenu("Fit layout to 1920x1080")]
    public void FitToFullHd()
    {
        squarePilePosition = new Vector2(-520f, 260f);
        squarePileSize = new Vector2(340f, 180f);
        dropZonePosition = new Vector2(-100f, 260f);
        dropZoneSize = new Vector2(340f, 180f);
        televisionPosition = new Vector2(570f, 235f);
        televisionSize = new Vector2(320f, 210f);
        televisionInterfacePosition = Vector2.zero;
        televisionInterfaceSize = new Vector2(1180f, 420f);
        bedPosition = new Vector2(540f, -300f);
        bedSize = new Vector2(520f, 220f);
        sleepZonePosition = bedPosition;
        sleepZoneSize = bedSize;
    }

    [ContextMenu("Restore original layout")]
    public void RestoreOriginalLayout()
    {
        squarePilePosition = new Vector2(-250f, -128f);
        squarePileSize = new Vector2(300f, 150f);
        dropZonePosition = new Vector2(250f, -128f);
        dropZoneSize = new Vector2(300f, 150f);
        televisionPosition = new Vector2(600f, -190f);
        televisionSize = new Vector2(260f, 170f);
        televisionInterfacePosition = Vector2.zero;
        televisionInterfaceSize = new Vector2(1050f, 360f);
        bedPosition = new Vector2(-120f, 92f);
        bedSize = new Vector2(420f, 170f);
        bedRotation = 0f;
        sleepZonePosition = new Vector2(-120f, 92f);
        sleepZoneSize = new Vector2(420f, 170f);
    }

    private void OnDrawGizmos()
    {
        DrawRect(squarePilePosition, squarePileSize, new Color(0.2f, 0.7f, 1f), "SQUARE PILE");
        DrawRect(dropZonePosition, dropZoneSize, new Color(1f, 0.7f, 0.15f), "DROP ZONE");
        DrawRect(televisionPosition, televisionSize, new Color(0.3f, 1f, 0.45f), "TELEVISION");
        DrawRect(televisionInterfacePosition, televisionInterfaceSize, new Color(0.7f, 0.35f, 1f), "TV INTERFACE");
        DrawRect(bedPosition, bedSize, bedRotation, new Color(1f, 0.35f, 0.35f), "BED VISUAL");
        DrawRect(sleepZonePosition, sleepZoneSize, 0f, new Color(1f, 0.65f, 0.35f), "SLEEP INTERACTION");
        DrawRect(bookPosition, bookSize, bookRotation, new Color(0.35f, 1f, 0.8f), "BOOK");
    }

    private static void DrawRect(Vector2 position, Vector2 size, Color color, string label)
    {
        DrawRect(position, size, 0f, color, label);
    }

    private static void DrawRect(Vector2 position, Vector2 size, float rotation, Color color, string label)
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(new Vector3(position.x, position.y, 0f), Quaternion.Euler(0f, 0f, rotation), Vector3.one);
        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 1f));
        Gizmos.matrix = previousMatrix;
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(new Vector3(position.x, position.y + size.y * 0.5f + 18f, 0f), label);
#endif
    }
}
