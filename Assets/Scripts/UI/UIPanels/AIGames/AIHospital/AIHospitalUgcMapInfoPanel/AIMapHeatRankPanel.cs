using BUD.AnimPose;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using Message;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;

public class AIMapHeatRankPanel : MonoBehaviour
{
    [SerializeField] private AIMapRankAdapter _adapter;
    [SerializeField] private AIMapHeatRankLoader _dataLoader;
    [SerializeField] private PullToRefreshBehaviour _controller;

    [SerializeField] private GameObject _emptyTips;
    //todo加载avatar
    [SerializeField] private GameObject _avatarParentObj;
    private CharacterWrap _characterWrap;
    //private PlayerAnimationCtrl _animationCtrl;
    private PlayerIdleBehaviour _idleBehaviour;

    private string _lastAvatar;
    private void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        _controller.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        _adapter.OnItemsUpdated.AddListener(_controller.HideGizmo);
        _adapter.Data = new SimpleDataHelper<RankItem>(_adapter);
        _adapter.Init();
        AddListener();
    }

    private void AddListener()
    {
        MessageHelper.AddListener<string>(MessageName.OnS9HeatRankItemClick,InitCharacter);
    }

    private void RemoveListener()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnS9HeatRankItemClick, InitCharacter);
    }


    #region adapter
    public void GetData(string mapID)
    {
        HideGizmo();
        _dataLoader.SetMapID(mapID);
        _dataLoader.GetLeaderboardData(OnGetFirstPageDatas);
    }

    public void ResetAdpater()
    {
        if (!_adapter.IsInitialized)
            return;

        _adapter.ResetItems(0);
        _adapter.ClearPool();
    }

    private void HideGizmo()
    {
        //需要动态拉取数据必须要做的初始化操作
        _controller.HideGizmo();
    }

    public virtual void OnGetFirstPageDatas(List<RankItem> leaderboardItemDatas)
    {
        ResetAdpater();
        IsRankEmpty(leaderboardItemDatas == null || leaderboardItemDatas.Count == 0);
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            _adapter.OnItemsUpdated?.Invoke();
            return;
        }
        _adapter.Data.ResetItems(leaderboardItemDatas);
        _adapter.OnItemsUpdated?.Invoke();
    }

    private void OnPullReleased()
    {
        _dataLoader.GetLeaderboardData(OnReceivedNewModelsForInsert);
        //if (topListData != null)
        //{
        //    timeText.text = topListData.displayTime;
        //    LoggerUtils.LogError(topListData.displayTime);
        //}
        //else
        //{
        //    LoggerUtils.LogError("未找到topListData");
        //}
    }

    private void OnReceivedNewModelsForInsert(List<RankItem> leaderboardItemDatas)
    {
        if (leaderboardItemDatas == null || leaderboardItemDatas.Count == 0)
        {
            _adapter.OnItemsUpdated?.Invoke();
            return;
        }
        _adapter.Data.List.AddRange(leaderboardItemDatas);
        _adapter.Refresh(false);
    }

    private void IsRankEmpty(bool value)
    {
        _emptyTips.gameObject.SetActive(value);
    }
    #endregion

    public void InitCharacter(string avatar)
    {
        // 如果是相同的avatar数据，直接返回不重复加载
        if (avatar == _lastAvatar && _characterWrap != null)
        {
            LoggerUtils.Log($"相同的avatar数据，不重复加载: {avatar}");
            return;
        }

        // 销毁之前的角色
        DestroyCharacter();
        
        // 记录当前avatar数据
        _lastAvatar = avatar;
        
        // 创建新角色
        PlayerAnimationCtrl avatarAnimCtrl = null;
        CharacterData avatarInfo = CharacterData.DeserializeObject(avatar);
        if (avatarInfo != null)
        {
            AvatarDataManager.Inst.SelfCharacterData = avatarInfo;
            // 同一帧调用两次会有问题 待解决
            var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo, _avatarParentObj.transform);
            _characterWrap = wrap;
            
            avatarAnimCtrl = wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarAnimCtrl.CheckAndOverrideSpecialAnim();
            
            _idleBehaviour = wrap.Avatar.AddComponent<PlayerIdleBehaviour>();
            _idleBehaviour.Init(avatarAnimCtrl);

            var ugcBehaivour = wrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = wrap.Avatar.GetComponent<AnimIKController>();
            ugcBehaivour.Init(ikController);
            
            LoggerUtils.Log($"成功加载新avatar: {avatar}");
        }
        else
        {
            LoggerUtils.LogError($"加载avatar失败，无效的数据: {avatar}");
            _lastAvatar = null;
        }
    }

    public void DestroyCharacter()
    {
        if (_characterWrap != null)
        {
            // 清除引用的组件
            if (_idleBehaviour != null)
            {
                Destroy(_idleBehaviour);
                _idleBehaviour = null;
            }
            
            // 销毁整个角色包装器
            Destroy(_characterWrap.Avatar.gameObject);
            _characterWrap = null;
            
            LoggerUtils.Log($"已销毁之前的avatar: {_lastAvatar}");
        }
    }

    protected void OnDestroy()
    {
        DestroyCharacter();
        RemoveListener();
    }

}
