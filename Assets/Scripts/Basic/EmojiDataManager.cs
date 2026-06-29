using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class EmojiDataManager : GlobalInstance<EmojiDataManager>
{
    private Dictionary<string, int> emojiDic = new Dictionary<string, int>();
    private atlasSize size = new atlasSize();
    private AssetBundle emoji;
    private Texture emojiTexture;
    private char[] trimEnd = ".png".ToCharArray();

    public EmojiDataManager()
    {
        if (emojiDic.Count == 0)
        {
            GetEmojiDic();
        }
    }

    public AssetBundle GetEmojiAB()
    {
        if (emoji == null)
        {
            emoji = AssetBundle.LoadFromFile("");
        }

        return emoji;
    }

    public Texture GetEmojiTexture(GameObject refObj)
    {
        if (emojiTexture == null)
        {
#if UNITY_IOS
            string resPath = "Assets/Loadable/Emoji/ios/emoji.png";
#else
            string resPath = "Assets/Loadable/Emoji/android/emoji.png";
#endif
            emojiTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(resPath, refObj);
        }

        return emojiTexture;
    }

    public void GetEmojiDic()
    {
#if UNITY_IOS
        string resPath = "Assets/Loadable/Emoji/ios/json.json";
#else
        string resPath = "Assets/Loadable/Emoji/android/json.json";
#endif

        var UIRoot = GameObject.Find("UIRoot");
        TextAsset textAsset = XAssetLoaderMgr.Inst.LoadResource<TextAsset>(resPath, UIRoot);
        emojiData data = JsonConvert.DeserializeObject<emojiData>(textAsset.text);

        var atlasSize = data.meta.size;
        size.h = atlasSize.h / 64;
        size.w = atlasSize.w / 64;
        emojiDic = new Dictionary<string, int>();
        foreach (var v in data.frames)
        {
            emojiDic.Add(v.filename.TrimEnd(trimEnd), (v.frame.x / 64) + (size.h - 1 - v.frame.y / 64) * size.w);
        }
    }

    public int GetEmojiPos(string name)
    {
        int pos = -1;
        if (!emojiDic.TryGetValue(name, out pos))
        {
            bool is_non_qualified = false;
            pos = -1;
            foreach (var v in emojiDic)
            {
                if (v.Key.StartsWith(name))
                {
                    pos = v.Value;
                    is_non_qualified = true;
                    break;
                }
            }

            if (is_non_qualified)
            {
                emojiDic.Add(name, pos);
            }
        }
        return pos;
    }

    public atlasSize GetEmojiSize()
    {
        return size;
    }


    public void Clear()
    {
        if (emoji != null)
        {
            emoji.Unload(true);
            emoji = null;
        }
        emojiTexture = null;
        emojiDic.Clear();
    }

}

class emojiData
{
    public atlasFrames[] frames;
    public atlasMeta meta;
}

class atlasMeta
{
    public atlasSize size;
}

public class atlasSize
{
    public int w;
    public int h;
}

class atlasFrame
{
    public int x;
    public int y;
}

class atlasFrames
{
    public string filename;
    public atlasFrame frame;
}
