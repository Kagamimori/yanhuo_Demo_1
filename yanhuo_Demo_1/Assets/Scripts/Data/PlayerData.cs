using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public int playerIndex;          // 0,1,2
    public bool isRealMerchant;      // 身份：true=真货商人，false=假货商人
    public int gold;
    public List<CardData> handCards;
    public List<CardData> openCards; // 明牌区，固定9格，没有卡牌的位置用null占位

    public bool hasBankPurchaseFailed = false;  // 是否向银行购买失败过
    public bool hasRejectedThisTurn;// 本回合是否已经拒绝过卖牌
    public bool hasStolenThisTurn;
    // 回合标记
    public bool hasSoldThisTurn;     // 阶段1是否已出售
    public bool hasPlacedThisTurn;   // 阶段2是否已执行过明牌/替换
    public bool hasBoughtThisTurn;   // 阶段2是否已购买过
    public List<int> rejectedBuyers; // 本回合拒绝过的买家索引（用于购买拒绝机制）
}