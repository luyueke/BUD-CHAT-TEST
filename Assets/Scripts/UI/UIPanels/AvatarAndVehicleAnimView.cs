
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Vehicle.PGCVehicle;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;

public class AvatarAndVehicleAnimView{
    private CharacterWrap characterWrap;
    private VehicleInfo vehicleInfo;
    private Transform avatarParent;
    // 双人载具的乘客（如大厅 AI 伙伴）。单人载具或无乘客时为 null。
    private CharacterWrap passengerWrap;

    public AvatarAndVehicleAnimView(CharacterWrap characterWrap, VehicleInfo vehicleInfo, Transform avatarParent = null, CharacterWrap passengerWrap = null)
    {
        this.characterWrap = characterWrap;
        this.vehicleInfo = vehicleInfo;
        this.avatarParent = avatarParent;
        this.passengerWrap = passengerWrap;
    }

    // 是否需要安置乘客：传入了乘客 + 当前是双人载具
    private bool HasPassenger =>
        passengerWrap?.Avatar != null && vehicleInfo != null && vehicleInfo.vehicleType == (int)VehicleType.Double;

    public void StartVehicleAnim(bool isHall = false){
        if (vehicleInfo != null) {
            GameVehicleManager.Inst.AddPropData(vehicleInfo);
            if(string.IsNullOrEmpty(vehicleInfo.id) || vehicleInfo.id == "0"){
                return;
            }

            if(int.TryParse(vehicleInfo.id, out int pgcId)){
                characterWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);
                // 双人载具传 [驾驶=玩家, 乘客=伙伴]，CreateUIPGCVehicle 按 seatOffset[i] 自动安置每个座位
                PGCVehicleManager.Inst.CreateUIPGCVehicle(
                    pgcId,
                    characterWrap.Avatar.transform.parent.parent,
                    BuildPgcSeatCtrls(),
                    (vehicleObject) => {
                        var config = DataTables.GetPgcVehicleConfig(pgcId);
                        if(config != null && avatarParent != null){
                            avatarParent.localPosition = config.hallAvatarOffset;
                        }
                    },
                    isHall ? PGCVehicleUIUsageType.Hall : PGCVehicleUIUsageType.FittingRoom);
            }else{
                PGCVehicleManager.Inst.RemoveUIPGCVehicle(PGCVehicleUIUsageType.Hall);
                characterWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);
                characterWrap.ChangeUGCVehicle(AccountDataManager.Inst.UserInfo.uid, vehicleInfo);
                if (HasPassenger) {
                    SeatUgcPassenger();
                }
            }
       }
    }

    // PGC 座位控制器数组：双人载具且有乘客时返回 [玩家, 伙伴]，否则只有玩家
    private PlayerAnimationCtrl[] BuildPgcSeatCtrls()
    {
        var driverCtrl = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
        if (HasPassenger)
        {
            var passengerCtrl = passengerWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            if (passengerCtrl != null)
            {
                return new PlayerAnimationCtrl[]{ driverCtrl, passengerCtrl };
            }
        }
        return new PlayerAnimationCtrl[]{ driverCtrl };
    }

    // UGC 双人乘客安置：对标 VehiclePlayPanel.SetCharacterPose / ApplyUgcPoseWithSeatPreserved。
    // 乘客位 = 驾驶位 - 第二人位置偏移(doubleUserDetail.pDef)，姿势用 doublePoseData，旋转用 rDef。
    private void SeatUgcPassenger()
    {
        var driverAvatar = characterWrap.Avatar;
        var passengerAvatar = passengerWrap.Avatar;
        if (driverAvatar == null || passengerAvatar == null) return;

        if (passengerAvatar.transform.parent != null
            && driverAvatar.transform.parent != null
            && vehicleInfo.doubleUserDetail != null)
        {
            passengerAvatar.transform.parent.localPosition =
                driverAvatar.transform.parent.localPosition - vehicleInfo.doubleUserDetail.pDef;
        }

        ApplyUgcPassengerPose(passengerWrap, vehicleInfo.doublePoseData, vehicleInfo.doubleUserDetail?.rDef);
    }

    private void ApplyUgcPassengerPose(CharacterWrap wrap, string poseJson, Vector3? seatEulerRotation)
    {
        if (wrap?.Avatar == null || string.IsNullOrEmpty(poseJson)) return;

        var animator = wrap.Avatar.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        var optNode = wrap.Avatar.transform.parent;
        var optLocalPos = optNode != null ? optNode.localPosition : Vector3.zero;
        var optLocalScale = optNode != null ? optNode.localScale : Vector3.one;

        var ikController = wrap.Avatar.GetComponent<AnimIKController>();
        if (ikController != null)
        {
            ikController.ChangeAnimResType(AnimResType.UGC);
            var frameData = JsonConvert.DeserializeObject<KeyFrameData>(poseJson);
            ikController.SetKeyFrameData(UgcPoseSubType.Single, frameData);
        }

        // 恢复姿势设置前的座位坐标（避免被 SetKeyFrameData 覆盖）
        if (optNode != null)
        {
            optNode.localPosition = optLocalPos;
            optNode.localScale = optLocalScale;
        }

        if (seatEulerRotation.HasValue && wrap.Avatar.transform.parent != null)
        {
            wrap.Avatar.transform.parent.localRotation = Quaternion.Euler(seatEulerRotation.Value);
        }
    }
}
