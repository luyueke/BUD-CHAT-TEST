using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;


public class ProfileTagPanel : BasePanel<ProfileTagPanel>
{
    public Text titleText;
    public Image icon;
    public Text descTxt;
    public CButton closeBtn;
    public CButton moreBtn;
    public GameObject newCreatorRoot;
    public GameObject lightChaserCreatorRoot;
    public GameObject popularCreatorRoot;
    private ProfileTagType type;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
        moreBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel);
        });
    }

    public void SetData(ProfileTagType type, Sprite iconSp, int titleId, string desc)
    {
        newCreatorRoot.SetActive(false);
        lightChaserCreatorRoot.SetActive(false);
        popularCreatorRoot.SetActive(false);
        string creatorTitle = "新晋创作者";
        switch (titleId)
        {
            case (int)CreatorTitleType.NewCreator:
                creatorTitle = "新晋创作者";
                newCreatorRoot.SetActive(true);
                break;
            case (int)CreatorTitleType.LightChaserCreator:
                creatorTitle = "逐光创作者";
                lightChaserCreatorRoot.SetActive(true);
                break;
            case (int)CreatorTitleType.PopularCreator:
                creatorTitle = "人气创作者";
                popularCreatorRoot.SetActive(true);
                break;
        }
        this.type = type;
        titleText.text = creatorTitle;
        this.descTxt.text = desc;
        icon.sprite = iconSp;
        
    }

    public enum ProfileTagType
    {
        Vip,
        Creator
    }
}