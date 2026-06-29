using Es;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

/// <summary>
/// 角色预览组件
/// 功能：负责在UI界面中创建、显示和操作3D角色模型
/// 主要用于皮肤预览、角色定制和部件展示等功能
/// </summary>
public class AvatarPreview : MonoBehaviour
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

    /// <summary>
    /// 开始预览指定皮肤的角色
    /// </summary>
    /// <param name="ugcInfo">用户自定义的皮肤信息</param>
    public void StartPreview(SkinInfo ugcInfo)
    {
        // 激活游戏对象，确保可见
        this.gameObject.SetActive(true);

        // 获取当前用户的角色数据并克隆，避免修改原始数据
        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
        // 应用新的皮肤数据
        tempAvatarInfo.ChangeSkinData(ugcInfo);

        if (characterWrap == null)
        {
            // 首次创建UI角色模型
            characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
            characterWrap.SetParent(characterRoot, true);
        }
        else
        {
            // 更新已有角色模型的外观
            characterWrap.RefreshAvatar(tempAvatarInfo);
        }

        // 设置拖拽工具的旋转目标为角色模型
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
    }

    /// <summary>
    /// 开始预览当前用户的角色（无自定义皮肤）
    /// </summary>
    public void StartPreview()
    {
        // 激活游戏对象，确保可见
        this.gameObject.SetActive(true);
        // 获取并克隆当前用户的角色数据
        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
        
        if (characterWrap == null)
        {
            // 首次创建UI角色模型
            characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
            characterWrap.SetParent(characterRoot, true);
        }
        // 设置拖拽工具的旋转目标为角色模型
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
    }

    /// <summary>
    /// 预览角色的特定部件
    /// </summary>
    /// <param name="subType">部件子类型（可能表示部位或变种）</param>
    /// <param name="pgcId">部件的唯一标识符</param>
    public void PartPreview(int subType, string pgcId)
    {
        // 获取部件的类型
        var type = UniqueType.GetAvatar(pgcId);
        // 获取部件的配置数据
        var config = DataTables.GetAvatarCommonData(pgcId);
        
        // 更换角色的指定部件
        characterWrap.ChangePart(type, pgcId);
        // 应用默认颜色
        characterWrap.ChangeColor(subType, config.defaultColor);
        // 设置部件的位置、旋转、缩放和左右朝向，使用配置的默认值
        characterWrap.Move(subType, config.pDef);
        characterWrap.Rotate(subType, config.rDef);
        characterWrap.Scale(subType, config.sDef);
        characterWrap.SetLeftOrRight(subType, config.leftRightType);
    }
}
