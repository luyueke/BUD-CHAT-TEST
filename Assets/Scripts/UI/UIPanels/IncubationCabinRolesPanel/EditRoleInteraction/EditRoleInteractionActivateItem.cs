/// <summary>
/// 唤醒动作列表项，继承自 EditRoleInteractionBaseItem。
/// 最多可启用 <see cref="CabinConfig.EditViewActivationEnabledMax"/> 个（disabled=0 的条目数）。
/// </summary>
public class EditRoleInteractionActivateItem : EditRoleInteractionBaseItem
{
    /// <summary>
    /// 初始化唤醒动作列表项，绑定数据委托和网络操作委托，并设置启用数量上限检查。
    /// </summary>
    /// <param name="_characterInfo">当前角色信息，用于读取 activation 列表计算启用数量</param>
    /// <param name="data">本条唤醒动作数据</param>
    /// <param name="idx">在 activation 列表中的索引</param>
    /// <param name="isOpen">初始折叠状态</param>
    public void Init(CabinCharacterUgcInfo _characterInfo, characterInteraction data, int idx, bool isOpen)
    {
        _getEmoteId = () => data.emoteId;
        _getUgcData = () => data.ugcData;
        _getDelaySecond = () => data.delaySecond;
        _setDelaySecond = v => data.delaySecond = v;
        _getIsMute = () => data.isMute;
        _setIsMute = v => data.isMute = v;
        _getDisabled = () => data.disabled;
        _setDisabled = v => data.disabled = v;
        _getText = () => data.text;
        _onModifyMute = CabinRolesNetManager.Inst.ModifyActivationMute;
        _onModifyDelaySecond = CabinRolesNetManager.Inst.ModifyActivationDelaySecond;
        _onModifyEmote = CabinRolesNetManager.Inst.ModifyActivationEmote;
        _onModifyTextId = CabinRolesNetManager.Inst.ModifyActivationTextId;
        _onDelete = CabinRolesNetManager.Inst.DeleteActivation;
        _onPreview = () => CabinRolesNetManager.Inst.PreviewActivation(data);

        // 捕获 _characterInfo 以在 lambda 中安全访问 activation 列表
        var capturedInfo = _characterInfo;

        // 启用前检查：已启用数量不超过上限（不含当前条目自身，因为此刻它还处于禁用状态）
        _canEnable = () =>
        {
            if (capturedInfo?.activation == null)
                return true;

            int enabledCount = 0;
            foreach (var a in capturedInfo.activation)
            {
                if (a.disabled != 1)
                {
                    enabledCount++;
                }
            }

            return enabledCount < CabinConfig.EditViewActivationEnabledMax;
        };
        _enableLimitMsg = $"唤醒动作最多启用{CabinConfig.EditViewActivationEnabledMax}个";

        InitCommon(_characterInfo, _characterInfo.toneId, "唤醒动作", idx, isOpen);
    }
}
