using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeeklyTaskItem : DailyTaskItem {


    public void SetUnlockUpgrade(bool isUnlock) {
        GameObjectEx.FindChildByName(rewardObjList[1], "UpgradeObj").gameObject.SetActive( !isUnlock );

    }

}
