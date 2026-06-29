using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Message;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class HeadViewWidget : MonoBehaviour
{
    public RemoteImageBehaviour Remote_HeadImg;
    public Image Img_HeadCycle;
    public Transform Trans_BottomEffect;
    public Transform Trans_TopEffect;

    private bool _isSelf;

    private void Awake()
    {
        MessageHelper.AddListener(MessageName.OnChangeHeadCycleSuccess, RefreshSelfHeadCycle);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnChangeHeadCycleSuccess, RefreshSelfHeadCycle);
    }

    private void RefreshSelfHeadCycle()
    {
        if(!_isSelf)
            return;

        InitHeadCycle(AccountDataManager.Inst.UserInfo);
    }

    public void InitHeadCycle(int cycleId)
    {
        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(cycleId, this.gameObject);

        Img_HeadCycle.sprite = headCycleData.Sp_HeadCycle;

        Trans_BottomEffect.ClearChildren();
        Trans_TopEffect.ClearChildren();
        
        if (headCycleData.Effect_Bottom != null)
        {
            headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
        } 
        
        if (headCycleData.Effect_Top != null)
        {
            headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
        } 
    }

    public void InitHeadCycle(AccountUserInfo userInfo)
    {
        _isSelf = AccountDataManager.Inst.IsSelf(userInfo.uid);
        
        Remote_HeadImg?.Load(userInfo.portraitUrl);

        Trans_BottomEffect.ClearChildren();
        Trans_TopEffect.ClearChildren();

        var cycleId = userInfo.avatarFrame;
        if (cycleId < 0) //默认头像为0
        {
            Img_HeadCycle.gameObject.SetActive(false);
            return;
        }

        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(cycleId, this.gameObject);

        Img_HeadCycle.sprite = headCycleData.Sp_HeadCycle;
        Img_HeadCycle.gameObject.SetActive(true);
        
        if (headCycleData.Effect_Bottom != null)
        {
            headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
        } 
        
        if (headCycleData.Effect_Top != null)
        {
            headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
        } 
    }
    
    public void InitHeadCycle(string uid, string portraitUrl = "", int avatarFrame = 0)
    {
        _isSelf = AccountDataManager.Inst.IsSelf(uid);
        
        Remote_HeadImg?.Load(portraitUrl);

        Trans_BottomEffect.ClearChildren();
        Trans_TopEffect.ClearChildren();

        var cycleId = avatarFrame;
        if (cycleId == 0)
        {
            Img_HeadCycle.gameObject.SetActive(false);
            return;
        }
    
        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(cycleId, this.gameObject);

        Img_HeadCycle.gameObject.SetActive(true);
        Img_HeadCycle.sprite = headCycleData.Sp_HeadCycle;

        
        if (headCycleData.Effect_Bottom != null)
        {
            headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
        } 
        
        if (headCycleData.Effect_Top != null)
        {
            headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
        } 
    }
}
