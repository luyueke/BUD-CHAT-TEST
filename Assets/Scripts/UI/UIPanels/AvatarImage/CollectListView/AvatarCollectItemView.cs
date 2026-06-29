using System;
using UnityEngine.UI;

public class AvatarCollectItemView : AvatarRoleItemView
{
    private RawImage rawImgView;

    protected override void InitUIIfNeed()
    {
        base.InitUIIfNeed();

        rawImgView = GameObjectEx.FindChildByName(transform, "RawImage").GetComponent<RawImage>();
    }

    public override void SetData<T>(T data, Action<RoleActionType, AvatarRoleItemProtocol> clickAction = null)
    {
        CurrentData = data;
        ClickAction = clickAction;

        deleteView.SetActive(data.isShowDelete);
        rawImgView.gameObject.SetActive(!data.isShowDelete);
        if (data.isShowDelete)
        {
            selectedView.gameObject.SetActive(false);
            collectIcon.SetActive(data.isCollected);
        }
    }
}
