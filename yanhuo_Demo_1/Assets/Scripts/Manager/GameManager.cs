using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }
    public static GameConfig Config { get; private set; }

    // 原来的字段大部分改为从配置读取，部分保留用于运行时修改（但初始值从配置加载）
    public List<PlayerData> players = new List<PlayerData>();
    public int currentTurnIndex = 0;
    public GameState currentState = GameState.GameStart;
    public Dictionary<CardData.CardType, int> bankStock = new Dictionary<CardData.CardType, int>();

    private List<CardData> realCardPool = new List<CardData>();
    private List<CardData> fakeCardPool = new List<CardData>();

    // 移除硬编码常量，改为从配置读取
    private int REAL_SUGAR_COUNT, REAL_OIL_COUNT, REAL_FLOUR_COUNT;
    private int FAKE_SUGAR_COUNT, FAKE_OIL_COUNT, FAKE_FLOUR_COUNT;
    private GameObject cardPrefab;
    [Header("UI引用")]
                // 当前玩家的详情面板
    public TMP_Text turnText;
    public TMP_Text stateText;
    public TMP_Text logText;

    [Header("对话框")]
    public GameObject sellerDialogPanel;
    public TMP_Text sellerDialogText;
    public Button acceptButton, rejectButton;
    public GameObject inspectDialogPanel;
    public TMP_Text inspectDialogText;
    public Button inspectButton, noInspectButton;
    public Transform sellerHandCardContainer;
    //
    public Button AButton;
    public Button BButton;

    [Header("字体")]
    public TMP_FontAsset chineseFont;

    // 交易临时数据
    private int currentTransactionBuyerIndex, currentTransactionSellerIndex;
    private CardData.CardType currentTransactionType;
    private int currentTransactionPrice;
    private CardData currentTransactionCard;
    private PlayerData currentTransactionBuyer, currentTransactionSeller;
    private bool isInInspectFlow = false;

    private int currentTransactionHandIndex = -1;  // 卖家选中的手牌索引
    private bool isSellerSelecting = false;        // 卖家是否正在选择手牌
    private CardData pendingTransactionCard;
    private PlayerData pendingTransactionBuyer;
    private PlayerData pendingTransactionSeller;
    private int pendingTransactionPrice;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 加载配置文件
        LoadConfig();
    }
    
    //
    private bool isInitialized = false;
    void Start()
    {
        //预防反复触发形成空值
        if (isInitialized) return;
        isInitialized = true;
        if (Panel.Instance != null)
            cardPrefab = Panel.Instance.cardPrefab;
        InitializeGame();
        
        UpdateAllUI();

        if (chineseFont == null)
        {
            Debug.LogError("请将中文字体拖拽到 GameManager 的 chineseFont 字段！");
            return;
        }

        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var text in allTexts)
        {
            text.font = chineseFont;
        }

        AButton.onClick.AddListener(() => AButton.gameObject.SetActive(false));
        BButton.onClick.AddListener(() => BButton.gameObject.SetActive(false));

        StartCoroutine(SetFontForNewTexts(chineseFont));//启动这个协程，协程是一种可以停住的函数（加载字体不是一直调用的）
    }
    
    IEnumerator SetFontForNewTexts(TMP_FontAsset font)//协程函数
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);//函数在这里会停一会
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
            foreach (var text in texts)
            {
                if (text.font != font && text.font != null)
                {
                    text.font = font;
                }
            }
        }
    }

    void InitializeGame()
    {
        players.Clear();
        GenerateCardPools(); // 现在根据配置生成，用泛型数组和random去获得打乱的牌组
        for (int i = 0; i < Config.playerCount; i++)
        {
            PlayerData p = new PlayerData();
            p.playerIndex = i;
            p.gold = Config.initialGold;  // 使用配置中的初始金币
            p.handCards = new List<CardData>();
            p.openCards = new List<CardData>();
            for (int j = 0; j < Config.openCardSlotCount; j++)
                p.openCards.Add(null);
            p.hasSoldThisTurn = false;//状态一定要重置
            p.hasPlacedThisTurn = false;
            p.hasBoughtThisTurn = false;
            p.rejectedBuyers = new List<int>();//用拒绝的玩家索引制造数组，在购买时检查该索引玩家是否在数组内

            // 添加初始手牌
            for (int t = 0; t < Config.initialRealCardsPerPlayer; t++)
            {
                CardData realCard = DrawRandomCard(true);//先获取泛型牌组，再打乱，再一张一张发
                if (realCard != null) p.handCards.Add(realCard);
            }
            for (int t = 0; t < Config.initialFakeCardsPerPlayer; t++)
            {
                CardData fakeCard = DrawRandomCard(false);
                if (fakeCard != null) p.handCards.Add(fakeCard);
            }
            players.Add(p);
        }

        // 随机设置假货商人
        int fakeIndex = Random.Range(0, Config.playerCount);
        for (int i = 0; i < Config.playerCount; i++)
            players[i].isRealMerchant = (i != fakeIndex);

        // 初始化银行库存
        bankStock.Clear();
        bankStock[CardData.CardType.Sugar] = Config.bankInitialStock["Sugar"];
        bankStock[CardData.CardType.Oil] = Config.bankInitialStock["Oil"];
        bankStock[CardData.CardType.Flour] = Config.bankInitialStock["Flour"];

        currentTurnIndex = Random.Range(0, Config.playerCount);
        StartTurn();
    }

    void StartTurn()
    {
        //
        AButton.gameObject.SetActive(true);
        //
        PlayerData cur = players[currentTurnIndex];
        cur.hasSoldThisTurn = cur.hasPlacedThisTurn = cur.hasBoughtThisTurn = false;//再次重置
        cur.hasBankPurchaseFailed = false;
        cur.hasStolenThisTurn = false;
        cur.hasRejectedThisTurn = false;
        cur.rejectedBuyers.Clear();

        // 使用配置中的基础金币和领先者惩罚
        int goldToAdd = Config.turnBaseGold;
        if (IsGoldLeader(cur)) goldToAdd -= Config.goldLeaderPenalty;
        cur.gold += goldToAdd;

        currentState = GameState.Phase1_Sell;
        
        // 刷新UI - 重要！
        if (Panel.Instance != null)
        {
            Panel.Instance.UpdateCurrentPlayerUI();
        }
        else
        {
            Debug.LogError("Panel.Instance 为空！");
        }
        Panel.Instance?.UpdateCurrentPlayerUI();//更新手牌和展示手牌的方法是分开的//4 5
        UpdateAllUI();
        Panel.Instance.AddCue($"你的回合开始，请选择是否出售真牌");
        Panel.Instance.AddLog($"玩家{currentTurnIndex + 1}回合开始，获得{goldToAdd}金币，当前金币{cur.gold}");
        Panel.Instance.AddLog($"阶段1：可以出售真牌");
        Panel.Instance?.ShowCurrentPlayerOpenCards();//自动显示
    }

    public void SkipToPhase2()
    {
        if (currentState != GameState.Phase1_Sell) return;
        currentState = GameState.Phase2_Action;
        // 更新UI按钮状态
        EnablePhase1Buttons(false);
        EnablePhase2Buttons(true);

        UpdateAllUI();
        Panel.Instance.AddCue("进入阶段2：进行明牌和购买操作");
        Panel.Instance.AddLog("进入阶段2：可以明牌或购买");
    }

    #region 牌池管理
    private void GenerateCardPools()//分为真卡池和假卡池
    {
        realCardPool.Clear();//泛型数组
        fakeCardPool.Clear();

        // 使用配置中的数量
        for (int i = 0; i < REAL_SUGAR_COUNT; i++)
            realCardPool.Add(new CardData(CardData.CardType.Sugar, CardData.CardQuality.Real));
        for (int i = 0; i < REAL_OIL_COUNT; i++)
            realCardPool.Add(new CardData(CardData.CardType.Oil, CardData.CardQuality.Real));
        for (int i = 0; i < REAL_FLOUR_COUNT; i++)
            realCardPool.Add(new CardData(CardData.CardType.Flour, CardData.CardQuality.Real));

        for (int i = 0; i < FAKE_SUGAR_COUNT; i++)
            fakeCardPool.Add(new CardData(CardData.CardType.Sugar, CardData.CardQuality.Fake));
        for (int i = 0; i < FAKE_OIL_COUNT; i++)
            fakeCardPool.Add(new CardData(CardData.CardType.Oil, CardData.CardQuality.Fake));
        for (int i = 0; i < FAKE_FLOUR_COUNT; i++)
            fakeCardPool.Add(new CardData(CardData.CardType.Flour, CardData.CardQuality.Fake));

        ShuffleCardPool(realCardPool);
        ShuffleCardPool(fakeCardPool);
    }

    private void ShuffleCardPool(List<CardData> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            int randomIndex = Random.Range(i, pool.Count);
            CardData temp = pool[i];
            pool[i] = pool[randomIndex];
            pool[randomIndex] = temp;
        }
    }
    #endregion
    void LoadConfig()//查找文件并修改为局部变量
    {
        TextAsset configFile = Resources.Load<TextAsset>("game_config");
        if (configFile != null)
        {
            Config = JsonMapper.ToObject<GameConfig>(configFile.text);
            Debug.Log("游戏配置加载成功");
        }
        else
        {
            Debug.LogError("未找到 game_config.json，使用默认值");
            Config = new GameConfig();
        }

        // 将配置中的数量赋值给局部变量
        REAL_SUGAR_COUNT = Config.realSugarCount;
        REAL_OIL_COUNT = Config.realOilCount;
        REAL_FLOUR_COUNT = Config.realFlourCount;
        FAKE_SUGAR_COUNT = Config.fakeSugarCount;
        FAKE_OIL_COUNT = Config.fakeOilCount;
        FAKE_FLOUR_COUNT = Config.fakeFlourCount;
    }

    
    private CardData DrawRandomCard(bool isReal)
    {
        List<CardData> targetPool = isReal ? realCardPool : fakeCardPool;

        if (targetPool.Count == 0)//先有牌池，再发牌
        {
            Debug.LogWarning($"{(isReal ? "真货" : "假货")}牌池已空！");
            return null;
        }
        //从“牌顶”发牌
        int lastIndex = targetPool.Count - 1;
        CardData drawnCard = targetPool[lastIndex];
        targetPool.RemoveAt(lastIndex);
        
        return drawnCard;
    }
  
    public void EndTurn()
    {
        if (CheckWinCondition())
        {
            currentState = GameState.GameEnd;
            Panel.Instance.AddCue("游戏结束！");
            return;
        }
        

        PlayerData cur = players[currentTurnIndex];
        Panel.Instance.AddLog($"玩家{currentTurnIndex + 1}回合结束");
        string leaderName = $"玩家{players.OrderByDescending(p => p.gold).First().playerIndex + 1}";
        Panel.Instance.AddLog($"当前金币领先者：{leaderName}");

        currentTurnIndex = (currentTurnIndex + 1) % 3;
        StartTurn();
    }

    public bool IsGoldLeader(PlayerData player) => player.gold == players.Max(p => p.gold);

    public void OnSellRealCard(CardData card)
    {
        PlayerData cur = players[currentTurnIndex];
        if (currentState != GameState.Phase1_Sell || cur.hasSoldThisTurn) return;
        if (card.quality != CardData.CardQuality.Real) return;

        cur.handCards.Remove(card);
        cur.gold += Config.sellRealCardReward;  // 使用配置的出售收益
        cur.hasSoldThisTurn = true;
        bankStock[card.type]++;
        UpdateAllUI();
        Panel.Instance.AddLog($"出售了一张{GetCardName(card)}，获得{Config.sellRealCardReward}金币");
        Panel.Instance.AddCue("出售成功，点击「Next」按钮进入下一阶段");
        EnablePhase1Buttons(false);
    }

    void BuyFromBank(CardData.CardType type, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        int actualPrice = IsGoldLeader(buyer) ? Config.bankPriceLeader : Config.bankPriceNormal;

        if (bankStock[type] <= 0)
        {
            Panel.Instance.AddCue("银行没有该货物");
            buyer.hasBankPurchaseFailed = true;
            Panel.Instance.OnBankBuyFailed();
            return;
        }

        if (buyer.gold < actualPrice)
        {
            Panel.Instance.AddCue($"金币不足，需要{actualPrice}金币");
            buyer.hasBankPurchaseFailed = true;
            Panel.Instance.OnBankBuyFailed();
            return;
        }

        buyer.gold -= actualPrice;
        bankStock[type]--;
        buyer.handCards.Add(new CardData(type, CardData.CardQuality.Real));
        buyer.hasBoughtThisTurn = true;
        buyer.hasBankPurchaseFailed = false;

        UpdateAllUI();
        Panel.Instance.AddLog($"从银行购买了{GetCardTypeName(type)}，花费{actualPrice}金币");
        Panel.Instance.OnBuyComplete();
    }

    public void SelectCardForPlace(CardData card, int handIndex) { } // 由Panel管理

    public void OnPlaceCardSelected(int slotIndex) { } // 由Panel管理

    public void RequestBuy(int targetIndex, CardData.CardType wantedType, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        if (currentState != GameState.Phase2_Action || buyer.hasBoughtThisTurn) return;

        if (targetIndex == -1)
        {
            BuyFromBank(wantedType, offerPrice);
        }
        else
        {
            if (buyer.rejectedBuyers.Contains(targetIndex))
            {
                Panel.Instance.AddCue("该卖家已拒绝过你，本回合不能再向他购买");
                return;
            }
            ShowSellerDialog(targetIndex, wantedType, offerPrice);
        }
    }

   

    private void ShowSellerDialog(int sellerIndex, CardData.CardType wantedType, int offerPrice)
    {
        currentTransactionBuyerIndex = currentTurnIndex;
        currentTransactionSellerIndex = sellerIndex;
        currentTransactionType = wantedType;
        currentTransactionPrice = offerPrice;

        // 重置选择状态
        currentTransactionHandIndex = -1;
        isSellerSelecting = true;
        PlayerData seller = players[sellerIndex];
        bool canReject = !seller.hasRejectedThisTurn;
        // 设置对话框标题
        if (sellerDialogText != null)
        {
            sellerDialogText.text = $"玩家{sellerIndex + 1}，\n" +
                                    $"玩家{currentTurnIndex + 1}想以{offerPrice}金币的价格\n" +
                                    $"向您购买【{GetCardTypeName(wantedType)}】\n\n" +
                                    $"请点击手牌选择要出售的{GetCardTypeName(wantedType)}";
        }

        // 刷新卖家的手牌显示
        RefreshSellerHandCards(sellerIndex, wantedType);

        // 设置按钮文本和事件
        // 设置 reject 按钮
        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => OnSellerResponse(false));
            rejectButton.interactable = canReject;

            TMP_Text rejectText = rejectButton.GetComponentInChildren<TMP_Text>();
            if (rejectText != null)
            {
                rejectText.text = canReject ? "Refuse" : "已拒绝过";
            }
        }
        if (acceptButton != null)
        {
            acceptButton.GetComponentInChildren<TMP_Text>().text = "Confirm";
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => OnSellerConfirm());
            acceptButton.interactable = false;  // 初始禁用，直到选中卡牌
        }

        

        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(true);

        //
        BButton.gameObject.SetActive(true);
        AButton.gameObject.SetActive(true);
    }
    private void RefreshSellerHandCards(int sellerIndex, CardData.CardType wantedType)
    {
        // 使用专门的容器
        if (sellerHandCardContainer == null)
        {
            Debug.LogError("sellerHandCardContainer 未赋值！请在 Inspector 中拖拽 SellerDialogPanel 下的 HandCardContainer");
            return;
        }

        // 清空容器
        foreach (Transform child in sellerHandCardContainer)
            Destroy(child.gameObject);

        PlayerData seller = players[sellerIndex];

        // 只显示符合购买类型的卡牌
        List<CardData> availableCards = seller.handCards.FindAll(c => c.type == wantedType);

        if (availableCards.Count == 0)
        {
            TMP_Text emptyText = CreateEmptyText(sellerHandCardContainer, "没有可出售的该类型货物");
            return;
        }

        // 创建卡牌按钮
        for (int i = 0; i < availableCards.Count; i++)
        {
            CardData card = availableCards[i];
            int cardIndex = i;

            // 使用 Panel 中的 cardPrefab
            GameObject cardObj = Instantiate(Panel.Instance.cardPrefab, sellerHandCardContainer);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            cardUI.SetCardData(card);
            cardUI.SetInteractable(true);

            // 绑定点击事件
            cardUI.OnCardClick = (ui) => OnSellerCardSelected(card, cardIndex);
        }

        // 设置 GridLayoutGroup
        GridLayoutGroup grid = sellerHandCardContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = sellerHandCardContainer.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 120);
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
    }
    private TMP_Text CreateEmptyText(Transform parent, string message)
    {
        GameObject textObj = new GameObject("EmptyText");
        textObj.transform.SetParent(parent);
        TMP_Text text = textObj.AddComponent<TMP_Text>();
        text.text = message;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }
    private void OnSellerCardSelected(CardData card, int cardIndex)
    {
        if (!isSellerSelecting) return;

        // 清除之前的高亮
        ClearSellerCardHighlights();

        // 记录选中的卡牌
        currentTransactionCard = card;
        currentTransactionHandIndex = cardIndex;

        // 高亮选中的卡牌
        HighlightSellerCard(card);

        // 启用确认按钮
        if (acceptButton != null)
        {
            acceptButton.interactable = true;
            acceptButton.GetComponentInChildren<TMP_Text>().text = "Sell";
        }

        // 更新提示文本
        if (sellerDialogText != null)
        {
            sellerDialogText.text = $"已选中 {card.GetCardName()}，点击「Sell」确认出售";
        }
    }
    private void HighlightSellerCard(CardData card)
    {
        if (sellerHandCardContainer == null) return;

        foreach (Transform child in sellerHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null && ui.GetCardData() == card)
            {
                ui.SetSelected(true);
            }
        }
    }

    // 清除卖家卡牌高亮
    private void ClearSellerCardHighlights()
    {
        if (sellerHandCardContainer == null) return;

        foreach (Transform child in sellerHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.SetSelected(false);
            }
        }
    }

    // 卖家确认出售（点击 Sell 按钮）
    private void OnSellerConfirm()
    {
        if (!isSellerSelecting) return;

        if (currentTransactionCard == null)
        {
            Panel.Instance?.AddCue("请先选择要出售的卡牌");
            return;
        }
        
        
        // 确认出售，执行交易
        isSellerSelecting = false;
        ProcessAcceptedTransaction();
    }


    private void OnSellerResponse(bool accepted)
    {
        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(false);

        if (!accepted)
        {
            ProcessRejectedTransaction();
        }
        // 注意：如果 accepted 为 true，实际交易在 OnSellerConfirm 中执行，这里不处理
    }

    private void ProcessAcceptedTransaction()
    {
        PlayerData buyer = players[currentTransactionBuyerIndex];
        PlayerData seller = players[currentTransactionSellerIndex];

        // 使用已选中的卡牌
        if (currentTransactionCard == null)
        {
            Panel.Instance?.AddCue("交易失败：未选中卡牌");
            return;
        }

        // 验证卡牌是否还在手牌中
        if (!seller.handCards.Contains(currentTransactionCard))
        {
            Panel.Instance?.AddCue("选中的卡牌已不存在");
            ProcessRejectedTransaction();
            return;
        }

        // 打印购买前的金币
        Panel.Instance?.AddLog($"=== 购买前 ===");
        Panel.Instance?.AddLog($"买家{buyer.playerIndex + 1}金币: {buyer.gold}");
        Panel.Instance?.AddLog($"卖家{seller.playerIndex + 1}金币: {seller.gold}");

        // 检查买家金币是否足够
        if (buyer.gold < currentTransactionPrice)
        {
            Panel.Instance?.AddCue($"玩家{buyer.playerIndex + 1}金币不足，需要{currentTransactionPrice}金币");

            // 处理买家金币不足：扣一张真牌，金币清零，卖家获得应得金币
            HandleInsufficientGold(buyer, currentTransactionPrice, seller, currentTransactionPrice);
        }
        else
        {
            // 正常支付
            buyer.gold -= currentTransactionPrice;
            seller.gold += currentTransactionPrice;
            Panel.Instance?.AddCue($"玩家{buyer.playerIndex + 1}支付{currentTransactionPrice}金币给玩家{seller.playerIndex + 1}");
        }

        // 注意：这里不转移卡牌！等待验货结果
        // 保存临时卡牌信息，用于验货后转移
        pendingTransactionCard = currentTransactionCard;
        pendingTransactionBuyer = buyer;
        pendingTransactionSeller = seller;
        pendingTransactionPrice = currentTransactionPrice;

        Panel.Instance?.AddLog($"玩家{currentTransactionBuyerIndex + 1}以{currentTransactionPrice}金币从玩家{currentTransactionSellerIndex + 1}购买了{GetCardName(currentTransactionCard)}（待验货）");

        // 打印购买后的金币
        Panel.Instance?.AddLog($"=== 购买后 ===");
        Panel.Instance?.AddLog($"买家{buyer.playerIndex + 1}金币: {buyer.gold}");
        Panel.Instance?.AddLog($"卖家{seller.playerIndex + 1}金币: {seller.gold}");

        // 打印所有玩家金币
        LogAllPlayersGold();

        Panel.Instance?.UpdateCurrentPlayerUI();

        // 关闭对话框
        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(false);

        Panel.Instance?.AddLog($"设置验货临时数据 - 卡牌: {GetCardName(pendingTransactionCard)}, 买家: {pendingTransactionBuyer.playerIndex + 1}, 卖家: {pendingTransactionSeller.playerIndex + 1}, 价格: {pendingTransactionPrice}");

        // 显示验货对话框
        ShowInspectDialog(pendingTransactionCard, pendingTransactionPrice);

        // 重置交易数据
        currentTransactionCard = null;
        currentTransactionHandIndex = -1;
        isSellerSelecting = false;
    }

    private void ProcessRejectedTransaction()
    {
        PlayerData buyer = players[currentTransactionBuyerIndex];
        PlayerData seller = players[currentTransactionSellerIndex];


        if (!buyer.rejectedBuyers.Contains(currentTransactionSellerIndex))
        {
            buyer.rejectedBuyers.Add(currentTransactionSellerIndex);
        }
        seller.hasRejectedThisTurn = true;
        Panel.Instance?.AddLog($"玩家{currentTransactionSellerIndex + 1}拒绝了交易请求");

        // 重置选择状态
        currentTransactionCard = null;
        currentTransactionHandIndex = -1;
        isSellerSelecting = false;

        // 通过 Panel 重新打开购买选择面板
        Panel.Instance?.ReopenBuyTargetPanel();
    }

    private void ShowInspectDialog(CardData card, int price)
    {
        // 使用传入的参数，不依赖全局变量
        string goodsName = GetCardTypeName(card.type);
        int inspectPrice = Mathf.CeilToInt(price * 0.5f);

        Panel.Instance?.AddLog($"显示验货对话框 - 卡牌: {GetCardName(card)}, 价格: {price}, 验货费: {inspectPrice}");

        if (inspectDialogText != null)
        {
            inspectDialogText.text = $"您购买了一张【{goodsName}】\n\n是否花费{inspectPrice}金币进行验货？\n\n" +
                                     $"验货后如果是真货，买家多付{inspectPrice}金币\n" +
                                     $"如果是假货，卖家退还{price}金币并赔偿{inspectPrice}金币";
        }

        if (inspectDialogPanel != null)
            inspectDialogPanel.SetActive(true);

        if (inspectButton != null)
        {
            inspectButton.onClick.RemoveAllListeners();
            inspectButton.onClick.AddListener(() => {
                Panel.Instance?.AddLog($"验货按钮点击 - 卡牌: {GetCardName(card)}");
                OnInspectChoice(true);
            });
        }

        if (noInspectButton != null)
        {
            noInspectButton.onClick.RemoveAllListeners();
            noInspectButton.onClick.AddListener(() => {
                Panel.Instance?.AddLog($"不验货按钮点击 - 卡牌: {GetCardName(card)}");
                OnInspectChoice(false);
            });
        }
    }

    private void OnInspectChoice(bool inspect)
    {
        Panel.Instance?.AddLog($"OnInspectChoice 被调用 - inspect: {inspect}");

        if (inspectDialogPanel != null)
            inspectDialogPanel.SetActive(false);

        // 使用临时变量
        if (pendingTransactionCard == null || pendingTransactionBuyer == null || pendingTransactionSeller == null)
        {
            Panel.Instance?.AddCue($"交易数据无效");
            Panel.Instance?.AddLog($"交易数据无效");
            pendingTransactionCard = null;
            pendingTransactionBuyer = null;
            pendingTransactionSeller = null;
            return;
        }

        // 保存数据
        CardData card = pendingTransactionCard;
        PlayerData buyer = pendingTransactionBuyer;
        PlayerData seller = pendingTransactionSeller;
        int price = pendingTransactionPrice;

        Panel.Instance?.AddLog($"处理交易结果 - 卡牌: {GetCardName(card)}, 买家: {buyer.playerIndex + 1}, 卖家: {seller.playerIndex + 1}");

        if (inspect)
        {
            // 验货：先处理金币，再转移卡牌
            bool isReal = (card.quality == CardData.CardQuality.Real);
            ExecuteInspect(isReal, price, buyer, seller);

            // 验货后转移卡牌
            seller.handCards.Remove(card);
            buyer.handCards.Add(card);
            Panel.Instance?.AddLog($"验货完成，卡牌已转移: {GetCardName(card)}");
            Panel.Instance?.AddCue($"验货完成，{GetCardName(card)}已加入你的手牌");
        }
        else
        {
            // 不验货：直接转移卡牌
            seller.handCards.Remove(card);
            buyer.handCards.Add(card);
            Panel.Instance?.AddLog($"玩家{buyer.playerIndex + 1}选择不验货，卡牌已转移: {GetCardName(card)}");
            Panel.Instance?.AddCue($"交易完成，{GetCardName(card)}已加入你的手牌");
        }
       
        // 标记买家已购买
        buyer.hasBoughtThisTurn = true;

        // 刷新UI
        Panel.Instance?.UpdateCurrentPlayerUI();

        // 打印所有玩家金币和手牌信息
        LogAllPlayersGold();
        Panel.Instance?.AddLog($"买家手牌数量: {buyer.handCards.Count}");
        Panel.Instance?.AddLog($"卖家手牌数量: {seller.handCards.Count}");

        // 清空临时数据
        pendingTransactionCard = null;
        pendingTransactionBuyer = null;
        pendingTransactionSeller = null;

        // 注意：这里不调用 CheckTurnEnd()，让玩家继续本回合的其他操作
        // 如果玩家已经完成了明牌操作，会在 Panel 的 Update 中自动结束回合
    }

    void ExecuteInspect(bool isReal, int price, PlayerData buyer, PlayerData seller)
    {
        // 添加空值检查
        if (buyer == null || seller == null)
        {
            Panel.Instance?.AddCue("验货失败：买家或卖家数据无效");
            return;
        }

        // 使用配置中的验货费比例
        int extra = Mathf.CeilToInt(price * Config.inspectFeeRatio);

        Panel.Instance?.AddLog($"=== 验货处理 ===");
        Panel.Instance?.AddLog($"买家{buyer.playerIndex + 1}金币: {buyer.gold}");
        Panel.Instance?.AddLog($"卖家{seller.playerIndex + 1}金币: {seller.gold}");

        if (isReal)
        {
            // 真货：买家多付 extra 金币给卖家
            Panel.Instance?.AddLog($"验货：真货，买家需多付{extra}金币");

            if (buyer.gold >= extra)
            {
                buyer.gold -= extra;
                seller.gold += extra;
                Panel.Instance?.AddCue($"验货结果：真货，买家多付{extra}金币");
            }
            else
            {
                Panel.Instance?.AddCue($"买家金币不足，需要{extra}金币，当前{buyer.gold}金币");
                // 处理买家金币不足：扣真牌，获得抵押金币，然后支付
                HandleInsufficientGold(buyer, extra, seller, extra);
            }
        }
        else
        {
            // 假货：卖家退还 price + 赔偿 extra 给买家
            int totalRefund = price + extra;
            Panel.Instance?.AddLog($"验货：假货，卖家需退还{price}并赔偿{extra}金币，共{totalRefund}金币");

            if (seller.gold >= totalRefund)
            {
                seller.gold -= totalRefund;
                buyer.gold += totalRefund;
                Panel.Instance?.AddCue($"验货结果：假货，卖家退还{price}并赔偿{extra}金币");
            }
            else
            {
                Panel.Instance?.AddCue($"卖家金币不足，需要退还{totalRefund}金币，当前{seller.gold}金币");
                // 处理卖家金币不足：扣真牌，获得抵押金币，然后支付
                HandleInsufficientGold(seller, totalRefund, buyer, totalRefund);
            }
        }

        Panel.Instance?.AddLog($"验货后 - 买家金币: {buyer.gold}, 卖家金币: {seller.gold}");
    }
    private bool HandleInsufficientGold(PlayerData player, int requiredGold, PlayerData receiver, int amountToGive)
    {
        if (player == null)
        {
            Panel.Instance?.AddCue("处理失败：玩家数据无效");
            return false;
        }

        Panel.Instance?.AddCue($"玩家{player.playerIndex + 1}需要{requiredGold}金币，当前{player.gold}金币，不足！");

        // 查找一张真牌
        CardData realCard = player.handCards.Find(c => c.quality == CardData.CardQuality.Real);
        if (realCard == null)
        {
            // 没有真牌可扣，玩家破产
            Panel.Instance?.AddCue($"玩家{player.playerIndex + 1}没有真牌可扣除，宣布破产！");
            HandleBankruptcy(player);
            return false;
        }

        // 扣除一张真牌（使用配置中的抵押获得金币）
        player.handCards.Remove(realCard);
        player.gold += Config.mortgageGain;  // 使用配置的抵押收益
        bankStock[realCard.type]++;
        Panel.Instance?.AddLog($"玩家{player.playerIndex + 1}因金币不足，被强制扣除一张{GetCardName(realCard)}，获得{Config.mortgageGain}金币");
        Panel.Instance?.AddCue($"玩家{player.playerIndex + 1}被强制扣除一张{GetCardName(realCard)}，获得{Config.mortgageGain}金币");

        // 检查是否还需要继续抵押
        if (player.gold < requiredGold)
        {
            // 递归调用继续抵押
            return HandleInsufficientGold(player, requiredGold, receiver, amountToGive);
        }

        // 支付所需金币
        player.gold -= requiredGold;

        // 接收方获得应得金币
        if (receiver != null && amountToGive > 0)
        {
            receiver.gold += amountToGive;
            Panel.Instance?.AddCue($"玩家{receiver.playerIndex + 1}获得{amountToGive}金币，当前{receiver.gold}金币");
        }

        return true;
    }

    public void CheckTurnEnd()
    {
        PlayerData cur = players[currentTurnIndex];
        if (cur.hasPlacedThisTurn && cur.hasBoughtThisTurn)
        {
            EndTurn();
        }
    }

    void UpdateAllUI()//分为panel上的玩家个人信息和全局的信息
    {
        Panel.Instance.UpdateCurrentPlayerUI();
       
        if (turnText != null) turnText.text = $"当前回合: 玩家{currentTurnIndex + 1}";
        if (stateText != null) stateText.text = (currentState == GameState.Phase1_Sell) ? "阶段1：出售真牌" : "阶段2：明牌或购买";
        UpdateBankUI();
    }

    void UpdateBankUI() { /* 简单显示银行库存，可省略 */ }

    bool CheckWinCondition()
    {
        foreach (var p in players)
        {
            if (p.isRealMerchant && CheckRealWin(p.openCards))
            {
                string winnerIdentity = "真货商人";
                Panel.Instance.AddCue($"玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");
                Panel.Instance.AddLog($"游戏结束！玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");
                Debug.Log($"=== 游戏结束 === 玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");

                // 退出运行
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
                return true;
            }
            else if (!p.isRealMerchant && CheckFakeWin(p.openCards))
            {
                string winnerIdentity = "假货商人";
                Panel.Instance.AddCue($"玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");
                Panel.Instance.AddLog($"游戏结束！玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");
                Debug.Log($"=== 游戏结束 === 玩家{p.playerIndex + 1}（{winnerIdentity}）获胜！");

                // 退出运行
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
                return true;
            }
        }
        return false;
    }

    // 处理破产
    private void HandleBankruptcy(PlayerData bankruptPlayer)
    {
        if (bankruptPlayer == null) return;

        Panel.Instance?.AddCue($"玩家{bankruptPlayer.playerIndex + 1}破产！游戏结束！");
        Panel.Instance?.AddLog($"玩家{bankruptPlayer.playerIndex + 1}因无法支付而破产");

        // 将破产玩家的金币归零
        bankruptPlayer.gold = 0;

        // 找出剩下的玩家
        List<PlayerData> remainingPlayers = new List<PlayerData>();
        foreach (var player in players)
        {
            if (player != bankruptPlayer)
            {
                remainingPlayers.Add(player);
            }
        }

        if (remainingPlayers.Count == 0)
        {
            Panel.Instance?.AddCue("所有玩家都破产了！");
            Panel.Instance?.AddLog("所有玩家都破产了，游戏结束！");
            Debug.Log("=== 游戏结束 === 所有玩家都破产了！");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
            return;
        }

        // 确定获胜者
        PlayerData winner = DetermineWinner(remainingPlayers);

        // 显示获胜信息
        if (winner != null)
        {
            string winnerIdentity = winner.isRealMerchant ? "真货商人" : "假货商人";
            Panel.Instance?.AddCue($"玩家{winner.playerIndex + 1}（{winnerIdentity}）获胜！");
            Panel.Instance?.AddLog($"游戏结束，玩家{winner.playerIndex + 1}（{winnerIdentity}）获胜");
            Debug.Log($"=== 游戏结束 === 玩家{winner.playerIndex + 1}（{winnerIdentity}）获胜！");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        currentState = GameState.GameEnd;
    }

    // 确定获胜者（破产场景）
    private PlayerData DetermineWinner(List<PlayerData> remainingPlayers)
    {
        if (remainingPlayers.Count == 1)
        {
            return remainingPlayers[0];
        }

        // 找出金币最多的玩家
        int maxGold = remainingPlayers.Max(p => p.gold);
        List<PlayerData> goldLeaders = remainingPlayers.Where(p => p.gold == maxGold).ToList();

        if (goldLeaders.Count == 1)
        {
            return goldLeaders[0];
        }

        // 金币相同，比较真牌数量（手牌+明牌区）
        PlayerData winner = null;
        int maxRealCards = -1;

        foreach (var player in goldLeaders)
        {
            int realCardCount = player.handCards.Count(c => c.quality == CardData.CardQuality.Real) +
                                player.openCards.Count(c => c != null && c.quality == CardData.CardQuality.Real);

            Panel.Instance?.AddLog($"玩家{player.playerIndex + 1} 真牌数量: {realCardCount}");

            if (realCardCount > maxRealCards)
            {
                maxRealCards = realCardCount;
                winner = player;
            }
        }

        return winner;
    }
    private void LogAllPlayersGold()
    {
        Panel.Instance?.AddLog("=== 当前所有玩家金币 ===");
        for (int i = 0; i < players.Count; i++)
        {
            Panel.Instance?.AddLog($"玩家{i + 1}: {players[i].gold}金币");
        }
        Panel.Instance?.AddLog("======================");
    }
    private void EnablePhase1Buttons(bool enable)
    {
        if (Panel.Instance != null && Panel.Instance.sellButton != null)
        {
            Panel.Instance.sellButton.interactable = enable;
        }
    }

    // 启用/禁用阶段2按钮（明牌和购买按钮）
    private void EnablePhase2Buttons(bool enable)
    {
        if (Panel.Instance != null)
        {
            if (Panel.Instance.placeButton != null)
                Panel.Instance.placeButton.interactable = enable;
            if (Panel.Instance.buyButton != null)
                Panel.Instance.buyButton.interactable = enable;
        }
    }
    bool CheckRealWin(List<CardData> openCards)
    {
        if (openCards == null) return false;
        // 检查是否所有槽位都有牌（真商需要9张全满）
        //if (openCards.Any(c => c == null)) return false;

        // 使用配置中的数量要求(只遍历非空的卡牌)
        int sugar = openCards.Count(c => c != null && c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Real);
        int oil = openCards.Count(c => c != null && c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Real);
        int flour = openCards.Count(c => c != null && c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Real);
        int total = sugar + oil + flour;
        if (total < Config.realWinTotalRequired) return false;
        List<int> counts = new List<int> { sugar, oil, flour };
        counts.Sort();
        return counts[2] >= Config.realWinMajorMin && counts[1] >= Config.realWinMidMin;
        
    }

    bool CheckFakeWin(List<CardData> openCards)
    {
        if (openCards == null) return false;

        int sugar = openCards.Count(c => c != null && c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Fake);
        int oil = openCards.Count(c => c != null && c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Fake);
        int flour = openCards.Count(c => c != null && c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Fake);

        
        int total = sugar + oil + flour;

        // 检查总假牌数量是否达到要求
        if (total < Config.fakeWinTotalRequired) return false;

        // 排序后检查分布
        List<int> counts = new List<int> { sugar, oil, flour };
        counts.Sort(); // 从小到大排序

        // 条件：最大的 >= majorMin，第二大的 >= midMin
        // 最小的自动满足（因为总数达标）
        return counts[2] >= Config.fakeWinMajorMin && counts[1] >= Config.fakeWinMidMin;
    }

    string GetCardName(CardData c) => $"{GetCardTypeName(c.type)}{(c.quality == CardData.CardQuality.Real ? "真" : "假")}";
    string GetCardTypeName(CardData.CardType t) => t == CardData.CardType.Sugar ? "糖" : t == CardData.CardType.Oil ? "油" : "面";



    public int GetBankStock(CardData.CardType type) => bankStock.ContainsKey(type) ? bankStock[type] : 0;
    public bool BankHasGoods(CardData.CardType type) => GetBankStock(type) > 0;


}