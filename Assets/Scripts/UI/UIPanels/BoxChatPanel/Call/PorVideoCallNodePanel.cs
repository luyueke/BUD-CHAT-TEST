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
/// args[2] (bool, 可选)：true = 角色扮演模式，按钮显示"角色扮演中"且不可点击。
/// </summary>
public class PorVideoCallNodePanel : BasePanel<PorVideoCallNodePanel>
{
    /// <summary>实际通话 UI 节点，在预制体中赋值</summary>
    [SerializeField] private ChatVideoCallNode callNode;

    private bool _shouldRestoreBgm;

    private CabinPublishData _character;
    private CabinBudBoxData  _box;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        _character = args != null && args.Length > 0 ? args[0] as CabinPublishData : null;
        _box       = args != null && args.Length > 1 ? args[1] as CabinBudBoxData  : null;
        bool isRolePlay = args != null && args.Length > 2 && args[2] is bool b && b;

        if (_character == null || _box == null)
        {
            LoggerUtils.LogError("[PorVideoCallNodePanel] OnShow 参数无效：character 或 box 为 null");
            CloseSelf();
            return;
        }

        AkSoundManager.Inst.SetBGMAudioVolume(0);
        _shouldRestoreBgm = false;

        callNode.onMinimize          = CloseSelf;
        callNode.onCallFailed        = () => { _shouldRestoreBgm = true; CloseSelf(); };
        callNode.onCallEnded         = () => { _shouldRestoreBgm = true; CloseSelf(); };
        callNode.onSwitchingToRolePlay = () => { _shouldRestoreBgm = true; };

        callNode.Init(_character, _box, isRolePlay);
        callNode.gameObject.SetActive(true);
    }

    public override void OnHidden()
    {
        base.OnHidden();

        if (_shouldRestoreBgm)
        {
            AkSoundManager.Inst.SetBGMAudioVolume(GlobalSettingManager.Inst.GetBgmVolume());
            _shouldRestoreBgm = false;
        }

        if (callNode != null)
        {
            callNode.onMinimize            = null;
            callNode.onCallFailed          = null;
            callNode.onCallEnded           = null;
            callNode.onSwitchingToRolePlay = null;
        }
    }
}
