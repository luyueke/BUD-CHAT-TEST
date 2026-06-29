using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CameraMode 通用资源列表 Item（用于 Frame/Filter 等）。
/// 目标：替代直接复用 EmoContentItem 时需要“反射取私有字段”的做法。
/// </summary>
public class CameraModeResItem : MonoBehaviour
{
    [SerializeField] private CButton btn;
    [SerializeField] private Text nameText;
    [SerializeField] private Image iconImage;

    private Action _onClick;

    private void Awake()
    {
        EnsureBind();
    }

    /// <summary>
    /// 初始化显示与点击回调
    /// </summary>
    public void Init(string displayName, Sprite icon, Action onClick, bool interactable = true)
    {
        EnsureBind();

        _onClick = onClick;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(displayName) ? string.Empty : displayName;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnBtnClick);
            btn.interactable = interactable;
        }
    }

    public void SetInteractable(bool interactable)
    {
        EnsureBind();
        if (btn != null) btn.interactable = interactable;
    }

    public void SetSelected(bool selected)
    {
        // 目前没有专门的选中态美术控制，先用不可点击来表现“已选中”
        SetInteractable(!selected);
    }

    private void OnBtnClick()
    {
        _onClick?.Invoke();
    }

    private void EnsureBind()
    {
        // 允许 prefab 未显式绑定引用时自动查找，减少接入成本
        btn ??= GetComponentInChildren<CButton>(true);

        if (nameText == null)
        {
            // 常见命名：EmoContentItem 用的是 Text emoName
            nameText = GameObjectEx.FindComponentByName<Text>(transform, "Title");
            nameText ??= GetComponentInChildren<Text>(true);
        }

        if (iconImage == null)
        {
            // 常见命名：EmoContentItem 用的是 Image emoIcon
            iconImage = GameObjectEx.FindComponentByName<Image>(transform, "Icon");
            iconImage ??= GetComponentInChildren<Image>(true);
        }
    }
}

