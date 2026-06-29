using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreAvatarItem : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour avatar;
    [SerializeField] private Text avatarName;
    [SerializeField] private Button avatarBtn;

    public Action<OCTheatreAvatarOc> onActorClick;
    private OCTheatreAvatarOc avatarInfo;

    private void Start() {
        avatarBtn.onClick.AddListener(OnAvatarBtnClick);
    }

    private void OnAvatarBtnClick()
    {
        if (avatarInfo == null) return;
        onActorClick?.Invoke(avatarInfo);
    }

    public void SetData(OCTheatreAvatarOc avatarInfo)
    {
        this.avatarInfo = avatarInfo;
        avatarName.text = avatarInfo.avatarName;
        avatar.Load(avatarInfo.avatarURL);
    }

    public void SetData(string name, string url)
    {
        avatarName.text = name;
        avatar.Load(url);
    }
}
