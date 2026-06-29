using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

class BUDPrivacyPolicy
{
    public enum PrivacyType
    {
        Privacy, // 隐私政策
        Permission, // 用户协议
        ChildrenPolicy, // 儿童保护指引
        ThreePartyInfo, // 三方信息清单
        IcpPolicy,//icp备案号
        OfficalPage,//官网
        TermsService,//服务条款
    }
    
    /// <summary>
    /// 隐私政策
    /// </summary>
    public const string PrivacyPath = "https://buddy.budapp.cn/policy/privacy.html";
    public const string PrivacyPath_US = "https://cdn.joinbudapp.com/privacy_policy/privacy.html";
    public const string TermsOfServiceUrl_US = "https://cdn.joinbudapp.com/privacy_policy/terms.html";
    
    /// <summary>
    /// 用户协议
    /// </summary>
    public const string PermissionPath = "https://buddy.budapp.cn/policy/permission.html";
    
    public const string ChildrenPolicy = "https://buddy.budapp.cn/policy/childrenPolicy.html";
    public const string ThirdPartyList = "https://buddy.budapp.cn/policy/thirdPartyList.html";
    public const string IcpPolicyUrl = "https://beian.miit.gov.cn/#/home";
    public const string OfficialUrl = "https://www.budapp.cn";

    public static void Open(PrivacyType type)
    {
        var path = "";
        switch (type)
        {
            case PrivacyType.Permission:
                path = PermissionPath;
                break;
            case PrivacyType.Privacy:
                path = PrivacyPath;
                #if PACKAGE_TYPE_US
                path = PrivacyPath_US;
                #endif
                break;
            case PrivacyType.ChildrenPolicy:
                path = ChildrenPolicy;
                break;
            case PrivacyType.ThreePartyInfo:
                path = ThirdPartyList;
                break;
            case PrivacyType.IcpPolicy:
                path = IcpPolicyUrl;
                break;
            case PrivacyType.OfficalPage:
                path = OfficialUrl;
                break;
            case PrivacyType.TermsService:
                path = TermsOfServiceUrl_US;
                break;
        }

        if (string.IsNullOrEmpty(path))
        {
            LoggerUtils.LogError("Check PrivacyType");
            return;
        }
        
        LoggerUtils.LogFormat($"$[WebView] open webView: {path}");
        var jb = new JObject
        {
            ["url"] = path
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openWebview, JsonConvert.SerializeObject(jb));
    }
}

public class BUDPrivacyPolicyView : MonoBehaviour
{
    [SerializeField] private CButton agreeBtn;
    [SerializeField] private CButton refuseBtn;
    
    [SerializeField] private Button PrivacyBtn;
    [SerializeField] private Button PermissionBtn;
    [SerializeField] private Button PermissionBtn1;
    [SerializeField] private Button ChildrenPolicyBtn;
    [SerializeField] private Button ThreePartyInfoBtn;

    public Action AgreeAction;
    
    private static string OnceKey()
    {
        return "BUDPrivacyPolicyViewOnceKey";
    }
    
    public static bool DidShow()
    {
#if UNITY_ANDROID || PACKAGE_TYPE_US
        return true;
#endif
        return PlayerPrefs.GetInt(OnceKey()) == 1;
    }
    
    public static void SetDidShow()
    {
        PlayerPrefs.SetInt(OnceKey(), 1);
        PlayerPrefs.Save();
    }
    
    // "  感谢你选择碧优蒂的世界。相关服务将由深圳零点一娱乐科技有限公司提供。我们非常重视您的个人信息和隐私保护。为了更好的保障您的个人权益，在使用我们的服务前，请您务必打开链接并认真阅读<color=#905CFF>《用户协议》、《隐私政策》、《儿童隐私保护指引》</color>与<color=#905CFF>《第三方信息共享清单》</color>的全部内容，在您同意并接受全部条款后方可开始使用我们的服务。\n    在您点击页面的“同意”按钮后，我们将进行集成SDK的初始化工作，会收集您的IMEI、IMSI、设备型号、设备品牌、系统版本和应用列表，已保证游戏正常数据统计和安全风控。\n         如您有任何问题，请通过邮件contact@pointone.tech联系我们，我们会及时帮助您。\n"
    private void Awake()
    {
        agreeBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicyView.SetDidShow();
            gameObject.SetActive(false);
            AgreeAction?.Invoke();
        });
        
        refuseBtn?.onClick.AddListener(() =>
        {
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.killApp, "");
        });
        
        PrivacyBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Privacy);
        });
        
        PermissionBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Permission);
        });
        
        PermissionBtn1?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Permission);
        });
        
        ChildrenPolicyBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ChildrenPolicy);
        });
        
        ThreePartyInfoBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ThreePartyInfo);
        });
    }
}
