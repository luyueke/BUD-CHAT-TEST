using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

[RequireComponent(typeof(SuperTextMesh))]
public class STMEmoji : MonoBehaviour 
{
    [SerializeField] private SuperTextMesh stm;

    [SerializeField] private Vector2 size = new Vector2(0.9f,0.9f);
    [SerializeField] private Vector3 offset = new Vector3(0f,-0.05f,0f);
    [SerializeField] private float advance = 0.05f;
    private bool isInit = false;

#if UNITY_IOS
    public static string ABPath = Path.Combine(Application.streamingAssetsPath, "assetbundle", "emoji", "img-apple-64.ab");
#else
    public static string ABPath = Path.Combine(Application.streamingAssetsPath, "assetbundle", "emoji", "img-google-64.ab");
#endif
    
    public void OnEnable()
    {
        Init();
    }

    private void OnDestroy()
    {
        Disable();
    }
    
    public void Init()
    {
        if (!isInit)
        {
            stm.OnPreParse += ReplaceEmoji;
            isInit = true;
        }
    }

    public void Disable()
    {
        if (isInit)
        {
            stm.OnPreParse -= ReplaceEmoji;
            isInit = false;
        }
    }
    void ReplaceEmoji(STMTextContainer x)
    {
        MatchCollection emojiMatch = Regex.Matches(x.text,"<q=.*?>");
        foreach (Match e in emojiMatch)
        {
            string v = e.Value.TrimStart("<q=".ToCharArray());
            v = v.TrimEnd('>');
            addSTMQuadData(v);
        }
        
        emojiMatch = Regex.Matches(x.text, DataUtil.EmojiRegular1);
        for(int i=0; i<emojiMatch.Count; i++)
        {
            string processedName = DataUtil.EmojiToBytes(emojiMatch[i].Value);
            x.text = x.text.Replace(emojiMatch[i].Value, "<q=" + processedName + ">");
            addSTMQuadData(processedName);
        }
        emojiMatch = Regex.Matches(x.text, DataUtil.EmojiRegular2);
        for(int i=0; i<emojiMatch.Count; i++)
        {
            string processedName = DataUtil.EmojiToBytes(emojiMatch[i].Value);
            x.text = x.text.Replace(emojiMatch[i].Value, "<q=" + processedName + ">");
            addSTMQuadData(processedName);
        }
    }
    
    private void addSTMQuadData(string name)
    {
        if(stm.data.quads.ContainsKey(name))
        {

        }
        else
        {
            var tex = EmojiDataManager.Inst.GetEmojiTexture(gameObject);
            if (tex)
            {
                var tsize = EmojiDataManager.Inst.GetEmojiSize();
                var pos = EmojiDataManager.Inst.GetEmojiPos(name);

                if (pos == -1)
                {
                    LoggerUtils.LogError("emoji code not found:" + name);
                    return;
                }
                STMQuadData emojiQuad = ScriptableObject.CreateInstance<STMQuadData>();
                emojiQuad.texture = tex;
                emojiQuad.name = name;
                emojiQuad.silhouette = false;
                emojiQuad.size = size;
                emojiQuad.offset = offset;
                emojiQuad.advance = advance;
                emojiQuad.columns = tsize.w;
                emojiQuad.rows = tsize.h;
                emojiQuad.iconIndex = pos;
                stm.data.quads[name] = emojiQuad;
            }
        }
    }
}