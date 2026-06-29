using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextEllipsis : MonoBehaviour
{
    public int widthLimit = -1; //优先判断宽度条件
    public int textLimit = 20;
    private Text textComp;

    public void SetText(string val)
    {
        if (textComp == null)
        {
            textComp = this.GetComponent<Text>();
        }
        if (widthLimit > 0)
        {
            textComp.text = StringEllipsis(textComp, widthLimit, val);
            return;
        }
        textComp.text = val.Length > textLimit ? val.Substring(0, textLimit) + "..." : val;
    }

    #region 根据长度截取文本
    private string StringEllipsis(Text text, int maxWidth, string content, string suffix = "...")
    {
        int textLeng = GetTextLeng(text, content);

        if (textLeng > maxWidth)
        {
            int suffixLeng = GetTextLeng(text, suffix);
            return StripLength(text, maxWidth - suffixLeng, content) + suffix;
        }
        else
        {
            return content;
        }
    }

    private string StripLength(Text text, int width, string str = null)
    {
        int totalLength = 0;
        Font myFont = text.font;
        string mStr = string.IsNullOrEmpty(str) ? text.text : str;
        myFont.RequestCharactersInTexture(mStr, text.fontSize, text.fontStyle);
        CharacterInfo characterInfo = new CharacterInfo();

        char[] charArr = mStr.ToCharArray();

        int i = 0;
        for (; i < charArr.Length; i++)
        {
            myFont.GetCharacterInfo(charArr[i], out characterInfo, text.fontSize);

            int newLength = totalLength + characterInfo.advance;
            if (newLength > width)
            {
                if (Mathf.Abs(newLength - width) > Mathf.Abs(width - totalLength))
                {
                    break;
                }
                else
                {
                    totalLength = newLength;
                    break;
                }
            }
            totalLength += characterInfo.advance;
        }
        return mStr.Substring(0, i);
    }

    private int GetTextLeng(Text text, string str = null)
    {
        Font mFont = text.font;
        string mStr = string.IsNullOrEmpty(str) ? text.text : str;
        mFont.RequestCharactersInTexture(mStr, text.fontSize, text.fontStyle);
        char[] charArr = mStr.ToCharArray();
        int totalTextLeng = 0;
        CharacterInfo character = new CharacterInfo();
        for (int i = 0; i < charArr.Length; i++)
        {
            mFont.GetCharacterInfo(charArr[i], out character, text.fontSize);
            totalTextLeng += character.advance;
        }
        return totalTextLeng;
    }
    #endregion
}
