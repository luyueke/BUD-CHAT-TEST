using System;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using Network.Message;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;


public class InteractiveEmoteSelectPanel : BasePanel<InteractiveEmoteSelectPanel> {
    public enum InteractiveEmoteType {
        BUD,
        UGC,
    }

    public enum UGCEmoteType {
        Created,
        Owned,
    }


    [SerializeField] internal Transform transBg;
    [SerializeField] internal Button backButton;

    [SerializeField] internal TabView mainTabView;
    [SerializeField] internal TabView subTabView;

    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    [SerializeField] internal RectTransform contentRoot;
    [SerializeField] internal FittingRoomAdapter assetAdapter;

    [SerializeField] internal GameObject emptyTip;

    private string selectEmoteId;
    private Action<string, string> onEmoteSelectCallback;

    private CharacterWrap characterWrap;
    private PlayerAnimationCtrl animationCtrl;
    private AnimIKController avatarIkController;

    private AvatarBagSceneHandler dataHandler;
    private GoodsDataClassifyList assetDataList = new();

    private InteractiveEmoteType selectedEmoteType = InteractiveEmoteType.BUD;
    private UGCEmoteType selectedUgcEmoteType = UGCEmoteType.Created;


    #region Env

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;

    #endregion



    public override void OnCreate() {
        base.OnCreate();
        InitUI();
        InitBtn();
        InitData();
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args.Length > 0) {
            selectEmoteId = args[0] as string;
        }

        if (args.Length > 1) {
            onEmoteSelectCallback = args[1] as Action<string, string>;
        }


        var resType = UniqueType.GetPgcResType(selectEmoteId);
        if (resType == ResourceType.Emote) {
            selectedEmoteType = InteractiveEmoteType.BUD;
        } else if (resType == ResourceType.UgcEmote) {
            selectedEmoteType = InteractiveEmoteType.UGC;
        }

        mainTabView.SelectWithoutCallback(selectedEmoteType.ToString());
        subTabView.SelectWithoutCallback(selectedUgcEmoteType.ToString());
        RefreshData();


        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();


    }


    public override void OnHidden() {
        base.OnHidden();
        AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
        AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
        AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
        AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
    }


    private void InitUI() {
        if (transBg == null) {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        string prefabPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab";
        var itemObj = Loader.Load<GameObject>(prefabPath).Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>() {
            "AvatarBg_icon1", "AvatarBg_icon5", "AvatarBg_icon3", "AvatarBg_icon4", "AvatarBg_icon2"
        });
        item.gameObject.SetActive(true);
        mainTabView.CreateItem(InteractiveEmoteType.BUD.ToString(), "官方");
        mainTabView.CreateItem(InteractiveEmoteType.UGC.ToString(), "社区");
        subTabView.CreateItem(UGCEmoteType.Created.ToString(), "我的创作");
        subTabView.CreateItem(UGCEmoteType.Owned.ToString(), "已拥有");

        CreateAvatar();
    }

    private void InitBtn() {
        backButton.onClick.AddListener(OnBackClick);
        mainTabView.AddItemSelectCallBack(OnMainTabSelect);
        subTabView.AddItemSelectCallBack(OnSubTabSelect);
    }

    private void OnSubTabSelect(TabItem tableItem, int index) {
        var ugcEmoteType = Enum.Parse<UGCEmoteType>(tableItem.gameObject.name);
        selectedUgcEmoteType = ugcEmoteType;
        RefreshData();
    }


    private void InitData() {
        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);
        assetAdapter.Data = new LazyDataHelper<GoodsData>(assetAdapter, CreateNewModel);
        assetAdapter.Init();
        assetAdapter.ResetColor();
        ColorUtility.TryParseHtmlString("#AEA6CC", out assetAdapter.BgColor);
        assetAdapter.OnItemSelected = OnItemSelect;
    }

    private GoodsData CreateNewModel(int index) {
        var assetsData = assetDataList.Get(index);
        assetsData.Selected = assetsData.Id == selectEmoteId;
        return assetsData;
    }

    private void RefreshData() {


        if (selectedEmoteType == InteractiveEmoteType.BUD) {
            contentRoot.offsetMax = new Vector2(contentRoot.offsetMax.x, -150);
            subTabView.gameObject.SetActive(false);
        } else {
            contentRoot.offsetMax = new Vector2(contentRoot.offsetMax.x, -250);
            subTabView.gameObject.SetActive(true);
        }



        var resType = selectedEmoteType == InteractiveEmoteType.BUD ? ResourceType.Emote : ResourceType.UgcEmote;
        var tmpDataList = new List<GoodsData>();
        tmpDataList.AddRange(dataHandler.GetGoodsData(UniqueType.Get(resType, (int)EmoteSubType.Single)));
        tmpDataList.AddRange(dataHandler.GetGoodsData(UniqueType.Get(resType, (int)EmoteSubType.SingleLoop)));
        Func<GoodsData, bool> predicate = null;
        if (selectedEmoteType == InteractiveEmoteType.UGC) {
            predicate = selectedUgcEmoteType == UGCEmoteType.Created ? CreatePredicate : BuyPredicate;
        } else {
            predicate = PGCPredicate;
        }

        assetDataList.SetData(tmpDataList, predicate);
        assetAdapter.Data.ResetItems(assetDataList.Count());
        emptyTip.SetActive(assetDataList.Count() == 0);
    }

    internal bool PGCPredicate(GoodsData goodsData) {
        if (goodsData == null) return false;
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return true;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is EmoteAssetsData)) return false;
        return true;
    }


    internal bool CreatePredicate(GoodsData goodsData) {
        if (goodsData == null) return false;
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return true;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) &&
            !(asset is UgcPoseAssetsData)) return false;
        if (asset.InventoryData == null) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    internal bool BuyPredicate(GoodsData goodsData) {
        if (goodsData == null) return false;
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return true;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) &&
            !(asset is UgcPoseAssetsData)) return false;
        if (asset.InventoryData == null) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }


    private void OnDataChange(AssetsData[] obj) {
        if (gameObject == null) {
            return;
        }

        RefreshData();
    }


    #region Button

    private void OnMainTabSelect(TabItem tabItem, int index) {
        var emoteType = Enum.Parse<InteractiveEmoteType>(tabItem.gameObject.name);
        selectedEmoteType = emoteType;

        RefreshData();
    }

    private void OnBackClick() {
        CloseSelf();

        var goodsData = assetDataList.Get(selectEmoteId);
        string emoteName = "";
        if (goodsData != null) {
            emoteName = goodsData.Assets[0].Name;
        }

        if (string.IsNullOrEmpty(emoteName)) {
            emoteName = "未命名";
        }

        onEmoteSelectCallback?.Invoke(selectEmoteId, emoteName);
    }

    private void OnItemSelect(GoodsData goodsData) {
        CancelEmote();
        selectEmoteId = goodsData.Id;
        assetAdapter.Data.ResetItems(assetDataList.Count());
        if (goodsData.GoodsType == GoodsType.SinglePgc) {
            avatarCameraController.SetEmoteView(selectEmoteId);
            goodsData.Loading(true);
            animationCtrl.PlaySingleEmoteForUICharacter(selectEmoteId, OnDownloadOver: () => goodsData.Loading(false));
        } else {
            var ueInfo = goodsData.Assets[0] as UgcAnimAssetsData;
            avatarCameraController.SetEmoteView((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType);
            avatarIkController.Play(ueInfo.UgcInfo.animInfo, null);
        }
    }


    internal void CancelEmote()
    {
        avatarCameraController.ResetEmoteView();
        avatarCameraController.SetCameraZoom(0);
        animationCtrl.ResetEmoteForUICharacter();
        avatarIkController.StopAnimAndResetJointNode();
        avatarIkController.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
        var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
        avatarIkController.transform.localPosition = poseModeData.RoleDefPos[0];
        avatarIkController.transform.localEulerAngles = Vector3.zero;
        avatarIkController.transform.localScale = Vector3.one;
        avatarIkController.transform.parent.localPosition = poseModeData.EditPos[0];
        avatarIkController.transform.parent.localEulerAngles = Vector3.zero;
        avatarIkController.transform.parent.localScale = Vector3.one;
    }

    #endregion

    private void CreateAvatar() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        avatarIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
        animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        avatarCameraController.RotateTarget = characterRoot;
    }
}
