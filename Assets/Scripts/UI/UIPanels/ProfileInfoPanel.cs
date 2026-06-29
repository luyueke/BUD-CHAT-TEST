using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ProfileInfoPanel : MonoBehaviour
{
    private CButton AvatarBtn;
    private RemoteImageBehaviour AvatarImg;
    private Text UserNick;
    private Text UserID;

    private Action<string> AvatarAction;
    private string ToUserId = "";
    private void Awake()
    {
        InitUIIfNeed();
        
        AvatarBtn.onClick.AddListener(OnClickAvatar);
    }

    private void InitUIIfNeed()
    {
        if (AvatarBtn != null)
        {
            return;
        }
        AvatarBtn = GameObjectEx.FindChildByName(transform, "ActionBg").GetComponent<CButton>();
        
        UserNick = GameObjectEx.FindChildByName(transform, "UserName").GetComponent<Text>();
        UserID = GameObjectEx.FindChildByName(transform, "UserId").GetComponent<Text>();
        AvatarImg = GameObjectEx.FindChildByName(transform, "RawImage").GetComponent<RemoteImageBehaviour>();
        LoggerUtils.Log("-------- InitUIIfNeed end---------");
    }

    private void OnClickAvatar()
    {
        if (string.IsNullOrEmpty(ToUserId))
        {
            LoggerUtils.Log("暂无赋值");
            return;
        }
        AvatarAction?.Invoke(ToUserId);
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="userInfo"></param>
    /// <typeparam name="T"></typeparam>
    public void SetUserInfo<T>(UserInfoProtocol userInfo) where T: UserInfoProtocol
    {
        InitUIIfNeed();
        ToUserId = userInfo.uid;
        UserNick.text = userInfo.nickname;
        UserID.text = "ID: " + userInfo.username;
        // TODO: 更新头像
        var path = userInfo.portraitUrl;
        if (!string.IsNullOrEmpty(path))
        {
            AvatarImg.Load(path, true, (from, success) => { });
        }
    }

    /// <summary>
    /// 注册头像点击响应， 返回uid
    /// </summary>
    /// <param name="avatarAction"></param>
    public void RegisterAvatarListener(Action<string> avatarAction)
    {
        AvatarAction = avatarAction;
    }
    
}
