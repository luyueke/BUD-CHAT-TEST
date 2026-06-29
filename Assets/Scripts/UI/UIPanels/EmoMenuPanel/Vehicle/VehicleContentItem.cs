using System;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData.BaseInfo;
using Newtonsoft.Json;
using Pb.Base;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class VehicleContentItem : MonoBehaviour
{
    [SerializeField] private CButton vehicleBtn;
    [SerializeField] private Text vehicleName;
    [SerializeField] private RemoteImageBehaviour vehicleIcon;
    [SerializeField] private GameObject extInfoRoot;
    [SerializeField] private Text extInfoText;
    [SerializeField] private GameObject alreadyOnRoot; //已经在载具上了
    [SerializeField] Image assetsIcon;
    [SerializeField] Sprite budSprite;
    private InventoryData vehicleData;
    private string url;
    private bool isCalling = false;

    private Action onVehicleBtnClickAction;

    public void InitData(InventoryData vehicleData, Action onVehicleBtnClickAction)
    {
        this.vehicleData = vehicleData;
        this.onVehicleBtnClickAction = onVehicleBtnClickAction;
        isCalling = false;
        InitUI();
    }

    private void InitUI(){
        vehicleBtn.onClick.RemoveAllListeners();
        vehicleBtn.onClick.AddListener(OnVehicleBtnClick);
        // //后续要区分ugc和pgc载具，这里暂时只处理ugc载具
        // AssetsDataManager.GetUgcVehicleInfo(vehicleData.Id, (isSuccess, serverData) =>
        // {
        //     if (!isSuccess) return;
        //     vehicleInfo = serverData.vehicleInfo;
        //     vehicleName.SetText(vehicleInfo.name);
        //     extInfoRoot.SetActive(true);
        //     extInfoText.SetText(vehicleInfo.vehicleType == (int)VehicleType.Single ? "单人" : "双人");
        //     if(AvatarController.Inst.SelfWrap.HasVehicle()
        //     && AvatarController.Inst.SelfWrap.ChaData.vehicleData.id == vehicleData.Id){
        //         alreadyOnRoot.SetActive(true);
        //     }else{
        //         alreadyOnRoot.SetActive(false);
        //         vehicleBtn.onClick.AddListener(OnVehicleBtnClick);
        //     }
        //     RefreshCover(serverData.vehicleInfo.cover);

        // });
        var sprite = PgcUtils.GetIconSpriteByPgcId(vehicleData.Id, gameObject);
        if (sprite != null) assetsIcon.sprite = sprite;
        else assetsIcon.sprite = budSprite;
        var pgcData = DataTables.GetPgcNameData(vehicleData.Id);
        if (!string.IsNullOrEmpty(pgcData.Name))
        {
            vehicleName.gameObject.SetActive(true);
            vehicleName.text = pgcData.Name;
        }
    }

    private void OnVehicleBtnClick()
    {
        if(isCalling){
            TipPanel.ShowToast("正在呼叫载具");
            return;
        }
        if (AvatarController.Inst.SelfStateController != null &&
            !AvatarController.Inst.SelfStateController.CanCallVehicle(out var toastReason))
        {
            TipPanel.ShowToast(toastReason);
            return;
        }
        VehicleInfo vehicleInfo = new VehicleInfo();
        vehicleInfo.templateId = vehicleData.Id;
        vehicleInfo.id = vehicleData.Id;

        if (vehicleInfo != null){
            isCalling = true;
            Vector3 position = AvatarController.Inst.SelfWrap.Avatar.transform.position;
            Quaternion rotation = AvatarController.Inst.SelfWrap.Avatar.transform.rotation;
            GameVehicleManager.Inst.SendCreateVehicle(
                AccountDataManager.Inst.Uid, 
                vehicleData.Id, 
                JsonConvert.SerializeObject(vehicleInfo), 
                position,
                rotation,
                (isSuccess)=>{
                    if(isSuccess){
                        TipPanel.ShowToast("载具召唤中");
                    }else{
                        TipPanel.ShowToast("请先下车再召唤载具");
                    }
                });
        }
        onVehicleBtnClickAction?.Invoke();

    }
    
}