using System.Collections.Generic;
using LitJson;

[System.Serializable]
public class GameConfig
{
    // 游戏基础数值
    public int playerCount = 3;                 // 玩家数量
    public int initialGold = 12;                // 初始金币
    public int turnBaseGold = 4;                // 每回合基础获得金币
    public int goldLeaderPenalty = 2;           // 金币领先者每回合少获得金币
    public int sellRealCardReward = 8;          // 出售真牌获得金币
    public int mortgageGain = 8;                // 抵押真牌获得金币

    // 银行相关
    public int bankPriceNormal = 12;             // 银行普通价格
    public int bankPriceLeader = 18;             // 银行金币领先者价格
    public Dictionary<string, int> bankInitialStock = new Dictionary<string, int>()
    {
        { "Sugar", 2 },
        { "Oil", 2 },
        { "Flour", 2 }
    };

    // 卡牌池数量
    public int realSugarCount = 18;
    public int realOilCount = 18;
    public int realFlourCount = 18;
    public int fakeSugarCount = 12;
    public int fakeOilCount = 12;
    public int fakeFlourCount = 12;

    // 初始手牌
    public int initialRealCardsPerPlayer = 4;    // 每个玩家初始真牌数量
    public int initialFakeCardsPerPlayer = 4;    // 每个玩家初始假牌数量

    // 明牌区
    public int openCardSlotCount = 9;            // 明牌区槽位数

    // 购买/验货
    public float inspectFeeRatio = 0.5f;          // 验货费比例（向上取整）
    public int defaultPlayerBuyPrice = 10;        // 玩家购买默认出价

    // 获胜条件
    public int realWinRequired = 3;               // 真商每种货物所需数量
    public int fakeWinTotal = 7;                  // 假商所需总假牌数
    public int fakeWinMinMajor = 3;               // 假商最多一种的数量
    public int fakeWinMid = 2;                    // 假商第二种的数量（排序后中间值≥2即可）

    // UI 提示延迟
    public float cueTextDuration = 0.8f;          // 提示文字显示时间（秒）
}