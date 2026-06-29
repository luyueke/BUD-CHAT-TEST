using System;
using Basic.Utils;
using UI.BaseWidgets;
using UnityEngine;

public class LikeButton : CommonUIWidget
{
    public CButton Btn_Like;
    public GameObject Go_Liked;
    public CText Txt_LikeNum;

    public Action<int> likeNumChange;
    private string _ugcId;
    private bool _isLike;
    private int _likeAmount;
    private InteractInfoView _interactInfoView;

    private void Awake()
    {
        Btn_Like.onClick.AddListener(OnLikeBtnClick);
    }

    /// <summary>
    /// 点赞需要的参数：
    /// args[0]:UgcId
    /// args[1]:当前点赞情况 0 未点赞 1 已点赞
    /// args[2]:当前点赞数量
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);

        _ugcId = (string)args[0];
        _isLike = (int)args[1] == 1;
        _likeAmount = (int)args[2];

        RefreshLikeState();
        RefreshLikeAmount();
    }

    public void BindInteractInfoView(InteractInfoView view)
    {
        _interactInfoView = view;
    }

    private void RefreshLikeState()
    {
        Go_Liked.SetActive(_isLike);
    }

    private void RefreshLikeAmount()
    {
        var likeNumStr = GameUtils.ToBudCommonNumString(_likeAmount);
        Txt_LikeNum.text = likeNumStr;

        if(_interactInfoView != null)
            _interactInfoView.RefreshLikeNum(_likeAmount);
    }

    private void OnLikeBtnClick()
    {
        if (string.IsNullOrEmpty(_ugcId))
        {
            return;
        }
        var reqLikeType = _isLike ? UGCCommonReq.LikeType.UnLike : UGCCommonReq.LikeType.Like;
        UGCCommonReq.Inst.UGCLikeReq(_ugcId, reqLikeType, (reqComplete) =>
        {
            if (_isLike)
            {
                _likeAmount--;
                if (_likeAmount < 0)
                {
                    _likeAmount = 0;
                }
            }
            else
            {
                _likeAmount++;
            }
            likeNumChange?.Invoke(_likeAmount);
            _isLike = !_isLike;
            RefreshLikeState();

            RefreshLikeAmount();
        });
    }
}
