using System;
using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ThemeItem : MonoBehaviour
{
    public RawImage themeImage;
    public CText themeText;
    public Image themeSelect;
    public Button root;

    public Sprite select;
    public Sprite unSelect;

    private Action<ThemeItem> clickAction;

    private MapTemplate _mapTemplate;

    public MapTemplate getMapTemplate()
    {
        return _mapTemplate;
    }

    public void Init(MapTemplate mapTemplate)
    {
        this._mapTemplate = mapTemplate;
        if (!string.IsNullOrEmpty(mapTemplate.Cover))
        {
            var coverTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(mapTemplate.Cover, gameObject);
            themeImage.texture = coverTexture;
        }

        if (!string.IsNullOrEmpty(mapTemplate.Name))
        {
            themeText.text = mapTemplate.Name;
        }


        root.onClick.AddListener(ThemeClick);
    }

    private void ThemeClick()
    {
        if (clickAction != null)
        {
            clickAction.Invoke(this);
        }
    }

    public void SetOnClickAction(Action<ThemeItem> clickAction)
    {
        this.clickAction = clickAction;
    }

    public void SetSelect(bool isSelect)
    {
        if (isSelect)
        {
            themeSelect.sprite = select;
        }
        else
        {
            themeSelect.sprite = unSelect;
        }
    }
}