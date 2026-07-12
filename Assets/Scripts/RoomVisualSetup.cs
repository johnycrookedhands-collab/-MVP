using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomVisualSetup : MonoBehaviour
{
    private const string OfficeScene = "SampleScene";
    private const float WallThickness = 0.45f;
    private static readonly Color FloorColor = new Color(0.48f, 0.38f, 0.27f, 1f);
    private static readonly Color WallColor = new Color(0.16f, 0.23f, 0.32f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != OfficeScene || Camera.main == null) return;
        FitToCamera(Camera.main);
    }

    private static void FitToCamera(Camera camera)
    {
        float height = camera.orthographicSize * 2f;
        float width = height * camera.aspect;
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        SetObject("RoomFloor", Vector3.zero + Vector3.forward, new Vector3(width, height, 0.1f), FloorColor);
        ApplyColor("WallTop", WallColor);
        SetObject("WallBottom", new Vector3(0f, -halfHeight + WallThickness * 0.5f, 0f), new Vector3(width, WallThickness, 1f), WallColor);
        SetObject("WallLeft", new Vector3(-halfWidth + WallThickness * 0.5f, 0f, 0f), new Vector3(WallThickness, height, 1f), WallColor);
        SetObject("WallRight", new Vector3(halfWidth - WallThickness * 0.5f, 0f, 0f), new Vector3(WallThickness, height, 1f), WallColor);
    }

    private static void ApplyColor(string objectName, Color color)
    {
        GameObject target = GameObject.Find(objectName);
        Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
        if (renderer == null) return;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
    }

    private static void SetObject(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null) return;
        target.transform.position = position;
        target.transform.localScale = scale;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
    }
}
