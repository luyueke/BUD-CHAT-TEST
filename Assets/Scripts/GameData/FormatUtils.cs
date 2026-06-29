using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using GameData.Base;

public class FormatUtils
{
    public static Vector3 LimitVector3(Vector3 target, float min = 0.0001f)
    {
        for (int i = 0; i < 3; i++)
        {
            target[i] = Mathf.Max(target[i],min);
        }
        return target;
    }
    
    public static string Vector2ToString(Vector2 target)
    {
        string str = target.ToString("f4");
        str = str.Substring(1, str.Length - 2);
        return str;
    }
    
    public static string Vector2IntToString(Vector2Int target)
    {
        string str = target.ToString();
        str = str.Substring(1, str.Length - 2);
        return str;
    }
    
    public static string Vector3ToString(Vector3 target)
    {
        string str = target.ToString("f4");
        str = str.Substring(1, str.Length - 2);
        return str;
    }
    
    public static Vector2 StringToVector2(string target)
    {
        target = target.Trim('(', ')');
        string[] split = target.Split(',');
        float x = float.Parse(split[0], CultureInfo.InvariantCulture);
        float y = float.Parse(split[1], CultureInfo.InvariantCulture);
        return new Vector2(x, y);
    }
    
    public static Vector2Int StringToVector2Int(string target)
    {
        string[] split = target.Split(',');
        int x = int.Parse(split[0], CultureInfo.InvariantCulture);
        int y = int.Parse(split[1], CultureInfo.InvariantCulture);
        return new Vector2Int(x, y);
    }
    
    public static Vector3 StringToVector3(string target)
    {
        target = target.Trim('(', ')');
        string[] split = target.Split(',');
        float x = float.Parse(split[0], CultureInfo.InvariantCulture);
        float y = float.Parse(split[1], CultureInfo.InvariantCulture);
        float z = float.Parse(split[2], CultureInfo.InvariantCulture);
        return new Vector3(CheckNaNFloat(x), CheckNaNFloat(y), CheckNaNFloat(z));
    }
    
    public static float CheckNaNFloat(float fValue)
    {
        if (float.IsNaN(fValue))
        {
            return 0;
        }
        return fValue;
    }
    
    public static string ColorToString(Color target)
    {
        return ColorUtility.ToHtmlStringRGB(target);
    }
    
    public static string ColorRGBAToString(Color target)
    {
        return ColorUtility.ToHtmlStringRGBA(target);
    }
    
    
    public static Color StringToColor(string target)
    {
        bool parseSuccess = ColorUtility.TryParseHtmlString("#" + target, out Color color);
        if (parseSuccess)
        {
            return color;
        }
        return Color.white;
    }
    
    /// <summary>
    ///  解析 RGBA(1.0, 1.0, 1.0, 1.0) 结构字符串
    /// </summary>
    /// <returns></returns>
    public static Color StringToRGBAColor(string target)
    {
        target = target.Replace("RGBA", "").Trim('(', ')');
        var targetArray = target.Split(',').Select(tmp => float.Parse(tmp, CultureInfo.InvariantCulture)).ToList();
        return new Color(targetArray[0],targetArray[1],targetArray[2],targetArray[3]);
    }
    
    
    public static Color StringToColorByHex(string target)
    {
        bool parseSuccess = ColorUtility.TryParseHtmlString(target, out Color color);
        if (parseSuccess)
        {
            return color;
        }
        return default;
    }

    public static string FilterNonStandardText(string content, string defCont = "")
    {
        string str = content;
        List<string> patten = new List<string>();
        patten.Add(@"\p{Cs}");
        patten.Add(@"\p{Co}");
        patten.Add(@"\p{Cn}");
        patten.Add(@"[\u2070-\u24ff]");
        patten.Add(@"[\u2580-\u2bff]");
        patten.Add(@"[\ud800-\uf8ff]");
        patten.Add(@"[\ufff0-\uffff]");
        for (int i = 0; i < patten.Count; i++)
        {
            str = Regex.Replace(str, patten[i], defCont);
        }
        return str;
    }


    public static bool IsAuditing(UgcBaseInfo baseInfo)
    {
        return baseInfo != null && baseInfo.auditInfo!=null && baseInfo.auditInfo.auditResult == (int)AuditResult.PendingToAudit;
    }
}
