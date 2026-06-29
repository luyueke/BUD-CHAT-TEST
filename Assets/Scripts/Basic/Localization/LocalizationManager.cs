using System;
using System.Collections.Generic;
using System.IO;
using Message;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public enum LangCode
{
    zh_Hans, //简体中文
    zh_Hant, //繁体中文
    en, //英语
    es, //西班牙语
    fil, //菲律宾
    id, //印尼
    de, //德语
    it, //意大利语
    ja, //日本语
    ko, //韩语
    ms, //马来西亚
    pt, //葡萄牙巴西语
    ru, //俄语
    th, //泰语
    vi, //越南语
}

public enum FontType
{
    Normal = 0,
    Heavy = 1,//标题类
    Regular = 2,
}




public class LocalizationManager : GlobalInstance<LocalizationManager>
{
    private const LangCode defaultLangCode = LangCode.zh_Hans;

    private LangCode _langCode = defaultLangCode;
    public LangCode LangCode
    {
        get => _langCode;
    }
    /// <summary>
    /// 翻译内容，key值：中文文本，value:翻译文本
    /// </summary>
    public Dictionary<string, string> LocalizationDict = new Dictionary<string, string>();
    private Dictionary<string, FontType> fontTypeDict = new Dictionary<string, FontType>();
    private List<SpecLangFont> specLangList;
    private Dictionary<int, SpecLangFont> specLangDict;


    public static List<string> StableKeys = new List<string>(); //仅编辑器模式下需要

    public LocalizationManager()
    {
        Init();
    }

    public void Init()
    {
        fontTypeDict = new Dictionary<string, FontType>();
        fontTypeDict.Add("WendyOne-Regular",FontType.Heavy);
        fontTypeDict.Add("SourceHanSansCN-Heavy",FontType.Heavy);
        fontTypeDict.Add("SourceHanSansCN-Medium",FontType.Normal);
        fontTypeDict.Add("SourceHanSansCN-Regular",FontType.Regular);

        var specialLanguageFont = Loader.Load<SpecialLanguageFont>("Assets/Arts/Config/Localization/SpecialLanguageFont.asset").RetainAsset();
        specLangList = specialLanguageFont.list;
        specLangDict = new Dictionary<int, SpecLangFont>();

        if (specLangList != null)
        {
            foreach (var config in specLangList)
            {
                var key = GetSpecLangKey(config.langCode,config.fontType);
                specLangDict.Add(key, config);
            }
        }

    }


    public void SetLang(LangCode langCode)
    {
        _langCode = langCode;
        if(_langCode == LangCode.zh_Hans) return;
        string path = GetAssetFilePath(langCode);
        LoadFromAsset(path);
    }

    private void LoadFromAsset(string path)
    {
        var langConfig = Loader.Load<LocalizationKV>(path).RetainAsset();
        LocalizationDict = new Dictionary<string, string>();
        if (langConfig != null && langConfig.list != null)
        {
            foreach (var item in langConfig.list)
            {
                LocalizationDict.Add(item.key,item.value);
            }
        }
        //切换语言后, 当前页面文本刷新
        TranslateAllContent();
        MessageHelper.Broadcast(MessageName.OnSwitchLanguage, LangCode);
    }

    private void LoadFromJson(string path)
    {
        try
        {
            var assetWrapper = Loader.LoadAsync<TextAsset>(path);
            assetWrapper.completed += (isSuccess) =>
            {
                if (isSuccess)
                {
                    var textAsset = assetWrapper.request.asset as TextAsset;
                    LocalizationDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                    //切换语言后, 当前页面文本刷新
                    TranslateAllContent();
                    MessageHelper.Broadcast(MessageName.OnSwitchLanguage, LangCode);
                }
                else
                {
                    LoggerUtils.LogError("Load Language  Error:" + path);
                }
            };
        }
        catch (Exception e)
        {
            LoggerUtils.LogError("SetLang Error:" + e.StackTrace + "," + e.Message + "," + path);
        }
    }

    private string GetAssetFilePath(LangCode langCode)
    {
        string fileName = string.Format("Lang-{0}.asset", langCode.ToString());
        string filaPath = Path.Combine("Assets","Arts","Config","Localization",fileName);
        return filaPath;
    }


    private string GetJsonFilePath(LangCode langCode)
    {
        string fileName = string.Format("locale-{0}.json", langCode.ToString());
        string filaPath = Path.Combine("Assets","Loadable","lang",fileName);
        return filaPath;
    }



    private void TranslateAllContent()
    {
        var allComps = GameObject.FindObjectsOfType<LocalizationComponent>(true);
        foreach (LocalizationComponent comp in allComps)
        {
            comp.Translate();
        }
    }

    public bool IsDefaultLang()
    {
        return LangCode == defaultLangCode;
    }

    /// <summary>
    /// 用于动态获取文案：需要配合字体设置
    /// </summary>
    /// <param name="localizationKey">本地化键(英文原文案)</param>
    /// <param name="formatArgs">设置格式项</param>
    /// <returns>翻译文案</returns>
    public string GetLocalizedText(string localizationKey, params object[] formatArgs)
    {
        if (localizationKey == null)
        {
            return "";
        }
#if UNITY_EDITOR && ADD_LOKEY
        if (!StableKeys.Contains(localizationKey)) StableKeys.Add(localizationKey);
#endif
        try
        {
            if (LocalizationDict != null && LocalizationDict.ContainsKey(localizationKey))
            {
                string zhValue = LocalizationDict[localizationKey];
                if (string.IsNullOrEmpty(zhValue))
                {
                    return localizationKey;
                }

                if (formatArgs == null || formatArgs.Length == 0)
                {
                    return zhValue;
                }
                return string.Format(zhValue, formatArgs);
            }

            if(formatArgs == null || formatArgs.Length == 0)
                return localizationKey;
            return string.Format(localizationKey, formatArgs);
        }
        catch (Exception err)
        {
            LoggerUtils.LogError("Applanga GetString Error : " + err);
            return string.Format(localizationKey, formatArgs);
        }
    }

    /// <summary>
    /// 用于动态设置文案
    /// </summary>
    /// <param name="textCom">Text字体组件</param>
    /// <param name="localizationKey">本地化键(英文原文案)</param>
    /// <param name="formatArgs">设置格式项</param>
    public void SetLocalizedContent(Text textCom, string localizationKey, params object[] formatArgs)
    {
        if (textCom == null || string.IsNullOrEmpty(localizationKey))
        {
            return;
        }
        SetSystemTextFont(textCom);
        textCom.text = GetLocalizedText(localizationKey, formatArgs);
        if (textCom.TryGetComponent<ContentSizeFitter>(out var sizeFitter) && sizeFitter.enabled) {
            sizeFitter.SetLayoutHorizontal();
            sizeFitter.SetLayoutVertical();
        }
    }

    public void SetLocalizedContent(SuperTextMesh textCom, string localizationKey, params object[] formatArgs)
    {
        if (textCom == null || string.IsNullOrEmpty(localizationKey))
        {
            return;
        }
        SetSystemTextFont(textCom);
        textCom.text = GetLocalizedText(localizationKey, formatArgs);
        if (textCom.TryGetComponent<ContentSizeFitter>(out var sizeFitter) && sizeFitter.enabled) {
            sizeFitter.SetLayoutHorizontal();
            sizeFitter.SetLayoutVertical();
        }
    }

    public void SetSystemTextFont(Text textCom)
    {
        if (LangCode == defaultLangCode) return;
        if (textCom == null) return;
        string fontName = textCom.font.name;
        if (IsFilterFont(fontName)) return;
        FontType fontType = GetFontTypeByName(fontName);
        var specConfig = GetSpecFontConfig(LangCode, fontType);
        if (specConfig == null || specConfig.fontName == fontName)
        {
            //如果找不到配置，或者与原字体相同，则不处理
            return;
        }
        var newFont = Loader.Load<Font>(specConfig.fontPath, textCom.gameObject);
        if (newFont != null)
        {
            textCom.font = newFont;
        }

        LoggerUtils.Log("SetSystemTextFont 更换字体：" + fontName + " ——> " +textCom.font.name);
    }

    public void SetSystemTextFont(SuperTextMesh textCom)
    {
        if (LangCode == defaultLangCode) return;
        if (textCom == null) return;
        string fontName = textCom.font.name;
        if (IsFilterFont(fontName)) return;
        FontType fontType = GetFontTypeByName(fontName);
        var specConfig = GetSpecFontConfig(LangCode, fontType);
        if (specConfig == null || specConfig.fontName == fontName)
        {
            //如果找不到配置，或者与原字体相同，则不处理
            return;
        }
        var newFont = Loader.Load<Font>(specConfig.fontPath, textCom.gameObject);
        if (newFont != null)
        {
            textCom.font = newFont;
        }

        LoggerUtils.Log("SetSystemTextFont 更换字体：" + fontName + " ——> " +textCom.font.name);
    }

    private bool IsFilterFont(string fontName)
    {
        return !string.IsNullOrEmpty(fontName) && (fontName.Contains("Montserrat") || fontName.Contains("LegacyRuntime"));
    }

    private int GetSpecLangKey(LangCode langCode,FontType fontType)
    {
        return (int)langCode * 1000 + (int)fontType;
    }

    private FontType GetFontTypeByName(string fontName)
    {
        if (fontTypeDict.ContainsKey(fontName))
        {
            return fontTypeDict[fontName];
        }

        return FontType.Normal;
    }


    private SpecLangFont GetSpecFontConfig(LangCode langCode,FontType fontType)
    {
        if (specLangDict == null) return null;

        int key = GetSpecLangKey(langCode,fontType);
        //如果获取不到，则获取默认值
        if (!specLangDict.ContainsKey(key))
        {
            key = GetSpecLangKey(defaultLangCode, fontType);
        }

        if (specLangDict.ContainsKey(key))
        {
            return specLangDict[key];
        }

        return null;
    }
}
