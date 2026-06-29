using Game.Avatar;
using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum RoleActionType
{
    // 点击item
    Item = 0,
    // 点击调节
    Adjust,
    // 点击移除此类型
    Delete,
    // 长按收藏
    Collect,
}

public class AvatarRoleItemView : MonoBehaviour
{
    protected GameObject deleteView;
    protected Image iconView;
    private RawImage rawImage;
    protected Image selectedView;
    protected CButton adjustBtn;
    protected GameObject collectIcon;

    protected Action<RoleActionType, AvatarRoleItemProtocol> ClickAction;

    private void Awake()
    {
        InitUIIfNeed();
        
        adjustBtn.onClick.AddListener(OnClickAdjust);
        var cLongBtn = transform.GetComponent<CLongButton>();
        cLongBtn.onClick.AddListener(OnClickItem);
        cLongBtn.onLongClick.AddListener(OnLongClickItem);
    }

    protected virtual void InitUIIfNeed()
    {
        if (iconView != null)
        {
            return;
        }

        iconView = GameObjectEx.FindChildByName(transform, "Image").GetComponent<Image>();
        rawImage = GameObjectEx.FindChildByName(transform, "RawImage").GetComponent<RawImage>();
        deleteView = GameObjectEx.FindChildByName(transform, "DeleteIcon").gameObject;
        selectedView = GameObjectEx.FindChildByName(transform, "Selected").GetComponent<Image>();
        adjustBtn = GameObjectEx.FindChildByName(transform, "Selected/Adjust").GetComponent<CButton>();
        collectIcon = GameObjectEx.FindChildByName(transform, "CollectedIcon").gameObject;
    }

    public AvatarRoleItemProtocol ItemData
    {
        get
        {
            return CurrentData;
        }
    }
    protected AvatarRoleItemProtocol CurrentData;
    public virtual void SetData<T>(T data, Action<RoleActionType, AvatarRoleItemProtocol> clickAction = null) where T: AvatarRoleItemProtocol
    {
        CurrentData = data;
        ClickAction = clickAction;

        if (data.isPGCItem)
        {
            InitUIIfNeed();
            deleteView.SetActive(data.isShowDelete);
            iconView.gameObject.SetActive(!data.isShowDelete);
            rawImage.texture = null;
            rawImage.gameObject.SetActive(false);

            if (data.isShowDelete)
            {
                selectedView.gameObject.SetActive(false);
                collectIcon.SetActive(false);
            }
            else
            {
                // #warning TODO 记录能否调节
                selectedView.gameObject.SetActive(data.isSelected);
                adjustBtn.gameObject.SetActive(data.isSelected && data.adjustType != (int)AdjustViewUtils.AdjustType.NotSupport);
                collectIcon.SetActive(data.isCollected);

                if (!string.IsNullOrEmpty(data.iconPath))
                {
                    var atlasPath = "Assets/Loadable/UI/SpriteAltas/Avatar.spriteatlas";
                    iconView.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, data.iconPath, gameObject);
                }
            }
        }
        else
        {
            rawImage.gameObject.SetActive(true);
            iconView.gameObject.SetActive(false);
            iconView.sprite = null;

            collectIcon.SetActive(data.isCollected);
        }
    }

    public void UpdateSelected(bool isSelected)
    {
        if (CurrentData.isShowDelete)
        {
            selectedView.gameObject.SetActive(false);
            return;
        }
        CurrentData.isSelected = isSelected;
        selectedView.gameObject.SetActive(isSelected);
        adjustBtn.gameObject.SetActive(isSelected && CurrentData.adjustType != (int)AdjustViewUtils.AdjustType.NotSupport);
    }

    public void UpdateCollect(bool isCollect)
    {
        CurrentData.isCollected = isCollect;

        collectIcon.SetActive(isCollect);
    }

    private void OnClickAdjust()
    {
        var currentId = CurrentData.itemId;
        if (string.IsNullOrEmpty(currentId))
        {
            return;
        }
        ClickAction?.Invoke(RoleActionType.Adjust, CurrentData);
    }

    private void OnClickItem()
    {
        var currentId = CurrentData.itemId;
        if (string.IsNullOrEmpty(currentId))
        {
            return;
        }
        if (CurrentData.isShowDelete)
        {
            ClickAction?.Invoke(RoleActionType.Delete, CurrentData);
        }
        else
        {
            ClickAction?.Invoke(RoleActionType.Item, CurrentData);
        }
    }

    private void OnLongClickItem()
    {
        var currentId = CurrentData.itemId;
        if (string.IsNullOrEmpty(currentId))
        {
            return;
        }

        ClickAction?.Invoke(RoleActionType.Collect, CurrentData);
    }
}
