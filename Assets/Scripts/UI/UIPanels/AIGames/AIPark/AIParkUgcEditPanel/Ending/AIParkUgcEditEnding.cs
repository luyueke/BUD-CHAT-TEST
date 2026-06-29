using GameData.BaseInfo;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Dropdown;

namespace AIGame.Base
{
    public class AIParkUgcEditEndingItem
    {
        public int Idx;
        public Toggle Tog;
        public AICommonGameConfig_Ending EventConfig;
    }
    public class AIParkUgcEditEnding : SettingContentBase
    {
        public CButton HelpBtn;

        public InputField DescTxt;
        public CButton DescBtn;

        public CButton AddBtn;
        public CButton DelBtn;
        public Transform TogParent;
        public CButton TogsBtn;
        public RectTransform TogsBg;
        private List<AIParkUgcEditEndingItem> ItemLs;

        public Dropdown TypeDrop;

        public Transform ConditionGroup;
        public CButton ChooseBtn;
        public Dropdown ConditionDrop;

        private AIParkUgcEditEndingItem CurItem;

        private KeyBoardInfo _descKBInfo;

        private List<string> answers;
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            if (curMapInfo.gameSetting.AICommonGameConfig.endings == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.endings = new List<AICommonGameConfig_Ending>();
            }

            ItemLs = new List<AIParkUgcEditEndingItem>();

            var endings = curMapInfo.gameSetting.AICommonGameConfig.endings;
            var count = TogParent.childCount;
            for (int i = 0; i < count; i++)
            {
                var item = new AIParkUgcEditEndingItem();
                item.Tog = TogParent.GetChild(i).GetComponent<Toggle>();
                item.Idx = i;
                if (i < endings.Count)
                {
                    item.EventConfig = endings[i];
                }

                if (i == 0 && item.EventConfig == null)
                {
                    item.EventConfig = new AICommonGameConfig_Ending();
                }

                item.Tog.gameObject.SetActive(item.EventConfig != null);

                item.Tog.onValueChanged.AddListener((succ) => { OnTog(succ, item); });
                ItemLs.Add(item);
            }
            RefreshAns();
            ItemLs[0].Tog.isOn = true;
            OnTog(true, ItemLs[0]);

            RefreshTogsBg();

            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入描述",
                inputMode = 0,
                maxLength = 200,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            AddBtn.onClick.AddListener(OnAddBtn);
            DelBtn.onClick.AddListener(OnDelBtn);

            HelpBtn.onClick.AddListener(OnHelpBtn);

            DescBtn.onClick.AddListener(OnDescBtn);

            TypeDrop.onValueChanged.AddListener(OnDrop);

            ChooseBtn.onClick.AddListener(OnChooseBtn);
            ConditionDrop.onValueChanged.AddListener(OnConditionDrop);
        }

        public override void SaveData()
        {
            base.SaveData();
            curMapInfo.gameSetting.AICommonGameConfig.endings = new List<AICommonGameConfig_Ending>();
            foreach (var item in ItemLs)
            {
                if (item.EventConfig != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.endings.Add(item.EventConfig);
                }
            }
        }

        private void OnEnable()
        {
            RefreshAns();
        }

        void RefreshAns() {
            answers = new List<string>();
            var events = curMapInfo.gameSetting.AICommonGameConfig.events;
            if (events != null)
            {
                var options = new List<OptionData>();
                foreach (var item in events)
                {
                    if (item != null && item.type == 1)
                    {
                        foreach (var subitem in item.answers)
                        {
                            answers.Add(subitem);
                            options.Add(new OptionData(subitem));
                        }
                    }
                }

                ConditionDrop.ClearOptions();
                ConditionDrop.AddOptions(options);
            }
        }

        private void RefreshView()
        {
            DescTxt.text = CurItem.EventConfig.desc;
            TypeDrop.SetValueWithoutNotify(CurItem.EventConfig.type);
            ConditionGroup.gameObject.SetActive(CurItem.EventConfig.type == 1);
            if (CurItem.EventConfig.type == 1)
            {
                if (!string.IsNullOrEmpty(CurItem.EventConfig.triggerEvent))
                {
                    ChooseBtn.gameObject.SetActive(false);
                    ConditionDrop.transform.localScale = Vector3.one;
                    ConditionDrop.value = answers.FindIndex((x) => x == CurItem.EventConfig.triggerEvent);
                }
                else
                {
                    ChooseBtn.gameObject.SetActive(true);
                    ConditionDrop.transform.localScale = Vector3.zero;
                }
            }
        }

        #region 按钮
        private void OnTog(bool bo, AIParkUgcEditEndingItem item)
        {
            OnToggleValueChanged(item.Tog.gameObject, bo);
            if (bo)
            {
                CurItem = item;

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
            var even = new AICommonGameConfig_Ending();
            for (int i = 0; i < ItemLs.Count; i++)
            {
                if (ItemLs[i].EventConfig == null)
                {
                    ItemLs[i].Tog.gameObject.SetActive(true);
                    ItemLs[i].EventConfig = even;
                    ItemLs[i].Tog.isOn = true;
                    break;
                }
            }

            RefreshTogsBg();
        }

        private void OnDelBtn()
        {
            if (CurItem == null)
            {
                return;
            }
            if (CurItem.Idx == 0)
            {
                return;
            }

            CurItem.Tog.gameObject.SetActive(false);
            CurItem.EventConfig = null;
            ItemLs[0].Tog.isOn = true;

            RefreshTogsBg();
        }

        private void RefreshTogsBg() {
            var count = 0;
            foreach (var item in ItemLs)
            {
                if (item.Tog.gameObject.activeSelf)
                {
                    count++;
                }
            }
            TogsBtn.gameObject.SetActive(count >= 7);

            AddBtn.gameObject.SetActive(count < ItemLs.Count);

            count = count >= 7 ? 7 : count;
            TogsBg.sizeDelta = new Vector2(143 * count + 30,74);
        }

        private void OnDrop(int idx)
        {
            CurItem.EventConfig.type = idx;
            RefreshView();
        }

        private void OnConditionDrop(int idx)
        {
            CurItem.EventConfig.triggerEvent = answers[idx];
        }

        private void OnChooseBtn()
        {
            if (answers.Count <= 0)
            {
                TipPanel.ShowToast("还未设置选择事件");
                return;
            }
            ChooseBtn.gameObject.SetActive(false);
            ConditionDrop.transform.localScale = Vector3.one;
            ConditionDrop.Show();
        }

        private void OnHelpBtn()
        {
            var panel = UIManager.Inst.FindPanel<AIParkUgcEditPanel>(PanelId.AIParkUgcEditPanel);
            panel.HelpGroup.Set(AIParkUgcEditHelpType.Ending);
        }
        private void OnDescBtn()
        {
            if (!string.IsNullOrEmpty(CurItem.EventConfig.desc))
            {
                _descKBInfo.defaultText = CurItem.EventConfig.desc;
            }
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, (str) =>
            {
                CurItem.EventConfig.desc = str;
                DescTxt.text = CurItem.EventConfig.desc;
            });
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        #endregion
    }
}