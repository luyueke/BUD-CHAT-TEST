using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UI.UIPanels.CommonConfirm;
using UnityEngine;
using UnityEngine.UI;

public enum AIBuddyItemType
{
    ToBuy,
    ToAdd,
    Added,
}

public class AIBuddyPreviewItem : MonoBehaviour
{
    [Header("UI 部分")]
    [SerializeField] protected CButton ItemBtn;
    [SerializeField] protected GameObject PlaceHoldImg;
    [SerializeField] protected GameObject PlaceHoldImg1;//特殊占位icon
    [SerializeField] protected GameObject SelectImg;
    [SerializeField] protected RawImage RawImage;
    [SerializeField] protected CButton ResortBtn;

    private AIBuddyItemType _itemType;

    public AIBuddyItemType ItemType
    {
        get
        {
            return _itemType;
        }
    }

    private Action<AIBuddyInfo> onSortClickListener;
    private Action<AIBuddyPreviewItem,AIBuddyInfo> onItemCllickListener;
    private Action onBuySlotClickListener;
    private AIBuddyInfo _buddyInfo;
    private AIBuddyAnimAvatar _animAvatar;
    private bool isShowSort = false;

    private void Awake()
    {
        ItemBtn.onClick.AddListener(OnItemClick);
        ResortBtn.onClick.AddListener(OnSortBtnClick);
        PlaceHoldImg.SetActive(false);
        ResortBtn.gameObject.SetActive(false);
        RawImage.gameObject.SetActive(false);
        SetSelectWithoutNotify(false);
    }


    public void SetData(AIBuddyInfo buddyInfo,AIBuddyItemType itemType)
    {
        _buddyInfo = buddyInfo;
        SetSelectWithoutNotify(false);
        SetItemType(itemType);
        _animAvatar?.ResetAnim();
        if (itemType == AIBuddyItemType.Added && buddyInfo != null)
        {
            var characterData =  CharacterData.DeserializeObject(buddyInfo.npc.npcAvatarJson);
            _animAvatar?.RefreshAvatar(characterData);
            _animAvatar?.PlayIdleAnim(buddyInfo);
        }
        
        // SetSortVisible(isShowSort);
    }

    public AIBuddyInfo GetBindData()
    {
        return _buddyInfo;
    }

    public void SetItemType(AIBuddyItemType itemType)
    {
        _itemType = itemType;
        PlaceHoldImg.SetActive(false);
        PlaceHoldImg1.SetActive(false);
        RawImage.gameObject.SetActive(false);
        ResortBtn.gameObject.SetActive(false);
        if (itemType == AIBuddyItemType.ToBuy)
        {
            PlaceHoldImg.SetActive(true);
        }
        else if (itemType == AIBuddyItemType.ToAdd)
        {
            PlaceHoldImg.SetActive(true);
            PlaceHoldImg1.SetActive(true);
        }
        else
        {
            RawImage.gameObject.SetActive(true);
        }
    }

    
    public void SetRenderTexture(RenderTexture targetTexture)
    {
        if(targetTexture == null) return;
        RawImage.texture = targetTexture;
    }

    public void BindAnimAvatar(AIBuddyAnimAvatar animAvatar)
    {
        if (animAvatar == null) return;
        _animAvatar = animAvatar;
        SetRenderTexture(animAvatar.GetRenderTexture());
        ResizeAvatar();
    }

    private void ResizeAvatar()
    {
        if (_animAvatar == null) return;
        Vector2 designResolution = new Vector2(2436, 1125); // 设计分辨率
        float screenAspectRatio = (float)Screen.width / Screen.height;
        float designAspectRatio = designResolution.x / designResolution.y;
        float scale = 1;
        // 根据宽高比调整缩放策略
        if (screenAspectRatio < designAspectRatio) // 屏幕更窄，按宽度适配
        {
            scale = designAspectRatio / screenAspectRatio;
        }
        _animAvatar.transform.localScale = new Vector3(100 * scale, 100 * scale, 100 * scale);
    }

    public void SetSelectWithoutNotify(bool isSelect)
    {
        SelectImg.SetActive(isSelect);
    }

    public void SetSelect(bool isSelect)
    {
        SetSelectWithoutNotify(isSelect);
        if (isSelect)
        {
            OnItemClick();
        }
    }

    public void SetSortVisible(bool isVisible)
    {
        isShowSort = isVisible;
        if (_itemType == AIBuddyItemType.Added && isVisible)
        {
            ResortBtn.gameObject.SetActive(true);
        }
        else
        {
            ResortBtn.gameObject.SetActive(false);
        }
        
    }

    public void SetUIMode(AIBuddyUIMode aiBuddyUIMode)
    {
        ItemBtn.SetClickAble(aiBuddyUIMode == AIBuddyUIMode.Normal);
    }

    public void AddItemClickListener(Action<AIBuddyPreviewItem ,AIBuddyInfo> callback)
    {
        onItemCllickListener += callback;
    }

    public void AddSortClickListener(Action<AIBuddyInfo> callback)
    {
        onSortClickListener += callback;
    }

    public void AddBuySlotClickListener(Action callback)
    {
        onBuySlotClickListener += callback;
    }

    private void OnItemClick()
    {
        if (_itemType == AIBuddyItemType.ToAdd)
        {
            OnAddBtnClick();
        }
        else if(_itemType == AIBuddyItemType.Added)
        {
            OnSelectItem();
        }
        else
        {
            OnBuyBtnClick();
        }
    }

    private void OnSelectItem()
    {
        LoggerUtils.Log("##AIBuddyPreviewItem OnSelectItem");
        onItemCllickListener?.Invoke(this,_buddyInfo);
        SetSelectWithoutNotify(true);
    }

    private void OnAddBtnClick()
    {
        LoggerUtils.Log("##AIBuddyPreviewItem OnAddBtnClick");
        UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
    }

    private void OnBuyBtnClick()
    {
        LoggerUtils.Log("##AIBuddyPreviewItem OnBuyBtnClick");
        onBuySlotClickListener?.Invoke();
    }

    private void OnSortBtnClick()
    {
        onSortClickListener?.Invoke(_buddyInfo);
    }
    

}
