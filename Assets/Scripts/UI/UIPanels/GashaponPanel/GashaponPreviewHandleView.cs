using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Es;
using Game.Avatar;
using Game.Config;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponPreviewHandleView : MonoBehaviour
{
    [SerializeField] private GashaponCharacterPreview characterPreview;
    [SerializeField] private GashaponPreviewListView previewListView;
    [SerializeField] private GashaponPonyPreviewListView ponyPreviewListView;
    [SerializeField] private BundleExpandView bundleExpandView;
    [SerializeField] private Transform bgRootNode;
    [SerializeField] private Text titleText;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text itemDescText;
    [SerializeField] private CButton infoBtn;
    [SerializeField] private CButton musicPreviewBtn;
    [SerializeField] private CButton changeOtherOcBtn;
    [SerializeField] private CButton backBtn;
    [SerializeField] private AccountWidget customAccountWidget;
    [SerializeField] private RectTransform previewListScrollView;

    private string rulePath;
    private string curGashaponId;
    private bool _syncIdleOnLoad;

    public bool IsPonyPreview = false; //多奖池预览

    // 供虾虾崽扭蛋面板调用，开启后单件预览走顺序加载并在完成后播待机动画
    public void EnableIdleSync() => _syncIdleOnLoad = true;

    // 供 huhu/wuwu 等普通套装预览调用：卸下角色自带的特殊皮肤，不让云出现
    public void TakeOffSpecialSkin()
    {
        if (characterPreview != null) characterPreview.TakeOffSpecialSkin();
    }

    public void SetPonyData(GashaponData gashaponData, List<GashaponPonyPreviewItemData> gashaponDataList, GashaponInfoRsp gashaponInfoRsp = null, bool isNeedPreDeal = true)
    {
        IsPonyPreview = true;
        curGashaponId = gashaponData.Id;

        for (int i = 0; i < gashaponDataList.Count; i++)
        {
            var data = gashaponDataList[i];
            var tRewardList = data.rewardList;
            if (isNeedPreDeal)
            {
                tRewardList = GashaponDataManager.Inst.PreDealData(tRewardList);
            }
            gashaponDataList[i].rewardList = tRewardList;
        }
        ponyPreviewListView.SetGashaponData(gashaponData);
        ponyPreviewListView.gameObject.SetActive(true);
        previewListView.gameObject.SetActive(false);
        previewListScrollView = GameObjectEx.FindChildByName(ponyPreviewListView.transform, "Scroll View").GetComponent<RectTransform>();
        ponyPreviewListView.UpdateListview(gashaponDataList, gashaponInfoRsp);
    }

    public void SetData(GashaponData gashaponData, GashaponInfoRsp gashaponInfoRsp = null, bool isNeedPreDeal = true)
    {
        IsPonyPreview = false;
        curGashaponId = gashaponData.Id;
        var rewardList = gashaponData.RewardList;
        if (isNeedPreDeal)
        {
            rewardList = GashaponDataManager.Inst.PreDealData(gashaponData.RewardList);
        }
        previewListView.gameObject.SetActive(true);
        ponyPreviewListView.gameObject.SetActive(false);
        previewListScrollView = GameObjectEx.FindChildByName(previewListView.transform, "Scroll View").GetComponent<RectTransform>();
        previewListView.UpdateListview(rewardList, gashaponInfoRsp);
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
        Invoke("DefClickFirst", 0.2f);
    }

    public void Hide()
    {
        previewListView.HideItemsLoading();
        characterPreview.StopAllAnim();
        characterPreview.StopAllEmoteSound();
        this.gameObject.SetActive(false);
    }

    public void SetBg(string path)
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(curGashaponId);
        if (viewCfg == null)
        {
            return;
        }
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        if (string.IsNullOrEmpty(path))
        {
            item.InitCustomBgItem(viewCfg.BgColor, viewCfg.AtlasPath, viewCfg.BgSpriteIds);
        }
        else
        {
            item.InitCustomTextureBg(path);
        }

        item.gameObject.SetActive(true);

        characterPreview.SetCameraColor(DataUtil.DeSerializeColorByHex(viewCfg.BgColor + "00"));
    }

    public void SetTitle(string title)
    {
        titleText.SetLocalText(title);
    }

    public void SetAnimPreviewBtnColor(Color outlineColor, Color selectColor, Color normalColor)
    {
        if (characterPreview != null)
        {
            characterPreview.SetAnimPreviewBtnColor(outlineColor, selectColor, normalColor);
        }
    }


    public void SetBundleViewBgClolr(string colorStr)
    {
        bundleExpandView?.SetBgColor(colorStr);
    }

    public void SetAccountWidgetType(CurrencyType type)
    {
        customAccountWidget?.ChangeType(type);
    }

    public void SetRulePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            var gashaponData = GashaponDataManager.Inst.gashaponData(curGashaponId);
            if (gashaponData.CurrencyType != CurrencyType.GreenCoin && gashaponData.CurrencyType != CurrencyType.Coin)
            {
                path = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultLimitRule.json";
            }
            else
            {
                path = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultRule.json";
            }
        }
        rulePath = path;

    }

    void Awake()
    {
        MessageHelper.AddListener<int>(MessageName.OnPhantomSoundPartyBigRewardToggleChanged, OnPhantomSoundPartyBigRewardToggleChanged);
    }

    void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        infoBtn.onClick.AddListener(() => OnInfoClick());
        previewListView.AddItemClickListener(OnItemClick);
        ponyPreviewListView.AddItemClickListener(OnItemClick);
        musicPreviewBtn?.onClick.AddListener(OnMusicalInstrumentsPreview);
        changeOtherOcBtn?.onClick.AddListener(ChangeOtherOc);
        backBtn.onClick.AddListener(OnBackBtnClick);
    }

    private void OnPhantomSoundPartyBigRewardToggleChanged(int pgcId)
    {
        var name = GashaponPhantomSoundPartyPanel.GetVehicleName(pgcId);
        if (string.IsNullOrEmpty(name)) return;
        itemNameText.gameObject.SetActive(true);
        itemNameText.text = name;
    }

    private void DefClickFirst()
    {
        characterPreview.StopAllEmoteSound();
        if (IsPonyPreview)
        {
            ponyPreviewListView.DefClickFirst();
        }
        else
        {
            previewListView.DefClickFirst();
        }
    }

    public void Turn2Preview(string bundleId)
    {
        if (IsPonyPreview)
        {
            ponyPreviewListView.Turn2Preview(bundleId);
        }
        else
        {
            previewListView.Turn2Preview(bundleId);
        }
    }

    private void OnItemClick(GashaponRewardData info)
    {
        itemNameText.gameObject.SetActive(false);
        itemDescText.gameObject.SetActive(false);
        musicPreviewBtn?.gameObject.SetActive(false);
        changeOtherOcBtn?.gameObject.SetActive(false);
        bundleExpandView.Hide();
        if (previewListScrollView.offsetMin.y == 259)
        {
            previewListScrollView.offsetMin = new Vector2(previewListScrollView.offsetMin.x, 50);
        }
        string pgcId = "";
        if (GashaponUtils.HasPGCData(info))
        {
            pgcId = info.PgcDatas[0].Id;
        }

        if (!string.IsNullOrEmpty(pgcId))
        {
            GameResData resData = Es.DataTables.GetGameResData(pgcId);
            if (resData == null)
            {
                LoggerUtils.LogError("resData is null:" + pgcId);
                return;
            }
            if (resData.ResourceType == (int)ResourceType.Avatar || resData.ResourceType == (int)ResourceType.UgcAvatar)
            {
                bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                musicPreviewBtn.gameObject.SetActive(isMusic);
            }
            else if (resData.ResourceType == (int)ResourceType.Emote)
            {
                var emoteSubType = ((EmoteSubType)resData.SubType);
                changeOtherOcBtn.gameObject.SetActive(emoteSubType.IsDouble() && !emoteSubType.IsPet());
            }

            if (info.PgcDatas.Count > 1)
            {
                if (previewListScrollView.offsetMin.y != 259)
                {
                    previewListScrollView.offsetMin = new Vector2(previewListScrollView.offsetMin.x, 259);
                }
                bundleExpandView.Init(info);
                bundleExpandView.Show();
                itemNameText.gameObject.SetActive(true);
                itemNameText.SetLocalText(PgcUtils.GetBundleName(info.BundleId));

            }
            else
            {
                itemNameText.gameObject.SetActive(true);
                // 娃娃机货币换名：水晶/碎片即便带 pgcId 也会走到这里，优先用覆盖名(原文直显)
                var nameOverride = PgcUtils.GetScopeCurrencyName(GameUtils.ConvertRewardType((int)info.RewardType));
                if (!string.IsNullOrEmpty(nameOverride))
                {
                    itemNameText.text = nameOverride;
                }
                else
                {
                    itemNameText.SetLocalText(info.PgcDatas[0].Name);
                }
            }

        }
        else
        {
            var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
            if (PgcUtils.CurrencyName.ContainsKey(currencyType) && !string.IsNullOrEmpty(PgcUtils.CurrencyName[currencyType]))
            {
                itemNameText.gameObject.SetActive(true);
                // 作用域覆盖(如娃娃机把 水晶/碎片 改名为 星辉夹/泡泡夹)：有覆盖名则用原文直显，否则走本地化
                var nameOverride = PgcUtils.GetScopeCurrencyName(currencyType);
                if (!string.IsNullOrEmpty(nameOverride))
                {
                    itemNameText.text = nameOverride;
                }
                else
                {
                    itemNameText.SetLocalText(PgcUtils.CurrencyName[currencyType]);
                }

                if (PgcUtils.CurrencyClearTips.ContainsKey(currencyType))
                {
                    itemDescText.gameObject.SetActive(true);
                    itemDescText.SetLocalText(PgcUtils.CurrencyClearTips[currencyType]);
                }
            }

            if (info.RewardType == RewardType.RewardAvatarFrame || info.RewardType == RewardType.RewardChatBubbles)
            {
                itemNameText.gameObject.SetActive(true);
                itemNameText.text = info.Name;
            }

            if ((int)info.RewardType == (int)BUDRewardType.RewardSkinSlot || (int)info.RewardType == (int)BUDRewardType.RewardAiBuddySlot 
            || (int)info.RewardType == (int)BUDRewardType.RewardVipFreeTrail ||(int)info.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame
            || (int)info.RewardType == (int)BUDRewardType.RewardTypeTitle)
            {
                itemNameText.gameObject.SetActive(true);
                itemNameText.text = info.Name;
            }
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardTypeMiaoCoin)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardUgcTemplateResource)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardHomepageSkin)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardCrystal)
        {
            itemNameText.gameObject.SetActive(true);
            // 娃娃机换名：水晶→星辉夹，有覆盖名则优先(这两块在最后执行，否则会覆写掉前面的覆盖名)
            var ov = PgcUtils.GetScopeCurrencyName(CurrencyType.Crystal);
            itemNameText.text = string.IsNullOrEmpty(ov) ? info.Name : ov;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardCrystalShards)
        {
            itemNameText.gameObject.SetActive(true);
            // 娃娃机换名：水晶碎片→泡泡夹
            var ov = PgcUtils.GetScopeCurrencyName(CurrencyType.CrystalShards);
            itemNameText.text = string.IsNullOrEmpty(ov) ? info.Name : ov;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardTypeZZZCoin
            || (int)info.RewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal
            || (int)info.RewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystalShards)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if((int)info.RewardType == (int)BUDRewardType.RewardTypeSockTailTicket)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if((int)info.RewardType == (int)BUDRewardType.RewardTypeSockYunyunTicket)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
         if((int)info.RewardType == (int)BUDRewardType.RewardTypeZZZCoin || (int)info.RewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal || (int)info.RewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystalShards)
        {
            itemNameText.gameObject.SetActive(true);
            itemNameText.text = info.Name;
        }
        if (_syncIdleOnLoad && GashaponUtils.HasPGCData(info) && info.PgcDatas.Count == 1)
        {
            characterPreview.StartPreviewWithIdleSync(info.PgcDatas[0].Id, resultId =>
                OnTryComplete(new List<string> { resultId }));
        }
        else
        {
            characterPreview.StartPreview(info, OnTryComplete);
        }
    }

    private void OnTryComplete(List<string> pgcIds)
    {
        if (pgcIds != null && pgcIds.Count > 0)
        {
            if (IsPonyPreview)
            {
                if (ponyPreviewListView.CurSelectItem != null)
                {
                    var bindData = ponyPreviewListView.CurSelectItem.GetBindData();
                    if (bindData.PgcDatas != null)
                    {
                        if (GashaponUtils.IsAssetsDataEqual(bindData.PgcDatas, pgcIds))
                        {
                            ponyPreviewListView.CurSelectItem.SetLoadingVisible(false);
                        }
                    }
                }
            }
            else
            {
                if (previewListView.CurSelectItem != null)
                {
                    var bindData = previewListView.CurSelectItem.GetBindData();
                    if (bindData.PgcDatas != null)
                    {
                        if (GashaponUtils.IsAssetsDataEqual(bindData.PgcDatas, pgcIds))
                        {
                            previewListView.CurSelectItem.SetLoadingVisible(false);
                        }
                    }
                }
            }
        }
    }

    private void OnInfoClick()
    {
        if (!string.IsNullOrEmpty(rulePath))
        {
            UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
        }
    }

    private void OnBackBtnClick()
    {
        Hide();
    }

    private void OnMusicalInstrumentsPreview()
    {
        if (characterPreview == null || characterPreview.characterWrap == null)
        {
            return;
        }

        UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, (characterPreview.characterWrap).ChaData.Clone());
    }

    public void ChangeOtherOc()
    {
        UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.DoubleEmote).OnCloseAction = ChangeOtherOc;
    }

    private void ChangeOtherOc(BaseAvatarData baseAvatarData)
    {
        if (baseAvatarData != null && characterPreview != null)
        {
            try
            {
                var data = (CharacterData)baseAvatarData;
                characterPreview.otherCharacterWrap?.SetCharacterData(data);
                PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid, CharacterData.SerializeObject(data));
            }
            catch { }
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<int>(MessageName.OnPhantomSoundPartyBigRewardToggleChanged, OnPhantomSoundPartyBigRewardToggleChanged);
    }
}
