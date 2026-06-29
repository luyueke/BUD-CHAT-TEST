using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class BaseHospitalNpcInfoView : MonoBehaviour
    {
        [Header("基础UI")]
        public Text npcNameText;
        public Text npcRoleTypeText;
        public RemoteImageBehaviour npcImage;

        protected HospitalNPCData _npcData;

        public virtual void InitData(HospitalNPCData npcData)
        {
            _npcData = npcData;
            // 加载NPC图像
            if (!string.IsNullOrEmpty(_npcData.cover))
            {
                npcImage.Load(_npcData.cover);
            }
            npcNameText.text = _npcData.name;
            if (_npcData.role == (int)HospitalNPCType.Provost)
            {
                npcRoleTypeText.text = "身份：监管者";
            }
            else if (_npcData.role == (int)HospitalNPCType.Runagate)
            {
                npcRoleTypeText.text = "身份：逃亡者";
            }   
            this.gameObject.SetActive(true);
        }
    }

    public enum Hospital_Persuade_Type
    {
        Default = 0,
        Persuade = 1,// 1: 剧情说服,
        Answer = 2,// 2: 回答问题
    }

    public class ProvostNpcInfoCard : BaseHospitalNpcInfoView
    {

        [Header("对应类型的View")]
        public GameObject persuadeTypeView;
        public GameObject answerTypeView;

        [Header("说服类型")]
        public CButton persuadeTypeDropdownBtn;
        public Text persuadeTypeText;
        public GameObject dropdownPanel;
        public CButton storyPersuadeBtn; // 剧情说服
        public CButton answerQuestionBtn; // 回答问题

        #region 剧情说服
        [Header("说服方式输入")]
        public CButton persuadeContentInputBtn;
        public Text persuadeContentText;
        public GameObject emptyPersuadeContentObj;
        
        [Header("剧情输入")]
        public CButton plotInputBtn;
        public Text plotText;
        public GameObject emptyPlotObj;
        #endregion 
        
        #region 回答问题
        [Header("问题输入")]
        public CButton questionInputBtn;
        public Text questionText;
        public GameObject emptyQuestionObj;     
        
        [Header("答案输入")]
        public CButton answerInputBtn;
        public Text answerText;
        public GameObject emptyAnswerObj;
        
        [Header("回答问题剧情输入")]
        public CButton answer_plotInputBtn;
        public Text answer_plotText;
        public GameObject answer_emptyPlotObj;
        #endregion
        
        private Hospital_Persuade_Type _persuadeType = Hospital_Persuade_Type.Persuade;
        
        // 键盘信息
        private KeyBoardInfo _persuadeContentKBInfo;
        private KeyBoardInfo _plotKBInfo;
        private KeyBoardInfo _questionKBInfo;
        private KeyBoardInfo _answerKBInfo;
        private KeyBoardInfo _answerPlotKBInfo;
        private const int PersuadeContentLimitCount = 100;
        private const int PlotLimitCount = 500;
        private const int QuestionLimitCount = 200;
        private const int AnswerLimitCount = 300;
        private const int AnswerPlotLimitCount = 500;
        
        public override void InitData(HospitalNPCData npcData)
        {
            base.InitData(npcData);
            
            if (npcData.npcConfig != null)
            {
                _persuadeType = (Hospital_Persuade_Type)npcData.npcConfig.winCondition;
            }
            
            InitUI();
            InitListeners();
            
            // 初始化键盘信息
            InitKeyboardInfo();
            
            // 更新UI
            UpdatePersuadeTypeUI();
            UpdateAllInputFields();
        }
        
        private void InitUI()
        {
            // 隐藏下拉面板
            if (dropdownPanel != null)
            {
                dropdownPanel.SetActive(false);
            }
        }
        
        private void InitListeners()
        {
            // 下拉框按钮
            persuadeTypeDropdownBtn.onClick.AddListener(OnPersuadeTypeDropdownClick);
            
            // 下拉选项按钮
            storyPersuadeBtn.onClick.AddListener(() => OnPersuadeTypeSelected(Hospital_Persuade_Type.Persuade));
            answerQuestionBtn.onClick.AddListener(() => OnPersuadeTypeSelected(Hospital_Persuade_Type.Answer));
            
            // 输入框按钮
            persuadeContentInputBtn.onClick.AddListener(OnPersuadeContentInputClick);
            plotInputBtn.onClick.AddListener(OnPlotInputClick);
            questionInputBtn.onClick.AddListener(OnQuestionInputClick);
            answerInputBtn.onClick.AddListener(OnAnswerInputClick);
            answer_plotInputBtn.onClick.AddListener(OnAnswerPlotInputClick);
        }
        
        private void InitKeyboardInfo()
        {
            _persuadeContentKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入说服方式",
                inputMode = 0,
                maxLength = PersuadeContentLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = "字数超出限制",
                returnKeyType = (int)ReturnType.Return
            };
            
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
            
            _questionKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入通关问题",
                inputMode = 0,
                maxLength = QuestionLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = "字数超出限制",
                returnKeyType = (int)ReturnType.Return
            };
            
            _answerKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入参考答案",
                inputMode = 0,
                maxLength = AnswerLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = "字数超出限制",
                returnKeyType = (int)ReturnType.Return
            };
            
            _answerPlotKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入回答问题后的剧情（选填）",
                inputMode = 0,
                maxLength = AnswerPlotLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = "字数超出限制",
                returnKeyType = (int)ReturnType.Return
            };
        }
        
        private void OnPersuadeTypeDropdownClick()
        {
            dropdownPanel.SetActive(!dropdownPanel.activeSelf);
        }
        
        private void OnPersuadeTypeSelected(Hospital_Persuade_Type type)
        {
            if(type == Hospital_Persuade_Type.Persuade)
            {
                storyPersuadeBtn.transform.SetAsFirstSibling();
                storyPersuadeBtn.GetComponentInChildren<Text>().color = Color.white;
                answerQuestionBtn.GetComponentInChildren<Text>().color = Color.black;
            }
            else if(type == Hospital_Persuade_Type.Answer)
            {
                answerQuestionBtn.transform.SetAsFirstSibling();
                answerQuestionBtn.GetComponentInChildren<Text>().color = Color.white;
                storyPersuadeBtn.GetComponentInChildren<Text>().color = Color.black;
            }
            
            _persuadeType = type;
            _npcData.npcConfig.winCondition = (int)type;
            UpdatePersuadeTypeUI();
            UpdateAllInputFields();
            dropdownPanel.SetActive(false);
        }
        
        private void UpdatePersuadeTypeUI()
        {
            persuadeTypeText.text = _persuadeType == Hospital_Persuade_Type.Persuade ? "剧情说服" : "回答问题";
            
            // 根据说服类型显示/隐藏不同的输入面板
            persuadeTypeView.SetActive(_persuadeType == Hospital_Persuade_Type.Persuade);
            answerTypeView.SetActive(_persuadeType == Hospital_Persuade_Type.Answer);
        }
        
        private void UpdateAllInputFields()
        {
            UpdatePersuadeContentUI();
            
            if (_persuadeType == Hospital_Persuade_Type.Persuade)
            {
                UpdatePlotUI(); // 剧情说服模式下更新剧情UI
            }
            else if (_persuadeType == Hospital_Persuade_Type.Answer)
            {
                UpdateQuestionUI();
                UpdateAnswerUI();
                UpdateAnswerPlotUI(); // 回答问题模式下更新回答问题剧情UI
            }
        }
        
        private void OnPersuadeContentInputClick()
        {
            _persuadeContentKBInfo.defaultText = _npcData.npcConfig.persuadeContent ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPersuadeContentFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_persuadeContentKBInfo));
        }
        
        private void OnGetPersuadeContentFromNative(string text)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _npcData.npcConfig.persuadeContent = text;
            UpdatePersuadeContentUI();
        }
        
        private void UpdatePersuadeContentUI()
        {
            string content = _npcData.npcConfig.persuadeContent ?? "";
            
            if (string.IsNullOrEmpty(content))
            {
                emptyPersuadeContentObj.SetActive(true);
                persuadeContentText.gameObject.SetActive(false);
            }
            else
            {
                persuadeContentText.text = content;
                emptyPersuadeContentObj.SetActive(false);
                persuadeContentText.gameObject.SetActive(true);
            }
        }
        
        private void OnPlotInputClick()
        {
            if (_persuadeType != Hospital_Persuade_Type.Persuade)
            {
                return;
            }
            
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
        
        private void OnQuestionInputClick()
        {
            _questionKBInfo.defaultText = _npcData.npcConfig.question ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetQuestionFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_questionKBInfo));
        }
        
        private void OnGetQuestionFromNative(string text)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _npcData.npcConfig.question = text;
            UpdateQuestionUI();
        }
        
        private void UpdateQuestionUI()
        {
            string content = _npcData.npcConfig.question ?? "";
            
            if (string.IsNullOrEmpty(content))
            {
                emptyQuestionObj.SetActive(true);
                questionText.gameObject.SetActive(false);
            }
            else
            {
                questionText.text = content;    
                emptyQuestionObj.SetActive(false);
                questionText.gameObject.SetActive(true);
            }
        }
        
        private void OnAnswerInputClick()
        {
            _answerKBInfo.defaultText = _npcData.npcConfig.answer ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetAnswerFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_answerKBInfo));
        }
        
        private void OnGetAnswerFromNative(string text)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _npcData.npcConfig.answer = text;
            UpdateAnswerUI();
        }
        
        private void UpdateAnswerUI()
        {
            string content = _npcData.npcConfig.answer ?? "";
            
            if (string.IsNullOrEmpty(content))
            {
                emptyAnswerObj.SetActive(true);
                answerText.gameObject.SetActive(false);
            }
            else
            {
                answerText.text = content;
                emptyAnswerObj.SetActive(false);
                answerText.gameObject.SetActive(true);
            }
        }
        
        private void OnAnswerPlotInputClick()
        {
            if (_persuadeType != Hospital_Persuade_Type.Answer)
            {
                return;
            }
            
            _answerPlotKBInfo.defaultText = _npcData.npcConfig.plot ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetAnswerPlotFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_answerPlotKBInfo));
        }
        
        private void OnGetAnswerPlotFromNative(string text)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _npcData.npcConfig.plot = text;
            UpdateAnswerPlotUI();
        }
        
        private void UpdateAnswerPlotUI()
        {
            if (_persuadeType != Hospital_Persuade_Type.Answer)
            {
                return;
            }
            
            string content = _npcData.npcConfig.plot ?? "";
            
            if (string.IsNullOrEmpty(content))
            {
                answer_emptyPlotObj.SetActive(true);
                answer_plotText.gameObject.SetActive(false);
            }
            else
            {
                answer_plotText.text = content;
                answer_emptyPlotObj.SetActive(false);
                answer_plotText.gameObject.SetActive(true);
            }
        }
    }
}