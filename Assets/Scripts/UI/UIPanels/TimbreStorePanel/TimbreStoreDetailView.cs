
using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.MusicalInstrument;
using Game.Store;
using GameData.BaseInfo;
using Message;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TimbreStoreDetailView : MonoBehaviour
{
    [SerializeField] private UserInfoView UserInfoView;
    [SerializeField] private CButton Btn_UserHead;
    [SerializeField] private PurchaseButton PurchaseButton;
    [SerializeField] private CText Txt_ItemName;
    [SerializeField] private CButton Btn_TryPlay;
    [SerializeField] private GameObject timbreTypeGameObject;
    [SerializeField] private Text Txt_TimbreType;
    [SerializeField] private RemoteImageBehaviour CoverImage;

    [SerializeField] private Sprite MusicPlaySprite;
    [SerializeField] private Sprite MusicPuaseSprite;
    [SerializeField] private AccountWidget GemWidget;
    [SerializeField] private AccountWidget PinkWidget;
    
    private bool IsPlaying = false;
    
    private RecommendItemData curData;
    public Action<RecommendItemData> DidPayAssetAction;

    public void Awake()
    {
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);

        if (GemWidget != null)
        {
            GemWidget.DidClickAction = () =>
            {
                StopPlayIfNeed();
            };  
        }
        if (PinkWidget != null)
        {
            PinkWidget.DidClickAction = () =>
            {
                StopPlayIfNeed();
            };  
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
    }
    
    public void RefreshUIByData(RecommendItemData data)
    {
        this.curData = data;
        RefreshUIInfo(data);
    }

    private void RefreshUIInfo(RecommendItemData data)
    {
        var toneData = data.UgcInfo as ToneInfo;
        if (toneData == null)
        {
            return;
        }
        
        var ugcId = toneData?.id;
        var consumed = data?.interactInfo?.consumed ?? 0;
        var propName = data?.UgcInfo?.name;
        var paymentInfo = toneData?.paymentInfo;
        AccountUserInfo accountUserInfo = data?.creatorInfo;
        UserInfoView.IsOpenProfilePanel = false;
        UserInfoView.SetData(accountUserInfo);
        Btn_UserHead.onClick.RemoveAllListeners();
        Btn_UserHead.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            StopPlayIfNeed();
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicTone, ugcId);
        });
        // 已拥有：服务端 consumed ∥ 本地背包 IsOwned ∥ 自己创作（与列表项一致），任一成立购买按钮即显示「已拥有」
        bool isOwned = consumed == 1 ||
                       (!string.IsNullOrEmpty(ugcId) && AssetsDataManager.IsOwned(ugcId)) ||
                       data?.creatorInfo?.uid == AccountDataManager.Inst?.Uid;
        PurchaseButton.SetData(toneData, isOwned ? 1 : 0, paymentInfo);
        Txt_ItemName.text = propName;

        UserInfoView.gameObject.SetActive(true);
        Txt_ItemName.gameObject.SetActive(true);

        var coverPath = toneData.cover;
        if (!string.IsNullOrEmpty(coverPath))
        {
            CoverImage.Load(coverPath);
        }
        Btn_TryPlay.gameObject.SetActive(true);
        timbreTypeGameObject.SetActive(true);
        Txt_TimbreType.text = toneData.toneType == (int)ToneType.Fifteen ? "15音" : "22音";
        
        Btn_TryPlay?.onClick.RemoveAllListeners();
        Btn_TryPlay?.onClick.AddListener(OnClickTryPlay);
        IsPlaying = false;
        AdjustTryPlayButton(true);
    }

    private void OnBuyUgcItemSuccess(string ugcId)
    {
        if (curData == null)
        {
            return;
        }

        if (ugcId != curData.UgcInfo.id)
        {
            return;
        }
        
        if (curData.interactInfo != null)
        {
            curData.interactInfo.consumed = 1;
            RefreshUIInfo(curData);
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        DidPayAssetAction?.Invoke(curData);
    }

    private void OnClickTryPlay()
    {
        AdjustTryPlayButton(!IsPlaying);
    }

    public void StopPlayIfNeed()
    {
        AdjustTryPlayButton(false);
    }

    /// <summary>
    /// 更新播放状态
    /// </summary>
    /// <param name="isPlay">是否要播放</param>
    private void AdjustTryPlayButton(bool isPlay)
    {
        if (curData == null)
        {
            return;
        }
        
        if (Btn_TryPlay == null)
        {
            return;
        }
        
        var toneData = curData.UgcInfo as ToneInfo;
        if (toneData == null)
        {
            return;
        }

        var imgView = GameObjectEx.FindChildByName(Btn_TryPlay.transform, "Icon").GetComponent<Image>();
        if (imgView == null)
        {
            return;
        }
        IsPlaying = isPlay;
        imgView.sprite = !isPlay ? MusicPlaySprite : MusicPuaseSprite;
        
        MusicalInstrumentManager.Inst.StopPreviewUgcTone(this.gameObject);
        
        if (isPlay)
        {
            var previewList = new List<SyllableType>();
            previewList.AddRange(MusicalInstrumentUtils.Low_Config);
            previewList.AddRange(MusicalInstrumentUtils.Middle_Config);
            previewList.AddRange(MusicalInstrumentUtils.High_Config);
            MusicalInstrumentManager.Inst.PreviewUgcTone(toneData, this.gameObject, previewList); 
        }
    }
}