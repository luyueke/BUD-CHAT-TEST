using Game;
using Game.Audio;
using Game.BudBox;
using Game.GameSetting;
using UI.Base;
using UnityEngine;

/// <summary>
/// PorVideoCallNode 通话面板的 UIManager 壳层，供控制台（IncubationCabinControll）
/// 通过 UIManager.OpenPanel(PanelId.PorVideoCallNode, character, box) 打开。
/// 本类仅负责生命周期桥接，具体通话逻辑由 ChatVideoCallNode 承载。
/// </summary>
public class PorVideoCallNodePanel : BasePanel<PorVideoCallNodePanel>
{
    /// <summary>实际通话 UI 节点，在预制体中赋值</summary>
    [SerializeField] private ChatVideoCallNode callNode;

    /// <summary>
    /// 标记通话面板打开时是否已静音游戏 BGM。
    /// 仅在通话真正结束（onCallEnded / onCallFailed）时置 true 并恢复 BGM；
    /// 小窗最小化时不置 true，BGM 继续静音直到通话结束。
    /// </summary>
    private bool _shouldRestoreBgm;

    /// <summary>
    /// UIManager 打开面板时调用。
    /// args[0]: CabinPublishData（角色信息，用于头像和名称展示）
    /// args[1]: CabinBudBoxData（设备信息，用于 MQTT 通信）
    /// </summary>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var character = args != null && args.Length > 0 ? args[0] as CabinPublishData : null;
        var box       = args != null && args.Length > 1 ? args[1] as CabinBudBoxData  : null;

        if (character == null || box == null)
        {
            LoggerUtils.LogError("[PorVideoCallNodePanel] OnShow 参数无效：character 或 box 为 null");
            CloseSelf();
            return;
        }

        // 通话面板打开，立即静音游戏 BGM，避免与通话音频混杂
        AkSoundManager.Inst.SetBGMAudioVolume(0);
        _shouldRestoreBgm = false;

        // 注入回调：通话真正结束时标记恢复 BGM，小窗最小化则保持静音（通话仍进行中）
        callNode.onMinimize   = CloseSelf;
        callNode.onCallFailed = () => { _shouldRestoreBgm = true; CloseSelf(); };
        callNode.onCallEnded  = () => { _shouldRestoreBgm = true; CloseSelf(); };

        callNode.Init(character, box);
    }

    /// <summary>
    /// 面板关闭时清空回调引用，避免 callNode 在其他场景复用时意外调用。
    /// </summary>
    public override void OnHidden()
    {
        base.OnHidden();

        // 通话真正结束时恢复游戏 BGM（小窗最小化时 _shouldRestoreBgm 为 false，BGM 继续静音）
        if (_shouldRestoreBgm)
        {
            AkSoundManager.Inst.SetBGMAudioVolume(GlobalSettingManager.Inst.GetBgmVolume());
            _shouldRestoreBgm = false;
        }

        if (callNode != null)
        {
            callNode.onMinimize   = null;
            callNode.onCallFailed = null;
            callNode.onCallEnded  = null;
        }
    }
}
