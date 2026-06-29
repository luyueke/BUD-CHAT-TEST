using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationData 
{
   
}

[Serializable]
public class SpecLangFont
{
    public LangCode langCode;
    public FontType fontType;
    public string fontName;
    public string fontPath;
}



[Serializable]
public struct LangKV
{
    public string key;
    public string value;
}
