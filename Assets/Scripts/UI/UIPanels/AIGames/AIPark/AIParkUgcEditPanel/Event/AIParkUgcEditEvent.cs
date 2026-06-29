using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System;
using Newtonsoft.Json;

namespace AIGame.Base
{
    [Serializable]
    public class AIParkUgcEditEventItem 
    {
        public int idx;
        public Toggle Tog;
        public AICommonGameConfig_Event EventConfig;
    }

    public class AIParkUgcEditEvent : SettingContentBase
    {
        public CButton AddBtn;
        public CButton DelBtn;
        public List<AIParkUgcEditEventItem> ItemLs;

        public InputField Name;
        public CButton NameBtn;
        public CButton HelpBtn;

        public InputField Target;
        public CButton TargetBtn;

        public InputField DescTxt;
        public Text DescNum;
        public CButton DescBtn;

        public InputField TimeLimit;
        public CButton TimeLimitBtn;

        public Dropdown TypeDrop;

        public CButton QuestionBtn;
        public InputField Question;
        public Transform QuestionGroup;
        public CButton AnswerBtn;
        public List<AIParkUgcEditEventAnswerItem> AnswerItemLs;

        public Transform AnswerGroup;
        public CButton ConfirmBtn;
        public CButton AnswerInputBtn;
        public InputField AnswerInput;

        public AIParkUgcEditEventAction ActionGroup;

        [HideInInspector]public AIParkUgcEditEventItem curItem;
        private AIParkUgcEditEventAnswerItem curAnswer;

        private KeyBoardInfo _nameKBInfo;
        private KeyBoardInfo _targetKBInfo;
        private KeyBoardInfo _descKBInfo;
        private KeyBoardInfo _timeKBInfo;
        private KeyBoardInfo _questionKBInfo;
        private KeyBoardInfo _answerKBInfo;

        public MapInfo CurMapInfo => curMapInfo;
        public override void InitUIComponent()
        {
            base.InitUIComponent();

            AddBtn.onClick.AddListener(OnAddBtn);
            DelBtn.onClick.AddListener(OnDelBtn);

            HelpBtn.onClick.AddListener(OnHelpBtn);
            NameBtn.onClick.AddListener(OnNameBtn);

            TargetBtn.onClick.AddListener(OnTargetBtn);

            DescBtn.onClick.AddListener(OnDescBtn);

            TimeLimitBtn.onClick.AddListener(OnTimeBtn);

            TypeDrop.onValueChanged.AddListener(OnDrop);

            QuestionBtn.onClick.AddListener(OnQuestionBtn);
            AnswerBtn.onClick.AddListener(OnAnswerBtn);

            AnswerInputBtn.onClick.AddListener(OnAnswerInputBtn);
            ConfirmBtn.onClick.AddListener(OnConfirmBtn);

        }

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            for (int i = 0; i < AnswerItemLs.Count; i++)
            {
                AnswerItemLs[i].Init(this,i);
            }

            var events = mapInfo.gameSetting.AICommonGameConfig.events;
            if (events == null || events.Count <= 0)
            {
                events = new List<AICommonGameConfig_Event>();
                events.Add(new AICommonGameConfig_Event());
            }
            for (int i = 0; i < ItemLs.Count; i++)
            {
                var item = ItemLs[i];
                if (i < events.Count)
                {
                    item.EventConfig = events[i];
                    item.Tog.gameObject.SetActive(true);
                }
                else
                {
                    item.Tog.gameObject.SetActive(false);
                }
                item.Tog.onValueChanged.AddListener((succ) => { OnTog(succ,item); });
            }

            OnTog(true, ItemLs[0]);

            #region 输入

            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入名称",
                inputMode = 0,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _targetKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入指引",
                inputMode = 0,
                maxLength = 15,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入描述",
                inputMode = 0,
                maxLength = 150,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _timeKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入限时",
                inputMode = 1,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
            _questionKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入问题",
                inputMode = 0,
                maxLength = 50,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _answerKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入名称",
                inputMode = 0,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            #endregion
        }

        private void OnDisable()
        {
            curMapInfo.gameSetting.AICommonGameConfig.events = new List<AICommonGameConfig_Event>();
            foreach (var item in ItemLs)
            {
                if (item.EventConfig != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.events.Add(item.EventConfig);
                }
            }
        }

        public override void SaveData()
        {
            base.SaveData();
            curMapInfo.gameSetting.AICommonGameConfig.events = new List<AICommonGameConfig_Event>();
            foreach (var item in ItemLs)
            {
                if (item.EventConfig != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.events.Add(item.EventConfig);
                }
            }
        }

        private void RefreshView() {
            TypeDrop.SetValueWithoutNotify(curItem.EventConfig.type);
            Name.text = curItem.EventConfig.name;
            Target.text = curItem.EventConfig.target;
            if (string.IsNullOrEmpty(curItem.EventConfig.desc))
            {
                // 显示空描述提示
                DescTxt.text = "";
                DescNum.text = "0/150";
            }
            else
            {
                // 显示描述内容
                DescTxt.text = curItem.EventConfig.desc;
                DescNum.text = $"{DescTxt.text.Length}/150";
            }
            if (curItem.EventConfig.limitDuration > 0)
            {
                TimeLimit.text = curItem.EventConfig.limitDuration + "秒";
            }
            else
            {
                TimeLimit.text = "";
            }
            QuestionGroup.gameObject.SetActive(curItem.EventConfig.type == 1);
            if (curItem.EventConfig.type == 1)
            {
                Question.text = curItem.EventConfig.question;
                for (int i = 0; i < AnswerItemLs.Count; i++)
                {
                    if (curItem.EventConfig.answers != null && i < curItem.EventConfig.answers.Count)
                    {
                        AnswerItemLs[i].SetData(curItem.EventConfig.answers[i]);
                    }
                    else
                    {
                        AnswerItemLs[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void OpenAnswer(AIParkUgcEditEventAnswerItem answer,string str) {
            curAnswer = answer;
            AnswerGroup.gameObject.SetActive(true);
            AnswerInput.text = str;
        }

        #region 按钮
        private void OnAnswerBtn()
        {
            for (int i = 0; i < AnswerItemLs.Count; i++)
            {
                if (string.IsNullOrEmpty(AnswerItemLs[i].Str))
                {
                    AnswerItemLs[i].gameObject.SetActive(true);
                    OpenAnswer(AnswerItemLs[i],"");
                    return;
                }
            }
            TipPanel.ShowToast("最多添加8个选项");
        }

        private void OnTog(bool bo, AIParkUgcEditEventItem item)
        {
            OnToggleValueChanged(item.Tog.gameObject,bo);
            if (bo)
            {
                curItem = item;

                ActionGroup.SetData(this);

                RefreshView();
            }
        }

        public void OnToggleValueChanged(GameObject obj, bool isOn)
        {
            var togSwitch = obj.GetComponent<CommonToggleSwitch>();
            togSwitch.SetSelectState(isOn);
        }

        private void OnAddBtn()
        {
            foreach (var item in ItemLs)
            {
                if (item.EventConfig != null && item.EventConfig.type == 1)
                {
                    TipPanel.ShowToast("选择事件后不能添加新事件");
                    return;
                }
            }
            var even = new AICommonGameConfig_Event();
            for (int i = 0; i < ItemLs.Count; i++)
            {
                if (ItemLs[i].EventConfig == null)
                {
                    ItemLs[i].Tog.gameObject.SetActive(true);
                    ItemLs[i].EventConfig = even;
                    ItemLs[i].Tog.isOn = true;
                    if (i == ItemLs.Count  - 1)
                    {
                        AddBtn.gameObject.SetActive(false);
                    }
                    return;
                }
            }
        }

        private void OnDelBtn()
        {
            if (curItem == null)
            {
                return;
            }
            if (curItem.idx == 0)
            {
                TipPanel.ShowToast("至少保留一个事件");
                return;
            }

            curItem.Tog.gameObject.SetActive(false);
            curItem.EventConfig = null;
            ItemLs[0].Tog.isOn = true;
            AddBtn.gameObject.SetActive(true);
        }

        private void OnDrop(int idx)
        {
            curItem.EventConfig.type = idx;
            RefreshView();
        }
        private void OnConfirmBtn()
        {
            curAnswer.SetData(AnswerInput.text);
            AnswerGroup.gameObject.SetActive(false);
            curItem.EventConfig.answers.Add(AnswerInput.text);
        }
        private void OnHelpBtn()
        {
            var panel = UIManager.Inst.FindPanel<AIParkUgcEditPanel>(PanelId.AIParkUgcEditPanel);
            panel.HelpGroup.Set(AIParkUgcEditHelpType.Event);
        }
        private void OnNameBtn()
        {
            _nameKBInfo.defaultText = curItem.EventConfig.name;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str)=> {
                curItem.EventConfig.name = str;
                Name.text = curItem.EventConfig.name;
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        private void OnTargetBtn()
        {
            _targetKBInfo.defaultText = curItem.EventConfig.target;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                curItem.EventConfig.target = str;
                Target.text = curItem.EventConfig.target;
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_targetKBInfo));
        }

        private void OnDescBtn()
        {
            _descKBInfo.defaultText = curItem.EventConfig.desc;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                curItem.EventConfig.desc = str;
                if (string.IsNullOrEmpty(curItem.EventConfig.desc))
                {
                    // 显示空描述提示
                    DescTxt.text = "";
                    DescNum.text = "0/150";
                }
                else
                {
                    // 显示描述内容
                    DescTxt.text = curItem.EventConfig.desc;
                    DescNum.text = $"{DescTxt.text.Length}/150";
                }
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        private void OnTimeBtn()
        {
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                if (int.TryParse(str, out var value))
                {
                    if (value < 30)
                    {
                        TipPanel.ShowToast("事件时长大于30秒");
                        return;
                    }
                    if (value > 999)
                    {
                        TipPanel.ShowToast("事件时长小于999秒");
                        return;
                    }
                    curItem.EventConfig.limitDuration = value;
                    TimeLimit.text = curItem.EventConfig.limitDuration + "秒";
                }
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_timeKBInfo));
        }

        private void OnQuestionBtn()
        {
            if (!string.IsNullOrEmpty(curItem.EventConfig.question))
            {
                _questionKBInfo.defaultText = curItem.EventConfig.question;
            }
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                curItem.EventConfig.question = str;
                Question.text = curItem.EventConfig.question;
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_questionKBInfo));
        }

        private void OnAnswerInputBtn()
        {
            if (!string.IsNullOrEmpty(curAnswer.Str))
            {
                _answerKBInfo.defaultText = curItem.EventConfig.name;
            }
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                AnswerInput.text = str;
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_answerKBInfo));
        }
        #endregion

    }
}