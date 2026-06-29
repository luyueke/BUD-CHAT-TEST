using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Database;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PartnerBoxItem : MonoBehaviour
{
    [SerializeField] private CButton SelectBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private RemoteImageBehaviour IconImage;
    [SerializeField] private Text PriceTxt;
    [SerializeField] private GameObject OwnedGo;
    [SerializeField] private GameObject SelectedGo;

    private CharacterBoxInfo _data;
    private Action<CharacterBoxInfo> _onSelected;

    public void SetData(CharacterBoxInfo data, Action<CharacterBoxInfo> onSelected, bool isSelected = false)
    {
        _data = data;
        _onSelected = onSelected;

        SelectBtn.onClick.RemoveAllListeners();
        SelectBtn.onClick.AddListener(OnSelectBtnClick);

        BuyBtn.onClick.RemoveAllListeners();
        BuyBtn.onClick.AddListener(OnBuyBtnClick);

        IconImage.Load(data.cover);

        var isOwned = IsOwned(data);
        OwnedGo.SetActive(isOwned);
        BuyBtn.gameObject.SetActive(!isOwned);
        PriceTxt.gameObject.SetActive(!isOwned);
        if (!isOwned) PriceTxt.text = data.paymentInfo?.price.ToString() ?? "0";

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (SelectedGo != null) SelectedGo.SetActive(isSelected);
    }

    private void OnSelectBtnClick() => _onSelected?.Invoke(_data);

    private void OnBuyBtnClick()
    {
        // 后续补充购买界面逻辑
    }

    private static bool IsOwned(CharacterBoxInfo data)
    {
        var inventoryData = BagDatabase.Inst.Select(data.id);
        return (inventoryData != null && inventoryData.OwnedNum > 0)
               || data.creator == AccountDataManager.Inst.Uid;
    }
}
