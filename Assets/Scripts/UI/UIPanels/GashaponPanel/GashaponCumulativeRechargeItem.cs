using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.UIPanels.GashaponPanel
{
    public class GashaponCumulativeRechargeItem : MonoBehaviour
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
        private Action<GashaponCumulativeRechargeItem, GashaponRewardData> _clickAction;
        private GashaponRewardData _data;
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

        public void Init(GashaponRewardData data,Action<GashaponCumulativeRechargeItem,GashaponRewardData> click)
        {
            InitSprite(data);
            int level = (int)data.Level;

            if (levelColor.ContainsKey(level))
            {
                bgImg.color = DataUtil.DeSerializeColor(levelColor[level]);
            }
            _data = data;
            _clickAction = click;
            numText.text = data.Num > 1 ? "x" + data.Num : "";
            bool isOwned = AssetsDataManager.IsOwned(data.Id);
            ownObj.SetActive(isOwned);
            SetLoadingVisible(false);
        }

        public void InitSprite(GashaponRewardData data)
        {
            pgcIcon.gameObject.SetActive(false);
            currencyIcon.gameObject.SetActive(false);
            //货币类
            var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
            if (data.PgcDatas == null && currencyType != CurrencyType.None)
            {
                PgcUtils.LoadCurrencyIconAsync(currencyType,gameObject, (iconSprite) =>
                {
                    if (this != null && currencyIcon!= null && iconSprite != null)
                    {
                        currencyIcon.gameObject.SetActive(true);
                        currencyIcon.sprite = iconSprite;
                    }
                });
            }
            else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.Avatar)
            {//皮肤
                if (!string.IsNullOrEmpty(data.BundleId))
                {
                    PgcUtils.LoadBundleIconAsync(data.BundleId,gameObject, (iconSprite) =>
                    {
                        if (this != null && pgcIcon != null && iconSprite != null)
                        {
                            pgcIcon.gameObject.SetActive(true);
                            pgcIcon.sprite = iconSprite;
                        }
                    });
                }
                else
                {
                    PgcUtils.LoadAvatarIconAsync(data.Id,gameObject, (iconSprite) =>
                    {
                        if (this != null && pgcIcon != null && iconSprite != null)
                        {
                            pgcIcon.gameObject.SetActive(true);
                            pgcIcon.sprite = iconSprite;
                        }
                    });
                }
            }  else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.PGCPetAvatar)
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
            else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.Emote)
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
            else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.AvatarFrame)
            { //称号
                var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(data.Id);
                if (titleData != null && !string.IsNullOrEmpty(titleData.Icon))
                {
                    var spriteWrapper = Loader.Load<Sprite>(titleData.Icon);
                    var sp = spriteWrapper?.RetainAsset(gameObject);
                    if (sp != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = sp;
                        pgcIcon.SetNativeSize();
                        pgcIcon.transform.localScale = Vector2.one*0.7f;
                    }
                }
            }
        }

        public GashaponRewardData GetBindData()
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
}
