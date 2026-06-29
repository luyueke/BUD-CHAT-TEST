using System;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorLeftMenu : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private USwitchToggle theatreEditorToggle;
    [SerializeField] private USwitchToggle theatreAvatarToggle;
    [SerializeField] private Button createBtn;
    [SerializeField] private Button deleteBtn;

    public Action<bool> OnEditorToggleChanged;
    public Action<bool> OnAvatarToggleChanged;
    public Action OnCreateClicked;
    public Action OnDeleteClicked;

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        if (theatreEditorToggle == null)
            theatreEditorToggle = GameObjectEx.FindComponentByName<USwitchToggle>(transform, "Theatre_Manage_Toggle");
        if (theatreAvatarToggle == null)
            theatreAvatarToggle = GameObjectEx.FindComponentByName<USwitchToggle>(transform, "OCAvatar_Manage_Toggle");
        if (createBtn == null)
            createBtn = GameObjectEx.FindComponentByName<Button>(transform, "NewSection_btn");
        if (deleteBtn == null)
            deleteBtn = GameObjectEx.FindComponentByName<Button>(transform, "DeleteSection_btn");

        theatreEditorToggle?.Init();
        theatreAvatarToggle?.Init();
        theatreEditorToggle?.onValueChanged.AddListener(isOn => OnEditorToggleChanged?.Invoke(isOn));
        theatreAvatarToggle?.onValueChanged.AddListener(isOn => OnAvatarToggleChanged?.Invoke(isOn));
        createBtn?.onClick.AddListener(() => OnCreateClicked?.Invoke());
        deleteBtn?.onClick.AddListener(() => OnDeleteClicked?.Invoke());
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        base.OnShow(param);
        // 每次打开重置：EditorToggle 默认激活，AvatarToggle 默认未选中
        if (theatreEditorToggle != null) theatreEditorToggle.isOn = true;
        if (theatreAvatarToggle != null) theatreAvatarToggle.isOn = false;
    }

    public void SetEditorToggleOff()
    {
        if (theatreEditorToggle != null) theatreEditorToggle.isOn = false;
    }

    public void SetAvatarToggleOff()
    {
        if (theatreAvatarToggle != null) theatreAvatarToggle.isOn = false;
    }

    public void SetAvatarToggleOn()
    {
        if (theatreAvatarToggle != null) theatreAvatarToggle.isOn = true;
    }

    public override void OnHide()
    {
        base.OnHide();
    }
}
