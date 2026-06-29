using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EventTracking;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class SearchPanel : BasePanel<SearchPanel>
{
    public enum SearchType
    {
        Skin,
        Prop,
        Map,
        MusicInstrument,
        MusicScore,
        MusicTone,
        PetSkin,
        UgcEmote,
        UgcPose,
        Npc,
        Vehicle,
        UgcAnimMusic,
    }
    private CButton closeBtn;
    private Text titalText;
    private Text contentText;
    private CButton inputBtn;
    private Text inputText;
    private CButton confirmBtn;
    private Action _confirmAction;
    private const int CodeLength = 7;
    private SearchType _searchType;
    private string httpUrl;
    private int _avatarSubType;
    private string errorTip1;
    private string errorTip2;
    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn = GameObjectEx.FindComponentByName<CButton>(transform, "CloseBtn");
        inputBtn = GameObjectEx.FindComponentByName<CButton>(transform, "InputButton");
        confirmBtn = GameObjectEx.FindComponentByName<CButton>(transform, "YesButton");
        titalText = GameObjectEx.FindComponentByName<Text>(transform, "TitalText");
        contentText = GameObjectEx.FindComponentByName<Text>(transform, "ContentText");
        inputText = GameObjectEx.FindComponentByName<Text>(transform, "InputText");
        closeBtn.onClick.AddListener(CloseSelf);
        inputBtn.onClick.AddListener(OnInputBtnClick);
        confirmBtn.onClick.AddListener(OnConfirmClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length >= 1)
        {
            _searchType = (SearchType)args[0];
            if (args.Length >= 2)
            {
                _avatarSubType = (int)args[1];
            }
            switch (_searchType)
            {
                // case SearchType.Map:
                //     titalText.SetText("地图搜索");
                //     contentText.SetText("请输入7位地图设计码");
                //     httpUrl = HttpUrlDefine.SearchMap;
                //     break;
                case SearchType.Prop:
                    titalText.SetLocalText("素材搜索");
                    contentText.SetLocalText("请输入7位素材设计码");
                    errorTip1 = "请输入正确的素材设计码";
                    break;
                case SearchType.PetSkin:
                    titalText.SetLocalText("商品搜索码");
                    contentText.SetLocalText("请输入7位皮肤设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.Skin:
                    titalText.SetLocalText("商品搜索码");
                    contentText.SetLocalText("请输入7位商品设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.MusicInstrument:
                    titalText.SetLocalText("商品搜索码");
                    contentText.SetLocalText("请输入7位商品设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.MusicScore:
                    titalText.SetLocalText("商品搜索码");
                    contentText.SetLocalText("请输入7位商品设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.MusicTone:
                    titalText.SetLocalText("音色搜索");
                    contentText.SetLocalText("请输入7位音色设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.UgcAnimMusic:
                    titalText.SetLocalText("音色搜索");
                    contentText.SetLocalText("请输入7位音色设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.UgcEmote:
                    titalText.SetLocalText("动作搜索");
                    contentText.SetLocalText("请输入7位动作设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.UgcPose:
                    titalText.SetLocalText("姿势搜索");
                    contentText.SetLocalText("请输入7位姿势设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.Npc:
                    titalText.SetLocalText("NPC搜索");
                    contentText.SetLocalText("请输入7位NPC设计码");
                    errorTip1 = "请输入正确的设计码";
                    break;
                case SearchType.Vehicle:
                    titalText.SetLocalText("载具搜索");
                    contentText.SetLocalText("请输入7位载具设计码");
                    httpUrl = HttpUrlDefine.SearchVehicle;
                    errorTip1 = "请输入正确的设计码";
                    errorTip2 = "该设计码不存在";
                    break;
            }

            (httpUrl, errorTip2) = SetSearchParam(_searchType);
        }
        if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
        {
            LoadEvent.ReportPopupStatus("SearchDesignClick", "ClickSearchDesign");
        }
    }


    private void OnInputBtnClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = contentText.text,
            inputMode = (int)KeyBoardInputMode.All,
            maxLength = 250,
            inputFlag = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
            defaultText = inputText.text,
            returnKeyType = (int)ReturnType.Done,
            textSecurity = 1
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }
    private void OnKeyboard(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }
        inputText.SetText(input);
    }
    private void OnConfirmClick()
    {
        if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
        {
            LoadEvent.ReportPopupStatus("SearchDesignConfigClick", "ClickSearchDesignConfig");
        }
        if (string.IsNullOrEmpty(inputText.text) || !Regex.IsMatch(inputText.text, @"^[0-9A-Z]{7}$"))
        {
            TipPanel.ShowToast(errorTip1);
            return;
        }
        Search(inputText.text, _avatarSubType, _searchType);

    }

    public static (string httpUrl, string errorTip) SetSearchParam(SearchType searchType)
    {
        string httpUrl = "";
        string errorTip = "";
        switch (searchType)
        {
            case SearchType.Prop:
                httpUrl = HttpUrlDefine.SearchProp;
                errorTip = "该素材设计码不存在";
                break;
            case SearchType.PetSkin:
                httpUrl = HttpUrlDefine.SearchUgc;
                errorTip = "该设计码不存在";
                break;
            case SearchType.Skin:
                httpUrl = HttpUrlDefine.SearchUgc;
                errorTip = "该设计码不存在";
                break;
            case SearchType.MusicInstrument:
                httpUrl = HttpUrlDefine.SearchUgc;
                errorTip = "该设计码不存在";
                break;
            case SearchType.MusicScore:
                httpUrl = HttpUrlDefine.SearchUgc;
                errorTip = "该设计码不存在";
                break;
            case SearchType.MusicTone:
                httpUrl = HttpUrlDefine.SearchMusicTone;
                errorTip = "该设计码不存在";
                break;
            case SearchType.UgcEmote:
                httpUrl = HttpUrlDefine.SearchAnimation;
                errorTip = "该设计码不存在";
                break;
            case SearchType.UgcAnimMusic:
                httpUrl = HttpUrlDefine.SearchAnimationMusic;
                errorTip = "该设计码不存在";
                break;
            case SearchType.UgcPose:
                httpUrl = HttpUrlDefine.SearchPose;
                errorTip = "该设计码不存在";
                break;
            case SearchType.Npc:
                httpUrl = HttpUrlDefine.SearchNpc;
                errorTip = "该设计码不存在";
                break;
        }
        return (httpUrl, errorTip);
    }

    public static void Search(string searchWord, int avatarSubType, SearchType searchType,bool showTip = true)
    {
        var (searchHttpUrl, errorTip) = SetSearchParam(searchType);
        JObject jobj = new JObject()
        {
            ["searchWord"] = searchWord,
            ["subType"] = avatarSubType
        };
        NetworkManager.Inst.SendHttpRequest(searchHttpUrl, HttpMethod.GET, JsonConvert.SerializeObject(jobj), (content) =>
            {
                SearchReq mapListResponse = JsonConvert.DeserializeObject<SearchReq>(content);
                if (mapListResponse.list == null || mapListResponse.list[0] == null)
                {
                    if(showTip){
                        TipPanel.ShowToast(errorTip);
                    }
                    return;
                }
                var data = mapListResponse.list[0];
                switch (searchType)
                {
                    case SearchType.Prop:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop, data.ugcInfo.id);
                        break;
                    case SearchType.MusicTone:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicTone, data.ugcInfo.id);
                        break;
                    case SearchType.UgcAnimMusic:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcAnimMusic, data.ugcInfo.id);
                        break;
                    case SearchType.PetSkin:
                    case SearchType.Skin:
                    case SearchType.MusicInstrument:
                    case SearchType.MusicScore:
                    case SearchType.UgcEmote:
                    case SearchType.UgcPose:
                        switch (data.ugcType)
                        {
                            case GameData.UgcType.Clothes:
                                switch (data.skinInfo.subType)
                                {
                                    case (int)AvatarSubType.MusicalInstrument:
                                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Instrument, data.UgcInfo.id, data.skinInfo.ugcStyle);
                                        break;
                                    case (int)AvatarSubType.Bundle:
                                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcBundle, data.UgcInfo.id, data.skinInfo.ugcStyle);
                                        break;
                                    default:
                                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.UgcInfo.id, data.skinInfo.ugcStyle);
                                        break;
                                }
                                break;
                            case GameData.UgcType.MusicScore:
                                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicScore, data.UgcInfo.id);
                                break;
                            case GameData.UgcType.Anim:
                                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcAnim, data.UgcInfo.id);
                                break;
                            case GameData.UgcType.Pose:
                                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcPose, data.UgcInfo.id);
                                break;
                            case GameData.UgcType.UgcVehicle:
                                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Vehicle, data.UgcInfo.id);
                                break;
                        }
                        break;

                    case SearchType.Npc:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, data.UgcInfo.id);
                        break;
                }

            }, null);
    }
    private class SearchReq
    {
        public List<RecommendItemData> list;
    }
}
