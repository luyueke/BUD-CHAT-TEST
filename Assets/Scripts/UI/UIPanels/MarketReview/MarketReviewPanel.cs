using DG.Tweening;
using Es;
using EventTracking;
using Game.Avatar;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem;
using View.UI.PopupPanelSystem.Data;
using View.UI.PopupPanelSystem.ExtendsPopups;

/// <summary>
/// 评分界面 0~5星
/// </summary>
public class MarketReviewPanel : BasePanel<MarketReviewPanel>
{   
    public List<GameObject> starList;
    public List<GameObject> starList_dark;
    public List<GameObject> starList_bright;
    public Button btn_submit; // 去评价按钮
    public Button btn_close; // 关闭按钮
    
    private int currentStarRating = 0; // 当前选中的星星数，默认0星
    private bool isFirstSelection = true; // 是否是首次选择
    private List<Button> starButtons = new List<Button>(); // 星星按钮列表
    bool doJump = false;
    
    public override void OnCreate()
    {
        base.OnCreate();
        InitializeStars();
        SetupButtons();
        ResetPanel();
    }
    
    /// <summary>
    /// 初始化星星按钮
    /// </summary>
    private void InitializeStars()
    {
        starButtons.Clear();
        for (int i = 0; i < starList.Count; i++)
        {
            var starObj = starList[i];
            if (starObj != null)
            {
                // 尝试获取Button组件，如果没有则添加
                var button = starObj.GetComponent<Button>();
                if (button == null)
                {
                    button = starObj.AddComponent<Button>();
                }
                
                int starIndex = i + 1; // 星星编号从1开始
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnStarClicked(starIndex));
                starButtons.Add(button);
            }
        }
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        if (btn_submit != null)
        {
            btn_submit.onClick.RemoveAllListeners();
            btn_submit.onClick.AddListener(OnSubmitButtonClicked);
            btn_submit.gameObject.SetActive(false); // 默认隐藏
        }
        
        if (btn_close != null)
        {
            btn_close.onClick.RemoveAllListeners();
            btn_close.onClick.AddListener(OnCloseButtonClicked);
        }
    }
    
    /// <summary>
    /// 重置面板状态
    /// </summary>
    private void ResetPanel()
    {
        currentStarRating = 0;
        isFirstSelection = true;
        UpdateStarDisplay(0);
        if (btn_submit != null)
        {
            btn_submit.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 星星点击事件
    /// </summary>
    private void OnStarClicked(int starIndex)
    {
        currentStarRating = starIndex;
        UpdateStarDisplay(starIndex);
        
        // 如果是首次选择
        if (isFirstSelection)
        {
            isFirstSelection = false;
            
            // 如果选择1-3星，弹窗消失并显示toast
            if (starIndex >= 1 && starIndex <= 3)
            {
                TipPanel.ShowToast("感谢您的评价");
                CloseSelf();
                return;
            }
            
            // 如果选择4-5星，显示"去评价"按钮
            if (starIndex >= 4 && starIndex <= 5)
            {
                if (btn_submit != null)
                {
                    btn_submit.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // 非首次选择，如果改为4-5星，显示按钮；如果改为1-3星，隐藏按钮（但不关闭弹窗）
            // if (starIndex >= 4 && starIndex <= 5)
            // {
            //     if (btn_submit != null)
            //     {
            //         btn_submit.gameObject.SetActive(true);
            //     }
            // }
            // else if (starIndex >= 1 && starIndex <= 3)
            // {
            //     if (btn_submit != null)
            //     {
            //         btn_submit.gameObject.SetActive(false);
            //     }
            // }
        }
    }
    
    /// <summary>
    /// 更新星星显示状态
    /// </summary>
    private void UpdateStarDisplay(int rating)
    {
        // 使用 starList_dark 和 starList_bright 控制星星显示
        int maxCount = Mathf.Max(starList_dark != null ? starList_dark.Count : 0, 
                                 starList_bright != null ? starList_bright.Count : 0);
        
        for (int i = 0; i < maxCount; i++)
        {
            bool isSelected = i < rating; // 当前星星是否被选中（索引从0开始，rating从1开始）
            
            // 更新亮星星显示状态
            if (starList_bright != null && i < starList_bright.Count)
            {
                var brightStar = starList_bright[i];
                if (brightStar != null)
                {
                    brightStar.SetActive(isSelected);
                }
            }
            
            // 更新暗星星显示状态
            if (starList_dark != null && i < starList_dark.Count)
            {
                var darkStar = starList_dark[i];
                if (darkStar != null)
                {
                    darkStar.SetActive(!isSelected);
                }
            }
        }
    }
    
    /// <summary>
    /// 去评价按钮点击事件
    /// </summary>
    private void OnSubmitButtonClicked()
    {
        OpenAppStoreRating();
    }
    
    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseButtonClicked()
    {
        CloseSelf();
    }
    
    /// <summary>
    /// 打开应用商店评分
    /// </summary>
    private void OpenAppStoreRating()
    {
        doJump = true;
        MarketReviewManager.Inst.OpenAppStoreRating();
    }

    void OnApplicationFocus(bool focus)
    {
        if(doJump && focus)
        {
            CloseSelf();
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        ResetPanel();
    }
   
    protected override void OnDestroy()
    {
        // 清理事件监听
        if (btn_submit != null)
        {
            btn_submit.onClick.RemoveAllListeners();
        }
        
        if (btn_close != null)
        {
            btn_close.onClick.RemoveAllListeners();
        }
        
        foreach (var button in starButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        starButtons.Clear();
       
        base.OnDestroy();
    }
}


