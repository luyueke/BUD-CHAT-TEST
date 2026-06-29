using GameData.BaseInfo;
using Newtonsoft.Json;
using System.Collections;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditBackground : SettingContentBase
    {
        public CButton InputBtn;
        public Text DefTxt;
        public Text DescTxt;
        public Text DescNum;

        private KeyBoardInfo _descKBInfo;
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = 200,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            UpdateDescriptionUI();
        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            InputBtn.onClick.AddListener(OnInputBtn);
        }

        public override void SaveData()
        {
            base.SaveData();
        }

        private void OnInputBtn() {
            // 设置默认文本为当前描述
            _descKBInfo.defaultText = curMapInfo?.gameSetting?.AICommonGameConfig?.plot;

            // 注册回调并显示键盘
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescriptionFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        private void OnGetDescriptionFromNative(string description)
        {
            // 移除回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            // 更新描述
            curMapInfo.gameSetting.AICommonGameConfig.plot = description;

            // 更新UI
            UpdateDescriptionUI();
        }

        private void UpdateDescriptionUI() {
            if (string.IsNullOrEmpty(curMapInfo.gameSetting.AICommonGameConfig.plot))
            {
                // 显示空描述提示
                DefTxt.gameObject.SetActive(true);
                DescTxt.gameObject.SetActive(false);
                DescNum.text = "0/200";
            }
            else
            {
                // 显示描述内容
                DefTxt.gameObject.SetActive(false);
                DescTxt.gameObject.SetActive(true);
                DescTxt.text = curMapInfo.gameSetting.AICommonGameConfig.plot;
                DescNum.text = $"{DescTxt.text.Length}/200";
            }
        }
    }
}