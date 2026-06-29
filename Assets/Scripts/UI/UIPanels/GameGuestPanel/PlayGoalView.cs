using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayGoalView : MonoBehaviour {
    
    [SerializeField] private Text titleText;
    [SerializeField] private Text tipsText;
    
    public void SetTip(string tip) {
        tipsText.SetLocalText(tip);
    }

    public void SetTitle(string title)
    {
        titleText.SetText(title);
    }

    public void InitGoalView()
    {
        gameObject.SetActive(false);
        var winCondition = WinConditionManager.Inst.GetCurrentWinCondition();
        if (winCondition is CollectStarCondition)
        {
            SetTip("收集所有星星");
            gameObject.SetActive(true);
        }
        else if (winCondition is ReachEndFlagCondition)
        {
            SetTip("到达终点旗子");
            gameObject.SetActive(true);
        }
    }
 
}
