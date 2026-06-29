using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class BindThirdInfo
    {
        public string rmsg; //失败原因
        public string opendId;
    }

    /// <summary>
    /// 账号关联页
    /// </summary>
    public class AccountCard : BaseCard
    {
        [SerializeField]private GameObject contentView;
        [SerializeField]private GameObject finishView;
        [SerializeField]private CButton facebookBtn;
        [SerializeField]private CButton googleBtn;
        [SerializeField]private CButton appleBtn;
        [SerializeField]private CButton snapchatBtn;
        [SerializeField]private CButton ticktockBtn;

        private AccountPlatform selectPlatform = AccountPlatform.Tourists;
        
        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            InitListener();
#if UNITY_IOS
            appleBtn.gameObject.SetActive(true);
#else
            appleBtn.gameObject.SetActive(false);
#endif
            cardBgType = ProfileCardBgType.Bg3;

        }
        

        private void InitListener()
        {
            facebookBtn.onClick.AddListener(() => OnSelectLogin(AccountPlatform.Facebook));
            googleBtn.onClick.AddListener(() => OnSelectLogin(AccountPlatform.Google));
            appleBtn.onClick.AddListener(() => OnSelectLogin(AccountPlatform.Apple));
            snapchatBtn.onClick.AddListener(() => OnSelectLogin(AccountPlatform.Snapchat));
            ticktockBtn.onClick.AddListener(() => OnSelectLogin(AccountPlatform.TikTok));
        }
        
        private void OnSelectLogin(AccountPlatform platform)
        {
            LockButtons(true);
            
#if UNITY_EDITOR
            SetFinish(true);
#endif
            
            var jb = new JObject
            {
                ["provider"] = platform.ToString(),
            };

            selectPlatform = platform;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.bindThirdAccount, OnBindSuccess);
            MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.bindThirdAccount, OnBindFail);
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.bindThirdAccount, JsonConvert.SerializeObject(jb));
        }
        
        private void OnBindSuccess(string msg)
        {
            LoggerUtils.Log("###绑定成功："+msg);
            BindThirdInfo thirdInfo = JsonConvert.DeserializeObject<BindThirdInfo>(msg);
            if (thirdInfo != null && !string.IsNullOrEmpty(thirdInfo.opendId))
            {
                AccountDataManager.Inst.UpdateAccountPlatform(selectPlatform,thirdInfo.opendId);
            }
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.bindThirdAccount);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.bindThirdAccount);
            SetFinish(true);
        }

        private void OnBindFail(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.bindThirdAccount);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.bindThirdAccount);
            LockButtons(false);
        }

        private void SetFinish(bool isFinish)
        {
            contentView.SetActive(!isFinish);
            finishView.SetActive(isFinish);
        }
        
        private void LockButtons(bool isLock)
        {
            List<CButton> buttons = new List<CButton>()
            {
                facebookBtn,
                googleBtn,
                appleBtn,
                snapchatBtn,
                ticktockBtn
            };
            for(int i = 0; i < buttons.Count; ++i)
            {
                buttons[i].SetClickAble(!isLock);
            }
        }
    }
}