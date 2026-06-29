using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditNPC : SettingContentBase
    {
        public Transform Content;
        public AIParkUgcEditNPCItem ItemPrefab;

        public CButton Btn_InputDesc;
        public GameObject emptyDescObj;
        public Text descriptionText;
        private KeyBoardInfo _descKBInfo;

        private const int Count = 8;
        private List<AIParkUgcEditNPCItem> NPCList = new List<AIParkUgcEditNPCItem>();

        [HideInInspector]public AIParkUgcEditNPCItem curItem;
        public override void InitUIComponent()
        {
            base.InitUIComponent();
            Btn_InputDesc.onClick.AddListener(OnBtnInputDescClick);
        }

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            ItemPrefab.InitData(this, this.curMapInfo.id, CheckCanSelectNpc);
            for (int i = 0; i < Count; i++)
            {
                var obj = GameObject.Instantiate(ItemPrefab, Content).GetComponent<AIParkUgcEditNPCItem>();
                obj.InitData(this, this.curMapInfo.id, CheckCanSelectNpc);
                obj.gameObject.SetActive(false);
                NPCList.Add(obj);
            }
            SetNpcItemData();

            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = 100,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            // 初始化描述文本
            UpdateDescriptionUI();
        }

        public override void SaveData()
        {
            base.SaveData();
            var hospitalNPCs = new List<AICommonGameConfig_NPC>();
            NPCList.ForEach((provost) =>
            {
                if (provost.GetNpcData() != null)
                {
                    hospitalNPCs.Add(provost.GetNpcData());
                }
            });

            this.curMapInfo.gameSetting.AICommonGameConfig.npcData = hospitalNPCs;
        }

        private void OnDisable()
        {
            var hospitalNPCs = new List<AICommonGameConfig_NPC>();
            NPCList.ForEach((provost) =>
            {
                if (provost.GetNpcData() != null)
                {
                    hospitalNPCs.Add(provost.GetNpcData());
                }
            });

            this.curMapInfo.gameSetting.AICommonGameConfig.npcData = hospitalNPCs;
        }

        private void SetNpcItemData()
        {
            var ls = curMapInfo.gameSetting.AICommonGameConfig.npcData;
            if (ls == null || ls.Count <= 0)
                return;

            for (int i = 0; i < ls.Count; i++)
            {
                if (i < NPCList.Count)
                {
                    NPCList[i].SetData(ls[i]);
                }
                else
                {
                    ItemPrefab.gameObject.SetActive(false);
                    break;
                }
            }

            NPCList[0].OnBtnEditClick();
        }
        public void AddNpc(AICommonGameConfig_NPC npcData)
        {
            if (npcData == null)
            {
                curItem = null;
                foreach (var item in NPCList)
                {
                    if (item.GetNpcData() != null)
                    {
                        ClickNpc(item);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < NPCList.Count; i++)
                {
                    if (NPCList[i].GetNpcData() == null)
                    {
                        NPCList[i].SetData(npcData);
                        ClickNpc(NPCList[i]);
                        break;
                    }
                }
            }

            UpdateDescriptionUI();

            bool full = true;
            foreach (var item in NPCList)
            {
                if (item.GetNpcData() == null)
                {
                    full = false;
                    break;
                }
            }
            ItemPrefab.gameObject.SetActive(!full);
        }
        public void ClickNpc(AIParkUgcEditNPCItem npcData)
        {
            curItem = npcData;
            foreach (var item in NPCList)
            {
                if (item != npcData)
                {
                    item.Btn_Edit_On.gameObject.SetActive(false);
                }
            }
            curItem.OnBtnEditClick();
            UpdateDescriptionUI();
        }
        private bool CheckCanSelectNpc(string npcId)
        {
            foreach (var item in NPCList)
            {
                if (item.GetNpcData()!= null && item.GetNpcData().id == npcId)
                {
                    return false;
                }
            }
            return true;
        }
        private void OnBtnInputDescClick()
        {
            // 设置默认文本为当前描述
            if (curItem != null && curItem.GetNpcData() != null)
            {
                _descKBInfo.defaultText = curItem.GetNpcData().plot;
            }

            // 注册回调并显示键盘
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescriptionFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        private void OnGetDescriptionFromNative(string description)
        {
            // 移除回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            // 更新描述
            if (curItem != null)
            {
                curItem.GetNpcData().plot = description;
            }

            // 更新UI
            UpdateDescriptionUI();
        }

        private void UpdateDescriptionUI()
        {
            if (curItem == null || string.IsNullOrEmpty(curItem.GetNpcData()?.plot))
            {
                // 显示空描述提示
                emptyDescObj.SetActive(true);
                descriptionText.gameObject.SetActive(false);
            }
            else
            {
                // 显示描述内容
                descriptionText.text = curItem.GetNpcData().plot;
                descriptionText.gameObject.SetActive(true);
                emptyDescObj.SetActive(false);
            }
        }
    }
}