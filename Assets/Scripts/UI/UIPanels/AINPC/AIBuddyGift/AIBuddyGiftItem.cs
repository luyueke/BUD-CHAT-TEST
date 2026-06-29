using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Store;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyGiftItem : MonoBehaviour
{
    [SerializeField]private CButton ItemBtn;
    [SerializeField]private GameObject SelectBg;
    [SerializeField]private Text NameText;
    [SerializeField]private Image IconImg;

    private Action _onSelectListener;
    private Action  _onSendClickListener;

    private GoodsData curSelectData;
    private void Start()
    {
        ItemBtn.onClick.AddListener(OnItemBtnClick);
    }
    
    public void SetData(GoodsData data)
    {
        curSelectData = data;
        NameText.SetLocalText(data.Name);
        SetSelectState(data.Selected);
        SetIcon(data);
    }

    private void SetIcon(GoodsData goodsData)
    {
        var config = DataTables.GetEmoUIConfig(goodsData.Id);
        if (config == null) {
            LoggerUtils.LogError($"Can not find emote ui config ,pgcID:{goodsData.Id}");
            return ;
        }

        var atlas = AIBuddyDataManager.AIBuddyAtlas;
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, config.pgcEmoteIconName, IconImg.gameObject);
        if (sprite != null)
        {
            IconImg.gameObject.SetActive(true);
            IconImg.sprite = sprite;
        }
        
    }

    public void SetSelectState(bool isVisible)
    {
        SelectBg.gameObject.SetActive(isVisible);
        NameText.gameObject.SetActive(!isVisible);
    }

    private void OnItemBtnClick()
    {
        _onSelectListener?.Invoke();
    }
    

    private void OnSendItemClick()
    {
        LoggerUtils.Log("#####AIBuddyGiftItem 当前发送："+curSelectData.Id);
        _onSendClickListener?.Invoke();
    }

    public void SetClickListener(Action callback)
    {
        ClearClickListener();
        AddClickListener(callback);
    }

    public void AddClickListener(Action callback)
    {
        _onSelectListener += callback;
    }

    public void ClearClickListener()
    {
        _onSelectListener = null;
    }

    public void SetSendClickListener(Action callback)
    {
        _onSendClickListener = callback;
    }
}
