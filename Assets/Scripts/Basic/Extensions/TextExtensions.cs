using UnityEngine;
using UnityEngine.UI;

public static class TextExtensions
{
    /// <summary>
    /// 不需要翻译，但会自动根据语言替换字体
    /// </summary>
    /// <param name="uiText"></param>
    /// <param name="text"></param>
    public static void SetText(this Text uiText, string text)
    {
        if (uiText == null) return;
        LocalizationManager.Inst.SetSystemTextFont(uiText);
        uiText.text = text;
    }


    /// <summary>
    /// 不需要翻译，但会自动根据语言替换字体
    /// </summary>
    /// <param name="uiText"></param>
    /// <param name="text"></param>
    public static void SetText(this SuperTextMesh uiText, string text)
    {
        if (uiText == null) return;
        LocalizationManager.Inst.SetSystemTextFont(uiText);
        uiText.text = text;
    }


    public static void SetPreferredSize(this Text uiText) {
        if (uiText == null) {
            return;
        }
        uiText.GetComponent<RectTransform>().sizeDelta = new Vector2(uiText.preferredWidth, uiText.preferredHeight);
    }

    public static void SetPreferredSize(this SuperTextMesh uiText) {
        if (uiText == null) {
            return;
        }
        uiText.GetComponent<RectTransform>().sizeDelta = new Vector2(uiText.preferredWidth, uiText.preferredHeight);
    }



    /// <summary>
    /// 需要翻译，但会自动根据语言替换字体
    /// </summary>
    /// <param name="uiText"></param>
    /// <param name="localizationKey"></param>
    /// <param name="formatArgs"></param>
    public static void SetLocalText(this Text uiText, string localizationKey, params object[] formatArgs)
    {
        if (uiText == null) return;
        if (string.IsNullOrEmpty(localizationKey))
        {
            uiText.text = "";
            return;
        }

        LocalizationManager.Inst.SetLocalizedContent(uiText,localizationKey,formatArgs);
    }


    /// <summary>
    /// 需要翻译，但会自动根据语言替换字体
    /// </summary>
    /// <param name="uiText"></param>
    /// <param name="localizationKey"></param>
    /// <param name="formatArgs"></param>
    public static void SetLocalText(this SuperTextMesh uiText, string localizationKey, params object[] formatArgs)
    {
        if (uiText == null) return;
        LocalizationManager.Inst.SetLocalizedContent(uiText,localizationKey,formatArgs);
    }


    public static void SetHintText(this Text text, string hintTxt = "")
    {
        bool hasHint = !string.IsNullOrEmpty(hintTxt);
        text.gameObject.SetActive(hasHint);
        if (hasHint)
        {
            text.SetText(hintTxt);
        }
    }


}
