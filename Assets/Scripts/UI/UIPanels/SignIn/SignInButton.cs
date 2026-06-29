using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UI.BaseWidgets;

public class SignInButton : MonoBehaviour
{

    private GameObject ContentObj;
    private Tween _loadingTween;
    private GameObject LoadingNode;
    private Transform LoadingImg;

    private bool onceFlag = false;
    protected void Awake()
    {
        InitUIOnce();
    }
    
    private void InitUIOnce()
    {
        if (onceFlag)
        {
            return;
        }

        var loadingTrans = this.transform.Find("LoadingNode");
        if (loadingTrans != null)
        {
            LoadingNode = loadingTrans.gameObject;
            LoadingImg = LoadingNode.transform.Find("LoadingImg");
        }
        else
        {
            Debug.LogError("loadingTrans is null");
        }

        var contentTrans = this.transform.Find("ContentObj");
        if (contentTrans != null)
        {
            ContentObj = contentTrans.gameObject;
        }
        else
        {
            Debug.LogError("contentTrans is null");
        }
        onceFlag = true;
    }

    protected void OnDestroy()
    {
        _loadingTween?.Kill();
    }
    
    private void SetLoadingVisible(bool value)
    {
        InitUIOnce();
        if (LoadingNode != null)
        {
            LoadingNode.SetActive(value);
        }
        _loadingTween?.Kill();
        if (value == true)
        {
            _loadingTween = LoadingImg.DOLocalRotate(new Vector3(0, 0, -720), 2f).SetLoops(-1);
        }

        if (ContentObj != null)
        {
            ContentObj.SetActive(!value);
        }
    }

    public void ShowLoading()
    {
        SetLoadingVisible(true);
    }
    
    public void HideLoading()
    {
        SetLoadingVisible(false);
    }
    
    private AccountPlatform _platform = AccountPlatform.Tourists;
    public AccountPlatform platform
    {
        get
        {
            return _platform;
        }
    }

    public void UpdateStyle(AccountPlatform platform)
    {
        _platform = platform;

        var viewW = platform == AccountPlatform.AppleAudit ? 160 : 324;
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(viewW, rt.sizeDelta.y);
        
        var textObj = GameObjectEx.FindChildByName(transform, "ContentObj/Text").GetComponent<Text>();
        var platformObj = GameObjectEx.FindChildByName(transform, "ContentObj/Platform").gameObject;
        if (platform == AccountPlatform.AppleAudit)
        {
            textObj.SetLocalText(Title(platform));
        }

        if (platform == AccountPlatform.Tourists)
        {
            var label = GameObjectEx.FindChildByName(platformObj, "PlatformText").GetComponent<Text>();
            string platformTitle = Title(platform);
            label.SetLocalText(platformTitle);
        }

        bool isTextOnly = platform == AccountPlatform.Channel || platform == AccountPlatform.Tourists || platform == AccountPlatform.AppleAudit;
        textObj.gameObject.SetActive(isTextOnly);
        platformObj.SetActive(!isTextOnly);
        if (!isTextOnly)
        {
            var icon = GameObjectEx.FindChildByName(platformObj, "Icon").GetComponent<Image>();
            var label = GameObjectEx.FindChildByName(platformObj, "PlatformText").GetComponent<Text>();
            string iconName = IconName(platform);
            string platformTitle = Title(platform);
            if (!string.IsNullOrEmpty(iconName))
            {
                var atlasPath = "Assets/Loadable/UI/UIPanel/SignIn/SignInPanel.spriteatlas";
                icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
            }
            if (!string.IsNullOrEmpty(platformTitle))
            {
                label.SetLocalText(platformTitle);
            }
        }
    }

    private string Title(AccountPlatform platform)
    {
        switch (platform)
        {
            case AccountPlatform.Apple:
                return "苹果登录";
            case AccountPlatform.Wechat:
                return "微信登录";
            case AccountPlatform.Qq:
                return "QQ 登录";
            case AccountPlatform.Douyin:
                return "抖音登录";
            case AccountPlatform.Channel:
                return "开始游戏";
            case AccountPlatform.Taptap:
                return "TapTap登录";
            case AccountPlatform.AppleAudit:
                return "Demo";
            default:
                return "开始游戏";
        }
    }
    
    private string IconName(AccountPlatform platform)
    {
        switch (platform)
        {
            case AccountPlatform.Apple:
                return "icn_lobby_app";
            case AccountPlatform.Wechat:
                return "icn_lobby_wechat";
            case AccountPlatform.Qq:
                return "icn_lobby_qq";
            case AccountPlatform.Douyin:
                return "icn_lobby_tiktok";
            case AccountPlatform.Taptap:
                return "icn_lobby_taptap";
            default: return "";
        }
    }
    
}
