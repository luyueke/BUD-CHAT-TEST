using System;
using Game.MusicalInstrument;
using Game.Store;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class UgcAnimToneStoreItemView : MonoBehaviour
{
    [SerializeField] CButton Btn_View;
    [SerializeField] Image CurrencyIcon;
    [SerializeField] Image SelectBg;
    [SerializeField] Text assetName;
    [SerializeField] Text toneName;
    [SerializeField] private Color SelectColor;
    [SerializeField] Font numFont;
    [SerializeField] Font textFont;
    
    private RecommendItemData _curData;
    private Action<RecommendItemData> _onSelectAct;

    // 跨 Item 共享：记录当前正在试听的 item，切换时停止上一个
    private static UgcAnimToneStoreItemView _playingItem;

    private void Awake()
    {
        Btn_View.onClick.AddListener(OnBtnViewClick);
    }

    public void SetData(RecommendItemData data, Action<RecommendItemData> clickAction = null)
    {
        if (data == null)
            return;

        // OSA 复用：若此 item 正在播放则停止
        StopPreviewIfPlaying();

        this._curData = data;
        this._onSelectAct = clickAction;
        SetToneName(data.UgcInfo.name);
        AdjustPriceUI();
    }

    private void OnDisable()
    {
        // OSA 回收 / 面板关闭时停止试听
        StopPreviewIfPlaying();
    }

    private void StopPreview()
    {
        var loader = gameObject.GetComponent<UgcToneLoaderBehaviour>();
        loader?.Stop();
    }

    private void StopPreviewIfPlaying()
    {
        if (_playingItem == this)
        {
            StopPreview();
            _playingItem = null;
        }
    }

    private UgcToneLoaderBehaviour GetOrCreateToneLoader()
    {
        var loader = gameObject.GetComponent<UgcToneLoaderBehaviour>();
        return loader != null ? loader : gameObject.AddComponent<UgcToneLoaderBehaviour>();
    }

    public void SetOwned()
    {
        if (_curData == null)
        {
            return;
        }
        _curData.interactInfo.consumed = 1;
        AdjustPriceUI();
    }

    private void AdjustPriceUI()
    {
        var fixedData = _curData?.UgcInfo as AnimMusicInfo;

        if (fixedData == null)
        {
            return;
        }
        
        var isOwned = _curData?.interactInfo?.consumed == 1;
        if (isOwned)
        {
            CurrencyIcon.gameObject.SetActive(false);
            assetName.font = textFont;
            assetName.text = "已拥有";
        }
        else
        {
            var currencyType = fixedData.paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
            CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
            var price = fixedData.paymentInfo?.price ?? 0;
            
            CurrencyIcon.gameObject.SetActive(price > 0);
            if (price > 0)
            {
                assetName.font = numFont;
                assetName.text = price.ToString();
            }
            else
            {
                assetName.text = "免费";
                assetName.font = textFont;
            }
        }
    }

    private void OnBtnViewClick()
    {
        if (_curData == null) return;

        // 试听切换：同一 item 再点则停止，否则停旧播新
        if (_playingItem == this)
        {
            StopPreviewIfPlaying();
        }
        else
        {
            // 停止上一个正在播放的 item
            if (_playingItem != null)
            {
                _playingItem.StopPreview();
                _playingItem = null;
            }

            var info = _curData.UgcInfo as AnimMusicInfo;
            if (info != null && !string.IsNullOrEmpty(info.metaDataUrl))
            {
                _playingItem = this;
                // 在 item 自身的 GameObject 上播放 2D 音频，不依赖 GlobalMainCamera
                GetOrCreateToneLoader().LoadAudioClipAndPlay(PreviewAudioType.TwoD, info.metaDataUrl, volumeOverride: 1.0f);
            }
        }

        this._onSelectAct?.Invoke(_curData);
    }

    public void SetSelectState(bool isSelected)
    {
        SelectBg.color = isSelected ? SelectColor : Color.white;
    }

    private void SetToneName(string name)
    {
        this.toneName.text = name;
    }
}