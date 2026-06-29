using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// »ù´¡Í¨ÓÃµ¯´°
/// @stanley
/// </summary>
public class CommonLoadingPanel : BasePanel<CommonLoadingPanel>
{
    public string TitalTextString { get; private set; }
    public string ContentTextString { get; private set; }


    public Text TitalText;
    public CText ContentText;

    public override void OnCreate()
    {
        InitUI();
    }

    private void InitUI()
    {
        if (!string.IsNullOrEmpty(TitalTextString))
        {
            TitalText.SetText(TitalTextString);
        }
        if (!string.IsNullOrEmpty(ContentTextString))
        {
            ContentText.SetText(ContentTextString);
        }

    }

    public void SetText(string titalText, string contentText, string confirmText, string cancelText)
    {
        if (!string.IsNullOrEmpty(titalText))
        {
            TitalText.SetText(titalText);
        }
        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetText(contentText);
        }

    }

    public void SetLocalText(string titalText, string contentText)
    {
        if (!string.IsNullOrEmpty(titalText))
        {
            TitalText.SetLocalText(titalText);
        }
        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetLocalText(contentText);
        }

    }
}