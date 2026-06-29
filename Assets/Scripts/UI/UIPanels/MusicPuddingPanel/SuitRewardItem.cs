using System.Collections;
using System.Collections.Generic;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class SuitRewardItem : MonoBehaviour
{

    [SerializeField] private Transform Lock;

    [SerializeField] private Transform Claim;

    [SerializeField] private Transform BottomImg;
    public void SetData(GashaponExtraTaskInfo dataInfo)
    {
        if (BottomImg != null)
        {
            BottomImg.gameObject.SetActive(false);
        }
        switch (dataInfo.rewardStatus)
        {
            case 1:
                //Lock.gameObject.SetActive(true);
                Claim.gameObject.SetActive(false);
                break;
            case 2:
                //Lock.gameObject.SetActive(false);
                Claim.gameObject.SetActive(false);
                break;
            case 3:
                //Lock.gameObject.SetActive(false);
                Claim.gameObject.SetActive(true);
                if(BottomImg!=null)
                {
                    BottomImg.gameObject.SetActive(true);
                }
                break;
        }
    }
}
