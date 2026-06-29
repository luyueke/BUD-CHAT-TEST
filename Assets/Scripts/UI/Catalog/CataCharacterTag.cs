using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Catalog;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CataCharacterTag : MonoBehaviour
{
    // 静态事件：所有标签共享
    public static event Action<CataCharacterTag> OnTagSelected;

    public  Gallery gallery;

    public Text nameText;
    public RawImage charactreImage;
    public GameObject redPoint;
    public CButton ClickBt;

    public CatalogCharacterData characterData;

    public Action<Gallery> clickAction;

    public GameObject LockImage;

    
    // 标记是否被选中
    private bool isSelected = false;
    
    private void Awake()
    {
    }
    

    public void Init(Gallery _gallery , bool isUgcCata)
    {
        // 先取消之前可能存在的订阅，防止重复订阅
        OnTagSelected -= OnOtherTagSelected;
        
        // 订阅标签选择事件
        OnTagSelected += OnOtherTagSelected;
        
        gallery = _gallery;

        //ugc图片需要单独调整高度为202
        if (isUgcCata)
        {
            Vector2 currentSize = charactreImage.rectTransform.sizeDelta;
            currentSize.y = 202;
            charactreImage.rectTransform.sizeDelta = currentSize;

        }


        InitUI();
        
        // 确保ClickBt不为null再添加监听器
        if (ClickBt != null)
        {
            // 移除之前可能添加的监听器，防止重复
            ClickBt.onClick.RemoveAllListeners();
            ClickBt.onClick.AddListener(OnButtonClick);
        }
        else
        {
            LoggerUtils.LogError("CataCharacterTag: ClickBt is null in Init method");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        OnTagSelected -= OnOtherTagSelected;
        
        // 清除点击监听器
        if (ClickBt != null)
        {
            ClickBt.onClick.RemoveAllListeners();
        }
        
        // 清除回调引用
        clickAction = null;
    }
    
    void InitUI()
    {   
        if(gallery.isLock == 1)
        {
            charactreImage.gameObject.SetActive(false);
            LockImage.gameObject.SetActive(true);
            ClickBt.enabled = false;
            nameText.text = "未解锁";
        }
        
        // 确保组件不为null再使用
        nameText.text = gallery.name;
        
        
        // 确保charactreImage不为null
        if (charactreImage != null && gallery != null)
        {
            var remoteRewardRawImg = charactreImage.GetComponent<RemoteImageBehaviour>();
            if (remoteRewardRawImg != null)
            {
                remoteRewardRawImg.Load(
                    gallery.galleryCover,
                    true,
                    (fromCache, success) => {
                        if (!success)
                        {
                            LoggerUtils.LogError("无法加载图片");
                        }
                    }
                );
            }
        }
        


    }


    public void ShowRedPoint(bool isshow) {
        redPoint.SetActive(isshow);
    }



    public void OnButtonClick()
    {

        if (gallery == null)
        {
            LoggerUtils.LogError("CataCharacterTag: gallery is null");
            return;
        }

        // 触发点击事件
        clickAction?.Invoke(gallery);



        BaseCatalogPanel.Instant.InitCharacter(gallery.roleGallery.roleAvatar);

        // 设置为选中状态
        SetSelected(true);
        
        // 通知其他标签此标签被选中
        OnTagSelected?.Invoke(this);
    }
    
    // 当其他标签被选中时的处理
    private void OnOtherTagSelected(CataCharacterTag selectedTag)
    {   

        if(gallery.isLock!=1)
        // 如果不是自己被选中，则设置为未选中状态
        if (selectedTag != this)
        {
            SetSelected(false);
        }
    }
    
    // 设置标签选中状态
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        // 尝试获取ClickBt组件
        if (ClickBt == null)
        {
             LoggerUtils.LogError("CataCharacterTag: ClickBt is still null after attempting to find it");
             return;
                  return;
            
        }
        
        // 设置按钮的交互状态 - 选中后不可再点击
        ClickBt.interactable = !selected;
    }
}