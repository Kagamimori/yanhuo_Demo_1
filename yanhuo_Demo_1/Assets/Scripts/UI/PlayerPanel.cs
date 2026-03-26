using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    public static PlayerPanel Instance;

    [Header("UI组件")]
    public TMP_Text txtPlayerName;
    public TMP_Text txtPlayerGold;
    public TMP_Text txtPlayerIdentity;
    public Transform handCardRoot;
    public Transform openCardRoot;
    public GameObject prefabCard;

    [Header("操作按钮")]
    public Button closeButton;
    public Button sellButton;
    public Button placeButton;
    public Button buyButton;

    [Header("购买系统 - 主面板")]
    public GameObject buyMainPanel;           // 购买主面板
    public Button btnBuyFromBank;              // 向银行购买按钮
    public Button btnBuyFromPlayer1;           // 向玩家1购买按钮
    public Button btnBuyFromPlayer2;           // 向玩家2购买按钮
    public Button btnCancelBuy;                // 取消购买按钮

    [Header("购买系统 - 银行购买面板")]
    public GameObject bankBuyPanel;            // 银行购买面板
    public TMP_Dropdown bankGoodsDropdown;     // 货物下拉框
    public TMP_InputField bankPriceInput;      // 价格输入框
    public Button btnConfirmBankBuy;           // 确认银行购买按钮
    public Button btnCancelBankBuy;            // 取消银行购买按钮
    public TMP_Text bankPriceText;             // 银行价格显示

    [Header("购买系统 - 玩家购买面板")]
    public GameObject playerBuyPanel;          // 玩家购买面板
    public TMP_Text playerBuyTargetText;       // 显示购买目标
    public TMP_Dropdown playerGoodsDropdown;   // 货物下拉框
    public TMP_InputField playerPriceInput;    // 价格输入框
    public Button btnConfirmPlayerBuy;         // 确认玩家购买按钮
    public Button btnCancelPlayerBuy;          // 取消玩家购买按钮

    private int currentPlayerIndex;
    private PlayerData currentPlayerData;
    private CardData selectedCardForPlace;
    private bool isInitialized = false;

    // 购买相关变量
    private int selectedTargetPlayerIndex = -1;  // -1表示银行，0-2表示玩家索引

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
        InitBuyPanels();
        CheckComponents();
        isInitialized = true;
        Debug.Log("PlayerPanel 初始化完成");
    }

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

    /// <summary>
    /// 初始化所有购买面板
    /// </summary>
    private void InitBuyPanels()
    {
        // 初始化购买主面板
        if (buyMainPanel != null)
        {
            buyMainPanel.SetActive(false);

            if (btnBuyFromBank != null)
                btnBuyFromBank.onClick.RemoveAllListeners();
            if (btnBuyFromPlayer1 != null)
                btnBuyFromPlayer1.onClick.RemoveAllListeners();
            if (btnBuyFromPlayer2 != null)
                btnBuyFromPlayer2.onClick.RemoveAllListeners();
            if (btnCancelBuy != null)
                btnCancelBuy.onClick.RemoveAllListeners();
        }

        // 初始化银行购买面板
        if (bankBuyPanel != null)
        {
            bankBuyPanel.SetActive(false);

            if (btnConfirmBankBuy != null)
                btnConfirmBankBuy.onClick.RemoveAllListeners();
            if (btnCancelBankBuy != null)
                btnCancelBankBuy.onClick.RemoveAllListeners();
        }

        // 初始化玩家购买面板
        if (playerBuyPanel != null)
        {
            playerBuyPanel.SetActive(false);

            if (btnConfirmPlayerBuy != null)
                btnConfirmPlayerBuy.onClick.RemoveAllListeners();
            if (btnCancelPlayerBuy != null)
                btnCancelPlayerBuy.onClick.RemoveAllListeners();
        }

        // 初始化下拉框
        if (bankGoodsDropdown != null)
        {
            bankGoodsDropdown.ClearOptions();
            bankGoodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
        }

        if (playerGoodsDropdown != null)
        {
            playerGoodsDropdown.ClearOptions();
            playerGoodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
        }
    }

    /// <summary>
    /// 刷新购买按钮状态（根据是否被拒绝或已购买）
    /// </summary>
    private void RefreshBuyButtons()
    {
        if (buyMainPanel == null || !buyMainPanel.activeSelf) return;

        // 检查银行按钮
        if (btnBuyFromBank != null)
        {
            // 银行按钮始终可用（除非已购买过）
            btnBuyFromBank.interactable = !currentPlayerData.hasBoughtThisTurn;
        }

        // 检查其他玩家按钮
        for (int i = 0; i < 3; i++)
        {
            if (i == currentPlayerIndex) continue; // 跳过自己

            Button targetBtn = null;
            if (i == 0) targetBtn = btnBuyFromPlayer1;
            else if (i == 1) targetBtn = btnBuyFromPlayer2;
            else if (i == 2) targetBtn = null; // 玩家3需要另外添加

            if (targetBtn != null)
            {
                // 如果已被拒绝或已购买过，禁用按钮
                bool isRejected = currentPlayerData.rejectedBuyers.Contains(i);
                targetBtn.interactable = !currentPlayerData.hasBoughtThisTurn && !isRejected;

                // 更新按钮文字显示状态
                TMP_Text btnText = targetBtn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    if (isRejected)
                    {
                        btnText.text = $"玩家{i + 1} (已拒绝)";
                        btnText.color = Color.gray;
                    }
                    else
                    {
                        btnText.text = $"向玩家{i + 1}购买";
                        btnText.color = Color.white;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 显示购买主面板（选择向谁购买）
    /// </summary>
    private void ShowBuyMainPanel()
    {
        if (buyMainPanel != null)
        {
            RefreshBuyButtons();
            buyMainPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 显示银行购买面板
    /// </summary>
    private void ShowBankBuyPanel()
    {
        if (bankBuyPanel != null)
        {
            // 更新银行价格显示
            UpdateBankPriceDisplay();

            // 重置输入框
            if (bankPriceInput != null)
                bankPriceInput.text = "12";

            // 绑定确认按钮
            if (btnConfirmBankBuy != null)
            {
                btnConfirmBankBuy.onClick.RemoveAllListeners();
                btnConfirmBankBuy.onClick.AddListener(OnConfirmBankBuy);
            }

            // 绑定取消按钮
            if (btnCancelBankBuy != null)
            {
                btnCancelBankBuy.onClick.RemoveAllListeners();
                btnCancelBankBuy.onClick.AddListener(() => {
                    bankBuyPanel.SetActive(false);
                    ShowBuyMainPanel();
                });
            }

            // 隐藏主面板，显示银行面板
            if (buyMainPanel != null)
                buyMainPanel.SetActive(false);
            bankBuyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 显示玩家购买面板
    /// </summary>
    private void ShowPlayerBuyPanel(int targetPlayerIndex)
    {
        if (playerBuyPanel != null)
        {
            selectedTargetPlayerIndex = targetPlayerIndex;

            // 更新目标玩家显示
            if (playerBuyTargetText != null)
                playerBuyTargetText.text = $"向玩家{targetPlayerIndex + 1}购买";

            // 重置输入框
            if (playerPriceInput != null)
                playerPriceInput.text = "10";

            // 绑定确认按钮
            if (btnConfirmPlayerBuy != null)
            {
                btnConfirmPlayerBuy.onClick.RemoveAllListeners();
                btnConfirmPlayerBuy.onClick.AddListener(OnConfirmPlayerBuy);
            }

            // 绑定取消按钮
            if (btnCancelPlayerBuy != null)
            {
                btnCancelPlayerBuy.onClick.RemoveAllListeners();
                btnCancelPlayerBuy.onClick.AddListener(() => {
                    playerBuyPanel.SetActive(false);
                    ShowBuyMainPanel();
                });
            }

            // 隐藏主面板，显示玩家购买面板
            if (buyMainPanel != null)
                buyMainPanel.SetActive(false);
            playerBuyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 更新银行价格显示
    /// </summary>
    private void UpdateBankPriceDisplay()
    {
        if (GameManager.Instance == null) return;

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        bool isLeader = GameManager.Instance.IsGoldLeader(currentPlayer);
        int price = isLeader ? 18 : 12;

        if (bankPriceText != null)
        {
            bankPriceText.text = isLeader ? "价格: 18金币 (你是金币领先者)" : "价格: 12金币";
            bankPriceText.color = isLeader ? Color.red : Color.white;
        }
    }

    /// <summary>
    /// 确认银行购买
    /// </summary>
    private void OnConfirmBankBuy()
    {
        if (GameManager.Instance == null) return;

        // 获取选择的货物类型
        CardData.CardType selectedType = CardData.CardType.Sugar;
        if (bankGoodsDropdown != null)
        {
            switch (bankGoodsDropdown.value)
            {
                case 0: selectedType = CardData.CardType.Sugar; break;
                case 1: selectedType = CardData.CardType.Oil; break;
                case 2: selectedType = CardData.CardType.Flour; break;
            }
        }

        // 获取价格
        int price = 12;
        if (bankPriceInput != null && !string.IsNullOrEmpty(bankPriceInput.text))
        {
            int.TryParse(bankPriceInput.text, out price);
        }

        // 关闭购买面板
        if (bankBuyPanel != null)
            bankBuyPanel.SetActive(false);
        if (buyMainPanel != null)
            buyMainPanel.SetActive(false);

        // 发起购买请求（-1 表示向银行购买）
        GameManager.Instance.RequestBuy(-1, selectedType, price);

        // 刷新显示
        Refresh();
    }

    /// <summary>
    /// 确认玩家购买
    /// </summary>
    private void OnConfirmPlayerBuy()
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
        if (playerPriceInput != null && !string.IsNullOrEmpty(playerPriceInput.text))
        {
            int.TryParse(playerPriceInput.text, out price);
        }

        // 关闭购买面板
        if (playerBuyPanel != null)
            playerBuyPanel.SetActive(false);
        if (buyMainPanel != null)
            buyMainPanel.SetActive(false);

        // 发起购买请求
        GameManager.Instance.RequestBuy(selectedTargetPlayerIndex, selectedType, price);

        // 刷新显示
        Refresh();
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
        CreateBuyPanels();

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

            txtPlayerName = nameObj.AddComponent<TMP_Text>();
            txtPlayerName.font = TMP_Settings.defaultFontAsset;
            txtPlayerName.fontSize = 28;
            txtPlayerName.color = Color.white;
            txtPlayerName.alignment = TextAlignmentOptions.Left;
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

            txtPlayerGold = goldObj.AddComponent<TMP_Text>();
            txtPlayerGold.font = TMP_Settings.defaultFontAsset;
            txtPlayerGold.fontSize = 28;
            txtPlayerGold.color = Color.yellow;
            txtPlayerGold.alignment = TextAlignmentOptions.Center;
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

            txtPlayerIdentity = identityObj.AddComponent<TMP_Text>();
            txtPlayerIdentity.font = TMP_Settings.defaultFontAsset;
            txtPlayerIdentity.fontSize = 24;
            txtPlayerIdentity.color = Color.cyan;
            txtPlayerIdentity.alignment = TextAlignmentOptions.Right;
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

    /// <summary>
    /// 创建购买面板系统
    /// </summary>
    private void CreateBuyPanels()
    {
        // 创建购买主面板
        if (buyMainPanel == null)
        {
            buyMainPanel = new GameObject("BuyMainPanel");
            buyMainPanel.transform.SetParent(transform);

            RectTransform mainRect = buyMainPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.3f, 0.3f);
            mainRect.anchorMax = new Vector2(0.7f, 0.7f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image bgImage = buyMainPanel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // 创建标题
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(buyMainPanel.transform);
            TMP_Text titleText = titleObj.AddComponent<TMP_Text>();
            titleText.text = "选择购买对象";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // 创建按钮容器
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(buyMainPanel.transform);
            VerticalLayoutGroup vLayout = btnContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 20;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            RectTransform containerRect = btnContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.2f, 0.2f);
            containerRect.anchorMax = new Vector2(0.8f, 0.8f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // 创建银行按钮
            btnBuyFromBank = CreateButtonObject("BtnBank", "向银行购买").GetComponent<Button>();
            btnBuyFromBank.transform.SetParent(btnContainer.transform);
            btnBuyFromBank.onClick.AddListener(() => {
                buyMainPanel.SetActive(false);
                ShowBankBuyPanel();
            });

            // 创建玩家1按钮
            btnBuyFromPlayer1 = CreateButtonObject("BtnPlayer1", "向玩家1购买").GetComponent<Button>();
            btnBuyFromPlayer1.transform.SetParent(btnContainer.transform);
            btnBuyFromPlayer1.onClick.AddListener(() => {
                buyMainPanel.SetActive(false);
                ShowPlayerBuyPanel(0);
            });

            // 创建玩家2按钮
            btnBuyFromPlayer2 = CreateButtonObject("BtnPlayer2", "向玩家2购买").GetComponent<Button>();
            btnBuyFromPlayer2.transform.SetParent(btnContainer.transform);
            btnBuyFromPlayer2.onClick.AddListener(() => {
                buyMainPanel.SetActive(false);
                ShowPlayerBuyPanel(1);
            });

            // 创建取消按钮
            btnCancelBuy = CreateButtonObject("BtnCancel", "取消").GetComponent<Button>();
            btnCancelBuy.transform.SetParent(btnContainer.transform);
            btnCancelBuy.onClick.AddListener(() => {
                buyMainPanel.SetActive(false);
            });

            buyMainPanel.SetActive(false);
            Debug.Log("自动创建了购买主面板");
        }

        // 创建银行购买面板
        if (bankBuyPanel == null)
        {
            bankBuyPanel = new GameObject("BankBuyPanel");
            bankBuyPanel.transform.SetParent(transform);

            RectTransform bankRect = bankBuyPanel.AddComponent<RectTransform>();
            bankRect.anchorMin = new Vector2(0.3f, 0.3f);
            bankRect.anchorMax = new Vector2(0.7f, 0.7f);
            bankRect.offsetMin = Vector2.zero;
            bankRect.offsetMax = Vector2.zero;

            Image bankBg = bankBuyPanel.AddComponent<Image>();
            bankBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // 标题
            GameObject bankTitle = new GameObject("Title");
            bankTitle.transform.SetParent(bankBuyPanel.transform);
            TMP_Text bankTitleText = bankTitle.AddComponent<TMP_Text>();
            bankTitleText.text = "向银行购买";
            bankTitleText.fontSize = 24;
            bankTitleText.alignment = TextAlignmentOptions.Center;
            bankTitleText.color = Color.white;
            RectTransform bankTitleRect = bankTitle.GetComponent<RectTransform>();
            bankTitleRect.anchorMin = new Vector2(0, 0.85f);
            bankTitleRect.anchorMax = new Vector2(1, 1);
            bankTitleRect.offsetMin = Vector2.zero;
            bankTitleRect.offsetMax = Vector2.zero;

            // 价格显示
            GameObject priceObj = new GameObject("PriceText");
            priceObj.transform.SetParent(bankBuyPanel.transform);
            bankPriceText = priceObj.AddComponent<TMP_Text>();
            bankPriceText.fontSize = 18;
            bankPriceText.alignment = TextAlignmentOptions.Center;
            RectTransform priceRect = priceObj.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0, 0.7f);
            priceRect.anchorMax = new Vector2(1, 0.85f);
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;

            // 下拉框容器
            GameObject dropdownObj = new GameObject("GoodsDropdown");
            dropdownObj.transform.SetParent(bankBuyPanel.transform);
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.2f, 0.45f);
            dropdownRect.anchorMax = new Vector2(0.8f, 0.6f);
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = Vector2.zero;
            bankGoodsDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // 价格输入框
            GameObject inputObj = new GameObject("PriceInput");
            inputObj.transform.SetParent(bankBuyPanel.transform);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.2f, 0.25f);
            inputRect.anchorMax = new Vector2(0.8f, 0.4f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            bankPriceInput = inputObj.AddComponent<TMP_InputField>();

            // 按钮容器
            GameObject bankBtnContainer = new GameObject("ButtonContainer");
            bankBtnContainer.transform.SetParent(bankBuyPanel.transform);
            HorizontalLayoutGroup bankHLayout = bankBtnContainer.AddComponent<HorizontalLayoutGroup>();
            bankHLayout.childAlignment = TextAnchor.MiddleCenter;
            bankHLayout.spacing = 20;
            RectTransform bankBtnRect = bankBtnContainer.GetComponent<RectTransform>();
            bankBtnRect.anchorMin = new Vector2(0.2f, 0.05f);
            bankBtnRect.anchorMax = new Vector2(0.8f, 0.2f);
            bankBtnRect.offsetMin = Vector2.zero;
            bankBtnRect.offsetMax = Vector2.zero;

            btnConfirmBankBuy = CreateButtonObject("ConfirmBtn", "确认购买").GetComponent<Button>();
            btnConfirmBankBuy.transform.SetParent(bankBtnContainer.transform);

            btnCancelBankBuy = CreateButtonObject("CancelBtn", "返回").GetComponent<Button>();
            btnCancelBankBuy.transform.SetParent(bankBtnContainer.transform);

            bankBuyPanel.SetActive(false);
            Debug.Log("自动创建了银行购买面板");
        }

        // 创建玩家购买面板
        if (playerBuyPanel == null)
        {
            playerBuyPanel = new GameObject("PlayerBuyPanel");
            playerBuyPanel.transform.SetParent(transform);

            RectTransform playerRect = playerBuyPanel.AddComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.3f, 0.3f);
            playerRect.anchorMax = new Vector2(0.7f, 0.7f);
            playerRect.offsetMin = Vector2.zero;
            playerRect.offsetMax = Vector2.zero;

            Image playerBg = playerBuyPanel.AddComponent<Image>();
            playerBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // 标题
            GameObject playerTitle = new GameObject("Title");
            playerTitle.transform.SetParent(playerBuyPanel.transform);
            playerBuyTargetText = playerTitle.AddComponent<TMP_Text>();
            playerBuyTargetText.fontSize = 24;
            playerBuyTargetText.alignment = TextAlignmentOptions.Center;
            playerBuyTargetText.color = Color.white;
            RectTransform playerTitleRect = playerTitle.GetComponent<RectTransform>();
            playerTitleRect.anchorMin = new Vector2(0, 0.8f);
            playerTitleRect.anchorMax = new Vector2(1, 1);
            playerTitleRect.offsetMin = Vector2.zero;
            playerTitleRect.offsetMax = Vector2.zero;

            // 下拉框
            GameObject playerDropdownObj = new GameObject("GoodsDropdown");
            playerDropdownObj.transform.SetParent(playerBuyPanel.transform);
            RectTransform playerDropdownRect = playerDropdownObj.AddComponent<RectTransform>();
            playerDropdownRect.anchorMin = new Vector2(0.2f, 0.5f);
            playerDropdownRect.anchorMax = new Vector2(0.8f, 0.65f);
            playerDropdownRect.offsetMin = Vector2.zero;
            playerDropdownRect.offsetMax = Vector2.zero;
            playerGoodsDropdown = playerDropdownObj.AddComponent<TMP_Dropdown>();

            // 价格输入框
            GameObject playerInputObj = new GameObject("PriceInput");
            playerInputObj.transform.SetParent(playerBuyPanel.transform);
            RectTransform playerInputRect = playerInputObj.AddComponent<RectTransform>();
            playerInputRect.anchorMin = new Vector2(0.2f, 0.3f);
            playerInputRect.anchorMax = new Vector2(0.8f, 0.45f);
            playerInputRect.offsetMin = Vector2.zero;
            playerInputRect.offsetMax = Vector2.zero;
            playerPriceInput = playerInputObj.AddComponent<TMP_InputField>();

            // 按钮容器
            GameObject playerBtnContainer = new GameObject("ButtonContainer");
            playerBtnContainer.transform.SetParent(playerBuyPanel.transform);
            HorizontalLayoutGroup playerHLayout = playerBtnContainer.AddComponent<HorizontalLayoutGroup>();
            playerHLayout.childAlignment = TextAnchor.MiddleCenter;
            playerHLayout.spacing = 20;
            RectTransform playerBtnRect = playerBtnContainer.GetComponent<RectTransform>();
            playerBtnRect.anchorMin = new Vector2(0.2f, 0.05f);
            playerBtnRect.anchorMax = new Vector2(0.8f, 0.2f);
            playerBtnRect.offsetMin = Vector2.zero;
            playerBtnRect.offsetMax = Vector2.zero;

            btnConfirmPlayerBuy = CreateButtonObject("ConfirmBtn", "确认购买").GetComponent<Button>();
            btnConfirmPlayerBuy.transform.SetParent(playerBtnContainer.transform);

            btnCancelPlayerBuy = CreateButtonObject("CancelBtn", "返回").GetComponent<Button>();
            btnCancelPlayerBuy.transform.SetParent(playerBtnContainer.transform);

            playerBuyPanel.SetActive(false);
            Debug.Log("自动创建了玩家购买面板");
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
        TMP_Text text = textObj.AddComponent<TMP_Text>();
        text.text = buttonText;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
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
        TMP_Text nameText = nameTextObj.AddComponent<TMP_Text>();
        nameText.font = TMP_Settings.defaultFontAsset;
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Center;
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
                    if (OnCardSelected != null)
                    {
                        OnCardSelected(cardIndex, capturedCard);
                    }
                    OnHandCardClicked(capturedCard);
                };
            }
        }
    }

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
                    if (OnOpenSlotSelected != null)
                    {
                        OnOpenSlotSelected(slotIndex, slotIndex);
                    }
                    OnOpenSlotClicked(slotIndex);
                };
            }
        }
    }

    public void RefreshPanel()
    {
        if (gameObject != null && gameObject.activeSelf && currentPlayerData != null)
        {
            if (txtPlayerGold != null)
                txtPlayerGold.text = $"金币: {currentPlayerData.gold}";

            if (txtPlayerIdentity != null)
                txtPlayerIdentity.text = currentPlayerData.isRealMerchant ? "身份: 真货商人" : "身份: 假货商人";

            UpdateHandCards();
            UpdateOpenCards();
            UpdateButtonsByGameState();
        }
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

        // 显示购买主面板（选择向谁购买）
        ShowBuyMainPanel();
    }

    public void Refresh()
    {
        RefreshPanel();
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