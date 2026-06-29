using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class BundleExpandItem : MonoBehaviour
{
    [SerializeField] private Image bgImg;
    [SerializeField] private Image pgcIcon;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private CButton clickBtn;
    [SerializeField] private Text numText;
    [SerializeField] private GameObject ownObj ;
    [SerializeField] private Image outLine ;//用于选中状态
    [SerializeField] private Image ownBg ;//用于选中状态
    [SerializeField] private GameObject loadingGo;
    private Action<BundleExpandItem,AssetsData> _clickAction;
    private AssetsData _data;
    private Dictionary<int, string> levelColor = new Dictionary<int, string>()
    {
        {1,"FFA95A"},
        {2,"9F72FF"},
        {3,"92BEFF"},
    };

    public void Awake()
    {
        clickBtn.onClick.AddListener(OnItemClick);
    }

    public void Init(AssetsData data,Action<BundleExpandItem,AssetsData> click = null)
    {
        SetLevel(0);
        InitSprite(data);
        _data = data;
        _clickAction = click;
        if (_clickAction == null)
        {
            SetClickAble(false);
        }
        bool isOwned = GashaponUtils.IsOwnedAsset(data);
        ownObj.SetActive(isOwned);
        SetLoadingVisible(false);
    }

    public void SetLevel(int level)
    {
        if (levelColor.ContainsKey(level))
        {
            bgImg.color = DataUtil.DeSerializeColor(levelColor[level]);
        }
        else
        {
            bgImg.color = Color.white;
        }
    }

    public void InitSprite(AssetsData data)
    {
        pgcIcon.gameObject.SetActive(false);
        currencyIcon.gameObject.SetActive(false);
        if (data.ResourceType == ResourceType.Avatar)
        {//皮肤
            PgcUtils.LoadAvatarIconAsync(data.Id,gameObject, (iconSprite) =>
            {
                if (this != null && pgcIcon != null && iconSprite != null)
                {
                    pgcIcon.gameObject.SetActive(true);
                    pgcIcon.sprite = iconSprite;
                }
            });
        }  else if (data.ResourceType == ResourceType.PGCPetAvatar)
        {//Pet皮肤

            PgcUtils.LoadPetAvatarIconAsync(data.Id,gameObject, (iconSprite) =>
            {
                if (this != null && pgcIcon != null && iconSprite != null)
                {
                    pgcIcon.gameObject.SetActive(true);
                    pgcIcon.sprite = iconSprite;
                }
            });
        }
        else if (data.ResourceType == ResourceType.Emote)
        { //表情
            PgcUtils.LoadEmoteIconAsync(data.Id,gameObject, (iconSprite) =>
            {
                if (this != null && pgcIcon != null && iconSprite != null)
                {
                    pgcIcon.gameObject.SetActive(true);
                    pgcIcon.sprite = iconSprite;
                }
            });
        }
    }

    public void SetClickAble(bool value)
    {
        clickBtn?.SetClickAble(value);
    }

    public AssetsData GetBindData()
    {
        return _data;
    }

    public void OnItemClick()
    {
        _clickAction?.Invoke(this,_data);
        SetSelectStatus(true);
    }

    public void SetLoadingVisible(bool value)
    {
        loadingGo?.SetActive(value);
    }

    public void SetSelectStatus(bool isSelect)
    {
        outLine.color =  DataUtil.DeSerializeColor(isSelect?"FFD400":"FFFFFF");
        ownBg.color =  DataUtil.DeSerializeColor(isSelect?"FFD400":"FFFFFF");
    }
}

