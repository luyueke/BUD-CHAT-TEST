using Game.Avatar;
using UI.UIPanels.IncubationCabin;

public class InteractNode2RoleItem : BaseInteractRoleItem<characterInteraction>
{
    protected override string ItemTitle => "唤醒动作";
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

    // 广播预览消息，驱动角色播放对应唤醒动作动画
    protected override void NetPreview()
    {
        CabinNetManager.Inst.PreviewActivation(_data);
    }

    protected override void LocalDelete()
    {
        CabinNetManager.Inst.DeleteActivation(_characterUgcInfo, _idx);
    }

    // 本地更新语音文本和音频 url，不发网络请求
    protected override void LocalModifyTextId(string text, string url)
    {
        _data.text = text;
        _data.audioUrl = url;
        CabinNetManager.Inst.RefreshInteractContent();
    }

    protected override void OnOpenLayer1IconBtnClick()
    {
        var caracterData = CharacterData.DeserializeObject(_characterUgcInfo.skinPack?[0].avatarJson);
        var panel = UIManager.Inst.OpenPanel<IncubationCabinEmotePopPanel>(PanelId.IncubationCabinEmotePopPanel, false, EmoteTabType.Official | EmoteTabType.Community, caracterData, false, 3);
        SetupEmotePanelAction(panel);
    }
}
