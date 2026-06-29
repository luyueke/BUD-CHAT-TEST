using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

public class CommentButton : CommonUIWidget
{
    public CButton Btn_Comment;
    public int Max_Length;
    private KeyBoardInfo keyBoardInfo;
    private string _curMapId;

    /// <summary>
    /// 评论按钮所需要的参数
    /// args[0] ugcId
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        _curMapId = (string)args[0];

        keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = Max_Length,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };

        Btn_Comment.onClick.AddListener(OnShowKeyBoard);
    }

    private void OnShowKeyBoard()
    {
        keyBoardInfo.defaultText = "";
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, GetInputContentFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    private void GetInputContentFromNative(string inputContent)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

        if (inputContent.Length > Max_Length)
        {
            TipPanel.ShowToast("字数超出限制");
            return;
        }

        SetComment(inputContent);
    }

    private void SetComment(string comment)
    {
        var jb = new JObject
        {
            ["mapId"] = _curMapId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetComment , HttpMethod.GET, JsonConvert.SerializeObject(jb), null, null);
    }
}
