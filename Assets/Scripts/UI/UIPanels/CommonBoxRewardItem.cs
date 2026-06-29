using Com.TheFallenGames.OSA.Util.IO;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CommonBoxRewardItem : MonoBehaviour
{
    [SerializeField] private Image IconImage;
    [SerializeField] private RemoteImageBehaviour UgcImage;
    [SerializeField] private Text CountText;
    [SerializeField] private Image TagImage;

    public void Init(CommonRewardItemData data, Sprite tagSprite = null, Sprite iconOverride = null)
    {
        if (!string.IsNullOrEmpty(data.rewardSpecial))
        {
            CountText.gameObject.SetActive(true);
            CountText.text = data.rewardSpecial;
        }
        else if (data.RewardAmount > 0)
        {
            CountText.gameObject.SetActive(true);
            CountText.text = "x" + data.RewardAmount;
        }
        else
        {
            CountText.gameObject.SetActive(false);
        }

        // iconOverride 优先（特殊图标，如 aiCredit）
        if (iconOverride != null)
        {
            UgcImage.gameObject.SetActive(false);
            IconImage.gameObject.SetActive(true);
            IconImage.sprite = iconOverride;
            IconImage.SetNativeSize();
        }
        else if (!string.IsNullOrEmpty(data.UgcCover))
        {
            IconImage.gameObject.SetActive(false);
            UgcImage.gameObject.SetActive(true);
            UgcImage.Load(data.UgcCover, true, (_, success) =>
            {
                if (!success)
                {
                    UgcImage.gameObject.SetActive(false);
                    IconImage.gameObject.SetActive(true);
                    IconImage.sprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardUgcResource, gameObject);
                }
            });
        }
        else if (data.IconSp != null)
        {
            UgcImage.gameObject.SetActive(false);
            IconImage.gameObject.SetActive(true);
            IconImage.sprite = data.IconSp;
            IconImage.SetNativeSize();
        }
        else
        {
            UgcImage.gameObject.SetActive(false);
            IconImage.gameObject.SetActive(true);
            if (data.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(data.pgcId))
            {
                PgcUtils.GetIconSpriteByPgcIdAsync(data.pgcId, gameObject, sp => IconImage.sprite = sp);
            }
            else if (data.rewardType != (int)BUDRewardType.ErrRewardType)
            {
                IconImage.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
            }
        }

        // 左上角角标
        if (TagImage != null)
        {
            if (tagSprite != null)
            {
                TagImage.gameObject.SetActive(true);
                TagImage.sprite = tagSprite;
            }
            else
            {
                TagImage.gameObject.SetActive(false);
            }
        }
    }
}
