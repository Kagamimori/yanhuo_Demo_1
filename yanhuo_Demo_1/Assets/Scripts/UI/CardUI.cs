using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌UI组件，控制单张卡牌的显示和交互
/// </summary>
public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件引用")]
    public Image cardBackground;      // 卡牌背景图片
    public Text cardNameText;         // 卡牌名称文本
    public Text cardTypeText;         // 卡牌类型文本
    public Image qualityIcon;         // 品质图标（真/假）

    [Header("颜色配置")]
    public Color realCardColor = new Color(0.9f, 0.85f, 0.7f);  // 真货颜色（米黄色）
    public Color fakeCardColor = new Color(0.85f, 0.7f, 0.7f);   // 假货颜色（浅红色）
    public Color selectedColor = new Color(0.7f, 0.85f, 0.9f);    // 选中状态颜色

    [Header("图标配置")]
    public Sprite realIcon;           // 真货图标
    public Sprite fakeIcon;           // 假货图标

    // 卡牌数据
    private CardData cardData;
    private bool isSelected = false;

    // 回调委托（用于通知GameManager玩家点击了这张卡）
    public System.Action<CardUI> OnCardClick;
    public System.Action<CardUI> OnCardHover;

    /// <summary>
    /// 设置卡牌数据
    /// </summary>
    public void SetCardData(CardData data)
    {
        cardData = data;
        UpdateUI();
    }

    /// <summary>
    /// 设置为空位（明牌区专用）
    /// </summary>
    public void SetEmpty()
    {
        cardData = null;
        if (cardNameText != null)
            cardNameText.text = "空位";
        if (cardTypeText != null)
            cardTypeText.text = "";
        if (cardBackground != null)
            cardBackground.color = Color.gray;
        if (qualityIcon != null)
            qualityIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (cardData == null) return;

        // 设置卡牌名称
        if (cardNameText != null)
        {
            string qualityMark = cardData.quality == CardData.CardQuality.Real ? "【真】" : "【假】";
            cardNameText.text = $"{qualityMark}{GetCardTypeName()}";
        }

        // 设置卡牌类型
        if (cardTypeText != null)
        {
            cardTypeText.text = GetCardTypeName();
        }

        // 设置背景颜色（真货假货不同颜色）
        if (cardBackground != null)
        {
            cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
        }

        // 设置品质图标
        if (qualityIcon != null)
        {
            qualityIcon.gameObject.SetActive(true);
            qualityIcon.sprite = cardData.quality == CardData.CardQuality.Real ? realIcon : fakeIcon;
        }
    }

    /// <summary>
    /// 获取卡牌类型名称
    /// </summary>
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

    /// <summary>
    /// 获取当前卡牌数据
    /// </summary>
    public CardData GetCardData()
    {
        return cardData;
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
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
                // 恢复到原始颜色
                cardBackground.color = cardData.quality == CardData.CardQuality.Real ? realCardColor : fakeCardColor;
            }
        }
    }

    /// <summary>
    /// 清除选中状态
    /// </summary>
    public void ClearSelected()
    {
        SetSelected(false);
    }

    /// <summary>
    /// 指针点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnCardClick != null)
        {
            OnCardClick(this);
        }
    }

    /// <summary>
    /// 指针进入事件
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnCardHover != null)
        {
            OnCardHover(this);
        }

        // 添加悬停效果（可选）
        transform.localScale = Vector3.one * 1.05f;
    }

    /// <summary>
    /// 指针退出事件
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复原始大小
        transform.localScale = Vector3.one;
    }
}