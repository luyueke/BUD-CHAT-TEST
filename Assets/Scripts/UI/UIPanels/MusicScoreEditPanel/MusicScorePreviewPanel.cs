using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Pb.Base;
using Game.Database;
using Game.MusicalInstrument;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MusicScorePreviewPanel : BasePanel<MusicScorePreviewPanel>
{
    private AvatarPreview avatarPreview;
    private PlayerHoldBehaviour playerHoldBehaviour;
    private CButton backButton;
    
  
    public class BagTuple
    {
        public MISource.Source Item1;
        public string Item2;
        public bool Item3;
    }

    [SerializeField] internal Transform BG;
    [SerializeField] internal MISource miSourceUI;
    [SerializeField] internal FittingRoomAdapter assetsList;
    [SerializeField] internal Text tipText;
    [SerializeField] internal Button defaultButton;
    [SerializeField] internal GameObject defaultSelect;
    private const string defaultInstrumentPGCId = "12400014";
    
    private AvatarBagSceneHandler dataHandler;
    private GoodsDataClassifyList assetsDatas = new();
    private BagTuple bagTuple;
    private PlayMusicScoreBev _playMusicScoreBev;
    private MusicScoreInfo _musicScoreInfo;
    public override void OnCreate()
    {
        InitBG();
        backButton = GameObjectEx.FindComponentByName<CButton>(transform, "BackButton");
        avatarPreview = GameObjectEx.FindChildByName(this.transform, "AvatarPreview").GetComponent<AvatarPreview>();
        avatarPreview.characterRoot = GameObjectEx.FindChildByName(transform, "CharacterRoot");
        avatarPreview.StartPreview();
        _playMusicScoreBev = transform.GetComponent<PlayMusicScoreBev>();
        playerHoldBehaviour = avatarPreview.characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
        backButton.onClick.AddListener(OnCloseBtnClick);
        defaultButton.onClick.AddListener(()=>
        {
            OnDefaultSelected(defaultInstrumentPGCId);
        });
      
     
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _musicScoreInfo = args[0] as MusicScoreInfo;
        GoodsData selectData = null;
        if (args.Length>=2)
        {
            selectData = args[1] as GoodsData;
        }
       
        bagTuple = new BagTuple();
        bagTuple.Item1 = MISource.Source.Buy;
        if (selectData!=null)
        {
            bagTuple.Item1 =  GetMISourceType(selectData.GetFirstAsset<AssetsData>());
            bagTuple.Item2 = selectData.Id;
            StartPreview(selectData);
        }
        else
        {
            OnDefaultSelected(defaultInstrumentPGCId);
        }
        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);

        var list = dataHandler.GetGoodsData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument));
        var list1 = dataHandler.GetGoodsData(UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument));
        if ((list == null || list.Count <= 2) && (list1 == null || list1.Count <= 2))
        {
            // 没有任何乐器
            DefualtOn(defaultInstrumentPGCId);
            ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
            miSourceUI.gameObject.SetActive(false);
            assetsList.gameObject.SetActive(false);
           
            return;
        }
        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        miSourceUI.SetCallback(OnUgcSource);
        miSourceUI.DefualtOn(bagTuple.Item1);
      
       
    }
    
    private void InitBG()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "music_icon_1", "music_icon_2", "music_icon_3"
        });
        item.gameObject.SetActive(true);
    }
    
    private void OnCloseBtnClick()
    {
        CloseSelf();
       
    }
    internal void DefualtOn(string id)
    {
        bagTuple.Item2 = id;
        if (id == defaultInstrumentPGCId) defaultSelect.SetActive(true);
    }

    internal void OnDataChange(AssetsData[] changes)
    {
        OnRedDotUpdate();

        switch (bagTuple.Item1)
        {
            case MISource.Source.Bud:
                assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
                break;
            case MISource.Source.Buy:
            case MISource.Source.Create:
                var datas = dataHandler.GetGoodsData(GetSelectedClassType());
                switch (bagTuple.Item1)
                {
                    case MISource.Source.Buy:
                        assetsDatas.SetData(datas, BuyPredicate);
                        break;
                    case MISource.Source.Create:
                        assetsDatas.SetData(datas, CreatePredicate);
                        break;
                }
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void OnRedDotUpdate()
    {
        dataHandler.RedDotPgc.TryGetValue(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), out var count);
        miSourceUI.SetRedDot(MISource.Source.Bud, count > 0);
        var ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
        miSourceUI.SetRedDot(MISource.Source.Create, count1 > 0);
        miSourceUI.SetRedDot(MISource.Source.Buy, count2 > 0);
    }

    internal bool BudPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        return true;
    }

    internal bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    internal bool BuyPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }

    internal int GetSelectedClassType()
    {
        return UniqueType.Get(bagTuple.Item1 == MISource.Source.Bud ? ResourceType.Avatar : ResourceType.UgcAvatar, (int)AvatarSubType.MusicalInstrument);
    }
    internal MISource.Source GetMISourceType(AssetsData assetsData)
    {
        if (assetsData.ResourceType == ResourceType.Avatar)
        {
            return MISource.Source.Bud;
        }

        if (assetsData.InventoryData.Tag == BackpackTag.Creator)
        {
            return MISource.Source.Create;
        }
        return MISource.Source.Buy;
    }
    internal void OnUgcSource(MISource.Source source)
    {
        bagTuple.Item1 = source;

        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        var datas = dataHandler.GetGoodsData(GetSelectedClassType());
        ShowTip("");
        switch (source)
        {
            case MISource.Source.Bud:
                assetsDatas.SetData(datas, BudPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Buy:
                assetsDatas.SetData(datas, BuyPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Create:
                assetsDatas.SetData(datas, CreatePredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void ShowTip(string str)
    {
        tipText.gameObject.SetActive(true);
        tipText.SetLocalText(str);
    }
    internal void StartPreview(GoodsData data)
    {
        if (data.GoodsType == GoodsType.SinglePgc)
        {
            avatarPreview.PartPreview((int)AvatarSubType.MusicalInstrument, data.Id);
            playerHoldBehaviour.PreviewPGCInstrument(data.Id);
            MusicalInstrumentManager.Inst.CheckPGCInstrumentCanPlayMusicScore(data.Id,_musicScoreInfo, () =>
            {
                TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
            });
            PlayMusicScore();
        }
        else
        {
            GetMIDetailInfo(data.Id, (rspData) =>
            {
                avatarPreview.characterWrap.ChangeUGCPart(rspData.skinInfo);
                playerHoldBehaviour.PreviewUGCInstrument(rspData.skinActionInfo.instrumentInfo);
                MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(rspData.skinActionInfo.instrumentInfo.toneInfo,_musicScoreInfo, () =>
                {
                    TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
                });
                MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(rspData.skinActionInfo.instrumentInfo.toneInfo);
                PlayMusicScore();
            });
        }
        
    }
    internal void OnItemSelected(GoodsData data)
    {
        MessageHelper.Broadcast(MessageName.OnMusicScorePreviewSelect,data);
        bagTuple.Item2 = data.Id;
        bagTuple.Item3 = data.GoodsType == GoodsType.SinglePgc ? true : false;
        defaultSelect.SetActive(false);
        assetsList.Data.ResetItems(assetsDatas.Count());

        StartPreview(data);

    }

   
    internal void OnDefaultSelected(string id)
    {
        MessageHelper.Broadcast(MessageName.OnMusicScorePreviewSelect,new GoodsData());
        bagTuple.Item2 = id;
        bagTuple.Item3 = true;
        defaultSelect.SetActive(id == defaultInstrumentPGCId);
        assetsList.Data?.ResetItems(assetsDatas.Count());

        avatarPreview.PartPreview((int)AvatarSubType.MusicalInstrument, id);
        playerHoldBehaviour.PreviewPGCInstrument(id);
        PlayMusicScore();
    }
    private void PlayMusicScore()
    {
        _playMusicScoreBev.StartPLay(_musicScoreInfo,OnPlaySingleSyllable);
    }
    public GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = bagTuple.Item2 == assetsData.Id;
        return assetsData;
    }

    public void OnPlaySingleSyllable(List<SyllablePlayData> playData)
    {
        playerHoldBehaviour.PlayMusicSyllable(playData);
    }
  
    private void GetMIDetailInfo(string id, Action<DetailRsp> suc)
    {
        JObject req = new JObject()
        {
            ["idList"] = id,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this == null) return;
                BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
                if (rspData.skinList == null || rspData.skinList.Count == 0) return;
                suc?.Invoke(rspData.skinList[0]);
            },
            (msg) =>
            {

            });
    }

  
}
