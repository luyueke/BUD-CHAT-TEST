using System.Collections;
using System.Collections.Generic;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhantomSoundPartyBigRewardShowPanel : BasePanel<PhantomSoundPartyBigRewardShowPanel>
{
    private List<int> rewardIdList = new List<int>() {160100009, 160100010, 160100011};
    public List<Toggle> togs;
    public override void OnCreate()
    {
        base.OnCreate();
        for (int i = 0; i < togs.Count; i++)
        {
            int index = i;
            togs[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    MessageHelper.Broadcast(MessageName.OnPhantomSoundPartyBigRewardToggleChanged, rewardIdList[index]);
                }
            });
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    public void SelectFirst()
    {
        if (togs != null && togs.Count > 0)
            togs[0].onValueChanged.Invoke(true);
    }
}
