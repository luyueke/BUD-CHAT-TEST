using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class CommentView : CommonUIWidget
{
    public CText Txt_Desc;
    public CText Txt_Latest_UserName;
    public CText Txt_Latest_UserComment;
    public CButton Btn_AllComment;

    public override void SetData(params object[] args)
    {
        base.SetData(args);
        Btn_AllComment.onClick.AddListener(OnBtnAllCommentClick);
    }

    private void OnBtnAllCommentClick()
    {
        
    }
}
