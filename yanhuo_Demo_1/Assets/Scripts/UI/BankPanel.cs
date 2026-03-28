using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BankPanel : MonoBehaviour
{
    public static BankPanel Instance;

    [Header("UI组件")]
    public TMP_Text sugarStockText;
    public TMP_Text oilStockText;
    public TMP_Text flourStockText;
    public TMP_Text priceText;          // 显示价格
    public TMP_Dropdown goodsDropdown;
    public Button buyButton;
    public Button closeButton;

    private bool isBuyMode = false;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                gameObject.SetActive(false);
                // 如果 Panel 还在购买流程中（购买对象选择面板已打开），重新打开选择面板
                if (Panel.Instance != null && Panel.Instance.IsInBuyMode())
                {
                    Panel.Instance.ReopenBuyTargetPanel();
                }
            });
        }

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClick);

        // 初始化下拉框
        if (goodsDropdown != null)
        {
            goodsDropdown.ClearOptions();
            goodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
        }
        
    }

    void OnEnable()
    {
        //
        Debug.Log("BankPanel OnEnable 被调用");
        UpdateBankInfo();
    }
    public void SetMode(bool buyMode)
    {
        isBuyMode = buyMode;

        // 根据模式显示/隐藏购买按钮
        if (buyButton != null)
            buyButton.gameObject.SetActive(buyMode);

        // 更新价格显示
        UpdateBankInfo();
    }
    void UpdateBankInfo()
    {
        if (GameManager.Instance == null) return;

        sugarStockText.text = $"糖库存: {GameManager.Instance.GetBankStock(CardData.CardType.Sugar)}张";
        oilStockText.text = $"油库存: {GameManager.Instance.GetBankStock(CardData.CardType.Oil)}张";
        flourStockText.text = $"面库存: {GameManager.Instance.GetBankStock(CardData.CardType.Flour)}张";

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        bool isLeader = GameManager.Instance.IsGoldLeader(currentPlayer);
        int price = isLeader ? 18 : 12;

        if (priceText != null)
        {
            priceText.text = isLeader ? "价格: 18金币 (你是金币领先者)" : "价格: 12金币";
            priceText.color = isLeader ? Color.red : Color.black;   // 改为黑色
        }
        if (!isBuyMode && priceText != null)
        {
            priceText.text = "查看模式，如需购买请返回游戏点击「购买」按钮";
            priceText.color = Color.gray;
        }
    }

    void OnBuyButtonClick()
    {
        if (GameManager.Instance.currentState != GameState.Phase2_Action)
        {
            Panel.Instance?.AddCue("现在不是购买阶段");
            return;
        }

        CardData.CardType selectedType = CardData.CardType.Sugar;
        switch (goodsDropdown.value)
        {
            case 0: selectedType = CardData.CardType.Sugar; break;
            case 1: selectedType = CardData.CardType.Oil; break;
            case 2: selectedType = CardData.CardType.Flour; break;
        }

        if (!GameManager.Instance.BankHasGoods(selectedType))
        {
            Panel.Instance?.AddCue("银行没有这种货物了");
            // 购买失败，禁用该货物按钮或保持面板
            return;
        }

        int price = GameManager.Instance.IsGoldLeader(GameManager.Instance.players[GameManager.Instance.currentTurnIndex]) ? 18 : 12;
        GameManager.Instance.RequestBuy(-1, selectedType, price);

        // 购买成功后关闭面板（失败时面板保持打开）
        // 注意：购买成功或失败的处理在 GameManager 中已经完成
    }
}