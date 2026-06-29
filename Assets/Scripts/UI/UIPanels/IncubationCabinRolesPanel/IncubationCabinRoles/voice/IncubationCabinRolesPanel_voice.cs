using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinRolesPanel
    {
        int currentCharacterVoiceIndex = 0; //0:角色声音,1:语音包
        int currentVoiceTypeIndex = 0; //0:官方声音,1:我创造的,2:社区购买
        CabinCharacterUgcInfo cabinCharacter;
        public void InitVoiceUI(CabinCharacterUgcInfo cabinCharacter)
        {
            this.cabinCharacter = cabinCharacter;
            currentCharacterVoiceIndex = 0;
            currentVoiceTypeIndex = 0;
            characterVoiceToggleParent.onSelect -= OnSelectCharacterVoice;
            characterVoiceToggleParent.onSelect += OnSelectCharacterVoice;
            characterVoiceToggleParent.Init(0);
            voiceTypeToggleParent.onSelect -= OnSelectVoiceType;
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
                item.GetComponent<VoiceCreateItem>().Init(CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo());
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
            var item = Instantiate(voicePackAddItemPrefab, RoleVoicePackScrollRect.content);
            item.SetActive(true);
            item.GetComponent<VoicePackAddItem>().Init(cabinCharacter);
            for (int i = 0; i < showCount; i++)
            {
                var detailItem = Instantiate(voicePackDetailItemPrefab, RoleVoicePackScrollRect.content);
                detailItem.SetActive(true);
                detailItem.GetComponent<VoicePackDetailItem>().Init();
            }
        }
    }
}