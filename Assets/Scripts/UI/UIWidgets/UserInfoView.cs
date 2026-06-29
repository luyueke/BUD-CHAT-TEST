using System;
using Basic.Utils;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace UI.UIWidgets
{
    public class UserInfoView : MonoBehaviour
    {
        [SerializeField] private CButton headBtn;
        [SerializeField] private HeadViewWidget headViewWidget;
        [SerializeField] private Transform nickBgRoot;
        [SerializeField] private SuperTextMesh nickText;
        [SerializeField] private SuperTextMesh nameText;
        [SerializeField] private GameObject onlineImg;
        [SerializeField] private Image cerView;
        [SerializeField] private Image vipIcon;
        [SerializeField] private GameObject blockedTag;
        [SerializeField] private CButton copyIdBtn;
        [SerializeField] private Sprite vipSprite;
        [SerializeField] private Sprite sVipSprite;
        [SerializeField] private Image viptagImage;
        [SerializeField] private RectTransform vipRoot;
        [SerializeField] private Image creatorTag;
        [SerializeField] private CButton creatorBtn;
        // [SerializeField] private CButton vipBtn;


        [Header("是否点击头像打开个人主页")]
        [SerializeField] public bool IsOpenProfilePanel = true;
        
        private string vipIcon01 = "vip_icon01";
        private string vipIcon23 = "vip_icon23";
        private AccountUserInfo mUserInfo;

        private Action headClickListener;

        int Id = -1;
        GameObject NickBg;

        public SuperTextMesh NickText { get { return nickText; } }
        public SuperTextMesh NameText => nameText; 
        private void Awake()
        {
            AddBtnListener();
        }
        
        private void AddBtnListener()
        {
            headBtn.onClick.AddListener(OnHeadBtnClick);
            copyIdBtn.onClick.AddListener(OnNickCopyIdClick);
            // vipBtn.onClick.AddListener((() =>
            // {
            //     if (mUserInfo == null)
            //     {
            //      return;   
            //     }
            //     
            //     var panel = UIManager.Inst.OpenPanel<ProfileTagPanel>(PanelId.ProfileTagPanel);
            //     panel.SetData(ProfileTagPanel.ProfileTagType.Vip,viptagImage.sprite,"VIP会员", "成为VIP会员时间：" + ConvertTimestampToDate(mUserInfo.userTitle.claimTime));
            // }));
            creatorBtn?.onClick.AddListener((() =>
            {
                if (mUserInfo == null&&mUserInfo.creatorLevelInfo==null)
                {
                    return;   
                }
                var panel = UIManager.Inst.OpenPanel<ProfileTagPanel>(PanelId.ProfileTagPanel);
                panel.SetData(ProfileTagPanel.ProfileTagType.Creator,creatorTag.sprite,mUserInfo.creatorLevelInfo.titleType, "获得头衔时间："+  ConvertTimestampToDate(mUserInfo.creatorLevelInfo.unlockTime));
            }));
        }

        
        public void SetData(AccountUserInfo userInfo)
        {
            mUserInfo = userInfo;
            ResetUI();
            FillViewData(userInfo);

            headViewWidget.InitHeadCycle(userInfo);
        }

        public void SetCopyIdBtnVisible(bool value)
        {
            copyIdBtn.gameObject.SetActive(value);
        }

        public void AddHeadClickListener()
        {
            headClickListener?.Invoke();
        }


        private void OnHeadBtnClick()
        {
            if (IsOpenProfilePanel && mUserInfo != null) 
            {
               UIManager.Inst.SwapPanel(PanelId.ProfilePanel, mUserInfo.uid); 
            }
        }

        private void OnNickCopyIdClick()
        {
            GUIUtility.systemCopyBuffer = mUserInfo.username;
            TipPanel.ShowToast("已复制ID");
        }

        private void FillViewData(AccountUserInfo userInfo)
        {
            if (userInfo == null) return;

            SetNickNameBg(userInfo.nicknameFrame);
            nameText.SetText("ID:" + userInfo.username);
            string nickStr = DataUtil.RemoveRichTextAndEmoji(userInfo.nickname);
            nickText.SetText(nickStr);
            AccountUserInfo.SubScribeData subScribeData = userInfo.subscribeData;
            if (subScribeData != null)
            {
                viptagImage.gameObject.SetActive(false);
                int vipType = subScribeData.vipType;
                if (vipType == (int)SubscribeVipType.MonthCard)
                {
                    viptagImage.gameObject.SetActive(true);
                    viptagImage.sprite = vipSprite;
                } else if (vipType == (int)SubscribeVipType.YearCard)
                {
                    viptagImage.gameObject.SetActive(true);
                    viptagImage.sprite = sVipSprite;
                }
            }

            if (userInfo.creatorLevelInfo!=null)
            {
                int userTitle = userInfo.creatorLevelInfo.titleType;
                if (userTitle != null)
                {
                    creatorTag.gameObject.SetActive(userInfo.creatorLevelInfo.titleType> (int)CreatorTitleType.Newbie);
                    var titleId = userTitle;
                    var atlasPath = UserUIWidgetManager.Inst._userWidgetAtlas;
                    creatorTag.sprite = titleId switch
                    {
                        (int)CreatorTitleType.NewCreator => XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath,
                            "newcreator_icon", gameObject),
                        (int)CreatorTitleType.LightChaserCreator => XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath,
                            "lightcreator_icon", gameObject),
                        (int)CreatorTitleType.PopularCreator => XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath,
                            "popularcreator_icon", gameObject),
                        _ => creatorTag.sprite
                    };
                }
            }
           
        }

        private void ResetUI()
        {
            onlineImg.SetActive(false);
            nameText.SetText("");
            nickText.SetText("");
            cerView.gameObject.SetActive(false);
            vipIcon.gameObject.SetActive(false);
            blockedTag.SetActive(false);
        }

        /// <summary>
        /// 设置UserNick长度限制
        /// </summary>
        public void SetUserNickLengthLimit(int NumberOfBytes)
        {
            string nickStr = DataUtil.RemoveRichTextAndEmoji(mUserInfo.nickname);
            nickText.SetText(GameUtils.SubStringByBytes(nickStr, NumberOfBytes));
        }

        public void SetNickNameBg(int id) {
            if (Id == id)
            {
                return;
            }
            Id = id;
            var config = UserUIWidgetManager.Inst.GetNicknameData((int)Id);
            if (config != null)
            {
                if (NickBg != null)
                {
                    GameObject.Destroy(NickBg.gameObject);
                    NickBg = null;
                }
                if (nickBgRoot != null)
                {
                    var o = Loader.Load<GameObject>(config.Prefab, gameObject);
                    NickBg = GameObject.Instantiate(o, nickBgRoot.transform);
                    NickBg.transform.SetAsFirstSibling();
                    Color textColor = DataUtil.DeSerializeColorCheckHash(config.NameColor);
                    nickText.color = textColor;
                }
            }
        }
        
        private static string ConvertTimestampToDate(long timestamp)
        {
            // 创建一个 DateTime 对象，从1970-01-01开始，加上时间戳的秒数
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

            // 将 DateTime 转换为 "yyyy-MM-dd" 格式的字符串
            string formattedDate = dateTime.ToString("yyyy-MM-dd");

            return formattedDate;
        }

        private void InitCreatorLevelTag(CreatorLevelInfo info)
        {
            if(info == null)
                return;

            var creatorLevelTag = GameObjectEx.FindChildByName(vipRoot, "CreatorLevelTag").GetComponent<Image>();
            if (creatorLevelTag == null)
                return;

            //var spName = "icon_creatorLevel_";
            //vipRoot.gameObject.SetActive(true);
            //var sp = UserInfoAtlas.GetSprite(spName + info.level);
            //creatorLevelTag.sprite = sp;
        }

 
    }
}
