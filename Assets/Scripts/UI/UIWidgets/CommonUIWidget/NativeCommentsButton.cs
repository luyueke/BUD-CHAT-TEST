using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using UI.BaseWidgets;
using UnityEngine;

public class NativeCommentsButton : CommonUIWidget
{
    public CButton Btn_NativeComment;
    public CText Txt_CommentNum;


    private void Awake()
    {
        Btn_NativeComment.onClick.AddListener(OnCommentBtnClick);
    }

    /// <summary>
    /// 所需参数 args[0] 评论数
    /// </summary>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        Txt_CommentNum.text = args[0].ToString();
    }
    
    private void OnCommentBtnClick()
    {

    }
}
