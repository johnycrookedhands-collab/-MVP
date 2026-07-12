using UnityEngine;
using UnityEngine.SceneManagement;

public class OfficeSceneArtLayout : MonoBehaviour
{
    [Header("Office background")]
    public Sprite backgroundSprite;

    [Header("Phone sprite (applied to every Phone object)")]
    public Sprite phoneSprite;

    [Header("Phone positions and independent X/Y stretch")]
    public Vector2 phone1Position = new Vector2(2.5f, 0f);
    public Vector2 phone1Stretch = new Vector2(1.5f, 1.5f);
    public Vector2 phone2Position = new Vector2(-3.8f, 3.2f);
    public Vector2 phone2Stretch = new Vector2(1.5f, 1.5f);
    public Vector2 phone3Position = new Vector2(3.8f, 3.2f);
    public Vector2 phone3Stretch = new Vector2(1.5f, 1.5f);
    public Vector2 phone4Position = new Vector2(-3.8f, -3.2f);
    public Vector2 phone4Stretch = new Vector2(1.5f, 1.5f);
    public Vector2 phone5Position = new Vector2(3.8f, -3.2f);
    public Vector2 phone5Stretch = new Vector2(1.5f, 1.5f);

    [Header("Office ambience")]
    [Range(0f, 1f)] public float lampBuzzVolume = 0.22f;

    [Header("Table 1")]
    public Sprite table1Sprite;
    public Vector2 table1Position = new Vector2(-5f, 0f);
    public Vector2 table1Size = new Vector2(7f, 12f);
    [Tooltip("Независимое растяжение по X и Y. 1 = без изменения.")]
    public Vector2 table1Stretch = Vector2.one;
    public Vector2 table1ColliderSize = new Vector2(3f, 2f);
    public Vector2 table1ColliderOffset = Vector2.zero;
    [Range(-180f, 180f)] public float table1Rotation;

    [Header("Table 1 - second instance")]
    public Vector2 table1SecondPosition = new Vector2(-5f, -5f);
    public Vector2 table1SecondSize = new Vector2(7f, 12f);
    [Tooltip("Независимое растяжение по X и Y. 1 = без изменения.")]
    public Vector2 table1SecondStretch = Vector2.one;
    public Vector2 table1SecondColliderSize = new Vector2(3f, 2f);
    public Vector2 table1SecondColliderOffset = Vector2.zero;
    [Range(-180f, 180f)] public float table1SecondRotation;

    [Header("Table 2")]
    public Sprite table2Sprite;
    public Vector2 table2Position = new Vector2(5f, 0f);
    public Vector2 table2Size = new Vector2(7f, 12f);
    [Tooltip("Независимое растяжение по X и Y. 1 = без изменения.")]
    public Vector2 table2Stretch = Vector2.one;
    public Vector2 table2ColliderSize = new Vector2(3f, 2f);
    public Vector2 table2ColliderOffset = Vector2.zero;
    [Range(-180f, 180f)] public float table2Rotation;

    [Header("Table 2 - second instance")]
    public Vector2 table2SecondPosition = new Vector2(5f, -5f);
    public Vector2 table2SecondSize = new Vector2(7f, 12f);
    [Tooltip("Независимое растяжение по X и Y. 1 = без изменения.")]
    public Vector2 table2SecondStretch = Vector2.one;
    public Vector2 table2SecondColliderSize = new Vector2(3f, 2f);
    public Vector2 table2SecondColliderOffset = Vector2.zero;
    [Range(-180f, 180f)] public float table2SecondRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= Build;
        SceneManager.sceneLoaded += Build;
    }

    private static void Build(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene") return;
        OfficeSceneArtLayout layout = FindFirstObjectByType<OfficeSceneArtLayout>();
        if (layout == null) return;

        if (layout.backgroundSprite != null && Camera.main != null)
        {
            Renderer oldFloor = GameObject.Find("RoomFloor")?.GetComponent<Renderer>();
            if (oldFloor != null) oldFloor.enabled = false;
            float height = Camera.main.orthographicSize * 2f;
            float width = height * Camera.main.aspect;
            CreateSprite("OfficeBackgroundArt", layout.backgroundSprite, Vector2.zero, new Vector2(width, height), 0f, -100, Vector2.zero, Vector2.zero);
        }
        CreateLampBuzz(layout.lampBuzzVolume);
        CreateSprite("OfficeTable1Art", layout.table1Sprite, layout.table1Position, Vector2.Scale(layout.table1Size, layout.table1Stretch), layout.table1Rotation, -5, layout.table1ColliderSize, layout.table1ColliderOffset);
        CreateSprite("OfficeTable1Art_Second", layout.table1Sprite, layout.table1SecondPosition, Vector2.Scale(layout.table1SecondSize, layout.table1SecondStretch), layout.table1SecondRotation, -5, layout.table1SecondColliderSize, layout.table1SecondColliderOffset);
        CreateSprite("OfficeTable2Art", layout.table2Sprite, layout.table2Position, Vector2.Scale(layout.table2Size, layout.table2Stretch), layout.table2Rotation, -5, layout.table2ColliderSize, layout.table2ColliderOffset);
        CreateSprite("OfficeTable2Art_Second", layout.table2Sprite, layout.table2SecondPosition, Vector2.Scale(layout.table2SecondSize, layout.table2SecondStretch), layout.table2SecondRotation, -5, layout.table2SecondColliderSize, layout.table2SecondColliderOffset);

        if (layout.phoneSprite != null)
        {
            foreach (PhoneInteractable phone in FindObjectsByType<PhoneInteractable>(FindObjectsSortMode.None))
            {
                SpriteRenderer renderer = phone.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = layout.phoneSprite;
                    renderer.color = Color.white;
                    renderer.sortingOrder = 2;
                }
                layout.ApplyPhoneTransform(phone);
            }
        }
    }

    private void ApplyPhoneTransform(PhoneInteractable phone)
    {
        Vector2 position;
        Vector2 stretch;
        switch (phone.name)
        {
            case "Phone 2": position = phone2Position; stretch = phone2Stretch; break;
            case "Phone 3": position = phone3Position; stretch = phone3Stretch; break;
            case "Phone 4": position = phone4Position; stretch = phone4Stretch; break;
            case "Phone 5": position = phone5Position; stretch = phone5Stretch; break;
            default: position = phone1Position; stretch = phone1Stretch; break;
        }
        phone.transform.position = new Vector3(position.x, position.y, phone.transform.position.z);
        phone.transform.localScale = new Vector3(stretch.x, stretch.y, 1f);
    }

    private static void CreateLampBuzz(float volume)
    {
        AudioClip clip = Resources.Load<AudioClip>("GameAudio/lamp_buzz_loop");
        if (clip == null) return;
        GameObject obj = new GameObject("OfficeLampBuzzLoop");
        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.Play();
    }

    private static void CreateSprite(string name, Sprite sprite, Vector2 position, Vector2 targetSize, float rotation, int sortingOrder, Vector2 colliderWorldSize, Vector2 colliderWorldOffset)
    {
        if (sprite == null) return;
        GameObject obj = new GameObject(name);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        Vector2 spriteSize = sprite.bounds.size;
        obj.transform.localScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, 1f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;
        if (colliderWorldSize.x > 0f && colliderWorldSize.y > 0f)
        {
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(colliderWorldSize.x / obj.transform.localScale.x, colliderWorldSize.y / obj.transform.localScale.y);
            collider.offset = new Vector2(colliderWorldOffset.x / obj.transform.localScale.x, colliderWorldOffset.y / obj.transform.localScale.y);
            collider.isTrigger = false;
        }
    }

    private void OnDrawGizmos()
    {
        Draw(table1Position, Vector2.Scale(table1Size, table1Stretch), table1Rotation, Color.cyan, "OFFICE TABLE 1");
        Draw(table1SecondPosition, Vector2.Scale(table1SecondSize, table1SecondStretch), table1SecondRotation, new Color(0.2f, 0.8f, 1f), "OFFICE TABLE 1 - SECOND");
        Draw(table2Position, Vector2.Scale(table2Size, table2Stretch), table2Rotation, Color.magenta, "OFFICE TABLE 2");
        Draw(table2SecondPosition, Vector2.Scale(table2SecondSize, table2SecondStretch), table2SecondRotation, new Color(1f, 0.35f, 0.8f), "OFFICE TABLE 2 - SECOND");
        DrawCollider(table1Position, table1ColliderOffset, table1ColliderSize, table1Rotation);
        DrawCollider(table1SecondPosition, table1SecondColliderOffset, table1SecondColliderSize, table1SecondRotation);
        DrawCollider(table2Position, table2ColliderOffset, table2ColliderSize, table2Rotation);
        DrawCollider(table2SecondPosition, table2SecondColliderOffset, table2SecondColliderSize, table2SecondRotation);
        Draw(phone1Position, phone1Stretch, 0f, Color.yellow, "PHONE 1");
        Draw(phone2Position, phone2Stretch, 0f, Color.yellow, "PHONE 2");
        Draw(phone3Position, phone3Stretch, 0f, Color.yellow, "PHONE 3");
        Draw(phone4Position, phone4Stretch, 0f, Color.yellow, "PHONE 4");
        Draw(phone5Position, phone5Stretch, 0f, Color.yellow, "PHONE 5");
    }

    private static void DrawCollider(Vector2 position, Vector2 offset, Vector2 size, float rotation)
    {
        Vector2 rotatedOffset = Quaternion.Euler(0f, 0f, rotation) * offset;
        Draw(position + rotatedOffset, size, rotation, Color.green, "TABLE COLLIDER");
    }

    private static void Draw(Vector2 position, Vector2 size, float rotation, Color color, string label)
    {
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, 0f, rotation), Vector3.one);
        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = old;
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position + Vector2.up * (size.y * 0.5f), label);
#endif
    }
}
