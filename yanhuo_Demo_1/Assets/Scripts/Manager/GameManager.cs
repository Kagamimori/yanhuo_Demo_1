using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<PlayerData> players = new List<PlayerData>();
    public int currentTurnIndex = 0;
    public GameState currentState = GameState.GameStart;

    // 银行数据：真货剩余数量（按类型）
    public Dictionary<CardData.CardType, int> bankStock = new Dictionary<CardData.CardType, int>();

    // 卡池：所有真货和假货的初始牌堆
    private List<CardData> realCardPool = new List<CardData>();
    private List<CardData> fakeCardPool = new List<CardData>();
    private const int REAL_SUGAR_COUNT = 18;
    private const int REAL_OIL_COUNT = 18;
    private const int REAL_FLOUR_COUNT = 18;
    private const int FAKE_SUGAR_COUNT = 12;
    private const int FAKE_OIL_COUNT = 12;
    private const int FAKE_FLOUR_COUNT = 12;

    // UI组件引用 - 改为 TMP_Text
    [Header("阶段1按钮")]
    public Button sellRealCardButton;
    public GameObject sellCardPanel;

    [Header("阶段2按钮")]
    public Button placeCardButton;
    public Button buyButton;
    public GameObject placeCardPanel;

    [Header("通用按钮")]
    public Button endTurnButton;

    [Header("其他UI")]
    public TMP_Text turnText;          // 改为 TMP_Text
    public TMP_Text stateText;         // 改为 TMP_Text
    public TMP_Text logText;           // 改为 TMP_Text

    [Header("玩家面板")]
    public PlayerPanel[] playerPanels;
    public GameObject cardPrefab;

    [Header("银行UI组件")]
    public TMP_Text bankSugarText;     // 改为 TMP_Text
    public TMP_Text bankOilText;       // 改为 TMP_Text
    public TMP_Text bankFlourText;     // 改为 TMP_Text
    public GameObject bankPanel;

    [Header("对话框UI")]
    public GameObject sellerDialogPanel;
    public TMP_Text sellerDialogText;  // 改为 TMP_Text
    public Button acceptButton;
    public Button rejectButton;

    [Header("验货对话框")]
    public GameObject inspectDialogPanel;
    public TMP_Text inspectDialogText; // 改为 TMP_Text
    public Button inspectButton;
    public Button noInspectButton;

    // 临时存储数据
    private CardData selectedCardForPlace;
    private int selectedHandCardIndex = -1;

    // 交易数据
    private int currentTransactionBuyerIndex;
    private int currentTransactionSellerIndex;
    private CardData.CardType currentTransactionType;
    private int currentTransactionPrice;
    private CardData currentTransactionCard;
    private PlayerData currentTransactionBuyer;
    private PlayerData currentTransactionSeller;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeGame();
        SetupPlayerPanels();
        InitializeUI();
    }

    void InitializeUI()
    {
        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(false);
        if (inspectDialogPanel != null)
            inspectDialogPanel.SetActive(false);
        if (sellCardPanel != null)
            sellCardPanel.SetActive(false);
        if (placeCardPanel != null)
            placeCardPanel.SetActive(false);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnButton);

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);

        if (placeCardButton != null)
            placeCardButton.onClick.AddListener(OnPlaceCardButtonClicked);

        if (sellRealCardButton != null)
            sellRealCardButton.onClick.AddListener(OnSellButtonClicked);
    }

    void InitializeGame()
    {
        GenerateCardPools();

        for (int i = 0; i < 3; i++)
        {
            PlayerData p = new PlayerData();
            p.playerIndex = i;
            p.gold = 12;
            p.handCards = new List<CardData>();
            p.openCards = new List<CardData>();
            for (int j = 0; j < 9; j++)
                p.openCards.Add(null);
            p.hasSoldThisTurn = false;
            p.hasPlacedThisTurn = false;
            p.hasBoughtThisTurn = false;
            p.rejectedBuyers = new List<int>();

            for (int t = 0; t < 4; t++)
            {
                p.handCards.Add(DrawRandomCard(true));
                p.handCards.Add(DrawRandomCard(false));
            }
            players.Add(p);
        }

        int fakeIndex = Random.Range(0, 3);
        for (int i = 0; i < 3; i++)
        {
            players[i].isRealMerchant = (i != fakeIndex);
        }

        bankStock = new Dictionary<CardData.CardType, int>();
        bankStock[CardData.CardType.Sugar] = 2;
        bankStock[CardData.CardType.Oil] = 2;
        bankStock[CardData.CardType.Flour] = 2;

        currentTurnIndex = Random.Range(0, 3);
        StartTurn();
    }

    void SetupPlayerPanels()
    {
        for (int i = 0; i < players.Count && i < playerPanels.Length; i++)
        {
            int playerIndex = i;
            if (playerPanels[i] != null)
            {
                playerPanels[i].Initialize(i, players[i]);

                playerPanels[i].OnCardSelected = (index, card) =>
                {
                    OnPlayerCardSelected(index, card);
                };

                playerPanels[i].OnOpenSlotSelected = (index, slot) =>
                {
                    OnPlayerOpenSlotSelected(index, slot);
                };
            }
        }
    }

    #region 牌池管理
    private void GenerateCardPools()
    {
        realCardPool.Clear();
        fakeCardPool.Clear();

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

    private CardData DrawRandomCard(bool isReal)
    {
        List<CardData> targetPool = isReal ? realCardPool : fakeCardPool;

        if (targetPool.Count == 0)
        {
            Debug.LogWarning($"{(isReal ? "真货" : "假货")}牌池已空！");
            return null;
        }

        int lastIndex = targetPool.Count - 1;
        CardData drawnCard = targetPool[lastIndex];
        targetPool.RemoveAt(lastIndex);

        return drawnCard;
    }
    #endregion

    #region 游戏流程控制
    void StartTurn()
    {
        PlayerData currentPlayer = players[currentTurnIndex];

        currentPlayer.hasSoldThisTurn = false;
        currentPlayer.hasPlacedThisTurn = false;
        currentPlayer.hasBoughtThisTurn = false;
        currentPlayer.rejectedBuyers.Clear();

        int goldToAdd = 4;
        if (IsGoldLeader(currentPlayer))
            goldToAdd -= 2;
        currentPlayer.gold += goldToAdd;

        UpdateAllUI();
        Log($"玩家{currentTurnIndex + 1}回合开始，获得{goldToAdd}金币，当前金币{currentPlayer.gold}");

        currentState = GameState.Phase1_Sell;
        EnablePhase1Buttons(true);
        EnablePhase2Buttons(false);

        if (placeCardPanel != null)
            placeCardPanel.SetActive(false);
    }

    void EndTurn()
    {
        if (CheckWinCondition())
        {
            currentState = GameState.GameEnd;
            Log("游戏结束！");
            if (endTurnButton != null)
                endTurnButton.interactable = false;
            return;
        }

        currentTurnIndex = (currentTurnIndex + 1) % 3;
        StartTurn();
    }

    private void OnEndTurnButton()
    {
        if (currentState == GameState.Phase1_Sell || currentState == GameState.Phase2_Action)
        {
            EndTurn();
        }
    }
    #endregion

    #region 金币相关
    public bool IsGoldLeader(PlayerData player)
    {
        if (player == null) return false;
        int maxGold = players.Max(p => p.gold);
        return player.gold == maxGold;
    }
    #endregion

    #region 阶段1：出售真牌
    private void OnSellButtonClicked()
    {
        if (currentState != GameState.Phase1_Sell) return;

        if (sellCardPanel != null)
        {
            RefreshSellCardPanel();
            sellCardPanel.SetActive(true);
        }
    }

    private void RefreshSellCardPanel()
    {
        if (sellCardPanel == null) return;

        PlayerData currentPlayer = players[currentTurnIndex];
        List<CardData> realCards = currentPlayer.handCards.FindAll(c => c.quality == CardData.CardQuality.Real);

        foreach (Transform child in sellCardPanel.transform)
        {
            Destroy(child.gameObject);
        }

        if (realCards.Count == 0)
        {
            // 使用 TMP_Text 创建空提示
            GameObject emptyTextObj = new GameObject("EmptyText");
            emptyTextObj.transform.SetParent(sellCardPanel.transform);
            TMP_Text emptyText = emptyTextObj.AddComponent<TMP_Text>();
            emptyText.text = "没有可出售的真货";
            emptyText.color = Color.gray;
            emptyText.fontSize = 20;
            emptyText.alignment = TextAlignmentOptions.Center;

            RectTransform rect = emptyTextObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            return;
        }

        foreach (CardData card in realCards)
        {
            GameObject cardButton = new GameObject($"Card_{card.type}_{card.cardId}");
            cardButton.transform.SetParent(sellCardPanel.transform);

            RectTransform rect = cardButton.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 40);

            Button btn = cardButton.AddComponent<Button>();
            TMP_Text btnText = cardButton.AddComponent<TMP_Text>();
            btnText.text = GetCardName(card);
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 16;
            btnText.color = Color.black;

            btn.onClick.AddListener(() => {
                OnSellRealCard(card);
                sellCardPanel.SetActive(false);
            });
        }
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

        currentPlayer.handCards.Remove(card);
        currentPlayer.gold += 8;
        currentPlayer.hasSoldThisTurn = true;
        bankStock[card.type]++;

        UpdateAllUI();
        Log($"玩家{currentTurnIndex + 1}出售了一张{GetCardName(card)}，获得8金币");

        EnablePhase1Buttons(false);
        currentState = GameState.Phase2_Action;
        EnablePhase2Buttons(true);

        if (sellCardPanel != null)
            sellCardPanel.SetActive(false);
    }
    #endregion

    #region 阶段2：明牌/替换
    private void OnPlaceCardButtonClicked()
    {
        if (currentState != GameState.Phase2_Action) return;

        if (placeCardPanel != null)
        {
            RefreshPlaceCardPanel();
            placeCardPanel.SetActive(true);
        }
    }

    private void RefreshPlaceCardPanel()
    {
        if (placeCardPanel == null) return;

        PlayerData currentPlayer = players[currentTurnIndex];

        foreach (Transform child in placeCardPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentPlayer.openCards.Count; i++)
        {
            GameObject slot = new GameObject($"OpenSlot_{i}");
            slot.transform.SetParent(placeCardPanel.transform);

            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);

            Button btn = slot.AddComponent<Button>();
            TMP_Text slotText = slot.AddComponent<TMP_Text>();
            slotText.alignment = TextAlignmentOptions.Center;
            slotText.fontSize = 14;

            CardData card = currentPlayer.openCards[i];
            if (card != null)
            {
                slotText.text = GetCardName(card);
            }
            else
            {
                slotText.text = "空位";
            }

            int slotIndex = i;
            btn.onClick.AddListener(() => {
                OnPlaceCardSelected(slotIndex);
                placeCardPanel.SetActive(false);
            });
        }
    }

    public void OnPlaceCardSelected(int slotIndex)
    {
        if (currentState != GameState.Phase2_Action)
        {
            Log("现在不是明牌阶段");
            return;
        }

        PlayerData currentPlayer = players[currentTurnIndex];

        if (currentPlayer.hasPlacedThisTurn)
        {
            Log("本回合已经执行过明牌/替换操作");
            return;
        }

        if (selectedCardForPlace == null)
        {
            Log("请先选择一张手牌");
            return;
        }

        if (!currentPlayer.handCards.Contains(selectedCardForPlace))
        {
            Log("选中的卡牌已不存在");
            if (playerPanels[currentTurnIndex] != null)
                playerPanels[currentTurnIndex].ClearSelectedCardInPanel();
            selectedCardForPlace = null;
            return;
        }

        if (slotIndex < 0 || slotIndex >= currentPlayer.openCards.Count)
        {
            Log("无效的槽位");
            return;
        }

        ExecutePlaceCard(slotIndex);
    }

    private void ExecutePlaceCard(int slotIndex)
    {
        PlayerData currentPlayer = players[currentTurnIndex];
        CardData targetSlotCard = currentPlayer.openCards[slotIndex];

        if (targetSlotCard != null)
        {
            currentPlayer.handCards.Remove(selectedCardForPlace);
            currentPlayer.handCards.Add(targetSlotCard);
            currentPlayer.openCards[slotIndex] = selectedCardForPlace;
            Log($"玩家{currentTurnIndex + 1}将明牌区{slotIndex + 1}号位的{GetCardName(targetSlotCard)}替换为{GetCardName(selectedCardForPlace)}");
        }
        else
        {
            currentPlayer.handCards.Remove(selectedCardForPlace);
            currentPlayer.openCards[slotIndex] = selectedCardForPlace;
            Log($"玩家{currentTurnIndex + 1}在明牌区{slotIndex + 1}号位放置了{GetCardName(selectedCardForPlace)}");
        }

        currentPlayer.hasPlacedThisTurn = true;

        if (playerPanels[currentTurnIndex] != null)
            playerPanels[currentTurnIndex].ClearSelectedCardInPanel();

        selectedCardForPlace = null;

        UpdateAllUI();

        if (currentPlayer.hasPlacedThisTurn && currentPlayer.hasBoughtThisTurn)
        {
            EndTurn();
        }
    }

    public void SelectCardForPlace(CardData card, int handIndex)
    {
        if (currentState != GameState.Phase2_Action) return;

        PlayerData currentPlayer = players[currentTurnIndex];

        if (currentPlayer.hasPlacedThisTurn)
        {
            Log("本回合已经执行过明牌操作，不能再明牌");
            return;
        }

        if (!currentPlayer.handCards.Contains(card))
        {
            Log("卡牌不在手牌中");
            return;
        }

        selectedCardForPlace = card;
        selectedHandCardIndex = handIndex;
        Log($"已选中{GetCardName(card)}，请点击明牌区槽位");
    }
    #endregion

    #region 阶段2：购买
    private void OnBuyButtonClicked()
    {
        if (currentState != GameState.Phase2_Action) return;
        Log("请通过其他方式发起购买");
    }

    public void RequestBuy(int targetIndex, CardData.CardType wantedType, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        if (currentState != GameState.Phase2_Action || buyer.hasBoughtThisTurn)
        {
            Log("当前不能购买");
            return;
        }

        if (targetIndex == -1)
        {
            BuyFromBank(wantedType, offerPrice);
        }
        else
        {
            if (buyer.rejectedBuyers.Contains(targetIndex))
            {
                Log("该卖家已拒绝过你，本回合不能再向他购买");
                return;
            }
            ShowSellerDialog(targetIndex, wantedType, offerPrice);
        }
    }

    void BuyFromBank(CardData.CardType wantedType, int offerPrice)
    {
        PlayerData buyer = players[currentTurnIndex];
        int actualPrice = IsGoldLeader(buyer) ? 18 : 12;

        if (offerPrice < actualPrice)
        {
            Log($"银行不接受低于{actualPrice}金币的出价");
            return;
        }
        if (bankStock[wantedType] <= 0)
        {
            Log("银行没有该货物");
            return;
        }
        if (buyer.gold < actualPrice)
        {
            Log($"金币不足，需要{actualPrice}金币");
            return;
        }

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

    private void ShowSellerDialog(int sellerIndex, CardData.CardType wantedType, int offerPrice)
    {
        currentTransactionBuyerIndex = currentTurnIndex;
        currentTransactionSellerIndex = sellerIndex;
        currentTransactionType = wantedType;
        currentTransactionPrice = offerPrice;

        string sellerName = $"玩家{sellerIndex + 1}";
        string buyerName = $"玩家{currentTurnIndex + 1}";
        string goodsName = GetCardTypeName(wantedType);

        if (sellerDialogText != null)
        {
            sellerDialogText.text = $"{sellerName}，\n" +
                                    $"{buyerName}想以{offerPrice}金币的价格\n" +
                                    $"向您购买【{goodsName}】\n\n" +
                                    $"是否接受此交易？\n\n" +
                                    $"(提示：您可以选择拒绝来隐藏手牌信息)";
        }

        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(true);

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => OnSellerResponse(true));
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => OnSellerResponse(false));
        }
    }

    private void OnSellerResponse(bool accepted)
    {
        if (sellerDialogPanel != null)
            sellerDialogPanel.SetActive(false);

        if (accepted)
        {
            ProcessAcceptedTransaction();
        }
        else
        {
            ProcessRejectedTransaction();
        }
    }

    private void ProcessAcceptedTransaction()
    {
        PlayerData buyer = players[currentTransactionBuyerIndex];
        PlayerData seller = players[currentTransactionSellerIndex];

        CardData soldCard = seller.handCards.Find(c => c.type == currentTransactionType);

        if (soldCard == null)
        {
            Log($"卖家玩家{currentTransactionSellerIndex + 1}没有{GetCardTypeName(currentTransactionType)}货物");
            ProcessRejectedTransaction();
            return;
        }

        if (buyer.gold < currentTransactionPrice)
        {
            Log($"买家金币不足，交易失败");
            return;
        }

        buyer.gold -= currentTransactionPrice;
        seller.gold += currentTransactionPrice;
        seller.handCards.Remove(soldCard);
        buyer.handCards.Add(soldCard);

        Log($"玩家{currentTransactionBuyerIndex + 1}以{currentTransactionPrice}金币从玩家{currentTransactionSellerIndex + 1}购买了{GetCardName(soldCard)}");

        UpdateAllUI();

        currentTransactionCard = soldCard;
        currentTransactionBuyer = buyer;
        currentTransactionSeller = seller;

        ShowInspectDialog(soldCard, currentTransactionPrice);
    }

    private void ProcessRejectedTransaction()
    {
        PlayerData buyer = players[currentTransactionBuyerIndex];

        if (!buyer.rejectedBuyers.Contains(currentTransactionSellerIndex))
        {
            buyer.rejectedBuyers.Add(currentTransactionSellerIndex);
        }

        Log($"玩家{currentTransactionSellerIndex + 1}拒绝了交易请求");
    }

    private void ShowInspectDialog(CardData card, int price)
    {
        string cardName = GetCardName(card);
        int inspectPrice = Mathf.CeilToInt(price * 0.5f);

        if (inspectDialogText != null)
        {
            inspectDialogText.text = $"您购买了{cardName}\n\n是否花费{inspectPrice}金币进行验货？\n\n" +
                                     $"验货后如果是真货，买家多付{inspectPrice}金币\n" +
                                     $"如果是假货，卖家退还{price}金币并赔偿{inspectPrice}金币";
        }

        if (inspectDialogPanel != null)
            inspectDialogPanel.SetActive(true);

        if (inspectButton != null)
        {
            inspectButton.onClick.RemoveAllListeners();
            inspectButton.onClick.AddListener(() => OnInspectChoice(true));
        }

        if (noInspectButton != null)
        {
            noInspectButton.onClick.RemoveAllListeners();
            noInspectButton.onClick.AddListener(() => OnInspectChoice(false));
        }
    }

    private void OnInspectChoice(bool inspect)
    {
        if (inspectDialogPanel != null)
            inspectDialogPanel.SetActive(false);

        if (inspect)
        {
            bool isReal = (currentTransactionCard.quality == CardData.CardQuality.Real);
            ExecuteInspect(isReal, currentTransactionPrice, currentTransactionBuyer, currentTransactionSeller);
        }
        else
        {
            Log($"玩家{currentTransactionBuyerIndex + 1}选择不验货");
        }

        if (currentTransactionBuyer != null)
            currentTransactionBuyer.hasBoughtThisTurn = true;

        UpdateAllUI();

        if (currentTransactionBuyer != null && currentTransactionBuyer.hasPlacedThisTurn && currentTransactionBuyer.hasBoughtThisTurn)
        {
            EndTurn();
        }

        currentTransactionCard = null;
        currentTransactionBuyer = null;
        currentTransactionSeller = null;
    }

    public int GetBankStock(CardData.CardType type)
    {
        if (bankStock == null)
        {
            Debug.LogError("bankStock 未初始化");
            return 0;
        }

        if (bankStock.ContainsKey(type))
        {
            return bankStock[type];
        }

        return 0;
    }

    public bool BankHasGoods(CardData.CardType type)
    {
        return GetBankStock(type) > 0;
    }

    void ExecuteInspect(bool isReal, int price, PlayerData buyer, PlayerData seller)
    {
        int extra = Mathf.CeilToInt(price * 0.5f);

        if (isReal)
        {
            if (buyer.gold >= extra)
            {
                buyer.gold -= extra;
                seller.gold += extra;
                Log($"验货结果：真货，买家多付{extra}金币");
            }
            else
            {
                HandleInsufficientGold(buyer, extra);
                if (buyer.gold >= extra)
                {
                    buyer.gold -= extra;
                    seller.gold += extra;
                }
            }
        }
        else
        {
            int totalRefund = price + extra;
            if (seller.gold >= totalRefund)
            {
                seller.gold -= totalRefund;
                buyer.gold += totalRefund;
                Log($"验货结果：假货，卖家退还{price}并赔偿{extra}金币");
            }
            else
            {
                HandleInsufficientGold(seller, totalRefund);
                if (seller.gold >= totalRefund)
                {
                    seller.gold -= totalRefund;
                    buyer.gold += totalRefund;
                }
            }
        }
        UpdateAllUI();
    }

    void HandleInsufficientGold(PlayerData player, int requiredGold)
    {
        while (player.gold < requiredGold)
        {
            CardData realCard = player.handCards.Find(c => c.quality == CardData.CardQuality.Real);
            if (realCard == null) break;

            player.handCards.Remove(realCard);
            player.gold += 8;
            bankStock[realCard.type]++;
            Log($"玩家{player.playerIndex + 1}抵押了一张{GetCardName(realCard)}，获得8金币");
        }
        if (player.gold < requiredGold)
            player.gold = 0;
    }
    #endregion

    #region UI更新
    private void UpdateAllUI()
    {
        foreach (var playerPanel in playerPanels)
        {
            if (playerPanel != null)
                playerPanel.RefreshPanel();
        }

        if (turnText != null)
            turnText.text = $"当前回合: 玩家{currentTurnIndex + 1}";

        if (stateText != null)
            stateText.text = GetStateDescription();

        UpdateBankUI();
    }

    private void UpdateBankUI()
    {
        if (bankSugarText != null)
        {
            int sugarCount = bankStock.ContainsKey(CardData.CardType.Sugar) ? bankStock[CardData.CardType.Sugar] : 0;
            bankSugarText.text = $"糖: {sugarCount}张";
        }

        if (bankOilText != null)
        {
            int oilCount = bankStock.ContainsKey(CardData.CardType.Oil) ? bankStock[CardData.CardType.Oil] : 0;
            bankOilText.text = $"油: {oilCount}张";
        }

        if (bankFlourText != null)
        {
            int flourCount = bankStock.ContainsKey(CardData.CardType.Flour) ? bankStock[CardData.CardType.Flour] : 0;
            bankFlourText.text = $"面: {flourCount}张";
        }
    }

    private void EnablePhase1Buttons(bool enable)
    {
        if (sellRealCardButton != null)
            sellRealCardButton.interactable = enable;
    }

    private void EnablePhase2Buttons(bool enable)
    {
        if (placeCardButton != null)
            placeCardButton.interactable = enable;
        if (buyButton != null)
            buyButton.interactable = enable;

        if (enable)
        {
            PlayerData currentPlayer = players[currentTurnIndex];
            if (currentPlayer.hasPlacedThisTurn && placeCardButton != null)
                placeCardButton.interactable = false;
            if (currentPlayer.hasBoughtThisTurn && buyButton != null)
                buyButton.interactable = false;
        }
    }
    #endregion

    #region 辅助方法
    private string GetStateDescription()
    {
        switch (currentState)
        {
            case GameState.Phase1_Sell: return "阶段1：可以选择出售真牌";
            case GameState.Phase2_Action: return "阶段2：可以选择明牌或购买";
            case GameState.TurnEnd: return "回合结束";
            case GameState.GameEnd: return "游戏结束";
            default: return "等待...";
        }
    }

    private string GetCardName(CardData card)
    {
        if (card == null) return "无";
        string typeName = GetCardTypeName(card.type);
        string qualityMark = card.quality == CardData.CardQuality.Real ? "真" : "假";
        return $"{typeName}{qualityMark}";
    }

    private string GetCardTypeName(CardData.CardType type)
    {
        switch (type)
        {
            case CardData.CardType.Sugar: return "糖";
            case CardData.CardType.Oil: return "油";
            case CardData.CardType.Flour: return "面";
            default: return "未知";
        }
    }

    private void Log(string message)
    {
        Debug.Log(message);
        if (logText != null)
        {
            logText.text = $"{System.DateTime.Now:HH:mm:ss} - {message}\n" + logText.text;
            string[] lines = logText.text.Split('\n');
            if (lines.Length > 20)
            {
                logText.text = string.Join("\n", lines, 0, 20);
            }
        }
    }
    #endregion

    #region 玩家交互
    private void OnPlayerCardSelected(int playerIndex, CardData card)
    {
        if (playerIndex != currentTurnIndex) return;

        if (currentState == GameState.Phase1_Sell)
        {
            if (card.quality == CardData.CardQuality.Real)
            {
                OnSellRealCard(card);
            }
        }
        else if (currentState == GameState.Phase2_Action)
        {
            selectedCardForPlace = card;
            Log($"已选中{GetCardName(card)}，请点击明牌区槽位");
            if (placeCardPanel != null)
                placeCardPanel.SetActive(true);
        }
    }

    private void OnPlayerOpenSlotSelected(int playerIndex, int slotIndex)
    {
        if (playerIndex != currentTurnIndex) return;

        if (currentState == GameState.Phase2_Action && selectedCardForPlace != null)
        {
            OnPlaceCardSelected(slotIndex);
            selectedCardForPlace = null;
            if (playerPanels[currentTurnIndex] != null)
                playerPanels[currentTurnIndex].ClearSelectedCardInPanel();
        }
    }
    #endregion

    #region 胜利条件
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
        if (openCards.Any(c => c == null)) return false;

        int sugarCount = openCards.Count(c => c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Real);
        int oilCount = openCards.Count(c => c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Real);
        int flourCount = openCards.Count(c => c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Real);

        return sugarCount == 3 && oilCount == 3 && flourCount == 3;
    }

    bool CheckFakeWin(List<CardData> openCards)
    {
        int sugarCount = openCards.Count(c => c != null && c.type == CardData.CardType.Sugar && c.quality == CardData.CardQuality.Fake);
        int oilCount = openCards.Count(c => c != null && c.type == CardData.CardType.Oil && c.quality == CardData.CardQuality.Fake);
        int flourCount = openCards.Count(c => c != null && c.type == CardData.CardType.Flour && c.quality == CardData.CardQuality.Fake);

        int total = sugarCount + oilCount + flourCount;
        if (total != 7) return false;

        var counts = new List<int> { sugarCount, oilCount, flourCount };
        counts.Sort();
        return counts[2] == 3 && counts[1] == 2 && counts[0] == 2;
    }
    #endregion
}