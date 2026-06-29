using System;
using GameSync.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TheatreAccpetInviteItem : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private Text inviteMessageText;
    [SerializeField] private Button acceptBtn;
    [SerializeField] private Button cancelBtn;

    public string Key { get; private set; }

    public void SetData(string hostUid, string theatreName, string roomId,
        Action<string, string> onAccept, Action onCancel)
    {
        Key = $"{hostUid}:{roomId}";
        acceptBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.RemoveAllListeners();
        acceptBtn.onClick.AddListener(() => onAccept?.Invoke(hostUid, roomId));
        cancelBtn.onClick.AddListener(() => onCancel?.Invoke());

        string playerName = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoById(hostUid)?.Name ?? hostUid;
        if (inviteMessageText != null)
            inviteMessageText.text = $"<color=#FFAF2D>【{playerName}】</color>邀请你参演<color=#FFAF2D>【{theatreName}】</color>";
    }

    public void SetHighlight(bool highlighted)
    {
        if (bgImage != null)
            bgImage.color = highlighted ? Color.white : new Color(0x8E / 255f, 0x8E / 255f, 0x8E / 255f);
    }
}
