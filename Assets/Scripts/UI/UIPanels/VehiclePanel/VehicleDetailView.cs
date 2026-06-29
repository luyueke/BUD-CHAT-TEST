using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI;
using UI.BaseWidgets;
using UnityEngine;

public class VehicleDetailView : MonoBehaviour
{
    public CButton Btn_Close;
    public CButton Btn_DraftEdit;
    public CButton Btn_EditName;
    public CButton Btn_Copy;
    public CButton Btn_Delete;
    public CButton Btn_Publish;
    public CButton Btn_GameImage;
    public CButton Btn_ChangeType;
    public BUD_Text Txt_MapName;
    public CText Txt_LastEditTime;
    public RemoteImageBehaviour Remote_MapCover;

    private DraftListItem _draftListItem;

    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

    public void InitUI()
    {
        Btn_Close.onClick.AddListener(HidePanel);
        Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
        Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
        Btn_Copy.onClick.AddListener(OnBtnCopyClick);
        Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
        Btn_Publish.onClick.AddListener(GoToPublishView);
        Btn_GameImage.onClick.AddListener(GoToPublishView);
        Btn_ChangeType.onClick.AddListener(OnChangeTypeBtnClick);
    }

    /// <summary>
    /// 更新详情布局数据
    /// </summary>
    /// <param name="draftListItem"></param>
    /// <param name="studioType"></param>
    public void UpdateInfo(DraftListItem draftListItem)
    {
        if (draftListItem == null || draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("详情页 - vehicleInfo = null");
            return;
        }

        this.gameObject.SetActive(true);
        _draftListItem = draftListItem;

        Txt_MapName.text = GameStudioUtils.GetBaseInfo(draftListItem).name;
        Txt_LastEditTime.SetLocalText("最后编辑 {0}", TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));
        string coverUrl = GameStudioUtils.GetBaseInfo(draftListItem).cover;
        if (!string.IsNullOrEmpty(coverUrl))
        {
            Remote_MapCover.Load(coverUrl);
        }
    }

    private void HidePanel()
    {
        gameObject.SetActive(false);
    }

    private void OnDraftsEditBtnClick()
    {

        if (_draftListItem == null || _draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("详情页 - vehicleInfo = null");
            return;
        }
        if (!GameController.IsInHallScene())
        {
            TipPanel.ShowToast("在社区地图中无法进入试驾驶状态，请退出社区地图后重试");
            return;
        }
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCVehicle_1", gameObject);
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new VehicleInfo()
        {
            name = _draftListItem.vehicleInfo.name
        }, null, LoadingType.Vehicle, s: sprite);

        GameController.StartVehicleActionGame(EnterGameModel.UgcVehicleEmptyContinueEdit, _draftListItem.vehicleInfo);

        HidePanel();
    }

    private void OnEditNameBtnClick()
    {
        var name = GameStudioUtils.GetBaseInfo(_draftListItem).name;
        var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
        panel.SetOnClickAction(ConfirmEditName);
    }

    private void ConfirmEditName(string name)
    {
        if (_draftListItem == null || _draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("详情页 - vehicleInfo = null");
            return;
        }

        _draftListItem.vehicleInfo.name = name;

        var req = new SetVehicleReq
        {
            vehicleInfo = _draftListItem.vehicleInfo,
            setType = (int)SetType.Edit
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetVehicle, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
    }

    private void OnEditSuccess(string msg)
    {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        HidePanel();
        RefreshDraftsList();
    }

    private void OnEditFail(string failMsg)
    {
        UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
        LoggerUtils.LogError(failMsg);
    }

    private void OnBtnCopyClick()
    {
        if (_draftListItem == null || _draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("详情页 - vehicleInfo = null");
            return;
        }

        var req = new SetVehicleReq
        {
            vehicleInfo = _draftListItem.vehicleInfo,
            setType = (int)SetType.Copy
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetVehicle, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
    }

    private void CopySuccess(string msg)
    {
        HidePanel();
        RefreshDraftsList();
    }

    private void CopyFail(string failMsg)
    {
        LoggerUtils.LogError(failMsg);
    }

    private void OnBtnDeleteClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
        commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
    }

    private void ConfirmClick()
    {
        if (_draftListItem == null || _draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("详情页 - vehicleInfo = null");
            return;
        }

        var req = new SetVehicleReq
        {
            vehicleInfo = _draftListItem.vehicleInfo,
            setType = (int)SetType.Delete
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetVehicle, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
    }

    private void OnDeleteSuccess(string msg)
    {
        HidePanel();
        RefreshDraftsList();
    }

    private void OnDeleteFail(string failMsg)
    {
        LoggerUtils.LogError(failMsg);
    }

    private void CancelClick()
    {
    }

    private void GoToPublishView()
    {
        OnBtnPublishClick(CurrencyType.PinkCoin);
    }

    private void OnBtnPublishClick(CurrencyType currencyType)
    {
        if (_draftListItem.vehicleInfo == null)
        {
            LoggerUtils.LogError("载具发布 - 数据有误");
            return;
        }

        var tmpVehicleInfo = _draftListItem.vehicleInfo.Clone();

        //1.顶点数检查
        var vehicleDetai = _draftListItem.vehicleInfo.detailInfo;
        if (vehicleDetai != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Prop, vehicleDetai))
        {
            UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Prop, vehicleDetai);
            return;
        }

        //2.尺寸检查：限制 scale 最大值为 5
        // if (vehicleDetai != null && vehicleDetai.size != Vector3.zero)
        // {
        //     float maxValue = Mathf.Max(vehicleDetai.size.x, vehicleDetai.size.y, vehicleDetai.size.z);
        //     if (maxValue > 5f)
        //     {
        //         // 计算缩小比例：如果 size 是 10，需要缩小到 5，比例 = 5/10 = 0.5
        //         // 这样最终的显示尺寸 = size * sDef = 10 * 0.5 = 5
        //         float scaleFactor = 5f / maxValue;
                
        //         // 如果 sDef 未初始化，先设为 Vector3.one
        //         if (vehicleDetai.sDef == Vector3.zero)
        //         {
        //             vehicleDetai.sDef = Vector3.one;
        //         }
                
        //         // 按比例缩小 sDef
        //         vehicleDetai.sDef *= scaleFactor;
                
        //         // 同步到原始数据
        //         _draftListItem.vehicleInfo.detailInfo.sDef = vehicleDetai.sDef;
        //     }
        // }

        

        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase>()
        {
        };
        if(_draftListItem.vehicleInfo != null)
        {
            GameVehicleManager.Inst.AddPropData(_draftListItem.vehicleInfo);
        }

        if (_draftListItem.vehicleInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
        {
            stateList.Add(new UGCPublishStateBase(UGCPublishState.VehicleCoverView));
        }
        //stateList.Add(new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor));
        stateList.Add(new UGCVehicleDetailState());
        publishMachine.SetStates(stateList);

        var vehicleDraftInfo = VehicleAssetManager.Inst.GetOrCreateDraftInfo(_draftListItem.vehicleInfo);
        if (vehicleDraftInfo == null)
        {
            return;
        }
        publishMachine.SetEditData(new VehicleEditData()
        {
            skinActionDraftInfo = vehicleDraftInfo,
            currencyType = currencyType
        }); 
        publishMachine.SetCancelCallBack(() => {
            GlobalNodeManager.Inst.Release();
        });
        publishMachine.SetFinishCallBack(() => {
            HidePanel();
            RefreshPublishedList();
            GlobalNodeManager.Inst.Release(); 
        });

        publishMachine.Start();

    }

    private void OnChangeTypeBtnClick()
    {
        if (_draftListItem != null)
        {
            UIManager.Inst.OpenPanel(PanelId.ChangeUGCTypePanel, _draftListItem);
        }
        HidePanel();
    }

    private void RefreshDraftsList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcVehicleDraftsListChange);
    }

    private void RefreshPublishedList()
    {
        MessageHelper.Broadcast(MessageName.OnUgcVehiclePublishedListChange);
    }
}
