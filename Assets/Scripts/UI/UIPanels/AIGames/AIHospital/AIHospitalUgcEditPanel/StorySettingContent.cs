using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class StorySettingContent : SettingContentBase
    {
        public CButton Btn_InputDesc;
        public GameObject emptyDescObj;
        public Text descriptionText;
        private KeyBoardInfo _descKBInfo;
        private int DescLimitCount = 300;

        public override void InitUIComponent()
        {
            base.InitUIComponent();
            Btn_InputDesc.onClick.AddListener(OnBtnInputDescClick);
        }

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = DescLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            // 初始化描述文本
            UpdateDescriptionUI();
        }

        private void OnBtnInputDescClick()
        {
            // 设置默认文本为当前描述
            _descKBInfo.defaultText = curMapInfo?.gameSetting?.aIGameConfig?.plot;

            // 注册回调并显示键盘
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescriptionFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        private void OnGetDescriptionFromNative(string description)
        {
            // 移除回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            // 更新描述
            if (curMapInfo?.gameSetting?.aIGameConfig != null)
            {
                curMapInfo.gameSetting.aIGameConfig.plot = description;
            }

            // 更新UI
            UpdateDescriptionUI();

            // 通知数据变更
            SyncEditData();
        }

        private void UpdateDescriptionUI()
        {
            if (string.IsNullOrEmpty(curMapInfo?.gameSetting?.aIGameConfig?.plot))
            {
                // 显示空描述提示
                emptyDescObj.SetActive(true);
                if (descriptionText != null)
                {
                    descriptionText.gameObject.SetActive(false);
                }
            }
            else
            {
                // 显示描述内容
                if (descriptionText != null)
                {
                    descriptionText.text = curMapInfo?.gameSetting?.aIGameConfig?.plot;
                    descriptionText.gameObject.SetActive(true);
                }
                emptyDescObj.SetActive(false);
            }
        }

        public override void SyncEditData()
        {
            base.SyncEditData();
            UpdateDescriptionUI();
        }
    }
}
