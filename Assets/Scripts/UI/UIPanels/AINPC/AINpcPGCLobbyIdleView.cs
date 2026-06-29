using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UnityEngine;

public class AINpcPGCLobbyIdleView: AINpcBaseLobbyIdleView
{
    [Header("列表")]
    [SerializeField] private FittingRoomAdapter assetsList;
    [Header("已选表情")]
    [SerializeField] private List<ActionItem> actions;
    [SerializeField] private AvatarCameraController avatarCameraController;
    private CharacterWrap chaWrap;
    private AINpcInfo npcInfo;
    private AINpcAnimType curNpcType = AINpcAnimType.Idle;
    private string previewId;
    private PgcNpcIdleBehaviour playerBehaviour;
    private AnimIKController playerIkController;
    public override void OnShow(AINpcInfo info,CharacterWrap wrap)
    {
        npcInfo = info;
        chaWrap = wrap;
        InitAssetList();
        playerBehaviour = chaWrap.Avatar.GetComponent<PgcNpcIdleBehaviour>();
        playerIkController = chaWrap.Avatar.GetComponent<AnimIKController>();
        playerBehaviour.SetData(npcInfo.npcAnimations);
    }

    public override void PlayMainAnim()
    {
        playerBehaviour.PlayMainAnim();
    }

    private void InitAssetList()
    {
        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        assetsList.ResetColor();
        ColorUtility.TryParseHtmlString("#AEA6CC", out assetsList.BgColor);
        assetsList.OnItemSelected = OnItemSelected;
    }

    
    public override void UpdateAssetList(AINpcAnimType npcType)
    {
        curNpcType = npcType;
        ResetAssetList();
    }
    
    public override void ResetAssetList()
    {
        previewId = String.Empty;
        assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
        assetsList.Data.ResetItems(assetsDatas.Count());
        RefreshSelected();
    }
    
    private void RefreshSelected()
    {
        if (npcInfo.npcAnimations != null)
        {
            int npcType = (int) curNpcType;
            var npcAnim = npcInfo.npcAnimations.Find(x => x.npcAnimationType == npcType);
            if (npcAnim != null && npcAnim.pgcIdleList != null)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    actions[i].UpdateViews(npcAnim.pgcIdleList.Count > i ?npcAnim.pgcIdleList[i] : null, OnCancelAction);
                }
                return;
            }
        }
        for (int i = 0; i < actions.Count; i++)
        {
            actions[i].UpdateViews(null, OnCancelAction);
        }
    }
    
    private void OnCancelAction(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        int npcType = (int) curNpcType;
        var npcAnim = npcInfo.npcAnimations.Find(x => x.npcAnimationType == npcType);
        if (npcAnim != null && npcAnim.pgcIdleList != null && npcAnim.pgcIdleList.Contains(id))
        {
            npcAnim.pgcIdleList.Remove(id);
            RefreshSelected();
        }
        if (curNpcType == AINpcAnimType.Idle)
        {
            playerBehaviour.PlayMainAnim();
        }
    }

    private GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = EmoteIsSelected(assetsData.Id);
        return assetsData;
    }

    private void OnItemSelected(GoodsData data)
    {
        if (npcInfo.animResType == (int) AnimResType.UGC)
        {
            resetAnimAction?.Invoke();
            npcInfo.animResType = (int) AnimResType.PGC;
            playerIkController.ChangeAnimResType(AnimResType.PGC);
            playerIkController.RemovePropIks();
        }
        previewId = data.Id;
        OnSelectedAnim(data.Id);
        RefreshSelected();
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    private void OnSelectedAnim(string id)
    {
        if (npcInfo.npcAnimations == null)
        {
            npcInfo.npcAnimations = new List<AINpcIdleAnim>();
        }
        List<string> pgcList = new List<string>();
        int npcType = (int) curNpcType;
        var animData = npcInfo.npcAnimations.Find(x => x.npcAnimationType == npcType);
        if (animData == null)
        {
            animData = new AINpcIdleAnim
            {
                npcAnimationType = npcType,
                pgcIdleList = pgcList
            };
            npcInfo.npcAnimations.Add(animData);
        }
        pgcList = animData.pgcIdleList;

        if (pgcList.Contains(id))
        {
            PreviewEmote(id);
            return;
        }
        
        if (pgcList.Count < 5)
        {
            pgcList.Add(id);
            playerBehaviour.SetData(npcInfo.npcAnimations);
        }
        // 预览动画
        PreviewEmote(id);
    }
    
    private void PreviewEmote(string emoteId)
    {
        previewId = emoteId;
        SetEmoteView(emoteId);
        Action<string> playAnim = curNpcType == AINpcAnimType.Idle
            ? playerBehaviour.PlayMainAnimByUI
            : playerBehaviour.PlaySubAnimByUI;
        playAnim.Invoke(emoteId);
    }
    
    private void SetEmoteView(string id)
    {
        var uiConfig = Es.DataTables.GetEmoUIConfig(id);
        if (uiConfig != null)
        {
            avatarCameraController.SetEmoteView(id);
        }
    }
    
    private bool EmoteIsSelected(string pgcId)
    {
        return previewId == pgcId;
    }
    
    protected override void OnDataChange(AssetsData[] changes)
    {
        assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    //TODO:待修改
    private int GetSelectedClassType()
    {
        var subType = EmoteSubType.SingleAll;
        switch (curNpcType)
        {
            case AINpcAnimType.Idle:
                subType = EmoteSubType.SingleLoop;
                break;
            case AINpcAnimType.Assist:
                subType = EmoteSubType.Single;
                break;
        }
        return UniqueType.Get(ResourceType.Emote, (int) subType);
    }


}