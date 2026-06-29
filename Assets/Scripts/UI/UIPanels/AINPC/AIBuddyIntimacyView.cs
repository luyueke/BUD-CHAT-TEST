using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyIntimacyView : MonoBehaviour
{
    [SerializeField] private Text intimacyText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image levelImg;
    
    public void SetValue(int value)
    {
        string atlasPath = AIBuddyDataManager.AIBuddyAtlas;
        intimacyText.SetText(value+"");
        int titleLevel = AIBuddyDataManager.Inst.GetIntimacyTitleLevel(value);
        string iconName = "intimacy_lv_" + titleLevel;

        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
        levelImg.sprite = sprite;

        if (AIBuddyDataManager.Inst.IsMaxTitleLevel(titleLevel))
        {
            progressBar.value = 1;
            return;
        }
        
        var config = AIBuddyDataManager.Inst.GetTitleLevelConfig(titleLevel);
        int showValue = value - config.Min;
        int maxValue = config.Max - config.Min;
        
        float progress = (float)showValue / maxValue;
        progress = Mathf.Min(progress, 1);
        progress = Mathf.Max(progress, 0);
        progressBar.value = progress;
    }
}
