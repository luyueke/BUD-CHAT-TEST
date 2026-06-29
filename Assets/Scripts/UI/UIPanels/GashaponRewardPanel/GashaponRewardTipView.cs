using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GashaponRewardTipView : MonoBehaviour
{
    [SerializeField] private Text rewardText;
    [SerializeField] private Text textCn1;
    [SerializeField] private Text textCn2;
    [SerializeField] private Text textUs;
    public void SetData(int num)
    {
        bool isCn = LocalizationManager.Inst.LangCode == LangCode.zh_Hans;
        rewardText.gameObject.SetActive(isCn);
        textCn1.gameObject.SetActive(isCn);
        textCn2.gameObject.SetActive(isCn);
        textUs.gameObject.SetActive(!isCn);
        rewardText.text = num.ToString();
        if (!isCn)
        {
            textUs.SetLocalText("你 <color=#FFA95A>{0}</color> 抽就抽到了金奖哦！",num);
        }
        else
        {
            textUs.text = "";
        }
    }

}
