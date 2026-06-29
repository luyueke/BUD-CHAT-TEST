using Game.Avatar;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationCabinSkinNode : MonoBehaviour
    {
        [SerializeField] internal ScrollRect scrollRect;
        [SerializeField] internal GameObject skinRoleItemPrefab;
        [SerializeField] internal GameObject skinAddItemPrefab;

        private IncubationCabinPanel _panel;

        internal void Init(IncubationCabinPanel panel)
        {
            _panel = panel;
        }

        public void RefreshSkinRoleList()
        {
            LoggerUtils.Log("RefreshSkinRoleList");
            skinRoleItemPrefab.SetActive(false);
            skinAddItemPrefab.SetActive(false);

            var netCabinCharacterUgcInfo = _panel?.info;
            if (netCabinCharacterUgcInfo == null)
            {
                return;
            }

            var trans = scrollRect.content;
            int cnt = trans.childCount;
            for (int i = 0; i < cnt; i++)
            {
                Destroy(trans.GetChild(i).gameObject);
            }

            netCabinCharacterUgcInfo.skinPack.ForEach(item =>
            {
                var characterInfo = _panel?.info;
                var skinRoleItemGo = Instantiate(skinRoleItemPrefab, scrollRect.content);
                skinRoleItemGo.SetActive(true);
                var roleItem = skinRoleItemGo.GetComponent<SkinRoleItem>();
                roleItem.Init(item);
                roleItem.onSelectAction = (skinPackInfo) =>
                {
                    _panel.curSkinPack = skinPackInfo;
                    _panel.IsDefautSkinPack = skinPackInfo.isDefault == 1;
                    _panel.InitCharacterWrapper(CharacterData.DeserializeObject(skinPackInfo.avatarJson));
                };
                roleItem.onEditClose = (skinData, charData) =>
                {
                    CabinNetManager.Inst.SkinEditEnd(characterInfo,skinData.packId, charData,
                        CharacterData.SerializeObject(charData) != skinData.avatarJson);
                };
                roleItem.onDeleteConfirm = (skinData) =>
                {
                    CabinNetManager.Inst.DeleteSkinPack(characterInfo,skinData.packId, (isSuccess) =>
                    {
                        if (isSuccess)
                        {
                            TipPanel.ShowToast("删除成功");
                        }
                    });
                };
            });

            var addItem = Instantiate(skinAddItemPrefab, scrollRect.content);
            addItem.SetActive(true);
            addItem.GetComponent<SkinAddItem>().Init(netCabinCharacterUgcInfo);
        }
    }
}
