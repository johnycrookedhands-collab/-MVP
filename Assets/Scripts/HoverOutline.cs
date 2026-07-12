using UnityEngine;
using UnityEngine.EventSystems;

public class HoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject outline;
    public void SetOutline(GameObject value) { outline = value; Hide(); }
    public void OnPointerEnter(PointerEventData eventData) { if (outline != null) outline.SetActive(true); }
    public void OnPointerExit(PointerEventData eventData) { Hide(); }
    public void Hide() { if (outline != null) outline.SetActive(false); }
    private void OnDisable() { Hide(); }
    private void OnDestroy() { Hide(); }
}
