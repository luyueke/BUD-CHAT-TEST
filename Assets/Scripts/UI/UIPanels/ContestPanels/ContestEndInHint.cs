using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class ContestEndInHint : MonoBehaviour
{
    [SerializeField] private Text contestNameTxt;
    [SerializeField] private Text endInHintTxt;
    public Color endCountColor = new Color(1f, 0.84f, 0.14f);
    public Color endHintDefaultColor = Color.white;

    private void Awake()
    {
        contestNameTxt.text = string.Empty;
        endInHintTxt.text = string.Empty;
    }

    public void Refresh(ContestInfo contestInfo)
    {
        contestNameTxt.SetText(contestInfo.contestName);

        DataUtil.TryGetFromList(contestInfo.themeColorList, 0, out string themeColor1);
        endInHintTxt.color = string.IsNullOrEmpty(themeColor1) ? endHintDefaultColor : DataUtil.DeSerializeColorCheckHash(themeColor1);

        if (contestInfo.status == (int)ContestStatus.Completed)
        {
            endInHintTxt.SetLocalText("活动已结束");
        }
        else
        {
            string leftTimeRich = $"<color=#{FormatUtils.ColorToString(endCountColor)}>{contestInfo.leftTime}</color>";
            endInHintTxt.SetText($"{LocalizationManager.Inst.GetLocalizedText("距离活动结束:")} {leftTimeRich}");
        }
    }

    private TimeSpan GetEndInTime(long endTime)
    {
        long currentSec = DataUtil.GetUtcTimeStamp();
        long timeLeft = Math.Max(0, endTime - currentSec);
        return TimeSpan.FromSeconds(timeLeft);
    }
}
