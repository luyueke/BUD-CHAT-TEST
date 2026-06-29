
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class RewardBundleItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image Image;
    [SerializeField] GameObject ownedRoot;
    [SerializeField] RawImage colorImg;

    public void Awake()
    {
        button.onClick.AddListener(() =>
        {

        });
    }

    public void SetData(ActivityRewardInfo rewardInfo)
    {
        Image.sprite = PgcUtils.GetIconSpriteByPgcId(rewardInfo.pgcId, gameObject);
    }
    
    public void SetData(string pgcId, string bgColor = null)
    {
        Image.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
        if (bgColor != null)
        {
            colorImg.color =DataUtil.DeSerializeColorByHex(bgColor);
        }
    }
}
