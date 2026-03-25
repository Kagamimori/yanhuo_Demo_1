using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    GameStart,       // 游戏开始，初始化
    TurnStart,       // 回合开始，发金币
    Phase1_Sell,     // 阶段1：可选择向银行出售真牌
    Phase2_Action,   // 阶段2：可选择明牌/替换 或 购买
    TurnEnd,         // 回合结束，检查胜利条件
    GameEnd
}