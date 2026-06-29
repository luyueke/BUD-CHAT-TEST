using Game.Avatar;
using System;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
public class SkinAddItem : MonoBehaviour
{
    public GameObject vipImageGo;
    public Text textAdd;
    public Button selectBtn;
    private CabinCharacterBaseInfo characterUgcInfo;
    bool canAdd = false;

    void Awake()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
    }

    public void Init(CabinCharacterBaseInfo characterUgcInfo)
    {
        this.characterUgcInfo = characterUgcInfo;
        if (characterUgcInfo == null)
        {
            return;
        }

        var isVip = CabinNetManager.Inst.isVip;
        int maxCount = isVip ? 8 : Math.Max(2, characterUgcInfo.skinPack.Count);
        textAdd.text = characterUgcInfo.skinPack.Count.ToString() + "/" + 8;
        canAdd = characterUgcInfo.skinPack.Count < maxCount;

        vipImageGo.SetActive(!isVip && characterUgcInfo.skinPack.Count >= 2);
    }

    void OnSelectBtnClick()
    {
        Debug.LogError("OnSelectBtnClick1");
        if (!canAdd)
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
            TipPanel.ShowToast("添加更多需要vip");
            return;
        }
        var isVip = CabinNetManager.Inst.isVip;
        int curCnt = characterUgcInfo.skinPack.Count;
        if (!isVip)
        {
            if (curCnt >= 2)
            {
                //非vip最多2个皮肤包
                TipPanel.ShowToast("非vip最多2个皮肤包");
                return;
            }
        }

        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        FittingRoomPanel panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, "IncubationCabin", CharacterData.DeserializeObject(avatarJson));
        panel.OnCloseAction = (characterData) =>
        {
            CabinNetManager.Inst.SkinEditEndCreateSkinPack(characterUgcInfo, characterData, CharacterData.SerializeObject(characterData) != avatarJson);
        };
    }
}
