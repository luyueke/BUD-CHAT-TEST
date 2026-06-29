using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using Pb.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using xasset;
public class AIBuddyUgcEmoView : MonoBehaviour
{
    public MISource resMISource;
    [SerializeField] private Text emptyTip;
    [SerializeField] private Button GoStoreBtn;

    public string CurSelectId;
    [SerializeField] private FittingRoomAdapter assetsList;
    private UgcAnimSubType poseSubType = UgcAnimSubType.Single;
    private GoodsDataClassifyList assetsDatas = new();
    private AvatarBagSceneHandler dataHandler;
    private Action closeSelf;
    
    public enum  ResEmoType
    {
        Create,
        Store
    }

    private ResEmoType curResEmoType = ResEmoType.Create;
    
    
    public void OnStart(UgcAnimSubType subType,Action close)
    {
        poseSubType = subType;
        closeSelf = close;
        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(this.gameObject, OnDataChange);
        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        assetsList.OnItemSelected = OnItemSelected;
        resMISource.SetCallback(OnResValueChange);
        GoStoreBtn.onClick.AddListener(GotoAvatarStore);
    }

    // resMISource(SecondMISourceRoot) 与载具页共享同一对象，载具会改写其回调；本视图每次启用时夺回回调。
    private void OnEnable()
    {
        resMISource.SetCallback(OnResValueChange);
    }


    private void GotoAvatarStore()
    {
        FittingRoomPanel panel = null;
        if (poseSubType == UgcAnimSubType.Single || poseSubType == UgcAnimSubType.Double)
        {
            panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcEmote, (int)GameData.PgcData.UgcAnimSubType.PeopleAll));
        }
        else
        {
            panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, true);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcEmote, (int)GameData.PgcData.UgcAnimSubType.PetAll));
        }

     
        closeSelf?.Invoke();
    }


    
    public void SetDefaultMISource()
    {
        curResEmoType = ResEmoType.Store;
        resMISource.DefualtOn(MISource.Source.Create);
    }


    public void OnResValueChange(MISource.Source source)
    {
        curResEmoType = source == MISource.Source.Bud ? ResEmoType.Create : ResEmoType.Store;
        UpdateAssetDatas();
    }

    public void ChangeAnimType(UgcAnimSubType subType)
    {
        poseSubType = subType;
        UpdateAssetDatas();
    }

    private void UpdateAssetDatas()
    {
        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcEmote, (int) poseSubType));
        assetsDatas.SetData(datas, CreatePredicate);
        emptyTip.gameObject.SetActive(assetsDatas.Count() <= 0);
        GoStoreBtn.gameObject.SetActive(false);
        if (assetsDatas.Count() <= 0)
        {
            var text = curResEmoType == ResEmoType.Store? "可前往商城获取":"还没有创作过动作，可前往动作编辑器创作";
            if (curResEmoType == ResEmoType.Store)
            {
                GoStoreBtn.gameObject.SetActive(true);
            }
            emptyTip.SetLocalText(text);
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }


    private bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.UgcEmoteIdle) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UgcAnimAssetsData)) return false;
        var ugcAsset = asset as UgcAnimAssetsData;
        var tag = curResEmoType == ResEmoType.Create
            ? Network.Message.BackpackTag.Creator
            : Network.Message.BackpackTag.ErrBackpackTag;
        if (ugcAsset.UgcInfo != null && ugcAsset.UgcInfo.animInfo != null && ugcAsset.UgcInfo.animInfo.isBan > 0)
        {
            return false;
        }
        if (asset.InventoryData.Tag != tag) return false;
        return ugcAsset.UgcAnimSubType == poseSubType;
    }

    public GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = CurSelectId == assetsData.Id;
        return assetsData;
    }

    private void OnItemSelected(GoodsData data)
    {
        if (AIBuddyAvatarController.Inst.SelfStateController == null)
        {
            TipPanel.ShowToast("未召唤BUD伙伴");
            return;
        }
        
        var asset = data.GetFirstAsset<AssetsData>();
        var poseInfo = asset.UgcInfo.UgcInfo as AnimInfo;
        EnterEmote(poseInfo);
        closeSelf?.Invoke();
    }

     private void EnterEmote(AnimInfo animInfo)
     {
         if (animInfo != null && !string.IsNullOrEmpty(animInfo.metaDataUrl))
         {
             if (!AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.UgcEmote))
             {
                 return;
             }

             if (AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
             {
                 var emoteType = animInfo.animType + animInfo.loop;
                 if (emoteType == (int)EmoteType.DoubleOnce || emoteType == (int)EmoteType.DoubleLoop)
                 {
                     TipPanel.ShowToast("牵手状态下不可以做双人动作哦");
                     return;
                 }
                 
                 if (emoteType == (int)EmoteType.PetWithPlayer || emoteType == (int)EmoteType.PetWithPlayerLoop)
                 {
                     TipPanel.ShowToast("牵手状态下不可以做宠物交互动作哦");
                     return;
                 }
             }

             EmoteNetData netData = new EmoteNetData();
             netData.SenderId = AccountDataManager.Inst.Uid;
             netData.EmoteId = animInfo.id;
             netData.AnimResType = 1;
             netData.IsUgcLoop = animInfo.loop;
             netData.Msg = animInfo.name;
             netData.EmoteType = animInfo.loop == 1 ? EmoteType.BuddyWithPlayerLoop : EmoteType.BuddyWithPlayerOnce; 
             netData.UgcAnimUrl = animInfo.metaDataUrl;
             if (animInfo.propList != null && animInfo.propList.Count != 0)
             {
                 foreach (var propData in animInfo.propList)
                 {
                    if (propData.metaDataUrl == null || propData.id == null)
                    {
                        continue;
                    }
                    UgcAnimPropData animData = new UgcAnimPropData()
                     {
                         Index = propData.index,
                         BindIndex = propData.bindIndex,
                         MetaDataUrl = propData.metaDataUrl,
                         Id = propData.id
                     };
                     netData.PropList.Add(animData);
                 }
             }
             netData.Interact = InteractType.Start;
             netData.BuddyId =AIBuddyAvatarController.Inst.SelfAIBuddyInfo?.id;
             
             var selfStateCtr = AvatarController.Inst.SelfStateController;
             var selfBuddyStateCtr = AIBuddyAvatarController.Inst.SelfStateController;
             selfStateCtr.EnterState(PlayerState.UgcDoubleEmote, netData);
             selfBuddyStateCtr.EnterState(PlayerState.UgcDoubleEmote, netData);
             
             EmoteNetManager.Inst.SendUgcEmoteReq(netData);
         }
     }
     

    private void OnDataChange(AssetsData[] changes)
    {
        assetsDatas.SetData(dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int) poseSubType)));
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

}

