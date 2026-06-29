using System;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CommonRewardItem : MonoBehaviour {


    [SerializeField]
    protected Image bgImage;

    [SerializeField]
    protected GameObject doneObj;

    [SerializeField]
    protected GameObject lockObj;

    [SerializeField]
    public Image iconImage;

    [SerializeField]
    protected Text numText;

    [SerializeField]
    protected Text progressText;

    protected Action<CommonRewardItem> onClaimClicked;

    public CommonRewardItemData rewardData;

    [HideInInspector]
    public int itemId;


    public void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnClaimClicked);
    }

    private void OnClaimClicked() {
        onClaimClicked?.Invoke(this);
    }

    public virtual void Init(int id, Action<CommonRewardItem> callBack) {
        Init(id, rewardData, callBack);
    }

    public virtual void Init(int id, CommonRewardItemData data, Action<CommonRewardItem> callBack) {
        onClaimClicked = callBack;
        itemId = id;
        rewardData = data;
        if (rewardData.IconSp != null) {
            iconImage.sprite = rewardData.IconSp;
        }
        // 若有贴图，则默认使用设置贴图
        if (iconImage.sprite == null) {
            iconImage.gameObject.SetActive(false);
            if (data.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(data.pgcId)) {
                PgcUtils.GetIconSpriteByPgcIdAsync(data.pgcId, gameObject, sp => {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = sp;
                    rewardData.IconSp = sp;
                });
            }else if (data.rewardType == (int)BUDRewardType.RewardAvatarFrame)
            {
                UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.pgcId, gameObject, sp => {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = sp;
                    rewardData.IconSp = sp;
                });
            } else if (data.rewardType == (int)BUDRewardType.RewardPgcBundle && !string.IsNullOrEmpty(data.bundleId)) {
                PgcUtils.LoadBundleIconAsync(data.bundleId, gameObject, sp => {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = sp;
                    rewardData.IconSp = sp;
                });
            }
            else if (data.rewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(data.pgcId))
            {
                UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.pgcId, gameObject, sp =>
                {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = sp;
                    rewardData.IconSp = sp;
 
                });
            }
            else if (data.rewardType == (int)BUDRewardType.RewardHomepageSkin && !string.IsNullOrEmpty(data.pgcId))
            {   //主页皮肤
                var sp = ProfileThemeManager.Inst.LoadThemeIcon(int.Parse(data.pgcId), gameObject);
                if (sp != null)
                {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = sp;
                    rewardData.IconSp = sp;
                }
            }
            else if (data.rewardType != (int)BUDRewardType.ErrRewardType)
            {
                iconImage.gameObject.SetActive(true);
                rewardData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
                iconImage.sprite = rewardData.IconSp;
            }
        } else {
            if (rewardData.IconSp == null) {
                rewardData.IconSp = iconImage.sprite;
            }
        }

        if (rewardData.rewardType == (int)BUDRewardType.RewardVipFreeTrail) {
            numText.SetLocalText(data.rewardName);
        } else {
            if (rewardData.RewardAmount > 1) {
                numText.text = $"x{rewardData.RewardAmount}";
            } else {
                numText.text = "";
            }
        }

    }

    public void SetProcess(int process) {
        progressText.SetText(process.ToString());
    }


    public virtual void SetStatus(ClaimStatus status) {
        lockObj.SetActive(false);
        doneObj.SetActive(false);
        switch (status) {
            case ClaimStatus.Claimed:
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                break;
            case ClaimStatus.Lock:
                lockObj.SetActive(true);
                break;
        }
    }

#if UNITY_EDITOR
    public void Reset() {
        bgImage = GameObjectEx.FindComponentByName<Image>(transform, "BG");
        doneObj = GameObjectEx.FindChildByName(transform, "DoneObj")?.gameObject;
        lockObj = GameObjectEx.FindChildByName(transform, "LockObj")?.gameObject;
        iconImage = GameObjectEx.FindComponentByName<Image>(transform, "Icon");
        numText = GameObjectEx.FindComponentByName<Text>(transform, "NumText");
        progressText = GameObjectEx.FindComponentByName<Text>(transform, "Image/Text (Legacy)");
    }
#endif


}
