using System;
using Game.Store;
using Network.Message;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ActivityRewardItemView : MonoBehaviour
{
    [SerializeField] private Color selectColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Image bgImage;
    [SerializeField] private Button actionBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private Text priceNum;
    [SerializeField] private Image priceIcon;
    [SerializeField] private Image colorBg;

    [SerializeField] private GameObject infoRootObj;
    [SerializeField] private GameObject priceObj;
    [SerializeField] private GameObject ownedObj;

    [SerializeField] private GameObject infoObj;

    [SerializeField] private Text rewardNumText;
    [SerializeField] private Text rewardNumTextBig;
    [SerializeField] private Text previewRewardNumText;

    private ActivityRewardInfo rewardInfo;
    private Action<ActivityRewardInfo> clickAction;
    private ActivityId activityId;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";

    

    private void Awake()
    {
        actionBtn?.onClick.AddListener(OnClickItem);
    }

    public int RewardId
    {
        get
        {
            return rewardInfo?.rewardId ?? -1;
        }
    }


    public void SetPreviewData(ActivityId activityId, Color bgColor, ActivityRewardInfo rewardInfo, Action<ActivityRewardInfo> action,string atlasPath) {
        this.activityId = activityId;
        this.rewardInfo = rewardInfo;
        clickAction = action;


        // 兼容以前数据
        string pgcId = null;
        bool isOwned = false;
        if (rewardInfo.budRewardType == (int)(BUDRewardType.ErrRewardType)) {
            pgcId = rewardInfo.rewardType;
        } else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcResource) {
            pgcId = rewardInfo.pgcId;
        } else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardAvatarFrame )
        {
            pgcId = rewardInfo.pgcId;
        }
        else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcBundle)
        {
            pgcId = rewardInfo.pgcId;
        }
        else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeCameraPose)
        {
            pgcId = rewardInfo.pgcId;
        }
        else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
        {
            pgcId = rewardInfo.pgcId;
        }
        else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeTitle)
        {
            pgcId = rewardInfo.pgcId;
        }
        if (!string.IsNullOrEmpty(pgcId))
        {
    
            if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardAvatarFrame)
            {
                UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(rewardInfo.pgcId, gameObject, sp => {
                    iconImg.sprite = sp;
                });
            }
            else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
            {
                UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(rewardInfo.pgcId, gameObject,
                    sp => { iconImg.sprite = sp; });
            }
            else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeTitle)
            {
                var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(rewardInfo.pgcId);
                if (titleData != null)
                    iconImg.sprite = Loader.Load<Sprite>(titleData.Icon, gameObject);
            }
            else if(this.rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcBundle)
            {
                iconImg.sprite = PgcUtils.LoadBundleIcon(rewardInfo.bundleId, gameObject);
                previewRewardNumText.gameObject.SetActive(false);
            }
            else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeCameraPose )
            {
                iconImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardInfo.rewardIcon, gameObject);
                previewRewardNumText.gameObject.SetActive(false);
            }
            else
            {
                iconImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
                previewRewardNumText.gameObject.SetActive(false);
            }
        } else {
            if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardTypeSelfDefine)
            {
                iconImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardInfo.rewardIcon, gameObject);
            }
            else
            {
                iconImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.budRewardType, gameObject);
            }
              
            if (rewardInfo.rewardNum > 1) {
                previewRewardNumText.gameObject.SetActive(true);
                previewRewardNumText.SetText($"x {rewardInfo.rewardNum}");
            } else {
                previewRewardNumText.gameObject.SetActive(false);
            }
        }

        infoObj.gameObject.SetActive(false);
        colorBg.color = bgColor;
    }

    public void SetData(ActivityId activityId,ActivityRewardInfo rewardInfo, Action<ActivityRewardInfo> action)
    {
        this.activityId = activityId;
        this.rewardInfo = rewardInfo;
        clickAction = action;


        // 兼容以前数据
        string pgcId = null;
        bool isOwned = false;
        if (rewardInfo.budRewardType == (int)(BUDRewardType.ErrRewardType)) {
            pgcId = rewardInfo.rewardType;
        } else if (this.rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcResource) {
            pgcId = rewardInfo.pgcId;
        }


        if (!string.IsNullOrEmpty(pgcId))
        {
            iconImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
            isOwned = AssetsDataManager.IsOwned(pgcId);
            rewardNumText.gameObject.SetActive(false);
        } else {
            iconImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.budRewardType, gameObject);
            isOwned = rewardInfo.rewardStatus == 1;
            if (rewardInfo.rewardNum > 1) {
                rewardNumText.gameObject.SetActive(true);
                rewardNumText.SetText($"x {rewardInfo.rewardNum}");
            } else {
                rewardNumText.gameObject.SetActive(false);
            }
        }

        priceNum.text = rewardInfo.spendNum.ToString();
        priceObj.SetActive(!isOwned);
        ownedObj.SetActive(isOwned);

        string iconName = "icn_common_piano_big";
        string color = "#E577CD";
        if (this.activityId == ActivityId.AnimationStudio) {
            iconName = "icn_reward_movie_big";
            color = "#E39659";
        }
        else if (this.activityId == ActivityId.WinterCarnival) {
            color = "#71D6F8";
            infoRootObj.gameObject.SetActive(false);
        }

        priceIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);
        colorBg.color = DataUtil.DeSerializeColorCheckHash(color);

        if (this.activityId == ActivityId.WinterCarnival)
        {
            rewardNumText.gameObject.SetActive(false);
            rewardNumTextBig.gameObject.SetActive(true);
            if (rewardInfo.rewardNum > 1) {
                rewardNumTextBig.SetText($"x{rewardInfo.rewardNum}");
            }
        }
    }

    public void SetSelect(bool isSelect)
    {
        bgImage.color = isSelect ? selectColor : normalColor;
    }

    private void OnClickItem()
    {
        if (rewardInfo == null)
        {
            return;
        }

        clickAction?.Invoke(rewardInfo);
    }

    public void SetOwnedUI()
    {
        priceObj.SetActive(false);
        ownedObj.SetActive(true);
    }
}
