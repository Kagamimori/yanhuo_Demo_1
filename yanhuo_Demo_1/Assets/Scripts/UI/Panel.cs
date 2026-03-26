using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{
    public static Panel Instance;

    [Header("按钮")]
    public Button btn_bank;
    public Button btn_player1;
    public Button btn_player2;
    public Button btn_player3;

    [Header("玩家UI区域")]
    public TMP_Text[] playerNameTexts;
    public TMP_Text[] playerGoldTexts;
    public TMP_Text[] playerIdentityTexts;
    public Transform[] handCardContainers;
    public Button[] viewPlayerButtons;  // 查看明牌区按钮

    [Header("明牌区面板（单个）")]
    public GameObject openCardPanel;           // 明牌区面板
    public TMP_Text openCardPanelTitle;        // 面板标题
    public Transform openCardContainer;        // 明牌区槽位容器
    public Button closeOpenCardPanelBtn;       // 关闭按钮

    [Header("通用")]
    public GameObject cardPrefab;
    public TMP_Text gameStateText;
    public TMP_Text logText;

    // 当前正在查看哪个玩家的明牌区（-1表示没有打开）
    private int currentViewingPlayerIndex = -1;

    // 明牌阶段标记
    private bool isPlaceCardPhase = false;

    // 当前选中的用于出售的卡牌
    private CardData selectedCardForSell = null;

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
        // 绑定银行按钮
        if (btn_bank != null)
        {
            btn_bank.onClick.AddListener(() =>
            {
                if (BankPanel.Instance != null)
                    BankPanel.Instance.gameObject.SetActive(true);
            });
        }

        // 绑定玩家查看按钮
        for (int i = 0; i < viewPlayerButtons.Length && i < 3; i++)
        {
            int playerIndex = i;
            if (viewPlayerButtons[i] != null)
            {
                viewPlayerButtons[i].onClick.AddListener(() => TogglePlayerOpenCards(playerIndex));
            }
        }

        // 绑定明牌区关闭按钮
        if (closeOpenCardPanelBtn != null)
        {
            closeOpenCardPanelBtn.onClick.AddListener(() => CloseOpenCardPanel());
        }

        // 初始隐藏明牌区面板
        if (openCardPanel != null)
            openCardPanel.SetActive(false);
    }

    void Update()
    {
        // 监听游戏状态变化
        if (GameManager.Instance != null)
        {
            bool isNowPlaceCardPhase = (GameManager.Instance.currentState == GameState.Phase2_Action);

            // 进入明牌阶段时，关闭所有打开的明牌区
            if (isNowPlaceCardPhase && !isPlaceCardPhase)
            {
                CloseOpenCardPanel();
                currentViewingPlayerIndex = -1;
            }

            isPlaceCardPhase = isNowPlaceCardPhase;
        }
    }

    /// <summary>
    /// 切换显示/隐藏玩家的明牌区
    /// </summary>
    public void TogglePlayerOpenCards(int playerIndex)
    {
        if (GameManager.Instance == null) return;

        // 在明牌阶段，只能查看自己的明牌区
        if (isPlaceCardPhase && playerIndex != GameManager.Instance.currentTurnIndex)
        {
            AddLog($"明牌阶段只能查看自己的明牌区！");
            return;
        }

        // 如果点击的是当前正在查看的玩家，则关闭
        if (currentViewingPlayerIndex == playerIndex)
        {
            CloseOpenCardPanel();
            currentViewingPlayerIndex = -1;
        }
        else
        {
            // 关闭之前打开的
            if (currentViewingPlayerIndex != -1)
            {
                CloseOpenCardPanel();
            }

            // 打开新的
            OpenPlayerOpenCards(playerIndex);
            currentViewingPlayerIndex = playerIndex;
        }
    }

    /// <summary>
    /// 打开指定玩家的明牌区面板
    /// </summary>
    private void OpenPlayerOpenCards(int playerIndex)
    {
        if (GameManager.Instance == null) return;
        if (playerIndex >= GameManager.Instance.players.Count) return;

        PlayerData player = GameManager.Instance.players[playerIndex];

        // 更新面板标题
        if (openCardPanelTitle != null)
        {
            openCardPanelTitle.text = $"玩家{playerIndex + 1}的明牌区";
        }

        // 刷新明牌区显示
        UpdateOpenCardDisplay(player);

        // 显示面板
        if (openCardPanel != null)
            openCardPanel.SetActive(true);

        AddLog($"查看玩家{playerIndex + 1}的明牌区");
    }

    /// <summary>
    /// 关闭明牌区面板
    /// </summary>
    public void CloseOpenCardPanel()
    {
        if (openCardPanel != null)
            openCardPanel.SetActive(false);
        currentViewingPlayerIndex = -1;
    }

    /// <summary>
    /// 更新明牌区显示（单个面板）
    /// </summary>
    private void UpdateOpenCardDisplay(PlayerData player)
    {
        if (openCardContainer == null || cardPrefab == null) return;

        // 清空容器
        foreach (Transform child in openCardContainer)
        {
            Destroy(child.gameObject);
        }

        bool isCurrentPlayer = (player.playerIndex == GameManager.Instance.currentTurnIndex);

        // 创建9个明牌区槽位
        for (int i = 0; i < 9; i++)
        {
            GameObject slotObj = Instantiate(cardPrefab, openCardContainer);
            CardUI cardUI = slotObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                if (i < player.openCards.Count && player.openCards[i] != null)
                {
                    cardUI.SetCardData(player.openCards[i]);

                    // 其他玩家的明牌区不可点击，自己的明牌区在明牌阶段空位才可点击
                    if (!isCurrentPlayer || !isPlaceCardPhase)
                    {
                        cardUI.SetInteractable(false);
                    }
                }
                else
                {
                    cardUI.SetEmpty();

                    // 只有当前玩家在明牌阶段的空位才能点击放置
                    if (isCurrentPlayer && isPlaceCardPhase)
                    {
                        int slotIndex = i;
                        cardUI.OnCardClick = (ui) => OnOpenSlotForPlace(slotIndex);
                    }
                    else
                    {
                        cardUI.SetInteractable(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 明牌阶段点击明牌区槽位（用于放置卡牌）
    /// </summary>
    private void OnOpenSlotForPlace(int slotIndex)
    {
        if (GameManager.Instance == null) return;
        if (!isPlaceCardPhase) return;

        // 通知GameManager进行明牌操作
        GameManager.Instance.OnPlaceCardSelected(slotIndex);

        // 刷新明牌区显示（重新打开当前查看的面板）
        if (currentViewingPlayerIndex != -1)
        {
            PlayerData player = GameManager.Instance.players[currentViewingPlayerIndex];
            UpdateOpenCardDisplay(player);
        }

        ClearSelectedCardForSell();
    }

    /// <summary>
    /// 刷新所有玩家UI（金币、身份、手牌）
    /// </summary>
    public void UpdateAllPlayersUI()
    {
        // 更新玩家名称、金币、身份
        for (int i = 0; i < GameManager.Instance.players.Count && i < 3; i++)
        {
            PlayerData player = GameManager.Instance.players[i];

            if (playerNameTexts[i] != null)
                playerNameTexts[i].text = $"玩家{i + 1}";

            if (playerGoldTexts[i] != null)
                playerGoldTexts[i].text = $"金币: {player.gold}";

            if (playerIdentityTexts[i] != null)
                playerIdentityTexts[i].text = player.isRealMerchant ? "真商" : "假商";

            // 只有当前玩家才显示手牌
            if (i == GameManager.Instance.currentTurnIndex)
            {
                if (handCardContainers[i] != null)
                {
                    UpdateHandCards(handCardContainers[i], player.handCards);
                    handCardContainers[i].gameObject.SetActive(true);
                }
            }
            else
            {
                if (handCardContainers[i] != null)
                    handCardContainers[i].gameObject.SetActive(false);
            }
        }

        // 如果明牌区面板打开，刷新显示
        if (openCardPanel != null && openCardPanel.activeSelf && currentViewingPlayerIndex != -1)
        {
            PlayerData player = GameManager.Instance.players[currentViewingPlayerIndex];
            UpdateOpenCardDisplay(player);
        }
    }

    /// <summary>
    /// 更新手牌显示
    /// </summary>
    private void UpdateHandCards(Transform container, List<CardData> cards)
    {
        if (container == null || cardPrefab == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.SetCardData(card);
                int cardIndex = i;
                CardData capturedCard = card;

                cardUI.OnCardClick = (ui) => {
                    if (GameManager.Instance.currentState == GameState.Phase1_Sell)
                    {
                        OnHandCardSelectedForSell(capturedCard);
                    }
                    else if (GameManager.Instance.currentState == GameState.Phase2_Action)
                    {
                        OnHandCardSelectedForPlace(capturedCard, cardIndex);
                    }
                };
            }
        }
    }

    /// <summary>
    /// 阶段1：选中卡牌用于出售
    /// </summary>
    private void OnHandCardSelectedForSell(CardData card)
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase1_Sell)
        {
            AddLog("现在不是出售阶段");
            return;
        }

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasSoldThisTurn)
        {
            AddLog("本回合已经出售过真牌");
            return;
        }

        if (card.quality != CardData.CardQuality.Real)
        {
            AddLog("只能出售真货");
            return;
        }

        ClearSelectedCardForSell();
        selectedCardForSell = card;
        HighlightSelectedCardForSell(card);
        AddLog($"已选中 {card.GetCardName()}，请点击「出售真牌」按钮");
    }

    /// <summary>
    /// 高亮选中的出售卡牌
    /// </summary>
    private void HighlightSelectedCardForSell(CardData card)
    {
        if (GameManager.Instance == null) return;

        Transform container = handCardContainers[GameManager.Instance.currentTurnIndex];
        if (container == null) return;

        foreach (Transform child in container)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null && cardUI.GetCardData() == card)
            {
                cardUI.SetSelected(true);
            }
            else if (cardUI != null)
            {
                cardUI.ClearSelected();
            }
        }
    }

    /// <summary>
    /// 清除出售选中的卡牌
    /// </summary>
    public void ClearSelectedCardForSell()
    {
        selectedCardForSell = null;

        if (GameManager.Instance == null) return;

        Transform container = handCardContainers[GameManager.Instance.currentTurnIndex];
        if (container == null) return;

        foreach (Transform child in container)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.ClearSelected();
            }
        }
    }

    /// <summary>
    /// 执行出售选中的卡牌
    /// </summary>
    public void ExecuteSellSelectedCard()
    {
        if (GameManager.Instance == null) return;

        if (selectedCardForSell == null)
        {
            AddLog("请先点击手牌选中要出售的真货");
            return;
        }

        if (GameManager.Instance.currentState != GameState.Phase1_Sell)
        {
            AddLog("现在不是出售阶段");
            return;
        }

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasSoldThisTurn)
        {
            AddLog("本回合已经出售过真牌");
            return;
        }

        if (!currentPlayer.handCards.Contains(selectedCardForSell))
        {
            AddLog("选中的卡牌已不存在");
            ClearSelectedCardForSell();
            return;
        }

        GameManager.Instance.OnSellRealCard(selectedCardForSell);
        ClearSelectedCardForSell();
        UpdateAllPlayersUI();
    }

    /// <summary>
    /// 阶段2：选中卡牌用于明牌
    /// </summary>
    private void OnHandCardSelectedForPlace(CardData card, int handIndex)
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase2_Action)
        {
            AddLog("现在不是明牌阶段");
            return;
        }

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasPlacedThisTurn)
        {
            AddLog("本回合已经执行过明牌操作");
            return;
        }

        GameManager.Instance.SelectCardForPlace(card, handIndex);
        AddLog($"已选中 {card.GetCardName()}，请点击明牌区空位");

        HighlightSelectedCardForPlace(card);

        // 打开自己的明牌区面板
        OpenPlayerOpenCards(GameManager.Instance.currentTurnIndex);
    }

    /// <summary>
    /// 高亮选中的明牌卡牌
    /// </summary>
    private void HighlightSelectedCardForPlace(CardData card)
    {
        if (GameManager.Instance == null) return;

        Transform container = handCardContainers[GameManager.Instance.currentTurnIndex];
        if (container == null) return;

        foreach (Transform child in container)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null && cardUI.GetCardData() == card)
            {
                cardUI.SetSelected(true);
            }
            else if (cardUI != null)
            {
                cardUI.ClearSelected();
            }
        }
    }

    /// <summary>
    /// 清除所有卡牌选中状态
    /// </summary>
    public void ClearAllSelectedCards()
    {
        ClearSelectedCardForSell();

        if (GameManager.Instance == null) return;

        Transform container = handCardContainers[GameManager.Instance.currentTurnIndex];
        if (container == null) return;

        foreach (Transform child in container)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.ClearSelected();
            }
        }
    }

    /// <summary>
    /// 添加日志
    /// </summary>
    public void AddLog(string message)
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

    /// <summary>
    /// 更新游戏状态文本
    /// </summary>
    public void UpdateGameState(string state)
    {
        if (gameStateText != null)
        {
            gameStateText.text = state;
        }
    }
}