using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BankPanel : MonoBehaviour
{
    public static BankPanel Instance;

    [Header("UI组件")]
    public Text bankTitleText;
    public Text sugarStockText;
    public Text oilStockText;
    public Text flourStockText;
    public Text priceText;
    public InputField priceInput;
    public Dropdown goodsDropdown;
    public Button buyButton;
    public Button closeButton;

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
        if (closeButton != null)
            closeButton.onClick.AddListener(() => {
                if (gameObject != null)
                    gameObject.SetActive(false);
            });

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClick);

        if (goodsDropdown != null)
        {
            goodsDropdown.ClearOptions();
            goodsDropdown.AddOptions(new List<string> { "糖", "油", "面" });
        }
    }

    void OnEnable()
    {
        UpdateBankInfo();
    }

    void UpdateBankInfo()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 不存在");
            return;
        }

        // 更新库存 - 使用 GetBankStock 方法
        if (sugarStockText != null)
        {
            int stock = GameManager.Instance.GetBankStock(CardData.CardType.Sugar);
            sugarStockText.text = $"糖库存: {stock}张";
        }

        if (oilStockText != null)
        {
            int stock = GameManager.Instance.GetBankStock(CardData.CardType.Oil);
            oilStockText.text = $"油库存: {stock}张";
        }

        if (flourStockText != null)
        {
            int stock = GameManager.Instance.GetBankStock(CardData.CardType.Flour);
            flourStockText.text = $"面库存: {stock}张";
        }

        // 更新价格
        if (GameManager.Instance.players != null &&
            GameManager.Instance.players.Count > GameManager.Instance.currentTurnIndex)
        {
            PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
            bool isLeader = GameManager.Instance.IsGoldLeader(currentPlayer);
            int price = isLeader ? 18 : 12;

            if (priceText != null)
            {
                priceText.text = isLeader ? "价格: 18金币 (你是金币领先者)" : "价格: 12金币";
                priceText.color = isLeader ? Color.red : Color.white;
            }
        }
    }

    void OnBuyButtonClick()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState != GameState.Phase2_Action)
        {
            Panel.Instance?.AddLog("现在不是购买阶段");
            return;
        }

        // 获取选择的货物类型
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

        // 获取输入的价格
        int price = 12;
        if (priceInput != null && !string.IsNullOrEmpty(priceInput.text))
        {
            int.TryParse(priceInput.text, out price);
        }

        // 检查银行是否有货
        if (!GameManager.Instance.BankHasGoods(selectedType))
        {
            Panel.Instance?.AddLog("银行没有这种货物了");
            return;
        }

        // 发起购买请求（-1 表示向银行购买）
        GameManager.Instance.RequestBuy(-1, selectedType, price);

        // 刷新显示
        UpdateBankInfo();

        // 如果购买后不能再购买，关闭面板
        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasBoughtThisTurn)
        {
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}