using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public enum ProfileEditType
    {
        ChangeHead = 0,
        ChangeNick,
        EditBio,
        ChangeTheme,
        ChangeHeadCycle,
        ChangeChatBubble,
        ChangeNicknameBg,
        ChangeTitleBg,
    }
    
    public struct ProfileEditConfig
    {
        public ProfileEditType EditType;
        public string ShowName;
    }
    
    public class ProfileEditBoardView : MonoBehaviour
    {
        [SerializeField] private Transform mContent;
        [SerializeField] private ProfileEditItem ItemPrefab;
        [SerializeField] private Button bgBtn;

        private List<ProfileEditConfig> ItemConfigs = new List<ProfileEditConfig>()
        {
            new ProfileEditConfig{EditType = ProfileEditType.ChangeHead,ShowName  = "更换头像"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeNick,ShowName  = "更换昵称"},
            new ProfileEditConfig{EditType = ProfileEditType.EditBio,ShowName  = "编辑简介"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeTheme,ShowName  = "选择主页皮肤"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeHeadCycle,ShowName  = "设置头像框"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeChatBubble,ShowName  = "设置聊天气泡"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeNicknameBg,ShowName  = "设置昵称背景"},
            new ProfileEditConfig{EditType = ProfileEditType.ChangeTitleBg,ShowName  = "设置称号"},
        };
        
        
        private List<ProfileEditItem> items = new List<ProfileEditItem>();

        private void Awake()
        {
            bgBtn.onClick.AddListener(OnBgBtnClick);
            InitView();
            
        }

        private void InitView()
        {
            items.Clear();
            foreach (var config in ItemConfigs)
            {
                var item = CreateItem();
                item.SetData(config.ShowName,config.EditType);
                item.AddClickListener(() =>
                {
                    OnItemClick(config.EditType);
                });
                items.Add(item);
            }
        }


        private ProfileEditItem CreateItem()
        {
            var newItem = Instantiate(ItemPrefab,mContent);
            newItem.gameObject.SetActive(true);
            return newItem;
        }
        
        
        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
            if (isVisible)
            {
                AddClientListener();
            }
            else
            {
                RemoveListener();
            }
        }


        private void OnItemClick(ProfileEditType editType)
        {
            switch (editType)
            {
                case  ProfileEditType.ChangeHead:
                    UIManager.Inst.OpenPanel(PanelId.ChangeHeadImgPanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.ChangeNick:
                    UIManager.Inst.OpenPanel(PanelId.ChangeNickNamePanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.EditBio:
                    UIManager.Inst.OpenPanel(PanelId.EditDescPanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.ChangeTheme:
                    UIManager.Inst.OpenPanel(PanelId.ChangeThemePanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.ChangeHeadCycle: 
                    var changeHeadCyclePanel = UIManager.Inst.OpenPanel<ChangeHeadCyclePanel>(PanelId.ChangeHeadCyclePanel, AccountDataManager.Inst.UserInfo);
                    changeHeadCyclePanel.SetAction();
                    break;
                case ProfileEditType.ChangeChatBubble:
                    var changeChatBubblePanel = UIManager.Inst.OpenPanel<ChangeChatBubblePanel>(PanelId.ChangeChatBubblePanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.ChangeNicknameBg:
                    UIManager.Inst.OpenPanel<ChangeNicknameBgPanel>(PanelId.ChangeNicknameBgPanel, AccountDataManager.Inst.UserInfo);
                    break;
                case ProfileEditType.ChangeTitleBg:
                    Debug.LogError("ownedTitleList =" + Newtonsoft.Json.JsonConvert.SerializeObject(AccountDataManager.Inst.UserInfo.ownedTitleList));
                    UIManager.Inst.OpenPanel<ChangeTitleBgPanel>(PanelId.ChangeTitleBgPanel, AccountDataManager.Inst.UserInfo);
                    break;
            }
        }

        private void AddClientListener()
        {
            // MobileInterface.Instance.AddClientRespose(MobileInterface.verifySocialThirdParty, verifySocialThirdParty);
            // MobileInterface.Instance.AddClientRespose(MobileInterface.getBirthday, getBirthday);
            // MobileInterface.Instance.AddClientRespose(MobileInterface.linkThirdAccount, OnLinkThirdAccountSuccess);
        }

        private void RemoveListener()
        {
            // MobileInterface.Instance.DelClientResponse(MobileInterface.verifySocialThirdParty);
            // MobileInterface.Instance.DelClientResponse(MobileInterface.getBirthday);
            // MobileInterface.Instance.DelClientResponse(MobileInterface.linkThirdAccount);
        }

        private void OnBgBtnClick()
        {
            SetVisible(false);
        }

    }
}
