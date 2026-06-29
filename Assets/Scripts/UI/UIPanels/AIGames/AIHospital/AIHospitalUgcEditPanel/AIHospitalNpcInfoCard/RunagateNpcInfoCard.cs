using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace AIGame.Base
{
    public class RunagateNpcInfoCard : BaseHospitalNpcInfoView
    {
        [Header("剧情输入")]
        public CButton plotInputBtn;
        public Text plotText;
        public GameObject emptyPlotObj;

        private KeyBoardInfo _plotKBInfo;
        private const int PlotLimitCount = 500;

        public override void InitData(HospitalNPCData npcData)
        {
            base.InitData(npcData);
            
            InitListeners();
            
            // 初始化键盘信息
            InitKeyboardInfo();
            
            // 更新UI
            UpdatePlotUI();
        }
        
        private void InitListeners()
        {
            // 剧情输入按钮
            plotInputBtn.onClick.AddListener(OnPlotInputClick);
        }
        
        private void InitKeyboardInfo()
        {
            _plotKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入场景内NPC剧情（选填）",
                inputMode = 0,
                maxLength = PlotLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = "字数超出限制",
                returnKeyType = (int)ReturnType.Return
            };
        }
        
        private void OnPlotInputClick()
        {
            _plotKBInfo.defaultText = _npcData.npcConfig.plot ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPlotFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_plotKBInfo));
        }
        
        private void OnGetPlotFromNative(string text)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _npcData.npcConfig.plot = text;
            UpdatePlotUI();
        }
        
        private void UpdatePlotUI()
        {
            string content = _npcData.npcConfig.plot ?? "";
            
            if (string.IsNullOrEmpty(content))
            {
                emptyPlotObj.SetActive(true);
                plotText.gameObject.SetActive(false);
            }
            else
            {
                plotText.text = content;
                emptyPlotObj.SetActive(false);
                plotText.gameObject.SetActive(true);
            }
        }
    }
}