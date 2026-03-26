using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;  // 添加这个

public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件引用")]
    public Image cardBackground;
    public TMP_Text cardNameText;      // 改为 TMP_Text
    public TMP_Text cardTypeText;      // 改为 TMP_Text
    public Image qualityIcon;

    [Header("颜色配置")]
    public Color realCardColor = new Color(0.9f, 0.85f, 0.7f);
    public Color fakeCardColor = new Color(0.85f, 0.7f, 0.7f);
    public Color selectedColor = new Color(0.7f, 0.85f, 0.9f);
    public Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("图标配置")]
    public Sprite realIcon;
    public Sprite fakeIcon;

    private CardData cardData;
    private bool isSelected = false;

    public System.Action<CardUI> OnCardClick;
    public System.Action<CardUI> OnCardHover;

    public void SetCardData(CardData data)
    {
        cardData = data;
        UpdateUI();
    }

    public void SetEmpty()
    {
        cardData = null;
        if (cardNameText != null)
            cardNameText.text = "空位";
        if (cardTypeText != null)
            cardTypeText.text = "";
        if (cardBackground != null)
            cardBackground.color = emptyColor;
        if (qualityIcon != null)
            qualityIcon.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (cardData == null) return;

        if (cardNameText != null)
        {
            string qualityMark = cardData.quality == CardData.CardQuality.Real ? "【真】" : "【假】";
            cardNameText.text = $"{qualityMark}{GetCardTypeName()}";
        }

        if (cardTypeText != null)
        {
            cardTypeText.text = GetCardTypeName();
        }

        if (cardBackground != null)
        {
            cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
        }

        if (qualityIcon != null)
        {
            qualityIcon.gameObject.SetActive(true);
            qualityIcon.sprite = cardData.quality == CardData.CardQuality.Real ? realIcon : fakeIcon;
        }
    }

    private string GetCardTypeName()
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

    public CardData GetCardData()
    {
        return cardData;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (cardBackground != null)
        {
            if (selected)
            {
                cardBackground.color = selectedColor;
            }
            else if (cardData != null)
            {
                cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
            }
        }
    }

    public void ClearSelected()
    {
        SetSelected(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnCardClick != null)
        {
            OnCardClick(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnCardHover != null)
        {
            OnCardHover(this);
        }
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
}