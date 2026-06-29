using System;
using System.Collections.Generic;
using DG.Tweening;
using EventTracking;
using GameData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 此panel中只做选择，从SelectedFinshAction 中抛出。不做其他逻辑相关
/// Date:23-07-17 17:10:55
/// </summary>
public class GenderSelectPanel : BasePanel<GenderSelectPanel>
{
    [SerializeField] private Transform BG;
    [SerializeField] private NavigationBar navNar;
    [SerializeField] private CButton MaleBtn;
    [SerializeField] private CButton ChangeAvataBtn;
    [SerializeField] private CButton FemaleBtn;
    [SerializeField] private LoadingButton DoneBtn;
    [SerializeField] private NewAccurateRecommendationPopPanel tagSelectView;
    [SerializeField] private RectTransform genderSelectView;
    string characterIconAlts = "Assets/Loadable/UI/UIPanel/NewbieRegister/Avatar/CharacterAtlas.spriteatlas";
   
    [SerializeField] private Image maleImage;
    [SerializeField] private Image femaleImage;
    /// <summary>
    /// 选择完成性别后回调，从 OnShow 注入。
    /// </summary>
    private Action<SavingData.GenderType ,int, string> SelectedFinshAction;
    bool isSelectTag = false;
    private SavingData.GenderType CurrentGender = SavingData.GenderType.Female;

    string avatarJason = "";

    public override void OnCreate()
    {
        AnalyticsManager.Inst.Track(AnalyticsEventName.GENDERPAGEVIEW);
        OnInitBgUI();
        
        AddListeners();
#if UNITY_ANDROID
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.forceLogout, ForceLogout);
#endif
}
    private void ForceLogout(string message)
    {
        Debug.Log("GenderSelectPanel forceLogout");
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.forceLogout);
        GameInstanceManager.Release();
        AccountDataManager.Inst.DeleteCache();
        UIManager.Inst.ClosePanel(PanelId.GenderSelectPanel);
        UIManager.Inst.OpenPanel(PanelId.SignInPanel);
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,
            "");
    }

    private void OnInitBgUI()
    {
        if (BG == null)
        {
            LoggerUtils.LogError("[BG] Check GenderSelectPanel Bg Object");
            return;
        }
        
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("FFFFFF", atlasPath, new List<string>()
        {
            "bg_icon2", "bg_icon1", "bg_icon3", "bg_icon4", "bg_icon5"
        });
        item.gameObject.SetActive(true);
        ShowViewAnimated(true);
    }
    public void InitTagView()
    {
        tagSelectView = UIManager.Inst.OpenPanel<NewAccurateRecommendationPopPanel>(PanelId.NewBieAccurateRecommendationPopPanel);
        tagSelectView.SetData(new View.UI.PopupPanelSystem.Data.WebtoolNewsData());
        tagSelectView.ShowLabelSelectionViewAnimated(false);
        tagSelectView.SetCallBack(FinishCallBack);
    }
    private void AddListeners()
    {
        navNar?.AddBackBtnClickListener(OnBack);
        navNar?.SetTitle("选择你在游戏内的形象");
        maleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts, "1", gameObject);
        maleImage.SetNativeSize();
        femaleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts,"5", gameObject);
        femaleImage.SetNativeSize();
        ChangeAvataBtn.onClick.AddListener(() =>
        {
           
            if (CurrentGender == SavingData.GenderType.Male)
            {
                int index = tagSelectView._currId + 1;
                if (index > 4) index = 1;
                tagSelectView.InitCharacter(index);
                maleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts, index.ToString(), gameObject);
                maleImage.SetNativeSize();
            }
            else
            {
                int index = tagSelectView._currId + 1;
                if (index > 8) index = 5;
                tagSelectView.InitCharacter(index);
                femaleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts, index.ToString(), gameObject);
                femaleImage.SetNativeSize();
            }
            
        });

        MaleBtn.onClick.AddListener(()=> {
            OnClickGender(SavingData.GenderType.Male);
            maleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts, "1", gameObject);
            maleImage.SetNativeSize();
            tagSelectView.InitCharacter(1);
        });
        
        FemaleBtn.onClick.AddListener(()=> {
            OnClickGender(SavingData.GenderType.Female);
            tagSelectView.InitCharacter(5);
            femaleImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(characterIconAlts, "5", gameObject);
            femaleImage.SetNativeSize();
        });
        
        DoneBtn.onClick.AddListener(() =>
        {
            if (SignInPanel.isNewPlayer)
            {
                LoadEvent.ReportPopupStatus("1", "guide_ID");
            }
            navNar?.SetTitle("选择兴趣可以获得更精准的内容推荐");
            tagSelectView.ShowLabelSelectionViewAnimated(true);
            avatarJason = tagSelectView.SetPlayerAvata();
            isSelectTag = true;
            ShowViewAnimated(false);
        });
        OnClickGender(CurrentGender);
    }
    void FinishCallBack()
    {
        SelectedFinshAction?.Invoke(CurrentGender ,tagSelectView._currId, avatarJason);
    }
    private void OnBack()
    {
        if (!isSelectTag)
        {
            if (SignInPanel.isNewPlayer)
            {
                LoadEvent.ReportPopupStatus("2", "guide_ID");
            }
            UIManager.Inst.ClosePanel(tagSelectView);
            UIManager.Inst.ClosePanel(this);
        }
        else
        {
            navNar?.SetTitle("选择你在游戏内的形象");
            isSelectTag = false;
            tagSelectView.ShowLabelSelectionViewAnimated(false);
            ShowViewAnimated(true);
        }
        
    }
    
    private void OnClickGender(SavingData.GenderType gender)
    {
        CurrentGender = gender;
        GameObjectEx.FindChildByName(MaleBtn.gameObject, "Checkmark").gameObject.SetActive(gender == SavingData.GenderType.Male);
        GameObjectEx.FindChildByName(FemaleBtn.gameObject, "Checkmark").gameObject.SetActive(gender == SavingData.GenderType.Female);
    }

    public void HideLoading()
    {
        DoneBtn?.HideLoading();
    }

    public override void OnShow(params object[] args)
    {
        SelectedFinshAction = args[0] as Action<SavingData.GenderType , int, string>;
        InitTagView();
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

    public void ShowViewAnimated(bool isShow,float animationDuration = 0.5f)
    {
        if (!isShow)
        {
            genderSelectView.gameObject.SetActive(false);
            return;
        }
        // 1. 激活GameObject，并记录下在编辑器中设置好的“最终位置”
        genderSelectView.gameObject.SetActive(true);
        Vector2 finalAnchoredPosition = genderSelectView.anchoredPosition;

        // 2. 计算屏幕外的起始位置
        // 我们需要它的父级容器来确定“屏幕外”是多远
        var parentRect = genderSelectView.parent as RectTransform;
        if (parentRect == null)
        {
            // 即使出错，也直接将它设置到最终位置，避免UI错乱
            genderSelectView.anchoredPosition = finalAnchoredPosition;
            return;
        }

        // 计算一个安全的、肯定在屏幕右侧之外的X坐标
        // 父容器宽度 + showView自身宽度的一半（确保整个view都在外面）
        float offscreenX = parentRect.rect.width + (genderSelectView.rect.width * (1 - genderSelectView.pivot.x));

        // 3. 立即将showView移动到屏幕外的起始位置
        // 注意：我们只改变X坐标，保持Y坐标不变，以实现平滑的水平滑入
        genderSelectView.anchoredPosition = new Vector2(offscreenX, finalAnchoredPosition.y);

        // 4. 使用DOTween创建动画，移动到我们记录好的最终位置
        genderSelectView.DOAnchorPos(finalAnchoredPosition, animationDuration)
                .SetEase(Ease.OutCubic); // 使用平滑的缓动函数
    }
}