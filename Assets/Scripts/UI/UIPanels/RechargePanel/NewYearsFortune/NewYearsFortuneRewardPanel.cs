using Game.Audio;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NewYearsFortuneRewardPanel : BasePanel
{
    [SerializeField] private Image rewardBg;
    [SerializeField] private Text title;
    [SerializeField] private GameObject bigRewardRoot;
    [SerializeField] private GameObject normalRewardRoot;
    [SerializeField] private Image rewardImage;
    [SerializeField] private Text rewardDesc;
    [SerializeField] private Text peopleNameText;
    [SerializeField] private Button closeBtn;

    public override void OnCreate()
    {
        closeBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnHidden()
    {

    }

    public override void OnShow(params object[] args)
    {
        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_GetRewards_A3");
    }

    public void SetRewardBg(Sprite sp)
    {
        rewardBg.sprite = sp;
    }

    public void SetData(bool bigReward, Sprite rewardIcon, string rewardWord, string peopleName)
    {
        title.text = bigReward ? "恭喜您中奖" : "感谢参与";
        bigRewardRoot.SetActive(bigReward);
        normalRewardRoot.SetActive(!bigReward);
        rewardImage.sprite = rewardIcon;
        rewardDesc.text = rewardWord;
        peopleNameText.text = peopleName;
        Invoke("CanClose", 1f);
    }

    private void CanClose()
    {
        closeBtn.gameObject.SetActive(true);
    }

    public override void OnWindowBeCovered(bool isCover)
    {

    }

    public override void OnWindowBeFocused()
    {

    }

    public override void OnWindowPop()
    {

    }

    public override void OnWindowShow()
    {

    }
}
