using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{
    public static Panel Instance;

    [Header("预制体")]
    public GameObject playerInfoPrefab;          // 当前玩家信息区域的预制体

    [Header("按钮")]
    public Button btn_bank;
    public Button btn_player1;                   // 查看玩家1明牌区按钮
    public Button btn_player2;                   // 查看玩家2明牌区按钮
    public Button btn_player3;                   // 查看玩家3明牌区按钮
    public Button sellButton;
    public Button placeButton;
    public Button buyButton;
    public Button nextButton;

    [Header("明牌区浮动面板")]
    public GameObject openCardPanel;
    public TMP_Text openCardPanelTitle;
    public Transform openCardContainer;
    public Button closeOpenCardPanelBtn;

    [Header("购买面板")]
    public GameObject buyTargetPanel;           // 购买对象选择面板
    public Button btnBuyBank;                   // 向银行购买按钮
    public Button btnBuyPlayer1;                // 向玩家1购买按钮
    public Button btnBuyPlayer2;                // 向玩家2购买按钮
    public Button btnBuyPlayer3;                // 向玩家3购买按钮
    public Button btnCancelBuyTarget;           // 取消购买按钮

    public GameObject bankBuyPanel;             // 银行购买面板（原 BankPanel）
    public GameObject playerBuyPanel;           // 玩家购买面板
    public TMP_Text playerBuyTargetText;        // 显示购买目标
    public TMP_Dropdown playerGoodsDropdown;    // 货物下拉框
    public TMP_InputField playerPriceInput;     // 价格输入框
    public Button btnConfirmPlayerBuy;          // 确认购买按钮
    public Button btnCancelPlayerBuy;           // 取消购买按钮

    [Header("拆牌按钮")]
    public Button attackButton;                     // 拆牌按钮（新增）

    private bool isStealMode = false;               // 是否处于拆牌模式
    private int stealTargetPlayer = -1;             // 拆牌目标玩家索引
    private int stealTargetSlot = -1;               // 拆牌目标槽位

    [Header("通用")]
    public GameObject cardPrefab;
    public TMP_Text gameStateText;
    public TMP_Text logText;
    public TMP_Text cueText;

    [Header("其他")]
    public GameObject bankPanel;


    // 当前玩家信息区域的 UI 元素
    private GameObject currentPlayerInfoObj;
    private TMP_Text currentPlayerNameText;
    private TMP_Text currentPlayerGoldText;
    private TMP_Text currentPlayerIdentityText;
    private Transform currentHandCardContainer;

    // 状态
    private int currentViewingPlayerIndex = -1;
    private bool isPlaceMode = false;
    private bool isBuyMode = false;
    private CardData selectedHandCard = null;
    private int selectedOpenSlot = -1;
    private Coroutine clearCueCoroutine;
    private CardData selectedCardForSell = null;
    private int selectedTargetPlayerIndex = -1; // -1表示银行，0-2表示玩家

    public bool IsInBuyMode()//私有变量，公有调用，只读
    {
        return isBuyMode;
    }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 绑定固定按钮事件
        btn_bank.onClick.AddListener(() => { if (bankPanel != null) bankPanel.SetActive(true); });
        btn_player1.onClick.AddListener(() => OnViewPlayerOpenCards(0));
        btn_player2.onClick.AddListener(() => OnViewPlayerOpenCards(1));
        btn_player3.onClick.AddListener(() => OnViewPlayerOpenCards(2));
        //在明牌模式下不能使用这个按钮，实现是靠进入时禁用，离开后激活
        closeOpenCardPanelBtn.onClick.AddListener(CloseOpenCardPanel);
        sellButton.onClick.AddListener(OnSellButtonClick);
        placeButton.onClick.AddListener(OnPlaceButtonClick);
        buyButton.onClick.AddListener(OnBuyButtonClick);
        nextButton.onClick.AddListener(OnNextButtonClick);

        if (btnBuyBank != null)
            btnBuyBank.onClick.AddListener(() => ShowBankBuyPanel());

        if (btnBuyPlayer1 != null)
            btnBuyPlayer1.onClick.AddListener(() => ShowPlayerBuyPanel(0));

        if (btnBuyPlayer2 != null)
            btnBuyPlayer2.onClick.AddListener(() => ShowPlayerBuyPanel(1));

        if (btnBuyPlayer3 != null)
            btnBuyPlayer3.onClick.AddListener(() => ShowPlayerBuyPanel(2));

        if (btnCancelBuyTarget != null)
            btnCancelBuyTarget.onClick.AddListener(CloseBuyTargetPanel);
        if (btn_bank != null)
        {
            btn_bank.onClick.RemoveAllListeners();
            btn_bank.onClick.AddListener(() => {
                CloseOpenCardPanel();
                ShowBankInfoOnly();  // 只读模式
            });
        }

        // 玩家购买面板按钮
        if (btnConfirmPlayerBuy != null)
            btnConfirmPlayerBuy.onClick.AddListener(OnConfirmPlayerBuy);

        if (btnCancelPlayerBuy != null)
            btnCancelPlayerBuy.onClick.AddListener(ClosePlayerBuyPanel);

        // 创建当前玩家信息区域（只创建一个）
        CreateCurrentPlayerInfoArea();

        // 初始隐藏浮动面板
        openCardPanel.SetActive(false);
        if (buyTargetPanel != null) buyTargetPanel.SetActive(false);
        if (bankBuyPanel != null) bankBuyPanel.SetActive(false);
        if (playerBuyPanel != null) playerBuyPanel.SetActive(false);

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClick);
    }
    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isPhase1 = (GameManager.Instance.currentState == GameState.Phase1_Sell);
        bool isPhase2 = (GameManager.Instance.currentState == GameState.Phase2_Action);
        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];

        // Sell 按钮：阶段1且未出售时可用
        if (sellButton != null)
            sellButton.interactable = isPhase1 && !currentPlayer.hasSoldThisTurn;

        // Place 按钮
        if (placeButton != null)
        {
            if (isPlaceMode)
                placeButton.interactable = true;
            else
                placeButton.interactable = isPhase2 && !currentPlayer.hasSoldThisTurn && !currentPlayer.hasPlacedThisTurn && !isBuyMode;
        }

        // Buy 按钮：阶段2且未购买、不在明牌模式、不在购买状态时可用
        if (buyButton != null)
            buyButton.interactable = isPhase2 && !currentPlayer.hasBoughtThisTurn && !isPlaceMode && !isBuyMode;

        // Next 按钮
        if (nextButton != null)
        {
            if (isPhase1)
                nextButton.interactable = true;
            else if (isPhase2)
                nextButton.interactable = !isPlaceMode && !isBuyMode;
            else
                nextButton.interactable = false;
        }

        // ========== 主界面银行按钮（只读查看） ==========
        if (btn_bank != null)
        {
            btn_bank.interactable = !isPlaceMode && !isBuyMode;
        }

        // ========== 购买选择面板内的按钮：不在这里控制 ==========
        // 购买选择面板内的按钮状态由 RefreshBuyTargetButtons() 在打开面板时设置
        // 不需要在 Update 中重复控制，否则会导致购买状态下按钮被禁用

        // Attack 按钮：阶段1可用，且不在其他模式时可用
        if (attackButton != null)
        {
            attackButton.interactable = isPhase1 && !isPlaceMode && !isBuyMode && !isStealMode;
        }
        // 拆牌模式下，其他操作按钮禁用
        if (isStealMode)
        {
            if (sellButton != null) sellButton.interactable = false;
            if (placeButton != null) placeButton.interactable = false;
            if (buyButton != null) buyButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            if (btn_bank != null) btn_bank.interactable = false;
            if (attackButton != null) attackButton.interactable = true;   // 拆牌按钮变为确认
        }
    }

    void CreateCurrentPlayerInfoArea()
    {
        if (playerInfoPrefab == null)
        {
            Debug.LogError("playerInfoPrefab 未赋值！");
            return;
        }

        currentPlayerInfoObj = Instantiate(playerInfoPrefab, transform);
        currentPlayerInfoObj.name = "CurrentPlayerInfo";

        // 查找子物体
        Transform nameTrans = currentPlayerInfoObj.transform.Find("PlayerNameText");
        Transform goldTrans = currentPlayerInfoObj.transform.Find("PlayerGoldText");
        Transform identityTrans = currentPlayerInfoObj.transform.Find("PlayerIdentityText");
        Transform handTrans = currentPlayerInfoObj.transform.Find("HandCardContainer");

        if (nameTrans == null || goldTrans == null || identityTrans == null || handTrans == null)
        {
            Debug.LogError($"预制体 {playerInfoPrefab.name} 缺少必要子物体（PlayerNameText, PlayerGoldText, PlayerIdentityText, HandCardContainer）");
            Destroy(currentPlayerInfoObj);
            return;
        }

        currentPlayerNameText = nameTrans.GetComponent<TMP_Text>();
        currentPlayerGoldText = goldTrans.GetComponent<TMP_Text>();
        currentPlayerIdentityText = identityTrans.GetComponent<TMP_Text>();
        currentHandCardContainer = handTrans;

        // 可选：设置手牌容器的 GridLayoutGroup（如果预制体已经设置好，可省略）
        GridLayoutGroup grid = currentHandCardContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = currentHandCardContainer.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 120);
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
    }

    void OnViewPlayerOpenCards(int playerIndex)
    {
        // 拆牌模式下：允许查看其他玩家，并使其可交互（用于选择卡牌）
        if (isStealMode)
        {
            if (playerIndex == GameManager.Instance.currentTurnIndex)
            {
                AddCue("不能对自己拆牌");
                return;
            }
            // 打开目标玩家明牌区，并设置为可交互模式
            OpenPlayerOpenCardsForSteal(playerIndex);
            return;
        }
        if (isPlaceMode)
        {
            AddCue("请先完成当前明牌操作");
            return;
        }
        if (GameManager.Instance == null) return;
        if (openCardPanel.activeSelf && currentViewingPlayerIndex == playerIndex)
        {
            CloseOpenCardPanel();
            AddCue($"已关闭玩家{playerIndex + 1}的明牌区");
        }
        else
        {
            // 否则打开对应玩家的明牌区
            OpenPlayerOpenCards(playerIndex);
        }
       
    }

    void OpenPlayerOpenCards(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= GameManager.Instance.players.Count) return;
        PlayerData player = GameManager.Instance.players[playerIndex];
        openCardPanelTitle.text = $"玩家{playerIndex + 1}的明牌区";

        // 只有在明牌模式且是当前玩家时，才可交互
        bool interactive = isPlaceMode && (playerIndex == GameManager.Instance.currentTurnIndex);

        UpdateOpenCardDisplay(player, interactive);
        openCardPanel.SetActive(true);
        currentViewingPlayerIndex = playerIndex;
    }
    void OpenPlayerOpenCardsForSteal(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= GameManager.Instance.players.Count) return;
        PlayerData player = GameManager.Instance.players[playerIndex];
        openCardPanelTitle.text = $"玩家{playerIndex + 1}的明牌区（拆牌）";

        // 清空容器，重新生成可交互卡牌
        foreach (Transform child in openCardContainer) Destroy(child.gameObject);

        for (int i = 0; i < 9; i++)
        {
            GameObject slot = Instantiate(cardPrefab, openCardContainer);
            CardUI cardUI = slot.GetComponent<CardUI>();

            if (i < player.openCards.Count && player.openCards[i] != null)
                cardUI.SetCardData(player.openCards[i]);
            else
                cardUI.SetEmpty();

            // 设置可交互
            int slotIndex = i;
            cardUI.OnCardClick = (ui) => OnStealSlotSelected(playerIndex, slotIndex);
            cardUI.SetInteractable(true);
        }

        openCardPanel.SetActive(true);
        currentViewingPlayerIndex = playerIndex;
    }
    void OnStealSlotSelected(int playerIndex, int slotIndex)
    {
        if (!isStealMode) return;
        // 检查槽位是否有牌
        PlayerData target = GameManager.Instance.players[playerIndex];
        if (slotIndex >= target.openCards.Count || target.openCards[slotIndex] == null)
        {
            AddCue("该槽位为空，不能选中");
            return;
        }
        // 如果已经选中了同一个目标
        if (stealTargetPlayer == playerIndex && stealTargetSlot == slotIndex)
        {
            // 取消选中
            ClearStealTarget();
            AddCue("已取消选中");
            // 按钮文本变回 "Cancel"
            if (attackButton != null)
            {
                TMP_Text btnText = attackButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "Cancel";
            }
            return;
        }

        // 清除之前的选中（如果有）
        if (stealTargetPlayer != -1)
        {
            // 可以清除之前高亮（如果需要）
            ClearStealTarget();
        }
        stealTargetPlayer = playerIndex;
        stealTargetSlot = slotIndex;
        // 按钮文本改为 "Confirm"
        if (attackButton != null)
        {
            TMP_Text btnText = attackButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Confirm";
        }
        AddCue($"已选中玩家{playerIndex + 1}的明牌区槽位{slotIndex + 1}，点击 Confirm 拆牌");
        // 高亮选中的槽位（可选）
        HighlightSlot(slotIndex);
    }
    void ClearStealTarget()
    {
        if (stealTargetPlayer != -1 && stealTargetSlot != -1)
        {
            // 清除槽位高亮（如果有记录）
            ClearSlotHighlight(stealTargetSlot);
            stealTargetPlayer = -1;
            stealTargetSlot = -1;
        }
    }
    public void CloseOpenCardPanel()
    {
        openCardPanel.SetActive(false);
        currentViewingPlayerIndex = -1;// 重置当前查看的玩家
    }

    // 更新当前玩家信息区域（名字、金币、身份、手牌）
    public void UpdateCurrentPlayerUI()
    {
        if (GameManager.Instance == null)
        {
            
            return;
        }
        PlayerData current = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];

        

        if (currentPlayerNameText != null) currentPlayerNameText.text = $"玩家{GameManager.Instance.currentTurnIndex + 1}";
        if (currentPlayerGoldText != null) currentPlayerGoldText.text = $"金币: {current.gold}";
        if (currentPlayerIdentityText != null) currentPlayerIdentityText.text = current.isRealMerchant ? "真商" : "假商";

        if (currentHandCardContainer != null)
        {
            
            UpdateHandCards(currentHandCardContainer, current.handCards);
        }
        else
        {
            Debug.LogError("currentHandCardContainer 为空！");
        }
    }

    void UpdateHandCards(Transform container, List<CardData> cards)
    {
        
        foreach (Transform child in container) Destroy(child.gameObject);
        for (int i = 0; i < cards.Count; i++)
        {
            
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardUI ui = cardObj.GetComponent<CardUI>();
            ui.SetCardData(cards[i]);
            CardData card = cards[i];
            ui.OnCardClick = (c) => OnHandCardClicked(card);
        }
        
    }

    void UpdateOpenCardDisplay(PlayerData player, bool interactive)
    {
        foreach (Transform child in openCardContainer) Destroy(child.gameObject);

        for (int i = 0; i < 9; i++)
        {
            GameObject slot = Instantiate(cardPrefab, openCardContainer);
            CardUI cardUI = slot.GetComponent<CardUI>();

            if (i < player.openCards.Count && player.openCards[i] != null)
                cardUI.SetCardData(player.openCards[i]);
            else
                cardUI.SetEmpty();

            // 根据传入的 interactive 参数决定是否可交互
            if (interactive)
            {
                int slotIndex = i;
                cardUI.OnCardClick = (ui) => OnOpenSlotSelected(slotIndex);
                cardUI.SetInteractable(true);
            }
            else
            {
                cardUI.OnCardClick = null;
                cardUI.SetInteractable(false);
            }
        }
    }

    void OnOpenSlotSelected(int slotIndex)
    {
        if (!isPlaceMode) return;
        if (selectedOpenSlot == slotIndex)
        {
            ClearSlotHighlight(slotIndex);
            selectedOpenSlot = -1;
            AddCue("已取消选中明牌区槽位");
        }
        else
        {
            if (selectedOpenSlot != -1) ClearSlotHighlight(selectedOpenSlot);
            selectedOpenSlot = slotIndex;
            HighlightSlot(slotIndex);
            AddCue($"已选中明牌区槽位 {slotIndex + 1}");
        }
    }

    void HighlightSlot(int slotIndex)
    {
        Transform slot = openCardContainer.GetChild(slotIndex);
        CardUI ui = slot.GetComponent<CardUI>();
        ui.SetSelected(true);
    }

    void ClearSlotHighlight(int slotIndex)
    {
        Transform slot = openCardContainer.GetChild(slotIndex);
        CardUI ui = slot.GetComponent<CardUI>();
        ui.SetSelected(false);
    }

    void OnHandCardClicked(CardData card)
    {
        // 阶段1：出售模式
        if (GameManager.Instance.currentState == GameState.Phase1_Sell)
        {
            if (isStealMode)
            {
                AddCue("拆牌模式下不能操作手牌");
                return;
            }
            // 如果点击的是假牌，清除选中并提示
            if (card.quality != CardData.CardQuality.Real)
            {
                AddCue("只能出售真货");
                ClearSelectedCardForSell();   // 清除当前选中的出售卡牌
                return;
            }

            PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
            if (currentPlayer.hasSoldThisTurn)
            {
                AddCue("本回合已经出售过真牌");
                return;
            }

            // 选中或取消选中卡牌
            if (selectedCardForSell == card)
            {
                ClearSelectedCardForSell();
                AddCue("已取消选中");
            }
            else
            {
                ClearSelectedCardForSell();
                selectedCardForSell = card;
                HighlightSelectedCardForSell(card);
                AddCue($"已选中 {card.GetCardName()}，售牌后本回合不能再明牌，点击「出售真牌」按钮确认");
            }
            return;
        }

        // 阶段2：明牌模式
        if (GameManager.Instance.currentState == GameState.Phase2_Action && !isPlaceMode)
        {
            // ... 原有的明牌选中逻辑 ...
            if (!isPlaceMode)
            {
                if (selectedHandCard == card)
                {
                    ClearHandCardHighlight();
                    selectedHandCard = null;
                }
                else
                {
                    ClearHandCardHighlight();
                    selectedHandCard = card;
                    HighlightHandCard(card);
                    AddCue($"已选中 {card.GetCardName()}，请点击「明牌」按钮进入明牌模式");
                }
            }
        }

        // 明牌模式下的手牌选中
        if (isPlaceMode)
        {
            if (selectedHandCard == card)
            {
                ClearHandCardHighlight();
                selectedHandCard = null;
                AddCue("已取消选中手牌");
            }
            else
            {
                ClearHandCardHighlight();
                selectedHandCard = card;
                HighlightHandCard(card);
                AddCue($"已选中手牌: {card.GetCardName()}");
            }
        }
    }
    void HighlightSelectedCardForSell(CardData card)
    {
        if (currentHandCardContainer == null) return;
        foreach (Transform child in currentHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null && ui.GetCardData() == card)
            {
                ui.SetSelected(true);
            }
        }
    }
    void ClearSelectedCardForSell()
    {
        if (currentHandCardContainer == null) return;
        foreach (Transform child in currentHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.SetSelected(false);
            }
        }
        selectedCardForSell = null;
    }

    void HighlightHandCard(CardData card)
    {
        if (currentHandCardContainer == null) return;
        foreach (Transform child in currentHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui.GetCardData() == card) ui.SetSelected(true);
        }
    }

    void ClearHandCardHighlight()
    {
        if (currentHandCardContainer == null) return;
        foreach (Transform child in currentHandCardContainer)
        {
            CardUI ui = child.GetComponent<CardUI>();
            ui.SetSelected(false);
        }
    }
    void OnAttackButtonClick()
    {
        // 如果已经在拆牌模式
        if (isStealMode)
        {
            // 如果有选中的目标，则执行拆牌
            if (stealTargetPlayer != -1 && stealTargetSlot != -1)
            {
                ExecuteSteal();
            }
            else
            {
                // 未选中任何目标，则退出拆牌模式
                ExitStealMode();
            }
            return;
        }

        // 进入拆牌模式
        if (GameManager.Instance.currentState != GameState.Phase1_Sell) return;
        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (cur.gold < GameManager.Config.stealCost)
        {
            AddCue($"金币不足，需要{GameManager.Config.stealCost}金币");
            return;
        }

        isStealMode = true;
        stealTargetPlayer = -1;
        stealTargetSlot = -1;

        // 改变按钮文本为 "Cancel"
        if (attackButton != null)
        {
            TMP_Text btnText = attackButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Cancel";
        }

        // 关闭其他面板
        CloseOpenCardPanel();
        if (buyTargetPanel != null) buyTargetPanel.SetActive(false);
        if (bankBuyPanel != null) bankBuyPanel.SetActive(false);

        AddCue("拆牌模式：点击其他玩家查看其明牌区，点击有牌的卡牌选中，再点 Confirm 拆牌");
    }

    void OnSellButtonClick()
    {
        CloseBankPanelIfOpen();
        if (GameManager.Instance.currentState != GameState.Phase1_Sell)
        {
            AddCue("现在不是出售阶段");
            return;
        }

        if (selectedCardForSell == null)
        {
            AddCue("请先点击手牌选中要出售的真货");
            return;
        }

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasSoldThisTurn)
        {
            AddCue("本回合已经出售过真牌");
            return;
        }

        if (selectedCardForSell.quality != CardData.CardQuality.Real)
        {
            AddCue("只能出售真货");
            ClearSelectedCardForSell();
            return;
        }

        if (!currentPlayer.handCards.Contains(selectedCardForSell))
        {
            AddCue("选中的卡牌已不存在");
            ClearSelectedCardForSell();
            return;
        }
       
        // 执行出售
        GameManager.Instance.OnSellRealCard(selectedCardForSell);
        ClearSelectedCardForSell();
        UpdateCurrentPlayerUI();
        AddCue("出售成功，点击「Next」进入下一阶段");
    }

    void OnPlaceButtonClick()
    {
        CloseBankPanelIfOpen();
        if (isPlaceMode) // 如果已经在明牌模式下，则执行确认操作
        {
            OnPlaceConfirm();
            return;
        }
        if (GameManager.Instance.currentState != GameState.Phase2_Action) return;
        
        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (cur.hasSoldThisTurn)
        {
            AddCue("本回合已出售过真牌，不能明牌");
            return;
        }
        if (cur.hasPlacedThisTurn)
        {
            AddCue("本回合已执行过明牌操作");
            return;
        }

        isPlaceMode = true;
        selectedHandCard = null;
        selectedOpenSlot = -1;
        CloseOpenCardPanel();
        bool isPhase1 = (GameManager.Instance.currentState == GameState.Phase1_Sell);
        bool isPhase2 = (GameManager.Instance.currentState == GameState.Phase2_Action);
        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];

        // OpenPlayerOpenCards 会自动判断 interactive = true（因为是明牌模式且查看自己）
        OpenPlayerOpenCards(GameManager.Instance.currentTurnIndex);
        // 禁用关闭按钮
        if (closeOpenCardPanelBtn != null)
            closeOpenCardPanelBtn.interactable = false;
        // 改变 Place 按钮文本为"确认"
        if (placeButton != null)
        {
            TMP_Text btnText = placeButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Comfirm";
        }
        if (attackButton != null)
        {
            attackButton.interactable = isPhase1 &&
                                        !currentPlayer.hasSoldThisTurn &&
                                        !currentPlayer.hasStolenThisTurn &&
                                        !isPlaceMode &&
                                        !isBuyMode &&
                                        !isStealMode;
        }

        AddCue("请点击手牌选择卡牌，再点击明牌区槽位，最后点击「明牌」按钮确认");
        buyButton.interactable = false;
    }

    public void OnPlaceConfirm()
    {
        if (!isPlaceMode) return;

        if (selectedHandCard == null || selectedOpenSlot == -1)
        {
            AddCue("未选中手牌或槽位，明牌失败");
            isPlaceMode = false;
            ExitPlaceMode();
            return;
        }

        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        CardData targetSlotCard = cur.openCards[selectedOpenSlot];

        if (targetSlotCard != null)
        {
            cur.handCards.Remove(selectedHandCard);
            cur.handCards.Add(targetSlotCard);
            cur.openCards[selectedOpenSlot] = selectedHandCard;
            AddCue($"替换成功：{selectedHandCard.GetCardName()} → {targetSlotCard.GetCardName()}");
        }
        else
        {
            cur.handCards.Remove(selectedHandCard);
            cur.openCards[selectedOpenSlot] = selectedHandCard;
            AddCue($"明牌成功：在槽位 {selectedOpenSlot + 1} 放置了 {selectedHandCard.GetCardName()}");
        }

        cur.hasPlacedThisTurn = true;
        UpdateCurrentPlayerUI();

        ClearHandCardHighlight();
        if (selectedOpenSlot != -1) ClearSlotHighlight(selectedOpenSlot);

        isPlaceMode = false;
        ExitPlaceMode();

        // 提示但不自动结束
        if (cur.hasPlacedThisTurn && cur.hasBoughtThisTurn)
        {
            AddCue("两项行动都已完成，点击「Next」结束回合");
        }
    }
    void ExecuteSteal()
    {
        if (!isStealMode) return;

        if (stealTargetPlayer == -1 || stealTargetSlot == -1)
        {
            AddCue("请先选择要拆牌的对手和明牌区卡牌");
            return;
        }

        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        PlayerData target = GameManager.Instance.players[stealTargetPlayer];

        if (cur.gold < GameManager.Config.stealCost)
        {
            AddCue($"金币不足，需要{GameManager.Config.stealCost}金币");
            ExitStealMode();  // 退出拆牌模式
            return;
        }
        // 检查目标槽位是否有牌
        if (stealTargetSlot >= target.openCards.Count || target.openCards[stealTargetSlot] == null)
        {
            AddCue("目标槽位为空，无法拆牌");
            ExitStealMode();
            return;
        }

        CardData stolenCard = target.openCards[stealTargetSlot];

        cur.gold -= GameManager.Config.stealCost;

        // 移动卡牌：从对手明牌区移到对手手牌
        target.openCards[stealTargetSlot] = null;
        target.handCards.Add(stolenCard);
        cur.hasStolenThisTurn = true;
        // 记录已执行拆牌，本回合不能再明牌
        cur.hasPlacedThisTurn = true;//反复确认？
        AddCue($"拆牌成功！花费{GameManager.Config.stealCost}金币，将对手{stealTargetPlayer + 1}的{stolenCard.GetCardName()}移回其手牌");
        AddLog($"玩家{cur.playerIndex + 1}拆牌成功，从玩家{stealTargetPlayer + 1}的明牌区移除{stolenCard.GetCardName()}");

        // 刷新UI
        UpdateCurrentPlayerUI();
        // 如果当前明牌区面板正打开的是目标玩家，刷新显示
        if (openCardPanel.activeSelf && currentViewingPlayerIndex == stealTargetPlayer)
        {
            UpdateOpenCardDisplay(target, false);
        }

        // 退出拆牌模式
        ExitStealMode();
    }
    void ExitStealMode()
    {
        isStealMode = false;
        stealTargetPlayer = -1;
        stealTargetSlot = -1;
        ClearStealTarget();   // 清除选中的目标
        // 恢复按钮文本
        if (attackButton != null)
        {
            TMP_Text btnText = attackButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Attack";
        }

        // 关闭可能打开的明牌区面板
        CloseOpenCardPanel();

        // 刷新按钮状态
        AddCue("拆牌模式已退出");
    }

    void ExitPlaceMode()
    {
        isPlaceMode = false;
        selectedHandCard = null;
        selectedOpenSlot = -1;
        ClearHandCardHighlight();
        ClearSelectedCardForSell();
        // 恢复关闭按钮
        if (closeOpenCardPanelBtn != null)
            closeOpenCardPanelBtn.interactable = true;
        buyButton.interactable = true;
        // 恢复 Place 按钮文本
        if (placeButton != null)
        {
            TMP_Text btnText = placeButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Place";
        }
        OpenPlayerOpenCards(GameManager.Instance.currentTurnIndex);
    }

    void OnBuyButtonClick()
    {
        CloseBankPanelIfOpen();
        if (GameManager.Instance.currentState != GameState.Phase2_Action) return;
        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (cur.hasBoughtThisTurn)
        {
            AddCue("本回合已购买过");
            return;
        }
        
        isBuyMode = true;
        CloseOpenCardPanel();
        if (buyTargetPanel != null)
            buyTargetPanel.SetActive(true);
        RefreshBuyTargetButtons();
        
    }
    void RefreshBuyTargetButtons()
    {
        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        int currentIndex = GameManager.Instance.currentTurnIndex;

        // 银行按钮始终可用
        if (btnBuyBank != null)
            btnBuyBank.interactable = true;

        // 玩家按钮：检查是否被拒绝
        if (btnBuyPlayer1 != null)
        {
            bool isRejected = cur.rejectedBuyers.Contains(0);
            btnBuyPlayer1.interactable = !isRejected && 0 != currentIndex;
            TMP_Text btnText = btnBuyPlayer1.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = isRejected ? "玩家1 (已拒绝)" : "向玩家1购买";
        }

        if (btnBuyPlayer2 != null)
        {
            bool isRejected = cur.rejectedBuyers.Contains(1);
            btnBuyPlayer2.interactable = !isRejected && 1 != currentIndex;
            TMP_Text btnText = btnBuyPlayer2.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = isRejected ? "玩家2 (已拒绝)" : "向玩家2购买";
        }

        if (btnBuyPlayer3 != null)
        {
            bool isRejected = cur.rejectedBuyers.Contains(2);
            btnBuyPlayer3.interactable = !isRejected && 2 != currentIndex;
            TMP_Text btnText = btnBuyPlayer3.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = isRejected ? "玩家3 (已拒绝)" : "向玩家3购买";
        }
    }
    void ShowBankBuyPanel()
    {
        playerPriceInput.text = GameManager.Config.defaultPlayerBuyPrice.ToString();
        // 关闭选择面板
        if (buyTargetPanel != null)
            buyTargetPanel.SetActive(false);

        // 打开银行面板并设置为购买模式
        if (bankBuyPanel != null)
        {
            BankPanel bankPanelScript = bankBuyPanel.GetComponent<BankPanel>();
            if (bankPanelScript != null)
            {
                bankPanelScript.SetMode(true);  // 购买模式
                bankBuyPanel.SetActive(true);
            }
        }
    }


    // 普通查看银行（只读）
    public void ShowBankInfoOnly()
    {
        if (bankBuyPanel != null)
        {
            BankPanel bankPanelScript = bankBuyPanel.GetComponent<BankPanel>();
            if (bankPanelScript != null)
            {
                bankPanelScript.SetMode(false);  // 只读模式
                bankBuyPanel.SetActive(true);
            }
        }
    }
    void ShowPlayerBuyPanel(int targetPlayerIndex)
    {
        selectedTargetPlayerIndex = targetPlayerIndex;

        if (buyTargetPanel != null)
            buyTargetPanel.SetActive(false);

        if (playerGoodsDropdown != null)
        {
            playerGoodsDropdown.ClearOptions();
            playerGoodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
            playerGoodsDropdown.value = 0;
            playerGoodsDropdown.RefreshShownValue();
        }

        if (playerBuyTargetText != null)
            playerBuyTargetText.text = $"向玩家{targetPlayerIndex + 1}购买";

        // 使用配置中的默认玩家购买价格
        if (playerPriceInput != null)
            playerPriceInput.text = GameManager.Config.defaultPlayerBuyPrice.ToString();

        if (playerBuyPanel != null)
            playerBuyPanel.SetActive(true);
    }

    void ClosePlayerBuyPanel()
    {
        if (playerBuyPanel != null)
            playerBuyPanel.SetActive(false);

        // 重新打开选择面板
        if (buyTargetPanel != null)
            buyTargetPanel.SetActive(true);
    }

    void OnConfirmPlayerBuy()
    {
        if (GameManager.Instance == null) return;

        // 获取选择的货物类型
        CardData.CardType selectedType = CardData.CardType.Sugar;
        if (playerGoodsDropdown != null)
        {
            switch (playerGoodsDropdown.value)
            {
                case 0: selectedType = CardData.CardType.Sugar; break;
                case 1: selectedType = CardData.CardType.Oil; break;
                case 2: selectedType = CardData.CardType.Flour; break;
            }
        }

        // 获取价格
        int price = 10;
        if (playerPriceInput != null && int.TryParse(playerPriceInput.text, out int p))
            price = p;

        // 关闭面板
        ClosePlayerBuyPanel();
        CloseBuyTargetPanel();

        // 发起购买请求
        GameManager.Instance.RequestBuy(selectedTargetPlayerIndex, selectedType, price);
    }
    // 银行购买失败时调用
    public void OnBankBuyFailed()
    {
        // 关闭银行面板
        if (bankBuyPanel != null && bankBuyPanel.activeSelf)
        {
            bankBuyPanel.SetActive(false);
        }

        // 保持购买状态，重新打开购买对象选择面板
        isBuyMode = true;
        if (buyTargetPanel != null)
        {
            RefreshBuyTargetButtons();
            buyTargetPanel.SetActive(true);
        }
    }
    void CloseBuyTargetPanel()
    {
        if (buyTargetPanel != null)
            buyTargetPanel.SetActive(false);
        isBuyMode = false;
    }
    public void OnBuyComplete()
    {
        // 关闭银行面板（如果还开着）
        if (bankBuyPanel != null && bankBuyPanel.activeSelf)
        {
            bankBuyPanel.SetActive(false);
        }

        // 关闭购买选择面板
        if (buyTargetPanel != null)
        {
            buyTargetPanel.SetActive(false);
        }

        // 退出购买状态
        isBuyMode = false;

        if (GameManager.Instance.currentState == GameState.Phase2_Action)
        {
            OpenPlayerOpenCards(GameManager.Instance.currentTurnIndex);
        }

        // 检查是否两项行动都已完成
        PlayerData cur = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (cur.hasPlacedThisTurn && cur.hasBoughtThisTurn)
        {
            AddCue("两项行动都已完成，点击「Next」结束回合");
        }
    }
    public void ReopenBuyTargetPanel()
    {
        isBuyMode = true;
        if (buyTargetPanel != null)
        {
            RefreshBuyTargetButtons();
            buyTargetPanel.SetActive(true);
        }
    }

    void OnNextButtonClick()
    {
        CloseBankPanelIfOpen();
        if (GameManager.Instance.currentState == GameState.Phase1_Sell)
        {
            GameManager.Instance.SkipToPhase2();
        }
        else if (GameManager.Instance.currentState == GameState.Phase2_Action)
        {
            GameManager.Instance.EndTurn();
        }
        
    }
    
    public void ShowCurrentPlayerOpenCards()
    {
        if (GameManager.Instance == null) return;
        int currentIndex = GameManager.Instance.currentTurnIndex;
        OpenPlayerOpenCards(currentIndex);
    }
    private void CloseBankPanelIfOpen()
    {
        if (bankBuyPanel != null && bankBuyPanel.activeSelf)
        {
            bankBuyPanel.SetActive(false);
            Debug.Log("银行面板已关闭");
        }
    }
    // 关闭银行购买面板（由 GameManager 调用）
    public void CloseBankBuyPanel()
    {
        if (bankBuyPanel != null && bankBuyPanel.activeSelf)
        {
            bankBuyPanel.SetActive(false);
        }
    }

    IEnumerator ClearCueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cueText != null) cueText.text = "";
        clearCueCoroutine = null;
    }

    public void AddLog(string msg)
    {
        Debug.Log(msg);
        if (logText != null)
        {
            logText.text = $"{System.DateTime.Now:HH:mm:ss} - {msg}\n" + logText.text;
            string[] lines = logText.text.Split('\n');
            if (lines.Length > 20) logText.text = string.Join("\n", lines, 0, 20);
        }
    }

    public void UpdateGameState(string state)
    {
        if (gameStateText != null) gameStateText.text = state;
    }
    public void AddCue(string msg)
    {
        if (cueText != null)
        {
            if (clearCueCoroutine != null) StopCoroutine(clearCueCoroutine);
            cueText.text = msg;
            clearCueCoroutine = StartCoroutine(ClearCueAfterDelay(GameManager.Config.cueTextDuration));
        }
        AddLog(msg);
    }
}