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
    // 拆牌机制
    public int stealCost = 14;
    // ========== 获胜条件配置 ==========
    // 真商获胜条件：每种真货所需数量
    public int realWinTotalRequired = 9;   // 总共需要多少张真牌
    public int realWinMajorMin = 3;        // 最多的那一类最少需要几张
    public int realWinMidMin = 3;          // 第二多的那一类最少需要几张

    // 假商获胜条件
    public int fakeWinTotalRequired = 7;           // 总假牌数量要求
    public int fakeWinMajorMin = 3;                // 最多一种假牌的最小数量
    public int fakeWinMidMin = 2;                  // 中间一种假牌的最小数量
    // 注意：假商获胜要求是 3/2/2 分布，即最大>=3，中间>=2，最小>=0（自动满足）
    // 可通过修改上面的值来调整，例如改成 4/2/1 或 3/3/1 等

    // UI 提示延迟
    public float cueTextDuration = 0.8f;          // 提示文字显示时间（秒）
}