using System;
using UnityEngine;

/// <summary>
/// 接接乐游戏物品类型
/// </summary>
public enum ConnectingGameItemType
{
    /// <summary>汤圆 +10</summary>
    Dumpling,
    /// <summary>彩灯 +50</summary>
    Lantern,
    /// <summary>炸弹 -20，中断连击</summary>
    Bomb
}

public class ConnectingGameMgr : GlobalInstance<ConnectingGameMgr>
{
    /// <summary>游戏时长（秒）</summary>
    public const float GameDuration = 30f;

    /// <summary>汤圆基础分</summary>
    public const int ScoreDumpling = 10;
    /// <summary>彩灯基础分</summary>
    public const int ScoreLantern = 50;
    /// <summary>炸弹扣分</summary>
    public const int ScoreBomb = -20;

    /// <summary>连击5次：2倍得分</summary>
    public const int ComboThreshold2x = 5;
    /// <summary>连击10次：3倍得分</summary>
    public const int ComboThreshold3x = 10;
    /// <summary>连击20次以上：固定20分/个</summary>
    public const int ComboThresholdFixed = 20;
    public const int ComboFixedScore = 20;

    /// <summary>根据类型获取基础分（炸弹返回负数）</summary>
    public static int GetBaseScore(ConnectingGameItemType type)
    {
        switch (type)
        {
            case ConnectingGameItemType.Dumpling: return ScoreDumpling;
            case ConnectingGameItemType.Lantern: return ScoreLantern;
            case ConnectingGameItemType.Bomb: return ScoreBomb;
            default: return 0;
        }
    }

    /// <summary>根据连击数计算得分倍率或固定分。返回 (最终得分, 是否炸弹)</summary>
    public static (int score, bool isBomb) GetScoreWithCombo(ConnectingGameItemType type, int combo)
    {
        if (type == ConnectingGameItemType.Bomb)
            return (ScoreBomb, true);

        int baseScore = GetBaseScore(type);
        int finalScore;
        if (combo >= ComboThresholdFixed)
            finalScore = ComboFixedScore;
        else if (combo >= ComboThreshold3x)
            finalScore = baseScore * 3;
        else if (combo >= ComboThreshold2x)
            finalScore = baseScore * 2;
        else
            finalScore = baseScore;
        return (finalScore, false);
    }
}
