using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using Newtonsoft.Json;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.UIPanels.GashaponPanel
{
    public class GashaponPriceItem : MonoBehaviour
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
        [SerializeField] private GameObject redDot;
        private Action<GashaponPriceItem,GashaponRewardData> _clickAction;
        private GashaponRewardData _data;
        private Dictionary<int, string> levelColor = new Dictionary<int, string>()
        {
            {1,"FFA95A"},
            {2,"9F72FF"},
            {3,"92BEFF"},
            {4, "FF785A"},
            {
                5,"7BED72"
            }
        };

        public void Awake()
        {
            clickBtn.onClick.AddListener(OnItemClick);
        }

        public void Init(GashaponRewardData data,Action<GashaponPriceItem,GashaponRewardData> click)
        {
            InitSprite(data);
            int level = (int)data.Level;

            if (levelColor.ContainsKey(level))
            {
                bgImg.color = DataUtil.DeSerializeColor(levelColor[level]);
            } else {
                // 未存在等级, 则使用最高等级
                bgImg.color = DataUtil.DeSerializeColor(levelColor[3]);
            }

            _data = data;
            _data.iconSprite = pgcIcon.sprite; //主页皮肤必须记录sprite，方便展示，后续更换为动态
            _clickAction = click;
            numText.text = data.Num > 1 ? "x" + data.Num : "";
            bool isCurrency = data.PgcDatas == null && GameUtils.ConvertRewardType((int)data.RewardType) != CurrencyType.None;
            bool isOwned = GashaponUtils.IsOwnedReward(data);
            ownObj.SetActive(!isCurrency && isOwned);
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
            else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0]?.ResourceType == ResourceType.Vehicle){
                var sprite = PgcUtils.LoadVehicleIcon(data.Id,gameObject);
                if (sprite != null && pgcIcon != null)
                {
                    pgcIcon.gameObject.SetActive(true);
                    pgcIcon.sprite = sprite;
                }
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
            else if (data.RewardType == RewardType.RewardAvatarFrame)
            { //头像框
                UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.Id,gameObject, (iconSprite) =>
                {
                    if (this != null && pgcIcon != null && iconSprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = iconSprite;
                    }
                });
            }
            else if (data.RewardType == RewardType.RewardChatBubbles)
            { //聊天气泡
                UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.Id,gameObject, (iconSprite) =>
                {
                    if (this != null && pgcIcon != null && iconSprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = iconSprite;
                    }
                });
            }
            else if ((int)data.RewardType == (int)BUDRewardType.RewardUgcTemplateResource)
            { //宠物模版
                PgcUtils.LoadPetUGCTemplateAsync(data.Id,gameObject, (iconSprite) =>
                {
                    if (this != null && pgcIcon != null && iconSprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = iconSprite;
                    }
                });
            }
            else if ((int)data.RewardType == (int)BUDRewardType.RewardHomepageSkin)
            {   //主页皮肤
                var sprite = ProfileThemeManager.Inst.LoadThemeIcon(data.Id, gameObject);
                    if (sprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = sprite;
                    }
            }
            else if((int)data.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
            {
                UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
                {
                    if (this != null && pgcIcon != null && iconSprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = iconSprite;
                        pgcIcon.SetNativeSize();
                        pgcIcon.transform.localScale = pgcIcon.transform.localScale * 0.4f;
                    }
                });
            }
            else if((int)data.RewardType == (int)BUDRewardType.RewardTypeTitle)
            {
                UserUIWidgetManager.Inst.GetTitleImgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
                {
                    if (this != null && pgcIcon != null && iconSprite != null)
                    {
                        pgcIcon.gameObject.SetActive(true);
                        pgcIcon.sprite = iconSprite;
                        pgcIcon.SetNativeSize();
                        pgcIcon.transform.localScale = pgcIcon.transform.localScale * 0.5f;
                    }
                });
            }
            else
            {
                var sprite1 = PgcUtils.LoadRewardIcon((BUDRewardType)data.RewardType, gameObject);
                if (sprite1 != null)
                {
                    pgcIcon.gameObject.SetActive(true);
                    pgcIcon.sprite = sprite1;
                }
            }
        }

        public void SetOwnedStatus(bool isOwned) {
            bool isCurrency = _data.PgcDatas == null && GameUtils.ConvertRewardType((int)_data.RewardType) != CurrencyType.None;
            ownObj.SetActive(!isCurrency && isOwned);
        }

        public GashaponRewardData GetBindData()
        {
            return _data;
        }

        public void OnItemClick()
        {
            _clickAction?.Invoke(this,_data);
            SetSelectStatus(true);
            SetRedDotVisible(false);
        }

        public void SetRedDotVisible(bool visible)
        {
            if (redDot != null) redDot.SetActive(visible);
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
