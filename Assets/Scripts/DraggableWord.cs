using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string Word { get; private set; }

    [SerializeField] private TMP_Text wordText;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform startParent;
    private Vector2 startAnchoredPosition;
    private Vector2 floatingBasePosition;
    private float floatingSeed;
    private float floatingStartTime;
    private bool isDragging;

    private WordSlot currentSlot;
    private WordSlot slotAtDragStart;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (wordText == null)
        {
            wordText = GetComponentInChildren<TMP_Text>();
        }
    }

    public void Init(string word, Canvas parentCanvas)
    {
        Word = word;
        canvas = parentCanvas;
        floatingSeed = Random.Range(0f, 100f);
        floatingStartTime = Time.time + 0.15f;
        ApplyBankLayout();

        if (wordText != null)
        {
            wordText.text = word;
            ConfigureText();
        }
    }

    private void Update()
    {
        if (canvas == null || rectTransform == null)
        {
            return;
        }

        if (isDragging || currentSlot != null || transform.parent == canvas.transform)
        {
            return;
        }

        if (Time.time < floatingStartTime)
        {
            floatingBasePosition = rectTransform.anchoredPosition;
            return;
        }

        float offset = Mathf.Sin(Time.time * 1.4f + floatingSeed) * 8f;
        rectTransform.anchoredPosition = floatingBasePosition + new Vector2(offset, 0f);
    }

    public void SetCurrentSlot(WordSlot slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        startParent = transform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;

        slotAtDragStart = currentSlot;

        if (currentSlot != null)
        {
            currentSlot.ClearOnly();
            currentSlot = null;
        }

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
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (currentSlot == null)
        {
            if (slotAtDragStart != null && slotAtDragStart.IsEmpty)
            {
                slotAtDragStart.PlaceWord(this);
            }
            else
            {
                ReturnToStart();
            }
        }

        slotAtDragStart = null;
    }

    public void PlaceInSlot(WordSlot slot)
    {
        currentSlot = slot;
        transform.SetParent(slot.transform, false);
        transform.localScale = Vector3.one;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        floatingBasePosition = Vector2.zero;
        ConfigureText();
    }

    public void ReturnToStart()
    {
        transform.SetParent(startParent, false);
        ApplyBankLayout();
        rectTransform.anchoredPosition = startAnchoredPosition;
        floatingBasePosition = startAnchoredPosition;
        floatingStartTime = Time.time + 0.15f;
    }

    public void ReturnToBank(Transform wordBankParent)
    {
        currentSlot = null;
        transform.SetParent(wordBankParent, false);
        ApplyBankLayout();
        rectTransform.anchoredPosition = Vector2.zero;
        floatingBasePosition = Vector2.zero;
        floatingStartTime = Time.time + 0.15f;
    }

    private void ApplyBankLayout()
    {
        transform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(92f, 28f);
        floatingBasePosition = rectTransform.anchoredPosition;

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = 92f;
        layoutElement.preferredWidth = 92f;
        layoutElement.minHeight = 28f;
        layoutElement.preferredHeight = 28f;
    }

    private void ConfigureText()
    {
        if (wordText == null)
        {
            return;
        }

        RectTransform textRect = wordText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);

        wordText.alignment = TextAlignmentOptions.Center;
        wordText.fontSize = 14f;
        wordText.enableAutoSizing = true;
        wordText.fontSizeMin = 8f;
        wordText.fontSizeMax = 14f;
        wordText.textWrappingMode = TextWrappingModes.NoWrap;
        wordText.overflowMode = TextOverflowModes.Ellipsis;
    }
}
