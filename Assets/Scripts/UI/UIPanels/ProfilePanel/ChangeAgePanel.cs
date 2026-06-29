using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ChangeAgePanel : BasePanel<ChangeAgePanel>
{
    public CButton NoAgeBtn;
    public GameObject NoAgeCheckNode;
    
    public CButton InputBtn;
    public GameObject InputCheckNode;
    
    public Text PlaceHolder;
    public Text EditText;
    
    public CButton CloseBtn;
    public CButton ComfirmBtn;

    private int curAge = -1;//-1 :不详
    public Action<int> OnComplete { set; private get; }
    public override void OnCreate()
    {
        base.OnCreate();
        NoAgeBtn.onClick.AddListener(OnNoAgeClick);
        InputBtn.onClick.AddListener(OnInputClick);
        ComfirmBtn.onClick.AddListener(OnComfirmClick);
        CloseBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        curAge = (int) args[0];
        if (curAge < 0)
        {
            OnNoAgeClick();
        }
        else
        {
            SetAgeValue(curAge);
        }
    }

    private void OnComfirmClick()
    {
        OnComplete?.Invoke(curAge);
        CloseSelf();
    }


    private void OnNoAgeClick()
    {
        curAge = -1;
        NoAgeCheckNode.SetActive(true);
        InputCheckNode.SetActive(false);
        EditText.text = "";
        EditText.gameObject.SetActive(false);
        PlaceHolder.gameObject.SetActive(true);
    }

    private void OnInputClick()
    {
        ShowKeyboard();
    }
    
    private void ShowKeyboard()
    {
        string holderStr = LocalizationManager.Inst.GetLocalizedText("请输入整数");
        string lengthStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", 0,9999);
        var keyBoardInfo = new KeyBoardInfo()
        {
            type = 0,
            placeHolder = holderStr,
            inputMode = 1,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = lengthStr,
            returnKeyType = (int)ReturnType.Return
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnKeyboard(string age)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (int.TryParse(age, out var value))
        {
            SetAgeValue(value);
        }
    }

    private void SetAgeValue(int age)
    {
        NoAgeCheckNode.SetActive(false);
        InputCheckNode.SetActive(true);
        PlaceHolder.gameObject.SetActive(false);
        EditText.gameObject.SetActive(true);
        EditText.text = age.ToString();
        curAge = age;
    }

}
