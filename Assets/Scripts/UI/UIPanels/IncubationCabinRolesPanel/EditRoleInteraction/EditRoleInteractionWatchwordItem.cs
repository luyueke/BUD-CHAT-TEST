/// <summary>
/// 口令互动列表项，继承自 EditRoleInteractionBaseItem。
/// 最多可启用 <see cref="CabinConfig.EditViewVoiceCommandEnabledMax"/> 个（disabled=0 的条目数）。
/// </summary>
public class EditRoleInteractionWatchwordItem : EditRoleInteractionBaseItem
{
    /// <summary>
    /// 初始化口令互动列表项，绑定数据委托和网络操作委托，并设置启用数量上限检查。
    /// </summary>
    /// <param name="characterUgcInfo">当前角色信息，用于读取 voiceCommands 列表计算启用数量</param>
    /// <param name="data">本条口令互动数据</param>
    /// <param name="idx">在 voiceCommands 列表中的索引</param>
    /// <param name="isOpen">初始折叠状态</param>
    public void Init(CabinCharacterUgcInfo characterUgcInfo, voiceCommands data, int idx, bool isOpen)
    {
        _getEmoteId      = () => data.emoteId;
        _getUgcData      = () => data.ugcData;
        _getDelaySecond  = () => data.delaySecond;
        _setDelaySecond  = v  => data.delaySecond = v;
        _getIsMute       = () => data.isMute;
        _setIsMute       = v  => data.isMute = v;
        _getDisabled     = () => data.disabled;
        _setDisabled     = v  => data.disabled = v;
        _getText         = () => data.text;
        _onModifyMute        = CabinRolesNetManager.Inst.ModifyVoiceCommandsMute;
        _onModifyDelaySecond = CabinRolesNetManager.Inst.ModifyVoiceCommandsDelaySecond;
        _onModifyEmote       = CabinRolesNetManager.Inst.ModifyVoiceCommandsEmote;
        _onModifyTextId      = CabinRolesNetManager.Inst.ModifyVoiceCommandsTextId;
        _onDelete            = CabinRolesNetManager.Inst.DeleteVoiceCommands;
        _onPreview           = () => CabinRolesNetManager.Inst.PreviewActivation(data);

        // 捕获 characterUgcInfo 以在 lambda 中安全访问 voiceCommands 列表
        var capturedInfo = characterUgcInfo;

        // 启用前检查：已启用数量不超过上限（不含当前条目自身，因为此刻它还处于禁用状态）
        _canEnable = () =>
        {
            if (capturedInfo?.voiceCommands == null)
                return true;

            int enabledCount = 0;
            foreach (var vc in capturedInfo.voiceCommands)
            {
                if (vc.disabled != 1)
                {
                    enabledCount++;
                }
            }

            return enabledCount < CabinConfig.EditViewVoiceCommandEnabledMax;
        };
        _enableLimitMsg = $"指令动作最多启用{CabinConfig.EditViewVoiceCommandEnabledMax}个";

        InitCommon(characterUgcInfo, characterUgcInfo.toneId, "口令", idx, isOpen);

        // 口令 item 标题直接显示用户自定义的口令内容，而非 "口令:序号"
        common_titleTxt.text = data.command;
    }
}
