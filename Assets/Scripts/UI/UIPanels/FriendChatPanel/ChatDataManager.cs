using System.Text.RegularExpressions;
using Network;
using Network.Http;
using Newtonsoft.Json;

public class ChatDataManager : GlobalInstance<ChatDataManager>
{
    // 定义正则表达式模式，匹配6位、7位或9位的字母和数字组合，数字部分可以为空
    private static readonly Regex regex = new Regex(@"([A-Za-z0-9]{6}|[A-Za-z0-9]{7}|[A-Za-z0-9]{9})");

    // 定义不同长度的正则表达式模式，按长度优先级顺序
    private static readonly Regex regex9 = new Regex(@"[A-Za-z0-9]{9}");
    private static readonly Regex regex7 = new Regex(@"[A-Za-z0-9]{7}");
    private static readonly Regex regex6 = new Regex(@"[A-Za-z0-9]{6}");

    public static string GetValidSubstring(string input)
    {
        // 替换富文本标签
        input = Regex.Replace(input, "<.*?>", string.Empty);
        // 优先检查9位匹配
        Match match = regex9.Match(input);
        if (match.Success)
        {
            return match.Value;
        }

        // 然后检查7位匹配
        match = regex7.Match(input);
        if (match.Success)
        {
            return match.Value;
        }

        // 最后检查6位匹配
        match = regex6.Match(input);
        if (match.Success)
        {
            return match.Value;
        }

        // 如果没有匹配到任何符合条件的子字符串，返回null或空字符串
        return null; // 或 return string.Empty;
    }

    public void SetReadMessage(string toUid)
    {
        SetChatReq setChatReq = new SetChatReq()
        {
            setType = 2,
            toUid = toUid,
            msgType = 1,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetChat,
            HttpMethod.POST,
            JsonConvert.SerializeObject(setChatReq),
            onReceive: msg => { }, onFail: arg0 => { });
    }
}

public class TextChatData
{
    public string message;
    public string fromUid;
    public string toUid;
    public string portraitUrl;
    public string nickName;
    public int chatBubbles;
    public int avatarFrame;
    public int nicknameFrame;
}
