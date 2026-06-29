
using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public enum RoleColorAction
{
    // 点击纯色
    SolidColor = 0,
    // 点击调色板
    Palette,
}

public class AvatarRoleColorItemView : MonoBehaviour
{
    private GameObject PaletteView;
    private Image SolidView;
    private GameObject SelectedView;

    private Vector2 originalVec
    {
        get{return this.GetComponent<RectTransform>().sizeDelta;}
    }

    private Action<RoleColorAction, string> ClickAction;
    private void Awake()
    {
        InitUIIfNeed();
        transform.GetComponent<CButton>().onClick.AddListener(OnClickItem);
    }

    private void InitUIIfNeed()
    {
        if (PaletteView != null)
        {
            return;
        }
        
        PaletteView = GameObjectEx.FindChildByName(transform, "Palette").gameObject;
        SolidView = GameObjectEx.FindChildByName(transform, "BaseColor").GetComponent<Image>();
        SelectedView = GameObjectEx.FindChildByName(transform, "IsSelected").gameObject;
    }

    public AvatarRoleItemProtocol ItemData
    {
        get
        {
            return CurrentData;
        }
    }
    private AvatarRoleItemProtocol CurrentData;
    public void SetData<T>(T data, Action<RoleColorAction, string> clickAction = null) where T: AvatarRoleItemProtocol
    {
        CurrentData = data;
        ClickAction = clickAction;

        InitUIIfNeed();

        if (!data.isColorItem)
        {
            return;
        }

        bool isPaletter = IsPaletter();
        PaletteView.SetActive(isPaletter);
        if (!isPaletter && !string.IsNullOrEmpty(data.itemId))
        {
            SolidView.color = DataUtil.DeSerializeColorByHex(data.itemId); 
        }
        // this.ColorBtn.GetComponent<RectTransform>().sizeDelta=isVisible? originalVec*0.75f:originalVec;
    }

    private bool IsPaletter()
    {
        return CurrentData?.itemId == AvatarColorManager.PaletteKey;
    }

    public void UpdateSelected(bool isSelected)
    {
        bool isPaletter = IsPaletter();
        if (isPaletter)
        {
            SelectedView.SetActive(false);
            return;
        }
        SelectedView.SetActive(isSelected);
    }

    private void OnClickItem()
    {
        var currentId = CurrentData.itemId;
        if (string.IsNullOrEmpty(currentId))
        {
            return;
        }
        if (IsPaletter())
        {
            ClickAction?.Invoke(RoleColorAction.Palette, currentId);
        }
        else
        {
            ClickAction?.Invoke(RoleColorAction.SolidColor, currentId);
        }
    }
    
}
