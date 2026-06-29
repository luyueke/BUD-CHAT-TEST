
using System;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class FollowButton : CommonUIWidget
{

    private string _uid;

    private bool isMe = false;

    public LoadingButton _loadingButton;
    public Sprite[] Sprites;
    public Image loadingBtnBg;

    private RelationShipInfo _relationShipInfo;
    private Action<int> _followSuccess;
    private void Start()
    {
        _loadingButton.onClick.AddListener(Follow);
    }

    public void SetRelation(string targetUid, RelationShipInfo relationShipInfo,Action<int> followSuccess = null)
    {
        if (string.IsNullOrEmpty(targetUid) || relationShipInfo == null)
        {
            return;
        }
        this._relationShipInfo = relationShipInfo;
        _followSuccess = followSuccess;
        _uid = targetUid;
        FollowBtnShow();
    }

    private void FollowBtnShow()
    {
        gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(_uid))
        {
            var myUid = AccountDataManager.Inst.Uid;
            if (myUid.Equals(_uid))
            {
                //是自己
                isMe = true;
                _loadingButton.SetLocalText("我");
            }
            else
            {
                isMe = false;
                int relationStatus = _relationShipInfo.relationStatus;
                int relationShip = _relationShipInfo.relationShip;
                if (relationStatus == (int)RelationStatusType.None)
                {
                    _loadingButton.SetLocalText("关注");
                    loadingBtnBg.sprite = Sprites[0];
                }
                else if (relationStatus == (int)RelationStatusType.Posi)
                {
                    _loadingButton.SetLocalText("已关注");
                    loadingBtnBg.sprite = Sprites[2];
                }
                else if (relationStatus == (int)RelationStatusType.Reve)
                {
                    _loadingButton.SetLocalText("回关");
                    loadingBtnBg.sprite = Sprites[1];
                }
                else if (relationStatus == (int)RelationStatusType.Each)
                {
                    _loadingButton.SetLocalText("互关");
                    loadingBtnBg.sprite = Sprites[2];
                }
            }
        }
    }

    private void Follow()
    {
        if (isMe)
        {
            return;
        }


        if (string.IsNullOrEmpty(_uid))
        {
            return;
        }

        if (_relationShipInfo == null)
        {
            return;
        }

        int relationStatus = _relationShipInfo.relationStatus;
        if (relationStatus == (int)RelationStatusType.None || relationStatus == (int)RelationStatusType.Reve)
        {
            SetRealtionParams setRelationReq = new SetRealtionParams();
            setRelationReq.setType = 1;
            setRelationReq.relationship = 1;
            setRelationReq.targetUid = _uid;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setRelation, HttpMethod.POST,
                JsonConvert.SerializeObject(setRelationReq),
                OnFollowSuccess, OnFollowFail);
        }
    }

    private void OnFollowSuccess(string message)
    {
        _loadingButton.HideLoading();
        int relationStatus = _relationShipInfo.relationStatus;
        if (relationStatus == (int)RelationStatusType.None)
        {
            _relationShipInfo.relationStatus = (int)RelationStatusType.Posi;
            _loadingButton.SetLocalText("已关注");
            loadingBtnBg.sprite = Sprites[2];
        }
        else if (relationStatus == (int)RelationStatusType.Reve)
        {
            _relationShipInfo.relationStatus = (int)RelationStatusType.Each;
            _loadingButton.SetLocalText("互关");
            loadingBtnBg.sprite = Sprites[2];
        }
        MessageHelper.Broadcast(MessageName.FollowSuccess);
        _followSuccess?.Invoke(_relationShipInfo.relationStatus);
    }

    private void OnFollowFail(string message)
    {
        _loadingButton.HideLoading();
        LoggerUtils.LogError(message);
    }
}
