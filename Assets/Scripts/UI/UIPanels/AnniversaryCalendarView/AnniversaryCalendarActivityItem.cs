using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class AnniversaryCalendarActivityItem : MonoBehaviour
{
    public Text title;
    public Image icon;
    public Image length;
    public Button skipBtn;
    public string iconsAlxs = "Assets/Loadable/UI/UIPanel/AnniversaryCalendarView/Icons.spriteatlas";

    public void SetData(string iconName, int _length, string _title, int off, bool isFirst, int skipId)
    {
        RectTransform trs = gameObject.GetComponent<RectTransform>();
        if(isFirst)
        {
            trs.anchoredPosition = new Vector2(20, trs.anchoredPosition.y);
        }
        icon.sprite = LoadSpriteInAltasInLoad(iconName, iconsAlxs, icon.gameObject);
        icon.SetNativeSize();
        length.sprite =LoadSpriteInAltasInLoad(  "Activity_" + _length, iconsAlxs, length.gameObject);
        length.SetNativeSize();
        skipBtn.onClick.AddListener(() =>
        {
            AnniversarySkipManager.Inst.Skip(skipId);
        });
        title.text = _title;
        if(_title == "甜心舞会")
        {
            RectTransform rt = length.GetComponent<RectTransform>();
            Vector2 size = rt.sizeDelta;
            size.x = Mathf.Max(0f, size.x - 100f);
            rt.sizeDelta = size;
        }
        if(_title == "天使与恶魔")
        {
            RectTransform rt = length.GetComponent<RectTransform>();
            Vector2 size = rt.sizeDelta;
            size.x = Mathf.Max(0f, size.x + 28f);
            rt.sizeDelta = size;
        }
        if (_title == "地雷千禧")
        {
            RectTransform rt = length.GetComponent<RectTransform>();
            Vector2 size = rt.sizeDelta;
            size.x = Mathf.Max(0f, size.x + 120);
            rt.sizeDelta = size;
        }
    }
    // XAssetLoaderMgr.cs中应该修改LoadSpriteInAltas方法
    public Sprite LoadSpriteInAltasInLoad(string spriteName, string atlasName, GameObject go)
    {
        // 使用完整的资源路径
        string atlasPath = "Assets/Loadable/UI/UIPanel/AnniversaryCalendarView/Icons.spriteatlas";

        // 使用xasset加载图集
        var atlas = xasset.Asset.Load(atlasPath, typeof(SpriteAtlas));
        if (atlas == null || atlas.asset == null)
        {
            Debug.LogError($"找不到图集: {atlasPath}");
            return null;
        }

        var spriteAtlas = atlas.asset as UnityEngine.U2D.SpriteAtlas;
        var sprite = spriteAtlas.GetSprite(spriteName);
        if (sprite == null)
        {
            Debug.LogError($"在图集 {atlasPath} 中找不到精灵: {spriteName}");
            return null;
        }

        return sprite;
    }
}
