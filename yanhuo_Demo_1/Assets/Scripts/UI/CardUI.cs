using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image cardBackground;
    public TMP_Text cardNameText;
    public Color realCardColor = new Color(0.9f, 0.85f, 0.7f);
    public Color fakeCardColor = new Color(0.85f, 0.7f, 0.7f);
    public Color selectedColor = new Color(0.7f, 0.85f, 0.9f);
    public Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

    private CardData cardData;
    private bool isSelected = false;
    private bool isInteractable = true;
    private Button button;

    public System.Action<CardUI> OnCardClick;

    void Awake()
    {
        // 确保 Button 组件存在
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        button.transition = Selectable.Transition.ColorTint;
    }

    public void SetCardData(CardData data)
    {
        cardData = data;
        UpdateUI();
        UpdateInteractable();
    }

    public void SetEmpty()
    {
        cardData = null;
        if (cardNameText != null)
            cardNameText.text = "空位";
        if (cardBackground != null)
            cardBackground.color = emptyColor;
        UpdateInteractable();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        UpdateInteractable();
    }

    void UpdateInteractable()
    {
        // 添加空值检查
        if (button != null)
            button.interactable = isInteractable;

        if (!isInteractable && cardBackground != null)
        {
            Color c = cardBackground.color;
            c.a = 0.6f;
            cardBackground.color = c;
        }
    }

    void UpdateUI()
    {
        if (cardData == null) return;
        if (cardNameText != null)
        {
            string qualityMark = cardData.quality == CardData.CardQuality.Real ? "【真】" : "【假】";
            cardNameText.text = $"{qualityMark}{GetCardTypeName()}";
        }
        if (cardBackground != null)
            cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
    }

    string GetCardTypeName()
    {
        if (cardData == null) return "";
        switch (cardData.type)
        {
            case CardData.CardType.Sugar: return "糖";
            case CardData.CardType.Oil: return "油";
            case CardData.CardType.Flour: return "面";
            default: return "未知";
        }
    }

    public CardData GetCardData() => cardData;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (cardBackground != null)
        {
            if (selected) cardBackground.color = selectedColor;
            else if (cardData != null) cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
            else//没被选择，自动灰
                cardBackground.color = emptyColor;
        }
    }

    public void ClearSelected() => SetSelected(false);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isInteractable && OnCardClick != null)
            OnCardClick(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isInteractable && cardBackground != null && cardData != null)
        {
            cardBackground.color = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isInteractable && cardBackground != null && cardData != null)
        {
            cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
        }
        if (isSelected) cardBackground.color = selectedColor;
    }
}