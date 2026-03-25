using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }//创建实例，最新的一回合

    public List<PlayerData> players = new List<PlayerData>();
    public int currentTurnIndex = 0;
    public GameState currentState = GameState.GameStart;

    // 银行数据：真货剩余数量（按类型）
    public Dictionary<CardData.CardType, int> bankStock = new Dictionary<CardData.CardType, int>();

    // 卡池：所有真货和假货的初始牌堆
    private List<CardData> realCardPool = new List<CardData>();
    private List<CardData> fakeCardPool = new List<CardData>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeGame();
    }


    void InitializeGame()
    {
        // 生成牌池
        GenerateCardPools();
        //需求：根据卡牌预设体数组随机打乱并分配牌组

        // 创建玩家数据
        for (int i = 0; i < 3; i++)
        {
            PlayerData p = new PlayerData();
            p.playerIndex = i;
            p.gold = 12;
            p.handCards = new List<CardData>();
            p.openCards = new List<CardData>();
            for (int j = 0; j < 9; j++) p.openCards.Add(null);
            p.hasSoldThisTurn = false;
            p.hasPlacedThisTurn = false;
            p.hasBoughtThisTurn = false;
            p.rejectedBuyers = new List<int>();

            // 抽4真4假
            for (int t = 0; t < 4; t++)
            {
                p.handCards.Add(DrawRandomCard(true));
                //按传入的真假分配牌的比例
                p.handCards.Add(DrawRandomCard(false));
            }
            players.Add(p);
        }

        // 随机分配身份
        int fakeIndex = Random.Range(0, 3);
        for (int i = 0; i < 3; i++)
        {
            players[i].isRealMerchant = (i != fakeIndex);
        }

        // 银行初始真货各2张
        bankStock[CardData.CardType.Sugar] = 2;
        bankStock[CardData.CardType.Oil] = 2;
        bankStock[CardData.CardType.Flour] = 2;

        // 随机起始玩家
        currentTurnIndex = Random.Range(0, 3);
        StartTurn();
    }


    void StartTurn()
    {
        PlayerData currentPlayer = players[currentTurnIndex];

        // 重置标记
        currentPlayer.hasSoldThisTurn = false;
        currentPlayer.hasPlacedThisTurn = false;
        currentPlayer.hasBoughtThisTurn = false;
        currentPlayer.rejectedBuyers.Clear();

        // 发放金币
        int goldToAdd = 4;
        if (IsGoldLeader(currentPlayer)) goldToAdd -= 2;
        //获取每个玩家的金币并计算
        currentPlayer.gold += goldToAdd;

        // 更新UI
        UpdateAllUI();
        //暂时不知道怎么写
        Log($"玩家{currentTurnIndex + 1}回合开始，获得{goldToAdd}金币，当前金币{currentPlayer.gold}");
        //输出到UI组件上
        currentState = GameState.Phase1_Sell;
        // 启用阶段1的UI按钮
        EnablePhase1Buttons(true);
    }


    public void OnSellRealCard(CardData card)
    {
        PlayerData currentPlayer = players[currentTurnIndex];
        if (currentState != GameState.Phase1_Sell || currentPlayer.hasSoldThisTurn)
        {
            Log("当前不能出售真牌");
            return;
        }
        if (card.quality != CardData.CardQuality.Real)
        {
            Log("只能出售真牌");
            return;
        }
        // 执行出售
        currentPlayer.handCards.Remove(card);
        currentPlayer.gold += 8;
        currentPlayer.hasSoldThisTurn = true;
        // 卡牌回银行（如果是真货，银行增加该类型库存）
        bankStock[card.type]++;

        UpdateAllUI();
        Log($"玩家{currentTurnIndex + 1}出售了一张{GetCardName(card)}，获得8金币");

        // 自动进入阶段2
        EnablePhase1Buttons(false);
        currentState = GameState.Phase2_Action;
        EnablePhase2Buttons(true);
    }


    public void OnPlaceCard(CardData card, int openSlotIndex)
    {
        PlayerData currentPlayer = players[currentTurnIndex];
        if (currentState != GameState.Phase2_Action || currentPlayer.hasPlacedThisTurn)
        {
            Log("当前不能进行明牌操作");
            return;
        }
        if (!currentPlayer.handCards.Contains(card))
        {
            Log("手牌中没有该卡");
            return;
        }

        // 替换逻辑
        CardData oldCard = currentPlayer.openCards[openSlotIndex];
        if (oldCard != null)
        {
            currentPlayer.handCards.Add(oldCard);
        }
        currentPlayer.handCards.Remove(card);
        currentPlayer.openCards[openSlotIndex] = card;
        currentPlayer.hasPlacedThisTurn = true;

        UpdateAllUI();
        Log($"玩家{currentTurnIndex + 1}在明牌区{openSlotIndex + 1}号位放置了{GetCardName(card)}");

        // 检查是否两项都已完成
        if (currentPlayer.hasPlacedThisTurn && currentPlayer.hasBoughtThisTurn)
            EndTurn();
    }


    public void RequestBuy(int targetIndex, CardData.CardType wantedType, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        if (currentState != GameState.Phase2_Action || buyer.hasBoughtThisTurn)
        {
            Log("当前不能购买");
            return;
        }

        if (targetIndex == -1) // 银行
        {
            BuyFromBank(wantedType, offerPrice);
        }
        else
        {
            // 检查是否之前拒绝过
            if (buyer.rejectedBuyers.Contains(targetIndex))
            {
                Log("该卖家已拒绝过你，本回合不能再向他购买");
                return;
            }
            // 显示对话框给卖家玩家（这里因为单机原型，我们简单弹出一个UI让当前玩家选择是否同意，或者我们可以实现一个简单AI）
            // 实际单机测试时，我们可以手动控制卖家是否同意，通过一个UI按钮。
            ShowSellerDialog(targetIndex, wantedType, offerPrice);
        }
    }

    void BuyFromBank(CardData.CardType wantedType, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        int actualPrice = (IsGoldLeader(buyer) ? 18 : 12);
        if (offerPrice < actualPrice)
        {
            Log("银行不接受低于标价的出价");
            return;
        }
        if (bankStock[wantedType] <= 0)
        {
            Log("银行没有该货物");
            return;
        }

        // 交易
        buyer.gold -= actualPrice;
        bankStock[wantedType]--;
        CardData newCard = new CardData(wantedType, CardData.CardQuality.Real);
        buyer.handCards.Add(newCard);

        buyer.hasBoughtThisTurn = true;
        UpdateAllUI();
        Log($"玩家{currentTurnIndex + 1}从银行购买了{GetCardTypeName(wantedType)}，花费{actualPrice}金币");

        if (buyer.hasPlacedThisTurn && buyer.hasBoughtThisTurn)
            EndTurn();
    }
    void ExecuteInspect(bool isReal, int price, PlayerData buyer, PlayerData seller)
    {
        if (isReal)
        {
            int extra = Mathf.CeilToInt(price * 0.5f);
            buyer.gold -= extra;
            seller.gold += extra;
            Log($"验货结果：真货，买家多付{extra}金币");
        }
        else
        {
            int penalty = Mathf.CeilToInt(price * 0.5f);
            seller.gold -= (price + penalty);
            buyer.gold += (price + penalty);
            Log($"验货结果：假货，卖家退还{price}并赔偿{penalty}金币");
        }
        UpdateAllUI();
    }

    void EndTurn()
    {
        // 检查胜利
        if (CheckWinCondition())
        {
            currentState = GameState.GameEnd;
            Log("游戏结束！");
            return;
        }

        // 下一个玩家
        currentTurnIndex = (currentTurnIndex + 1) % 3;
        StartTurn();
    }


    void HandleInsufficientGold(PlayerData player, int requiredGold)
    {
        while (player.gold < requiredGold)
        {
            CardData realCard = player.handCards.Find(c => c.quality == CardData.CardQuality.Real);
            if (realCard == null) break; // 没有真货可抵押，则金币为负（规则允许负？但规则说超出金额不退还，所以最后置0）
            player.handCards.Remove(realCard);
            player.gold += 8;
            bankStock[realCard.type]++;
            Log($"玩家{player.playerIndex + 1}抵押了一张{GetCardName(realCard)}，获得8金币");
        }
        if (player.gold < requiredGold) player.gold = 0; // 依然不足，则清零
    }


    bool CheckWinCondition()
    {
        foreach (var p in players)
        {
            if (p.isRealMerchant && CheckRealWin(p.openCards))
            {
                Log($"玩家{p.playerIndex + 1}（真货商人）获胜！");
                return true;
            }
            else if (!p.isRealMerchant && CheckFakeWin(p.openCards))
            {
                Log($"玩家{p.playerIndex + 1}（假货商人）获胜！");
                return true;
            }
        }
        return false;
    }

    bool CheckRealWin(List<CardData> openCards)
    {
        // 必须9格全部有牌
        if (openCards.Any(c => c == null)) return false;
        // 统计每种真货的数量
        int sugarCount = openCards.Count(c => c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Real);
        int oilCount = openCards.Count(c => c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Real);
        int flourCount = openCards.Count(c => c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Real);
        return sugarCount == 3 && oilCount == 3 && flourCount == 3;
    }

    bool CheckFakeWin(List<CardData> openCards)
    {
        // 统计每种假货的数量
        int sugarCount = openCards.Count(c => c != null && c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Fake);
        int oilCount = openCards.Count(c => c != null && c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Fake);
        int flourCount = openCards.Count(c => c != null && c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Fake);
        // 允许有两个空位（即总数7）
        int total = sugarCount + oilCount + flourCount;
        if (total != 7) return false;
        // 检查是否为3:2:2
        var counts = new List<int> { sugarCount, oilCount, flourCount };
        counts.Sort();
        return counts[2] == 3 && counts[1] == 2 && counts[0] == 2;
    }
}
/*1. 玩家面板控制器 PlayerPanelController
引用各个UI组件（金币Text，手牌容器，明牌区容器）

提供一个公共方法 Refresh(PlayerData player)，根据玩家数据更新显示

手牌容器动态生成卡牌按钮，绑定点击事件（选中卡牌）

明牌区容器动态生成9个槽位，每个槽位也是一个按钮（用于替换）

2. 全局UI控制
阶段1按钮（出售真牌）在阶段1时启用，点击后显示手牌中真货的选择面板

阶段2按钮（明牌/替换、购买）在阶段2时启用

购买时弹出界面选择目标、种类、出价

拒绝/接受对话框（单机测试时，如果目标为其他玩家，需要让当前玩家决定是否接受，可以用一个简单的选项框）

3. 日志输出
用一个Text组件或滚动窗口显示操作记录，方便调试。
*/