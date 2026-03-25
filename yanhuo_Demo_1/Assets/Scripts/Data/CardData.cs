[System.Serializable]
public class CardData
{
    public enum CardType { Sugar, Oil, Flour }
    public enum CardQuality { Real, Fake }

    public CardType type;
    public CardQuality quality;
    public string cardId;

    public CardData(CardType type, CardQuality quality)
    {
        this.type = type;
        this.quality = quality;
        this.cardId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 获取卡牌显示名称
    /// </summary>
    public string GetCardName()
    {
        string typeName = "";
        switch (type)
        {
            case CardType.Sugar: typeName = "糖"; break;
            case CardType.Oil: typeName = "油"; break;
            case CardType.Flour: typeName = "面"; break;
        }
        string qualityMark = quality == CardQuality.Real ? "真" : "假";
        return $"{typeName}{qualityMark}";
    }
}