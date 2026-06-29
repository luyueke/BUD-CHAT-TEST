using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class ChangeUGCTypeItem : MonoBehaviour
{
    //[SerializeField] private SpriteAtlas avatarAtlas;
    [SerializeField] private GameObject BanImg;//禁用bg
    [SerializeField] private GameObject SelectImg;//选中框
    [SerializeField] private Text NameText;
    [SerializeField] private Image IconImg;
    [SerializeField] private CButton ItemBtn;

    private ChangeUGCTypeConfig changeConfig;

    private Action _clickAction;

    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
    void Start()
    {
        ItemBtn.onClick.AddListener(OnItemClick);
    }

    public void InitData(ChangeUGCTypeConfig template)
    {
        SetConfigData(template);
        SetSelect(false);
        SetName(template.Name);
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, template.Cover, gameObject);
        var coverSprite = sprite;
        SetCover(coverSprite);
    }

    public void SetConfigData(ChangeUGCTypeConfig data)
    {
        changeConfig = data;
    }

    public ChangeUGCTypeConfig GetConfigData()
    {
        return changeConfig;
    }

    public void SetBan(bool value)
    {
        BanImg.SetActive(value);
        ItemBtn.SetClickAble(!value);
    }

    public void SetSelect(bool value)
    {
        SelectImg.SetActive(value);
    }

    public void SetName(string name)
    {
        NameText.SetLocalText(name);
    }

    public void SetCover(Sprite coverSprite)
    {
        IconImg.sprite = coverSprite;
    }

    public bool IsSelect()
    {
        return SelectImg.activeInHierarchy;
    }

    public void AddClickListener(Action action)
    {
        _clickAction += action;
    }

    private void OnItemClick()
    {
        _clickAction?.Invoke();
    }
    
}
