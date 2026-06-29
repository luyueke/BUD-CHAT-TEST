using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationCabinVoiceNode : MonoBehaviour
    {
        [SerializeField] internal CabinBtnToggleParent characterVoiceToggleParent;
        [SerializeField] internal CabinBtnToggleParent voiceTypeToggleParent;
        [SerializeField] internal GameObject RoleSoundGo;
        [SerializeField] internal ScrollRect RoleSoundScrollRect;
        [SerializeField] internal Text txt_selectVoiceName;
        [SerializeField] internal GameObject RoleVoicePackGo;
        [SerializeField] internal GameObject voiceItemPrefab;
        [SerializeField] internal GameObject voiceCreateItemPrefab;
        [SerializeField] internal GameObject voiceGetMoreItemPrefab;
        [SerializeField] internal ScrollRect RoleVoicePackScrollRect;
        [SerializeField] internal GameObject voicePackAddItemPrefab;
        [SerializeField] internal GameObject voicePackDetailItemPrefab;

        private IncubationCabinPanel _panel;
        private int currentCharacterVoiceIndex = 0; // 0:角色声音 1:语音包
        private int currentVoiceTypeIndex = 0; // 0:官方声音 1:我创造的 2:社区购买

        internal void Init(IncubationCabinPanel panel)
        {
            _panel = panel;
        }

        public void InitVoiceUI()
        {
            currentCharacterVoiceIndex = 0;
            currentVoiceTypeIndex = 0;
            characterVoiceToggleParent.Init(0);
            characterVoiceToggleParent.onSelect += OnSelectCharacterVoice;
            voiceTypeToggleParent.onSelect += OnSelectVoiceType;
            voiceTypeToggleParent.Init(0);

            voiceItemPrefab.SetActive(false);
            voiceCreateItemPrefab.SetActive(false);
            voiceGetMoreItemPrefab.SetActive(false);
            voicePackAddItemPrefab.SetActive(false);
            voicePackDetailItemPrefab.SetActive(false);

            OnSelectCharacterVoice(currentCharacterVoiceIndex);
        }

        public void OnSelectCharacterVoice(int index)
        {
            LoggerUtils.Log("OnSelectCharacterVoice: " + index);
            currentCharacterVoiceIndex = index;
            if (currentCharacterVoiceIndex == 0)
            {
                currentVoiceTypeIndex = 0;
            }
            RoleSoundGo.SetActive(index == 0);
            RoleVoicePackGo.SetActive(index == 1);
            RefreshVoiceContent();
        }

        public void OnSelectVoiceType(int index)
        {
            LoggerUtils.Log("OnSelectVoiceType: " + index);
            currentVoiceTypeIndex = index;
            RefreshRoleSoundContent();
        }

        public void RefreshVoiceContent()
        {
            if (currentCharacterVoiceIndex == 0)
            {
                RefreshRoleSoundContent();
            }
            else if (currentCharacterVoiceIndex == 1)
            {
                RefreshRoleVoicePackContent();
            }
        }

        public void RefreshRoleSoundContent()
        {
            int childCount = RoleSoundScrollRect.content.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(RoleSoundScrollRect.content.GetChild(i).gameObject);
            }
            if (currentVoiceTypeIndex == 0)
            {
                int showCount = 1;
                for (int i = 0; i < showCount; i++)
                {
                    var item = Instantiate(voiceItemPrefab, RoleSoundScrollRect.content);
                    item.SetActive(true);
                }
            }
            else if (currentVoiceTypeIndex == 1)
            {
                var item = Instantiate(voiceCreateItemPrefab, RoleSoundScrollRect.content);
                item.SetActive(true);
                item.GetComponent<VoiceCreateItem>().Init(_panel?.info as CabinCharacterUgcInfo);
            }
            else if (currentVoiceTypeIndex == 2)
            {
                var item = Instantiate(voiceGetMoreItemPrefab, RoleSoundScrollRect.content);
                item.SetActive(true);
                item.GetComponent<VoiceGetMoreItem>().Init();
            }
        }

        public void RefreshRoleVoicePackContent()
        {
            LoggerUtils.Log("RefreshRoleVoicePackContent");
            int showCount = 1;
            int childCount = RoleVoicePackScrollRect.content.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(RoleVoicePackScrollRect.content.GetChild(i).gameObject);
            }
            var addItem = Instantiate(voicePackAddItemPrefab, RoleVoicePackScrollRect.content);
            addItem.SetActive(true);
            addItem.GetComponent<VoicePackAddItem>().Init(_panel?.info as CabinCharacterUgcInfo);
            for (int i = 0; i < showCount; i++)
            {
                var detailItem = Instantiate(voicePackDetailItemPrefab, RoleVoicePackScrollRect.content);
                detailItem.SetActive(true);
                detailItem.GetComponent<VoicePackDetailItem>().Init();
            }
        }
    }
}
