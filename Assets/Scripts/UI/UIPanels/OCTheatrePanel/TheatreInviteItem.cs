using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;

public class TheatreInviteItem : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour avatar;
    [SerializeField] private Text nickName;
    [SerializeField] private Button inviteBtn;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Text btnText;

    private string _uid;
    private Action<string> _onInvite;

    public void SetData(string uid, string nickname, string portraitUrl, Action<string> onInvite)
    {
        _uid = uid;
        _onInvite = onInvite;
        nickName.text = nickname;
        avatar.Load(portraitUrl);
        inviteBtn.interactable = true;
        if (inviteBtn.image != null) inviteBtn.image.sprite = activeSprite;
        if (btnText != null) btnText.text = "邀请";
        inviteBtn.onClick.RemoveAllListeners();
        inviteBtn.onClick.AddListener(() => TryInvite());
    }

    public void TryInvite()
    {
        if (!inviteBtn.interactable) return;
        _onInvite?.Invoke(_uid);
        inviteBtn.interactable = false;
        if (inviteBtn.image != null) inviteBtn.image.sprite = inactiveSprite;
        if (btnText != null) btnText.text = "已邀请";
    }
}
