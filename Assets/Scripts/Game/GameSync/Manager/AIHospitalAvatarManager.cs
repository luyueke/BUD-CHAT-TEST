using Es;
using Game.Avatar;
using GameData;
using UnityEngine;

/// <summary>
/// AI医院场景专用的换装管理器
/// 简化版本，移除了网络同步相关逻辑
/// </summary>
public class AIHospitalAvatarManager : GameInstance<AIHospitalAvatarManager>
{
    private BudTimer changeTimer;

    public void OnEnterFittingRoom()
    {
        var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (stateCtrl == null) return;
        
        // 进入换装状态
        stateCtrl.EnterState(PlayerState.ChangeClothes);
    }

    public void OnExitFittingRoom(CharacterData data)
    {
        if (data == null) return;

        //这里因为有对特殊逻辑处理所有重新执行了序列化和反序列化操作，可以优化下
        var newData = CharacterData.ProcessSpecialBehavior(data);

        var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (stateCtrl == null) return;

        // 1. 先退出换装状态
        stateCtrl.ExitState(PlayerState.ChangeClothes);

        if (changeTimer != null)
        {
            TimerManager.Inst.Stop(changeTimer);
        }

        // 延迟刷新角色外观
        changeTimer = TimerManager.Inst.RunOnce("changeOcdelay", 1f, () =>
        {
            // 保存当前特殊动画状态
            string lastSpecialAnimPgcId = stateCtrl.PlayerAnimCtrl.specialAnimPgcId;
            
            // 刷新角色外观
            stateCtrl.Wrap.RefreshAvatar(data);

            // 处理特殊动画状态
            if (!string.IsNullOrEmpty(stateCtrl.PlayerAnimCtrl.specialAnimPgcId))
            {
                // 如果有特殊动画，重新应用
                stateCtrl.PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
                var specialKCC = DataTables.GetSpecialSkinConfig(stateCtrl.PlayerAnimCtrl.specialAnimPgcId);
                stateCtrl.PlayerKCCtrl.ChangeSpecialAnimKCC(specialKCC.KCC);
            }
            else if (!string.IsNullOrEmpty(lastSpecialAnimPgcId))
            {
                // 如果之前有特殊动画但现在没有，清除特殊动画
                stateCtrl.PlayerAnimCtrl.ClearOverrideSpecialAnim();
                stateCtrl.PlayerKCCtrl.ChangeSpecialAnimKCC("Default");
            }
        });
    }

    public void OnEnterPetFittingRoom()
    {
        var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (stateCtrl == null) return;
        
        stateCtrl.EnterState(PlayerState.ChangeClothes);
    }

    public void OnExitPetFittingRoom(PetData data)
    {
        if (data == null) return;

        var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (stateCtrl == null) return;

        // 处理宠物动画状态
        if (stateCtrl.PetAnimCtrl != null)
        {
            stateCtrl.PetEmoState.OnPetBeginFollow(true);
            if (stateCtrl.PetWrap.Avatar.activeInHierarchy)
            {
                stateCtrl.PetAnimCtrl.PlayChangeClothAni(null, true, AccountDataManager.Inst.Uid);
            }
        }
        
        stateCtrl.ExitState(PlayerState.ChangeClothes, false);

        // 停止之前的计时器
        if (changeTimer != null)
        {
            TimerManager.Inst.Stop(changeTimer);
        }

        // 延迟刷新宠物外观
        changeTimer = TimerManager.Inst.RunOnce("changeOcdelay", 1f, () =>
        {
            if (stateCtrl != null && stateCtrl.PetWrap != null)
            {
                stateCtrl.PetWrap.RefreshAvatar(data);
            }
        });
    }

    public override void Release()
    {
        base.Release();
        
        // 清理计时器
        if (changeTimer != null)
        {
            TimerManager.Inst.Stop(changeTimer);
            changeTimer = null;
        }
    }
}