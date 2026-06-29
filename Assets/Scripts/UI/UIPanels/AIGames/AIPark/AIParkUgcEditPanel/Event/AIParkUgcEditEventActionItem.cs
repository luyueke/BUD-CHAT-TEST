using GameData.BaseInfo;
using ICSharpCode.SharpZipLib;
using Message;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Dropdown;

namespace AIGame.Base
{
    public class AIParkUgcEditEventActionItem : MonoBehaviour
    {
        public CButton Btn_InputGameName;
        public InputField Txt_MapName;
        private KeyBoardInfo _nameKBInfo;

        public Text IndexTxt;

        public CButton ChooseBtn;
        public CButton DelBtn;
        public Dropdown Drop;

        public AICommonGameConfig_Action Config_Action;

        [HideInInspector] public AIParkUgcEditEvent Root;
        [HideInInspector] public int Idx;
        [HideInInspector] public List<AICommonGameConfig_NPC> NpcData;
        private void Awake()
        {
            DelBtn.onClick.AddListener(OnDel);
            Drop.onValueChanged.AddListener(OnDrop);
            Drop.template.gameObject.AddComponent<AIParkDropdownTrigger>();
            Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);
            ChooseBtn.onClick.AddListener(OnChooseBtn);

            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "本次事件中该NPC的行动和表现",
                inputMode = 0,
                maxLength = 200,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            MessageHelper.AddListener<Transform>(MessageName.OnDropdownTrigger, OnSelected);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<Transform>(MessageName.OnDropdownTrigger, OnSelected);
        }

        public void SetData(AIParkUgcEditEvent _root,AICommonGameConfig_Action _action,int _idx) {
            gameObject.SetActive(true);
            Root = _root;
            Idx = _idx;
            Config_Action = _action;

            IndexTxt.text = $"{Idx + 1}";

            NpcData = new List<AICommonGameConfig_NPC>();
            var npcData = Root.CurMapInfo.gameSetting.AICommonGameConfig.npcData;
            if (npcData != null)
            {
                var curIdx = 0;
                NpcData = npcData;
                var options = new List<OptionData>();
                for (int i = 0;i < NpcData.Count;i++)
                {
                    options.Add(new OptionData(NpcData[i].name));
                    if (Config_Action != null && Config_Action.id == NpcData[i].id)
                    {
                        curIdx = i;
                    }
                }

                Drop.ClearOptions();
                Drop.AddOptions(options);

                if (Config_Action != null && !string.IsNullOrEmpty(Config_Action.id))
                {
                    var desc = Config_Action.desc;
                    ChooseBtn.gameObject.SetActive(false);
                    Drop.transform.localScale = Vector3.one;
                    Drop.value = curIdx;
                    OnDrop(curIdx);
                    Config_Action.desc = desc;
                    Txt_MapName.text = Config_Action.desc;
                }
                else
                {
                    ChooseBtn.gameObject.SetActive(true);
                    Drop.transform.localScale = Vector3.zero;
                    Config_Action.desc = string.Empty;
                    Txt_MapName.text = Config_Action.desc;
                }
            }
        }

        private void OnDrop(int _idx)
        {
            Config_Action.id = NpcData[_idx].id;
            Config_Action.name = NpcData[_idx].name;
            Config_Action.desc = string.Empty;
            Txt_MapName.text = Config_Action.desc;
        }

        private void OnChooseBtn()
        {
            if (NpcData.Count <= 0)
            {
                return;
            }
            Drop.value = 0;
            OnDrop(0);
            ChooseBtn.gameObject.SetActive(false);
            Drop.transform.localScale = Vector3.one;
            Drop.Show();
        }

        private void OnBtnInputGameNameClick()
        {
            _nameKBInfo.defaultText = Config_Action.desc;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        private void OnGetNameFormNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (!string.IsNullOrEmpty(value))
            {
                Config_Action.desc = value;
                Txt_MapName.text = value;
            }
        }

        private void OnSelected(Transform _parent)
        {
            if (_parent == Drop.transform) 
            {
                var _id = NpcData[Drop.value].id;
                if (!Root.ActionGroup.CanSelect(_id,this))
                {
                    TipPanel.ShowToast("该NPC已设置行动");
                    ChooseBtn.gameObject.SetActive(true);
                    Drop.transform.localScale = Vector3.zero;
                    Config_Action.desc = string.Empty;
                    Txt_MapName.text = Config_Action.desc;
                    return;
                }
                Root.ActionGroup.RefreshAddBtn();
            }

        }

        public bool isSelect() {
            if (Drop.transform.localScale == Vector3.one && Config_Action != null && gameObject.activeSelf)
            {
                return true;
            }
            return false;
        }

        private void OnDel() 
        {
            Root.ActionGroup.DelBtn(this);
        }
    }
}