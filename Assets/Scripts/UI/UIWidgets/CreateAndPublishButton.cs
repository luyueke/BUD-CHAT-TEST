using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public enum CreateButtonState
{
    Create = 0,
    Public = 1,
    Update = 2,
    Confirm = 3
}

[Serializable]
public class CreateAndPublishButton : MonoBehaviour
{
    private LoadingButton mLoadingButton;
    private Dictionary<CreateButtonState, string> showName;
    public CreateButtonState CurState = CreateButtonState.Create;
    
    void Start()
    {
        InitConfig();
        mLoadingButton = GetComponent<LoadingButton>();
        SetShowState(CurState);
    }

    private void InitConfig()
    {
        showName = new Dictionary<CreateButtonState, string>();
        showName.Add(CreateButtonState.Create,"创作");
        showName.Add(CreateButtonState.Public,"发布");
        showName.Add(CreateButtonState.Update,"更新");
        showName.Add(CreateButtonState.Confirm,"确定");
    }

    public void SetShowState(CreateButtonState state)
    {
        CurState = state;
        mLoadingButton.SetText(showName[state]);
    }

    public void SetClickAble(bool value)
    {
        mLoadingButton.SetClickAble(value);
    }

    public void ShowLoading(bool value)
    {
        mLoadingButton.SetLoadingVisible(value);
    }
}
