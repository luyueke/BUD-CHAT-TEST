using EventTracking;
using System;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class DebugInputPanel : BasePanel<DebugInputPanel>
{
    public Button BgBtn;
    public Button DoneBtn;
    public InputField InputText;

    public override void OnCreate()
    {
        BgBtn.onClick.AddListener(OnBgClick);
        DoneBtn.onClick.AddListener(OnDoneBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        string defaultText = "";
        if (args != null && args.Length > 0 && args[0] is string infoJson && !string.IsNullOrEmpty(infoJson))
        {
            try { defaultText = JsonUtility.FromJson<KeyBoardInfo>(infoJson).defaultText ?? ""; }
            catch { }
        }
        InputText.text = defaultText;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void OnDoneBtnClick()
    {
        string inputStr = "";
        if (!String.IsNullOrEmpty(InputText.text))
        {
            inputStr = InputText.text;
        }

#if UNITY_EDITOR||UNITY_STANDARD_BUILD
        if (MobileInterface.Instance.onClientRespose.ContainsKey(MobileInterfaceDefine.showKeyboard))
        {
            MobileInterface.Instance.onClientRespose[MobileInterfaceDefine.showKeyboard]?.Invoke(inputStr);
        }
#endif
        LoggerUtils.Log("DebugInputPanel OnDoneBtnClick:" + inputStr);
        CloseSelf();
    }

    private void OnBgClick()
    {
#if UNITY_EDITOR || UNITY_STANDARD_BUILD
        if (MobileInterface.Instance.onClientRespose.ContainsKey(MobileInterfaceDefine.hideKeyboard))
        {
            MobileInterface.Instance.onClientRespose[MobileInterfaceDefine.hideKeyboard]?.Invoke(InputText.text);
        }
#endif
        LoggerUtils.Log("DebugInputPanel OnBgClick");
        CloseSelf();
    }
}