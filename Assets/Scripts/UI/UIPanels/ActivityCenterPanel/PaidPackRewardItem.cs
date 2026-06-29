using System;

using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PaidPackRewardItem : MonoBehaviour
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

    private Action<PaidPackRewardData> clickAction;

    private PaidPackRewardData paidPackRewardData;

    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";

    private void Awake()
    {
        actionBtn?.onClick.AddListener(OnClickItem);
    }

    public PaidPackRewardData PaidPackRewardData
    {
        get { return paidPackRewardData; }
    }


    public void SetPreviewData(PaidPackRewardData paidPackRewardData, Color bgColor, Action<PaidPackRewardData> action)
    {
        clickAction = action;

        this.paidPackRewardData = paidPackRewardData;
        bool isOwned = false;

        if (!paidPackRewardData.isBundle)
        {
            if (paidPackRewardData.pgcIds.Count <= 0)
            {
                return;
            }

            string pgcId = paidPackRewardData.pgcIds[0];
            if (!string.IsNullOrEmpty(pgcId))
            {
                iconImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
                previewRewardNumText.gameObject.SetActive(false);
            }
        }
        else
        {
            string bundleId = paidPackRewardData.bundleId;
            if (!string.IsNullOrEmpty(bundleId))
            {
                var sprite = PgcUtils.LoadBundleIcon(bundleId, gameObject);
                iconImg.sprite = sprite;
            }


        }

        infoObj.gameObject.SetActive(false);
        colorBg.color = bgColor;
    }

    public void SetSelect(bool isSelect)
    {
        bgImage.color = isSelect ? selectColor : normalColor;
    }

    private void OnClickItem()
    {
        if (paidPackRewardData == null)
        {
            return;
        }

        clickAction?.Invoke(paidPackRewardData);
    }

}