using System;
using System.Collections;
using System.Collections.Generic;
using GameData.Rewards;
using UI.Base;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class GashaponTwistAnimParam
{
    public string bgPath;
    public string gashaponId;
}


public class GashaponTwistAnimPanel : BasePanel<GashaponTwistAnimPanel> {
    [SerializeField] private GashaponTwistAnimView twistAnimView;

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args != null && args.Length > 0) {
            var animParam = args[0] as GashaponTwistAnimParam;
            twistAnimView.InitTwistAnimStyle(animParam.gashaponId);
            SetBg(animParam.bgPath);
        }
    }


    public void PlayOneTwistAnimation(List<RewardInfo> rewardInfoList, Action cb) {
        twistAnimView.PlayOneTwistAnimation(rewardInfoList, cb);
    }


    public void PlayTenTwistAnimation(List<RewardInfo> rewardInfoList, Action cb) {
        twistAnimView.PlayTenTwistAnimation(rewardInfoList, cb);
    }

    public void OnAnimationRunning(bool isShow) {
        twistAnimView.OnAnimationRunning(isShow);
    }

    public void AddBackListener(Action callback) {
        twistAnimView.AddBackListener(callback);
    }

    public void SetBg(string bgPath) {
        twistAnimView.SetBg(bgPath);
    }
}
