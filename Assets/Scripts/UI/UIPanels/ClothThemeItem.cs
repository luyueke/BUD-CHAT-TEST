using System;
using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ClothThemeItem : MonoBehaviour
{
    public RawImage themeImage;
    public CText themeText;
    public Image themeSelect;
    public Button root;

    public Sprite select;
    public Sprite unSelect;

    private Action<ClothThemeItem> clickAction;

    /// <summary>
    /// 衣服模版
    /// </summary>
    private ClothesTemplate _clothesTemplate;
    
    /// <summary>
    /// 素材模版
    /// </summary>
    private PropTemplate _propTemplate;

    /// <summary>
    /// 材质模版
    /// </summary>
    private UGCMaterialTemplate _materialTemplate;
    

    public ClothesTemplate getTemplate()
    {
        return _clothesTemplate;
    }

    public PropTemplate getPropTemplate()
    {
        return _propTemplate;
    }

    public UGCMaterialTemplate getMaterialTemplate()
    {
        return _materialTemplate;
    }

    /// <summary>
    /// 初始化衣服模版
    /// </summary>
    /// <param name="clothesTemplate"></param>
    public void Init(ClothesTemplate clothesTemplate)
    {
        this._clothesTemplate = clothesTemplate;
        if (!string.IsNullOrEmpty(clothesTemplate.Cover))
        {
            var coverTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(clothesTemplate.Cover, gameObject);
            themeImage.texture = coverTexture;
        }

        if (!string.IsNullOrEmpty(clothesTemplate.Name))
        {
            themeText.text = clothesTemplate.Name;
        }


        root.onClick.AddListener(ThemeClick);
    }

    /// <summary>
    /// 初始化素材模版 
    /// </summary>
    /// <param name="propTemplate"></param>
    public void InitProp(PropTemplate propTemplate)
    {
        this._propTemplate = propTemplate;
        if (!string.IsNullOrEmpty(propTemplate.Cover))
        {
            var coverTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(propTemplate.Cover, gameObject);
            themeImage.texture = coverTexture;
        }

        if (!string.IsNullOrEmpty(propTemplate.Name))
        {
            themeText.text = propTemplate.Name;
        }


        root.onClick.AddListener(ThemeClick);
    }
    
    /// <summary>
    /// 初始化素材模版 
    /// </summary>
    /// <param name="ugcItemTemplate"></param>
    public void InitMaterial(UGCMaterialTemplate ugcMaterialTemplate)
    {
        this._materialTemplate = ugcMaterialTemplate;
        if (!string.IsNullOrEmpty(ugcMaterialTemplate.Cover))
        {
            var coverTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(ugcMaterialTemplate.Cover, gameObject);
            themeImage.texture = coverTexture;
        }

        if (!string.IsNullOrEmpty(ugcMaterialTemplate.Name))
        {
            themeText.text = ugcMaterialTemplate.Name;
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

    public void SetOnClickAction(Action<ClothThemeItem> clickAction)
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