using System;
using Game.Avatar;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class ExtPackSkinAddItem : MonoBehaviour
    {
        public GameObject vipImageGo;
        public Text textAdd;
        public Button selectBtn;

        bool _canAdd;
        CabinCharacterPackInfo _packInfo;
        Action<CharacterData> creatCharacterOn;
        void Awake()
        {
            selectBtn.onClick.AddListener(OnSelectBtnClick);
        }

        public void Init(CabinCharacterPackInfo packInfo,Action<CharacterData> CreatCharacterOn)
        {
            _packInfo = packInfo;
            creatCharacterOn= CreatCharacterOn;
            if (_packInfo == null)
            {
                return;
            }

            var isVip = CabinNetManager.Inst.isVip;
            // VIP 最多 8 个；非 VIP 上限为当前数量与 2 取最大（防止已超限时误锁）
            int maxCount = isVip ? 8 : Math.Max(2, _packInfo.skinPack.Count);
            textAdd.text = _packInfo.skinPack.Count + "/" + 8;
            _canAdd = _packInfo.skinPack.Count < maxCount;

            vipImageGo.SetActive(!isVip && _packInfo.skinPack.Count >= 2);
        }

        void OnSelectBtnClick()
        {
            if (!_canAdd)
            {
                UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
                TipPanel.ShowToast("添加更多需要vip");
                return;
            }

            var isVip = CabinNetManager.Inst.isVip;
            // 双重校验：Init 之后如 VIP 状态变化，再次拦截
            int curCnt = _packInfo?.skinPack.Count ?? 0;
            if (!isVip && curCnt >= 2)
            {
                TipPanel.ShowToast("非vip最多2个皮肤包");
                return;
            }

            var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
            var panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, "IncubationCabin",
                CharacterData.DeserializeObject(avatarJson));
            panel.OnCloseAction = characterData =>
            {
                creatCharacterOn(characterData);
            };
        }
    }
}
