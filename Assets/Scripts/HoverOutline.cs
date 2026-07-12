using UnityEngine;
using UnityEngine.EventSystems;

public class HoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject outline;
    public void SetOutline(GameObject value) { outline = value; if (outline != null) outline.SetActive(false); }
    public void OnPointerEnter(PointerEventData eventData) { if (outline != null) outline.SetActive(true); }
    public void OnPointerExit(PointerEventData eventData) { if (outline != null) outline.SetActive(false); }
}
