using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BankPanel : MonoBehaviour
{
    public static BankPanel Instance;

    [Header("UI组件")]
    public TMP_Text bankTitleText;
    public TMP_Text sugarStockText;
    public TMP_Text oilStockText;
    public TMP_Text flourStockText;
    public TMP_Text priceText;
    public TMP_InputField priceInput;        // 改为 TMP_InputField
    public TMP_Dropdown goodsDropdown;       // 改为 TMP_Dropdown
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

        if (!GameManager.Instance.BankHasGoods(selectedType))
        {
            Panel.Instance?.AddLog("银行没有这种货物了");
            return;
        }

        GameManager.Instance.RequestBuy(-1, selectedType, price);
        UpdateBankInfo();

        PlayerData currentPlayer = GameManager.Instance.players[GameManager.Instance.currentTurnIndex];
        if (currentPlayer.hasBoughtThisTurn)
        {
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}