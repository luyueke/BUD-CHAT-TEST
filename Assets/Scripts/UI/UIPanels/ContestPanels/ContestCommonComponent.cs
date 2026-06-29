using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ContestCommonComponent : MonoBehaviour
{
    public Button detailsBtn;
    public Button prizesBtn;
    public Button backBtn;
    public RemoteImageBehaviour bannerRawImage;
    public RemoteImageBehaviour bgRawImage;
    public ContestBgItem bgItem;
    public ItemBgColor bannerBg;
    public ContestEndInHint endInHint;
    private ContestInfo info;

    private void Awake()
    {
        detailsBtn.onClick.AddListener(OnDetailsClick);
        prizesBtn.onClick.AddListener(OnPrizesClick);
    }

    public void SetCommonInfo(ContestInfo info)
    {
        this.info = info;
        if (bannerRawImage)
        {
            ContestEventManager.Inst.SetRawImage(bannerRawImage, info.rewardUrl, (isSucc, fromCache) =>
            {
                if (isSucc && this)
                {
                    Vector2 refer = (bannerRawImage.transform.parent as RectTransform).rect.size;
                    //banner图可能会有一条很细的黑边 所以比父节点大1点 让父节点mask掉
                    bannerRawImage.RawImage.FitTexture(refer.x + 1f, widthFirst: true);
                }
            });
        }
            
        SetBgImage(info);
        SetBannerBg(info);
        if (endInHint) endInHint.Refresh(info);
    }


    public void OnDetailsClick()
    {
        if (info == null)
        {
            return;
        }

        UIManager.Inst.OpenPanel<ContestDetailTipsPanel>(PanelId.ContestDetailTipsPanel, info);
    }

    public void OnPrizesClick()
    {
        if (info == null)
        {
            return;
        }
        
        UIManager.Inst.OpenPanel(PanelId.ContestRewardPanel, info);
    }
    
    public void AddBackButtonListener(UnityAction backClickAction)
    {
        backBtn.onClick.AddListener(backClickAction);
    }

    private void SetBgImage(ContestInfo info)
    {
        bgItem.Refresh(info);
    }

    private void SetBannerBg(ContestInfo info)
    {
        if (!bannerBg) return;
        DataUtil.TryGetFromList(info.themeColorList, 1, out string themeColor2);
        bool isValid = !string.IsNullOrEmpty(themeColor2);
        bannerBg.gameObject.SetActive(isValid);
        if (isValid)
        {
            bannerBg.SetColor(themeColor2);
        }
    }
}