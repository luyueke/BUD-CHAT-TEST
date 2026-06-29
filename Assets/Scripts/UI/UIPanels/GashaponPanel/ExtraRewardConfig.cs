using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ExtraRewardConfig
{
    public int Id;
    public CurrencyType currencyType;//奖励类型
    public int RewardNum;//奖励个数
    public int CountNum;//要求次数
    public string atlasPath;//图集路径
    public string iconName;//icon名字
}
