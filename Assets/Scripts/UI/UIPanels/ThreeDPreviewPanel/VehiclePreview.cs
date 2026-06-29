using Es;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using UI;
using UnityEngine;

/// <summary>
/// 预览组件
/// 主要用于载具展示等功能
/// </summary>
public class VehiclePreview : MonoBehaviour
{
    /// <summary>
    /// 角色模型的父节点，用于定位角色在场景中的位置
    /// </summary>
    public Transform characterRoot;

    /// <summary>
    /// 角色包装器，负责管理角色模型及其相关操作
    /// </summary>
    public CharacterWrap characterWrap;

    /// <summary>
    /// UI拖拽工具，用于实现角色旋转等交互功能
    /// </summary>
    public UIDragUtil dragUtil;


    private VehicleInfo selectedVehicleInfo = null;


    public void StartPreview(VehicleInfo vehicleInfo)
    {
        this.gameObject.SetActive(true);

        // 获取当前用户的角色数据并克隆，避免修改原始数据
        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
        characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
        characterWrap.SetParent(characterRoot, true);
        // 设置拖拽工具的旋转目标为角色模型
        dragUtil.RotateTarget = characterWrap.Avatar.transform;

        if (selectedVehicleInfo != null)
        {
            TakeOffVehicle();
        }

        selectedVehicleInfo = vehicleInfo;
        if (vehicleInfo != null)
        {
            GameVehicleManager.Inst.AddPropData(vehicleInfo);
        }

        if (vehicleInfo != null)
        {
            characterWrap.ChangeUGCVehicle(AccountDataManager.Inst.UserInfo.uid, vehicleInfo);
         //   avatarCameraController.SetVehicleView(UniqueType.VehicleSubType(vehicleInfo.vehicleType));
        }
    }




    public void TakeOffVehicle()
    {
        //因为载具装作是通过注入到角色数据中的，所以刷新角色数据就可以把载具脱下
        {
            characterWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);
        }
    }

}
