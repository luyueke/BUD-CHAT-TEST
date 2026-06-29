using System;
using GameData.Gashapon;
using UI.BaseWidgets;
using UnityEngine.UI;


/// <summary>
/// 扭蛋机规则弹窗
/// @stanley
/// </summary>
public class GachaMachineRulePanel : BaseCommonComfirmPanel<GachaMachineRulePanel>
{
    public Text guaranteeTitle; //保底次数
    public Text blueRateText;
    public Text purpleRateText;
    public Text goldRateText;

    protected override void Start()
    {
        InitUI();
    }

    public override void OnShow(params object[] args)
    {
        if (args == null || args[0] is not GashaRule rule)
        {
            return;
        }

        guaranteeTitle.text = $"{rule.purpleGuaranteed}次必中<color=#967BFF>紫色奖品</color>，{rule.goldGuaranteed}次必中<color=#FFB067>金色奖品</color>。";
        blueRateText.text = "蓝色奖品概率：" + rule.blueProbability + "%";
        purpleRateText.text = "紫色奖品概率：" +rule.purpleProbability + "%";
        goldRateText.text = "金色奖品概率：" +rule.goldProbability + "%";
    }

    private void InitUI()
    {
        
    }
    
}