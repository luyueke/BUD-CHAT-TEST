using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class OwnedNpcOpButton : MonoBehaviour
{
    public CButton Btn_OP;
    public Text Txt_BtnTitle;

    private AINpcInfo _curNpcInfo;
    private bool _isOwned;

    public void InitData(AINpcInfo npcInfo)
    {
        this._curNpcInfo = npcInfo;

        Btn_OP.onClick.RemoveAllListeners();
        Btn_OP.onClick.AddListener(OnBtnClick);
        SetIsOwned(AIBuddyDataManager.Inst.IsOwnedBuddyByNpcId(npcInfo.id));
    }

    private void SetIsOwned(bool value)
    {
        _isOwned = value;
        if (value)
        {
            Txt_BtnTitle.SetLocalText("前往主页");
        }
        else
        {
            Txt_BtnTitle.SetLocalText("设为伙伴");
        }
    }

    private void OnBtnClick()
    {
        if (_isOwned)
        {
            GoNpcHomePage();
        }
        else
        {
            SetCurNpcAsAIBuddy();
        }
    }

    private void GoNpcHomePage()
    {
        var curSelectBuddyInfo = AIBuddyDataManager.Inst.GetAIBuddyByNpcId(_curNpcInfo.id);
        if (curSelectBuddyInfo != null)
        {
            UIManager.Inst.OpenPanel(PanelId.AIBuddyProfilePanel, curSelectBuddyInfo);
        }
        else
        {
            TipPanel.ShowToast("所选伙伴信息为空");
        }
    }

    private void SetCurNpcAsAIBuddy()
    {
        AccountDataManager.Inst.CreateAIBuddy(this._curNpcInfo, (npcInfo) =>
        {
            if(this == null)
                return;

            if (this._curNpcInfo.id == npcInfo.id)
            {
                //预表现
                SetIsOwned(true);
                TipPanel.ShowToast("设置成功");
            }
            AIBuddyDataManager.Inst.RequestAIBuddyList(OnAIBuddyListUpdate);
        }, (failRsp) =>
        {
            //卡位不足
            if (failRsp != null && failRsp.result == 101)
            {
                CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
                commonConfirmPanel.SetText("伙伴卡位不足","是否前往BUD伙伴购买？", "前往", "取消");
                commonConfirmPanel.SetOnClickAction(() =>
                {
                    UIManager.Inst.SwapPanel(PanelId.AIBuddyListPanel);
                });
            }
        });
    }

    private void OnAIBuddyListUpdate(AIBuddyListRsp listRsp)
    {
        if(this == null)
            return;
        bool isOwned = AIBuddyDataManager.Inst.IsOwnedBuddyByNpcId(this._curNpcInfo.id);
        SetIsOwned(isOwned);
    }
}
