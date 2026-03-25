using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    public static PlayerPanel Instance;

    [Header("UI组件")]
    public Text txtPlayerName;           // 玩家名称
    public Text txtPlayerGold;           // 玩家金币
    public Text txtPlayerIdentity;       // 玩家身份
    public Transform handCardRoot;       // 手牌容器
    public Transform openCardRoot;       // 明牌区容器
    public GameObject prefabCard;        // 卡牌预制体

    [Header("操作按钮")]
    public Button closeButton;
    public Button sellButton;
    public Button placeButton;
    public Button buyButton;

    [Header("购买面板")]
    public GameObject buyPanel;
    public Dropdown goodsDropdown;
    public InputField priceInput;
    public Button confirmBuyButton;
    public Button cancelBuyButton;

    private int currentPlayerIndex;
    private PlayerData currentPlayerData;
    private CardData selectedCardForPlace;
    private bool isInitialized = false;

    // 添加缺失的委托定义
    public System.Action<int, CardData> OnCardSelected;
    public System.Action<int, int> OnOpenSlotSelected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (gameObject != null)
                gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return null;
        AutoCreateMissingComponents();
        BindButtons();
        InitBuyPanel();
        CheckComponents();
        isInitialized = true;
        Debug.Log("PlayerPanel 初始化完成");
    }

    // 添加 Initialize 方法
    public void Initialize(int playerIndex, PlayerData data)
    {
        currentPlayerIndex = playerIndex;
        currentPlayerData = data;

        if (txtPlayerName != null)
            txtPlayerName.text = $"玩家{playerIndex + 1}";

        if (txtPlayerGold != null)
            txtPlayerGold.text = $"金币: {data.gold}";

        if (txtPlayerIdentity != null)
            txtPlayerIdentity.text = data.isRealMerchant ? "身份: 真货商人" : "身份: 假货商人";

        UpdateHandCards();
        UpdateOpenCards();
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                if (gameObject != null)
                    gameObject.SetActive(false);
            });
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellButtonClick);
        }

        if (placeButton != null)
        {
            placeButton.onClick.RemoveAllListeners();
            placeButton.onClick.AddListener(OnPlaceButtonClick);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButtonClick);
        }
    }

    private void InitBuyPanel()
    {
        if (buyPanel != null)
        {
            buyPanel.SetActive(false);

            if (confirmBuyButton != null)
            {
                confirmBuyButton.onClick.RemoveAllListeners();
                confirmBuyButton.onClick.AddListener(OnConfirmBuy);
            }

            if (cancelBuyButton != null)
            {
                cancelBuyButton.onClick.RemoveAllListeners();
                cancelBuyButton.onClick.AddListener(() => {
                    if (buyPanel != null)
                        buyPanel.SetActive(false);
                });
            }
        }

        if (goodsDropdown != null)
        {
            goodsDropdown.ClearOptions();
            goodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
        }
    }

    // 修改 UpdateHandCards 方法，触发 OnCardSelected 事件
    private void UpdateHandCards()
    {
        if (handCardRoot == null)
        {
            Debug.LogError("手牌容器为空，无法显示手牌");
            return;
        }

        foreach (Transform child in handCardRoot)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        for (int i = 0; i < currentPlayerData.handCards.Count; i++)
        {
            var card = currentPlayerData.handCards[i];
            if (prefabCard == null)
            {
                Debug.LogError("prefabCard 为空，无法创建卡牌");
                return;
            }

            GameObject cardObj = Instantiate(prefabCard, handCardRoot);
            CardUI cardUI = cardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.SetCardData(card);
                int cardIndex = i;
                CardData capturedCard = card;
                cardUI.OnCardClick = (ui) => {
                    // 触发 OnCardSelected 事件
                    if (OnCardSelected != null)
                    {
                        OnCardSelected(cardIndex, capturedCard);
                    }
                    OnHandCardClicked(capturedCard);
                };
            }
        }
    }

    // 修改 UpdateOpenCards 方法，触发 OnOpenSlotSelected 事件
    private void UpdateOpenCards()
    {
        if (openCardRoot == null)
        {
            Debug.LogError("明牌区容器为空，无法显示明牌区");
            return;
        }

        foreach (Transform child in openCardRoot)
        {
            if (child != null)
                Destroy(child.gameObject);
        }

        for (int i = 0; i < 9; i++)
        {
            if (prefabCard == null)
            {
                Debug.LogError("prefabCard 为空，无法创建明牌区槽位");
                return;
            }

            GameObject slotObj = Instantiate(prefabCard, openCardRoot);
            CardUI cardUI = slotObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                if (i < currentPlayerData.openCards.Count && currentPlayerData.openCards[i] != null)
                {
                    cardUI.SetCardData(currentPlayerData.openCards[i]);
                }
                else
                {
                    cardUI.SetEmpty();
                }

                int slotIndex = i;
                cardUI.OnCardClick = (ui) => {
                    // 触发 OnOpenSlotSelected 事件
                    if (OnOpenSlotSelected != null)
                    {
                        OnOpenSlotSelected(slotIndex, slotIndex);
                    }
                    OnOpenSlotClicked(slotIndex);
                };
            }
        }
    }

    // 添加缺失的 Refresh 方法
    public void Refresh()
    {
        if (gameObject != null && gameObject.activeSelf && currentPlayerData != null)
        {
            // 刷新显示
            if (txtPlayerGold != null)
                txtPlayerGold.text = $"金币: {currentPlayerData.gold}";

            if (txtPlayerIdentity != null)
                txtPlayerIdentity.text = currentPlayerData.isRealMerchant ? "身份: 真货商人" : "身份: 假货商人";

            UpdateHandCards();
            UpdateOpenCards();
            UpdateButtonsByGameState();
        }
    }

    // 其他方法保持不变...
    private void AutoCreateMissingComponents()
    {
        RectTransform parentRect = GetComponent<RectTransform>();
        if (parentRect == null)
        {
            parentRect = gameObject.AddComponent<RectTransform>();
            parentRect.sizeDelta = new Vector2(800, 600);
        }

        if (handCardRoot == null)
        {
            GameObject container = new GameObject("HandCardContainer");
            container.transform.SetParent(transform);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.5f, 0.6f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(10, 10);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            handCardRoot = container.transform;
            Debug.Log("自动创建了手牌容器");
        }

        if (openCardRoot == null)
        {
            GameObject container = new GameObject("OpenCardContainer");
            container.transform.SetParent(transform);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(1, 0.6f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;

            openCardRoot = container.transform;
            Debug.Log("自动创建了明牌区容器");
        }

        CreateTitleArea();
        CreateButtonArea();

        if (buyPanel == null)
        {
            CreateBuyPanel();
        }

        if (prefabCard == null)
        {
            prefabCard = CreateCardPrefab();
            Debug.Log("自动创建了卡牌预制体");
        }
    }

    private void CreateTitleArea()
    {
        GameObject titleArea = new GameObject("TitleArea");
        titleArea.transform.SetParent(transform);

        RectTransform titleRect = titleArea.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 10);
        titleRect.offsetMax = new Vector2(-10, -10);

        if (txtPlayerName == null)
        {
            GameObject nameObj = new GameObject("PlayerNameText");
            nameObj.transform.SetParent(titleArea.transform);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.33f, 1);
            nameRect.offsetMin = new Vector2(10, 10);
            nameRect.offsetMax = new Vector2(-10, -10);

            txtPlayerName = nameObj.AddComponent<Text>();
            txtPlayerName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txtPlayerName.fontSize = 28;
            txtPlayerName.color = Color.white;
            txtPlayerName.alignment = TextAnchor.MiddleLeft;
            txtPlayerName.text = "玩家1";
            Debug.Log("自动创建了玩家名称文本");
        }

        if (txtPlayerGold == null)
        {
            GameObject goldObj = new GameObject("PlayerGoldText");
            goldObj.transform.SetParent(titleArea.transform);

            RectTransform goldRect = goldObj.AddComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.33f, 0.5f);
            goldRect.anchorMax = new Vector2(0.66f, 1);
            goldRect.offsetMin = new Vector2(10, 10);
            goldRect.offsetMax = new Vector2(-10, -10);

            txtPlayerGold = goldObj.AddComponent<Text>();
            txtPlayerGold.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txtPlayerGold.fontSize = 28;
            txtPlayerGold.color = Color.yellow;
            txtPlayerGold.alignment = TextAnchor.MiddleCenter;
            txtPlayerGold.text = "金币: 0";
            Debug.Log("自动创建了玩家金币文本");
        }

        if (txtPlayerIdentity == null)
        {
            GameObject identityObj = new GameObject("PlayerIdentityText");
            identityObj.transform.SetParent(titleArea.transform);

            RectTransform identityRect = identityObj.AddComponent<RectTransform>();
            identityRect.anchorMin = new Vector2(0.66f, 0.5f);
            identityRect.anchorMax = new Vector2(1, 1);
            identityRect.offsetMin = new Vector2(10, 10);
            identityRect.offsetMax = new Vector2(-10, -10);

            txtPlayerIdentity = identityObj.AddComponent<Text>();
            txtPlayerIdentity.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txtPlayerIdentity.fontSize = 24;
            txtPlayerIdentity.color = Color.cyan;
            txtPlayerIdentity.alignment = TextAnchor.MiddleRight;
            txtPlayerIdentity.text = "身份: 真货商人";
            Debug.Log("自动创建了玩家身份文本");
        }
    }

    private void CreateButtonArea()
    {
        GameObject buttonArea = new GameObject("ButtonArea");
        buttonArea.transform.SetParent(transform);

        RectTransform buttonRect = buttonArea.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0.6f);
        buttonRect.anchorMax = new Vector2(1, 0.8f);
        buttonRect.offsetMin = new Vector2(10, 10);
        buttonRect.offsetMax = new Vector2(-10, -10);

        HorizontalLayoutGroup layout = buttonArea.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        if (closeButton == null)
        {
            GameObject btnObj = CreateButtonObject("CloseButton", "关闭");
            btnObj.transform.SetParent(buttonArea.transform);
            closeButton = btnObj.GetComponent<Button>();
            Debug.Log("自动创建了关闭按钮");
        }

        if (sellButton == null)
        {
            GameObject btnObj = CreateButtonObject("SellButton", "出售真牌");
            btnObj.transform.SetParent(buttonArea.transform);
            sellButton = btnObj.GetComponent<Button>();
            Debug.Log("自动创建了出售按钮");
        }

        if (placeButton == null)
        {
            GameObject btnObj = CreateButtonObject("PlaceButton", "明牌");
            btnObj.transform.SetParent(buttonArea.transform);
            placeButton = btnObj.GetComponent<Button>();
            Debug.Log("自动创建了明牌按钮");
        }

        if (buyButton == null)
        {
            GameObject btnObj = CreateButtonObject("BuyButton", "购买");
            btnObj.transform.SetParent(buttonArea.transform);
            buyButton = btnObj.GetComponent<Button>();
            Debug.Log("自动创建了购买按钮");
        }
    }

    private GameObject CreateButtonObject(string name, string buttonText)
    {
        GameObject btnObj = new GameObject(name);
        Image image = btnObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.8f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 50);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    private void CreateBuyPanel()
    {
        buyPanel = new GameObject("BuyPanel");
        buyPanel.transform.SetParent(transform);

        RectTransform panelRect = buyPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.3f);
        panelRect.anchorMax = new Vector2(0.7f, 0.7f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bgImage = buyPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        GameObject dropdownObj = new GameObject("GoodsDropdown");
        dropdownObj.transform.SetParent(buyPanel.transform);
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.2f, 0.6f);
        dropdownRect.anchorMax = new Vector2(0.8f, 0.8f);
        dropdownRect.offsetMin = Vector2.zero;
        dropdownRect.offsetMax = Vector2.zero;
        goodsDropdown = dropdownObj.AddComponent<Dropdown>();

        GameObject inputObj = new GameObject("PriceInput");
        inputObj.transform.SetParent(buyPanel.transform);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.2f, 0.3f);
        inputRect.anchorMax = new Vector2(0.8f, 0.5f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        priceInput = inputObj.AddComponent<InputField>();

        GameObject confirmObj = CreateButtonObject("ConfirmButton", "确认购买");
        confirmObj.transform.SetParent(buyPanel.transform);
        RectTransform confirmRect = confirmObj.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.2f, 0.05f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.2f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;
        confirmBuyButton = confirmObj.GetComponent<Button>();

        GameObject cancelObj = CreateButtonObject("CancelButton", "取消");
        cancelObj.transform.SetParent(buyPanel.transform);
        RectTransform cancelRect = cancelObj.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0.05f);
        cancelRect.anchorMax = new Vector2(0.8f, 0.2f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
        cancelBuyButton = cancelObj.GetComponent<Button>();

        buyPanel.SetActive(false);
        Debug.Log("自动创建了购买面板");
    }

    private GameObject CreateCardPrefab()
    {
        GameObject card = new GameObject("CardPrefab");
        Image image = card.AddComponent<Image>();
        image.color = new Color(0.85f, 0.75f, 0.55f);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 120);

        CardUI cardUI = card.AddComponent<CardUI>();

        GameObject nameTextObj = new GameObject("NameText");
        nameTextObj.transform.SetParent(card.transform);
        Text nameText = nameTextObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nameText.fontSize = 14;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.black;

        RectTransform nameRect = nameTextObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        if (cardUI != null)
        {
            var backgroundField = cardUI.GetType().GetField("cardBackground");
            var nameTextField = cardUI.GetType().GetField("cardNameText");
            if (backgroundField != null) backgroundField.SetValue(cardUI, image);
            if (nameTextField != null) nameTextField.SetValue(cardUI, nameText);
        }

        return card;
    }

    private void CheckComponents()
    {
        Debug.Log("=== PlayerPanel 组件检查 ===");
        Debug.Log($"handCardRoot: {(handCardRoot != null ? handCardRoot.name : "未赋值")}");
        Debug.Log($"openCardRoot: {(openCardRoot != null ? openCardRoot.name : "未赋值")}");
        Debug.Log($"prefabCard: {(prefabCard != null ? prefabCard.name : "未赋值")}");
        Debug.Log($"txtPlayerName: {(txtPlayerName != null ? txtPlayerName.name : "未赋值")}");
        Debug.Log($"txtPlayerGold: {(txtPlayerGold != null ? txtPlayerGold.name : "未赋值")}");
        Debug.Log($"txtPlayerIdentity: {(txtPlayerIdentity != null ? txtPlayerIdentity.name : "未赋值")}");
        Debug.Log("==========================");
    }

    public void ShowPlayerInfo(int playerIndex)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("PlayerPanel 尚未初始化完成，稍后重试");
            StartCoroutine(ShowPlayerInfoAfterInit(playerIndex));
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 不存在");
            return;
        }

        if (playerIndex >= GameManager.Instance.players.Count)
        {
            Debug.LogError($"玩家索引 {playerIndex} 超出范围");
            return;
        }

        if (prefabCard == null)
        {
            Debug.LogError("Card Prefab 未赋值！");
            return;
        }

        currentPlayerIndex = playerIndex;
        currentPlayerData = GameManager.Instance.players[playerIndex];

        if (txtPlayerName != null)
            txtPlayerName.text = $"玩家{playerIndex + 1}";

        if (txtPlayerGold != null)
            txtPlayerGold.text = $"金币: {currentPlayerData.gold}";

        if (txtPlayerIdentity != null)
            txtPlayerIdentity.text = currentPlayerData.isRealMerchant ? "身份: 真货商人" : "身份: 假货商人";

        UpdateHandCards();
        UpdateOpenCards();
        UpdateButtonsByGameState();

        if (gameObject != null)
            gameObject.SetActive(true);
    }

    IEnumerator ShowPlayerInfoAfterInit(int playerIndex)
    {
        while (!isInitialized)
        {
            yield return null;
        }
        ShowPlayerInfo(playerIndex);
    }

    private void UpdateButtonsByGameState()
    {
        if (GameManager.Instance == null) return;

        bool isCurrentPlayer = (currentPlayerIndex == GameManager.Instance.currentTurnIndex);

        if (sellButton != null)
            sellButton.interactable = isCurrentPlayer &&
                (GameManager.Instance.currentState == GameState.Phase1_Sell) &&
                !currentPlayerData.hasSoldThisTurn;

        if (placeButton != null)
            placeButton.interactable = isCurrentPlayer &&
                (GameManager.Instance.currentState == GameState.Phase2_Action) &&
                !currentPlayerData.hasPlacedThisTurn;

        if (buyButton != null)
            buyButton.interactable = isCurrentPlayer &&
                (GameManager.Instance.currentState == GameState.Phase2_Action) &&
                !currentPlayerData.hasBoughtThisTurn;
    }

    private void OnHandCardClicked(CardData card)
    {
        if (GameManager.Instance == null) return;

        if (currentPlayerIndex != GameManager.Instance.currentTurnIndex)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("不是你的回合");
            return;
        }

        if (GameManager.Instance.currentState == GameState.Phase1_Sell)
        {
            if (card.quality == CardData.CardQuality.Real)
            {
                GameManager.Instance.OnSellRealCard(card);
                ShowPlayerInfo(currentPlayerIndex);
            }
            else
            {
                if (Panel.Instance != null)
                    Panel.Instance.AddLog("只能出售真货");
            }
        }
        else if (GameManager.Instance.currentState == GameState.Phase2_Action)
        {
            if (!currentPlayerData.hasPlacedThisTurn)
            {
                selectedCardForPlace = card;
                GameManager.Instance.SelectCardForPlace(card, currentPlayerData.handCards.IndexOf(card));
                if (Panel.Instance != null)
                    Panel.Instance.AddLog($"已选中 {card.GetCardName()}，请点击明牌区槽位");
                HighlightSelectedCard(card);
            }
            else
            {
                if (Panel.Instance != null)
                    Panel.Instance.AddLog("本回合已经执行过明牌操作");
            }
        }
    }

    private void OnOpenSlotClicked(int slotIndex)
    {
        if (GameManager.Instance == null) return;

        if (currentPlayerIndex != GameManager.Instance.currentTurnIndex)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("不是你的回合");
            return;
        }

        if (GameManager.Instance.currentState == GameState.Phase2_Action)
        {
            if (selectedCardForPlace != null)
            {
                GameManager.Instance.OnPlaceCardSelected(slotIndex);
                selectedCardForPlace = null;
                ShowPlayerInfo(currentPlayerIndex);
            }
            else
            {
                if (Panel.Instance != null)
                    Panel.Instance.AddLog("请先点击手牌选择要明牌的卡牌");
            }
        }
    }

    private void HighlightSelectedCard(CardData card)
    {
        if (handCardRoot == null) return;

        foreach (Transform child in handCardRoot)
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

    private void OnSellButtonClick()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase1_Sell)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("现在不是出售阶段");
            return;
        }

        List<CardData> realCards = currentPlayerData.handCards.FindAll(c => c.quality == CardData.CardQuality.Real);

        if (realCards.Count == 0)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("没有可出售的真货");
            return;
        }

        if (Panel.Instance != null)
            Panel.Instance.AddLog($"出售 {realCards[0].GetCardName()}");

        GameManager.Instance.OnSellRealCard(realCards[0]);
        ShowPlayerInfo(currentPlayerIndex);
    }

    private void OnPlaceButtonClick()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase2_Action)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("现在不是明牌阶段");
            return;
        }

        if (currentPlayerData.hasPlacedThisTurn)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("本回合已经执行过明牌操作");
            return;
        }

        if (Panel.Instance != null)
            Panel.Instance.AddLog("请点击手牌选择要明牌的卡牌，再点击明牌区槽位");
    }

    private void OnBuyButtonClick()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase2_Action)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("现在不是购买阶段");
            return;
        }

        if (currentPlayerData.hasBoughtThisTurn)
        {
            if (Panel.Instance != null)
                Panel.Instance.AddLog("本回合已经购买过货物");
            return;
        }

        if (buyPanel != null)
        {
            buyPanel.SetActive(true);
            if (priceInput != null)
                priceInput.text = "12";
        }
    }

    private void OnConfirmBuy()
    {
        if (GameManager.Instance == null) return;

        CardData.CardType selectedType = CardData.CardType.Sugar;
        if (goodsDropdown != null)
        {
            switch (goodsDropdown.value)
            {
                case 0: selectedType = CardData.CardType.Sugar; break;
                case 1: selectedType = CardData.CardType.Oil; break;
                case 2: selectedType = CardData.CardType.Flour; break;
            }
        }

        int price = 12;
        if (priceInput != null && !string.IsNullOrEmpty(priceInput.text))
        {
            int.TryParse(priceInput.text, out price);
        }

        if (buyPanel != null)
            buyPanel.SetActive(false);

        GameManager.Instance.RequestBuy(-1, selectedType, price);
        ShowPlayerInfo(currentPlayerIndex);
    }

    public void RefreshPanel()
    {
        if (gameObject != null && gameObject.activeSelf && currentPlayerData != null)
        {
            ShowPlayerInfo(currentPlayerIndex);
        }
    }

    public void ClearSelectedCardInPanel()
    {
        selectedCardForPlace = null;

        if (handCardRoot != null)
        {
            foreach (Transform child in handCardRoot)
            {
                CardUI cardUI = child.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.ClearSelected();
                }
            }
        }
    }
}