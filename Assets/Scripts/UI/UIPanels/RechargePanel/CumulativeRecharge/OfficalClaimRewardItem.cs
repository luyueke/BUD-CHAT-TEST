using System;
using Game.Store;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class OfficalClaimRewardItem : MonoBehaviour
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
    
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";
    private BUDRewardType _curRewardType;
    private string _rewardId;
    private string _pgcId;
    
    public void SetData(BUDRewardType budRewardType, string pgcId, int rewardNum)
    {
        this._curRewardType = budRewardType;
        this._rewardId = pgcId;
        this._pgcId = pgcId;
        // this._onItemClick = act;

        if (budRewardType == BUDRewardType.RewardPgcResource)
        {
            iconImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject); 
            var isOwned = AssetsDataManager.IsOwned(pgcId);
            ownedObj.SetActive(isOwned);
        }
        else
        {
            if (rewardNum > 1) {
                previewRewardNumText.gameObject.SetActive(true);
                previewRewardNumText.SetText($"x {rewardNum}");
            }
            iconImg.sprite = PgcUtils.LoadRewardIcon(budRewardType, gameObject);
        }

        SetSelect(true);
    }

    public void SetSelect(bool isSelect)
    {
        bgImage.color = isSelect ? selectColor : normalColor;
    }

    public void SetOwnedUI()
    {
        priceObj.SetActive(false);
        ownedObj.SetActive(true);
    }
}

