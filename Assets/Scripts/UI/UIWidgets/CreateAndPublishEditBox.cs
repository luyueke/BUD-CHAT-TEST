using System;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public struct KeyBoardInfo
{
    public int type;
    public string placeHolder;
    public int inputMode;
    public int maxLength;
    public int inputFlag;
    public int textSecurity; // 文本检测 0 默认检测， 1 不进行安全检测
    public string lengthTips;
    public string defaultText;
    public int returnKeyType;
    public int source;//其他0 房间内聊天1 大厅聊天2
    public int isFilterEmoji; // == 1时，输入过滤emoji
}

public enum KeyboardSource
{
    Other = 0,
    RoomChat = 1,
    HallChat = 2,
}

public enum KeyBoardInputMode
{
    All = 0,
    Number = 1,
    SingleLine = 2,
}

public enum ReturnType
{
#if UNITY_ANDROID
    Return = 1,
    Go = 2,
    Next = 5,
    Search = 3,
    Send = 4,
    Done = 6,
#endif

#if UNITY_IPHONE
    Return = 0,
    Go = 1,
    Next = 4,
    Search = 6,
    Send = 7,
    Done = 9,
#endif
}

public class CreateAndPublishEditBox : MonoBehaviour
{
    private Button editAreaBtn;

    private SuperTextMesh editAreaText;

    private Text editAreaLimitText;

    public int maxLength = 18;

    private Action<string> afterTextChangeAction;

    private Color hintColor = new Color(120f / 255, 120f / 255, 120f / 255, 1f);

    private Color textColor = Color.black;

    public int fontSize = 40;

    KeyBoardInfo keyBoardInfo;

    private string currentText = "";


    public void InitUI(string defaultText)
    {
        editAreaBtn = GameObjectEx.FindChildByName(transform, "editAreaBtn").GetComponent<Button>();
        editAreaText = GameObjectEx.FindChildByName(transform, "editAreaText").GetComponent<SuperTextMesh>();
        editAreaLimitText = GameObjectEx.FindChildByName(transform, "editAreaLimitText").GetComponent<Text>();

        editAreaText.color = hintColor;
        editAreaText.SetLocalText(defaultText);

        string lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制");
        keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = maxLength,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = lengthTips,
            returnKeyType = (int)ReturnType.Return
        };
        InitClick();
    }

    private void InitClick()
    {
        if (editAreaBtn != null)
        {
            editAreaBtn.onClick.AddListener(EditAreaClick);
        }
    }

    public void UpdateKeyBoardInfo(KeyBoardInfo nKeyBoardInfo)
    {
        keyBoardInfo = nKeyBoardInfo;
        keyBoardInfo.maxLength = maxLength;
    }

    public void SetText(string content)
    {
        if (editAreaText != null && !string.IsNullOrEmpty(content))
        {
            editAreaText.color = textColor;
            editAreaText.SetText(content);
            currentText = content;
            editAreaLimitText.text = content.Length + "/" + maxLength;
            afterTextChangeAction?.Invoke(content);
        }
    }

    public string GetText()
    {
        return currentText;
    }

    private void EditAreaClick()
    {
        OnShowKeyBoard();
    }

    public void SetAfterTextChangeAction(Action<string> afterTextChangeAction)
    {
        this.afterTextChangeAction = afterTextChangeAction;
    }
    
    public void RefreshLimitText()
    {
        if (editAreaLimitText)
        {
            editAreaLimitText.text = $"{currentText.Length}/{maxLength}";
        }
    }
    private void OnShowKeyBoard()
    {
        keyBoardInfo.defaultText = currentText;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, GetInputContentFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    public void GetInputContentFromNative(string inputContent)
    {
        if (inputContent.Length > maxLength)
        {
            TipPanel.ShowToast("字数超出限制");
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            return;
        }
        afterTextChangeAction?.Invoke(inputContent);

        SetText(inputContent);
        if (string.IsNullOrEmpty(inputContent))
        {
            editAreaLimitText.text = 0 + "/" + maxLength;
        }
        else
        {
            editAreaLimitText.text = inputContent.Length + "/" + maxLength;
        }

        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
    }
}