using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color errorColor = Color.red;

    private DraggableWord placedWord;
    private Transform wordBankParent;

    public bool IsEmpty => placedWord == null;

    public string CurrentWord
    {
        get
        {
            if (placedWord == null)
            {
                return "";
            }

            return placedWord.Word;
        }
    }

    private void Awake()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (background != null)
        {
            normalColor = background.color;
        }

        ConfigureLayout();
    }

    public void Setup(Transform bankParent)
    {
        wordBankParent = bankParent;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!IsEmpty)
        {
            return;
        }

        DraggableWord word = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (word == null)
        {
            return;
        }

        PlaceWord(word);
    }

    public void PlaceWord(DraggableWord word)
    {
        ConfigureLayout();
        placedWord = word;
        word.PlaceInSlot(this);
    }

    private void ConfigureLayout()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100f, 30f);

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = 100f;
        layoutElement.preferredWidth = 100f;
        layoutElement.minHeight = 30f;
        layoutElement.preferredHeight = 30f;
    }

    public void ClearOnly()
    {
        if (placedWord != null)
        {
            placedWord.SetCurrentSlot(null);
        }

        placedWord = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (placedWord == null)
        {
            return;
        }

        DraggableWord word = placedWord;
        placedWord = null;

        word.ReturnToBank(wordBankParent);
    }

    public void FlashRed()
    {
        StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator FlashRedRoutine()
    {
        if (background == null)
        {
            yield break;
        }

        background.color = errorColor;
        yield return new WaitForSeconds(0.2f);
        background.color = normalColor;
    }
}
