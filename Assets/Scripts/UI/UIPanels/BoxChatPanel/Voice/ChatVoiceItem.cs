using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChatVoiceItem : MonoBehaviour
    {
        public GameObject riGo;                // 日语标识
        public GameObject yingGo;              // 英语标识
        public GameObject zhongGo;             // 中文标识
        public RemoteImageBehaviour CoverImage;
        public Button buyBtn;
        public Text txtBuy;
        public Text text_buyBtn;  // 购买按钮价格文本
        public Button playBtn;

        public GameObject ownGo;
        public GameObject selectedImgGo;

        private CabinCharacterToneSearchSubData _data;
        private Action _onItemClick;
        private Action _onPlayClick;
        private Action _onBuyClick;

        private void Awake()
        {
            playBtn?.onClick.AddListener(() => _onPlayClick?.Invoke());
            buyBtn?.onClick.AddListener(() => _onBuyClick?.Invoke());
            var bg = GetComponent<Button>();
            if (bg != null)
                bg.onClick.AddListener(() => _onItemClick?.Invoke()); // 只触发选中
        }

        /// <summary>
        /// 绑定音色数据和回调。
        /// onItemClick : 点击 item 主体（仅选中）
        /// onPlayClick : 点击播放按钮
        /// onBuyClick  : 点击购买按钮（打开 ChatBuyVoiceCom）
        /// </summary>
        public void SetData(
            CabinCharacterToneSearchSubData data,
            Action onItemClick,
            Action onPlayClick,
            Action onBuyClick)
        {
            _data = data;
            _onItemClick = onItemClick;
            _onPlayClick = onPlayClick;
            _onBuyClick = onBuyClick;

            gameObject.SetActive(true);
            RefreshUI();
        }

        public void SetEmpty()
        {
            _data = null;
            gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            var tone = _data?.ugcInfo;
            if (tone == null) return;

            // 封面
            if (!string.IsNullOrEmpty(tone.cover))
            {
                CoverImage?.gameObject.SetActive(false);
                CoverImage?.Load(tone.cover, true,
                    (_, ok) => { if (CoverImage != null) CoverImage.gameObject.SetActive(true); });
            }

            // 语言标识
            var langs = tone.languageList;
            zhongGo?.SetActive(langs?.Exists(l => l.type == 0) ?? false);
            yingGo?.SetActive(langs?.Exists(l => l.type == 1) ?? false);
            riGo?.SetActive(langs?.Exists(l => l.type == 2) ?? false);

            // 已拥有判断：consumed==1 或 price==0（免费）
            int price = tone.paymentInfo?.price ?? 0;
            bool owned = price == 0 || (_data?.interactInfo?.consumed == 1);

            ownGo?.SetActive(owned);
            playBtn?.gameObject.SetActive(!owned);

            // 购买按钮：未拥有且有价格时显示
            bool showBuy = !owned && price > 0;
            buyBtn?.gameObject.SetActive(showBuy);
            if (showBuy)
            {
                string priceStr = price.ToString();
                if (txtBuy != null) txtBuy.text = priceStr;
                if (text_buyBtn != null) text_buyBtn.text = priceStr;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            selectedImgGo?.SetActive(selected);
        }

        /// <summary>返回中文预览 URL（type == 0），若无则取第一个可用 URL</summary>
        public string GetPreviewUrl()
        {
            var langs = _data?.ugcInfo?.languageList;
            if (langs == null || langs.Count == 0) return "";
            return langs.Find(l => l.type == 0)?.voiceUrl
                ?? langs.Find(l => !string.IsNullOrEmpty(l.voiceUrl))?.voiceUrl
                ?? "";
        }

        public CabinCharacterToneSearchSubData GetData() => _data;
    }
}
