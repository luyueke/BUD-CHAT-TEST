using Es;
using Game.Avatar;
using System;
using UI.UIPanels.IncubationCabin;
using UnityEngine.UI;

public class InteractNode3RoleItem : BaseInteractRoleItem<voiceCommands>
{
    public Button editCommandBtn;
    public Action<string> onCommandChanged;

    protected override string ItemTitle => "指令互动";
    protected override string DataEmoteId => _data.emoteId;
    protected override bool DataHasUgcData => _data.ugcData != null;
    protected override string DataUgcCoverUrl => _data.ugcData?.cover;
    protected override int DataIsMute
    {
        get => _data.isMute;
        set => _data.isMute = value;
    }
    protected override int DataDelaySecond
    {
        get => _data.delaySecond;
        set => _data.delaySecond = value;
    }
    protected override string DataText => _data.text;
    protected override string DataAudioUrl => _data.audioUrl;

    // 初始化指令按钮：点击后弹出重命名弹窗，确认时同步本地数据并通知外部
    protected override void OnInitExtra(voiceCommands data)
    {
        RefreshCommandBtnText(data.command);
        editCommandBtn.onClick.RemoveAllListeners();
        editCommandBtn.onClick.AddListener(() =>
        {
            var pop = UIManager.Inst.OpenPanel<IncubationReNamePop>(PanelId.IncubationReNamePop);
            pop.SetData(
                onConfirm: (input) =>
                {
                    if (input == null || input.Length < 3 || input.Length > 25)
                    {
                        TipPanel.ShowToast("字数不符合要求，需在3-25字以内");
                        return;
                    }

                    _data.command = input;
                    RefreshCommandBtnText(input);
                    onCommandChanged?.Invoke(input);
                },
                currentName: data.command,
                placeholder: "输入你的指令",
                title: "输入指令"
            );
        });
    }

    // 口令为空时显示引导文本，超过15字时截断并追加省略号，否则直接展示
    private void RefreshCommandBtnText(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            common_titleTxt.text = "点击设置指令";
            return;
        }

        common_titleTxt.text = command.Length > 15 ? command.Substring(0, 15) + "..." : command;
    }

    // 广播预览消息，驱动角色播放对应口令互动动画
    protected override void NetPreview()
    {
        CabinNetManager.Inst.PreviewActivation(_data);
    }

    // 本地从 voiceCommands 列表移除当前条目并广播 UI 刷新
    protected override void LocalDelete()
    {
        CabinNetManager.Inst.DeleteVoiceCommands(_characterUgcInfo, _idx);
    }

    // 本地更新语音文本和音频 url，不发网络请求
    protected override void LocalModifyTextId(string text, string url)
    {
        _data.text = text;
        _data.audioUrl = url;
        CabinNetManager.Inst.RefreshInteractContent();
    }

    protected override void OnOpenLayer3DeleteBtnClick()
    {
        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == DataEmoteId);
        if (emoAniDataList == null || emoAniDataList.Count == 0)
        {
            // return;
        }
        string tipStr = string.Format("确认删除【{0}】吗？", _data.command);
        CommonBoxConfirmWithTitlePanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
        commonConfirmPanel.SetLocalText("提示", tipStr, "确认", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            if (onDeleteConfirm != null) { onDeleteConfirm.Invoke(); return; }
            LocalDelete();
        }, () => { });
        commonConfirmPanel.HideCloseBtn();
    }

    protected override void OnOpenLayer1IconBtnClick()
    {
        var caracterData = CharacterData.DeserializeObject(_characterUgcInfo.skinPack?[0].avatarJson);
        var panel = UIManager.Inst.OpenPanel<IncubationCabinEmotePopPanel>(PanelId.IncubationCabinEmotePopPanel, false, EmoteTabType.Official | EmoteTabType.Community, caracterData, false, 4);
        SetupEmotePanelAction(panel);
    }
}
