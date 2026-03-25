using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{
    public static Panel Instance;

    public Button btn_bank;
    public Button btn_player1;
    public Button btn_player2;

    [System.Serializable]
    public class PlayerUI
    {
        public Text goldText;
        public Transform handCardContainer;
        public Transform openCardContainer;
        public Button[] actionButtons;
    }

    public PlayerUI[] playerUIs;
    public Button phase1Button;
    public Button phase2Button;
    public Text gameStateText;
    public Text logText;
    public GameObject cardPrefab; // 这个必须在Inspector中赋值

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

        // 绑定玩家1按钮
        if (btn_player1 != null)
        {
            btn_player1.onClick.AddListener(() =>
            {
                if (PlayerPanel.Instance != null)
                    PlayerPanel.Instance.ShowPlayerInfo(0);
            });
        }

        // 绑定玩家2按钮
        if (btn_player2 != null)
        {
            btn_player2.onClick.AddListener(() =>
            {
                if (PlayerPanel.Instance != null)
                    PlayerPanel.Instance.ShowPlayerInfo(1);
            });
        }
    }

    public void UpdatePlayerUI(int playerIndex, PlayerData player)
    {
        if (playerIndex >= playerUIs.Length || playerUIs[playerIndex] == null) return;

        var ui = playerUIs[playerIndex];
        if (ui.goldText != null)
            ui.goldText.text = $"金币: {player.gold}";

        // 更新手牌显示 - 添加空值检查
        if (ui.handCardContainer != null)
        {
            UpdateCardContainer(ui.handCardContainer, player.handCards);
        }
        else
        {
            Debug.LogWarning($"玩家{playerIndex}的手牌容器未赋值");
        }

        // 更新明牌区显示 - 添加空值检查
        if (ui.openCardContainer != null)
        {
            UpdateOpenCardContainer(ui.openCardContainer, player.openCards);
        }
        else
        {
            Debug.LogWarning($"玩家{playerIndex}的明牌区容器未赋值");
        }
    }

    private void UpdateCardContainer(Transform container, List<CardData> cards)
    {
        // 检查容器和预制体
        if (container == null)
        {
            Debug.LogError("手牌容器为空！");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("卡牌预制体未赋值！请在Inspector中拖拽卡牌预制体到 cardPrefab 字段");
            return;
        }

        // 清空容器
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 动态创建卡牌UI
        foreach (var card in cards)
        {
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetCardData(card);
            }
        }
    }

    private void UpdateOpenCardContainer(Transform container, List<CardData> openCards)
    {
        // 检查容器和预制体
        if (container == null)
        {
            Debug.LogError("明牌区容器为空！");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("卡牌预制体未赋值！请在Inspector中拖拽卡牌预制体到 cardPrefab 字段");
            return;
        }

        // 如果容器子物体数量不对，重新创建
        if (container.childCount != openCards.Count)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            // 创建9个槽位
            for (int i = 0; i < openCards.Count; i++)
            {
                GameObject slotObj = Instantiate(cardPrefab, container);
                slotObj.name = $"OpenSlot_{i}";
            }
        }

        // 更新每个槽位
        for (int i = 0; i < openCards.Count && i < container.childCount; i++)
        {
            Transform slot = container.GetChild(i);
            CardData card = openCards[i];
            CardUI cardUI = slot.GetComponent<CardUI>();

            if (cardUI != null)
            {
                if (card != null)
                {
                    cardUI.SetCardData(card);
                }
                else
                {
                    cardUI.SetEmpty();
                }
            }
        }
    }

    public void UpdateGameState(string state)
    {
        if (gameStateText != null)
        {
            gameStateText.text = state;
        }
    }

    public void AddLog(string message)
    {
        Debug.Log(message);
        if (logText != null)
        {
            logText.text = $"{System.DateTime.Now:HH:mm:ss} - {message}\n" + logText.text;

            // 限制日志行数
            string[] lines = logText.text.Split('\n');
            if (lines.Length > 20)
            {
                logText.text = string.Join("\n", lines, 0, 20);
            }
        }
    }
}