using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [Header("UI组件")]
    public TMP_Text txtPlayerName;
    public TMP_Text txtPlayerGold;
    public TMP_Text txtPlayerIdentity;
    public Transform handCardRoot;      // 手牌容器
    public GameObject prefabCard;

    [Header("操作按钮（由Panel控制）")]
    public Button sellButton;
    public Button placeButton;
    public Button buyButton;
    public Button nextButton;

    private int playerIndex;
    private PlayerData playerData;

    void Start()
    {
        // 按钮事件由外部Panel处理，这里只做显示
    }

    public void Initialize(int idx, PlayerData data)
    {
        playerIndex = idx;
        playerData = data;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (txtPlayerName == null || txtPlayerGold == null || txtPlayerIdentity == null)
        {
            Debug.LogWarning("PlayerPanel 的 UI 组件未赋值，请检查 Inspector 拖拽！");
            return;
        }

        if (txtPlayerName != null) txtPlayerName.text = $"玩家{playerIndex + 1}";
        if (txtPlayerGold != null) txtPlayerGold.text = $"金币: {playerData.gold}";
        if (txtPlayerIdentity != null) txtPlayerIdentity.text = playerData.isRealMerchant ? "真商" : "假商";

        UpdateHandCards();
    }

    void UpdateHandCards()
    {
        if (handCardRoot == null || prefabCard == null) return;
        foreach (Transform child in handCardRoot) Destroy(child.gameObject);
        foreach (var card in playerData.handCards)
        {
            GameObject cardObj = Instantiate(prefabCard, handCardRoot);
            CardUI ui = cardObj.GetComponent<CardUI>();
            ui.SetCardData(card);
            ui.OnCardClick = (c) => { }; // 点击由Panel处理，这里不处理
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}