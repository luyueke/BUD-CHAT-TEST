using System;
using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TheatreWardrobeItem : MonoBehaviour
{
    [SerializeField] private CButton ActorBtn;
    [SerializeField] private CButton ActorGridBtn;
    [SerializeField] private GameObject ActorSelect;
    [SerializeField] private GameObject GridSelect;
    [SerializeField] private RawImage ActorCardImg;
    [SerializeField] private Text ActorPrice;
    [SerializeField] private Text ActorName;
    [SerializeField] private List<WardrobeItem> WardrobeList;

    public Action OnSelect;
    public Action OnWardrobeSelectionChanged;
    public Action<int> OnClothesItemClick;
    public Action<string> OnDirectBuyOfficialItem;

    private OCTheatreAvatarInfo _data;
    private bool _isActorSelected = false;

    private void Awake()
    {
        ActorBtn.onClick.AddListener(() =>
        {
            OnSelect?.Invoke();
        });
        ActorGridBtn.onClick.AddListener(() =>
        {
            if (!_isActorSelected && IsActorOwned()) return;
            _isActorSelected = !_isActorSelected;
            GridSelect.SetActive(_isActorSelected);
            OnWardrobeSelectionChanged?.Invoke();
        });
    }

    public void SetData(OCTheatreAvatarInfo info)
    {
        _data = info;

        foreach (var item in WardrobeList)
            item.gameObject.SetActive(false);

        if (info == null) return;

        ActorName.text = info.name ?? string.Empty;

        if (!string.IsNullOrEmpty(info.cover))
        {
            Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(new Game.Utils.GameSimpleImageDownloader.Request
            {
                url = info.cover,
                onDone = result =>
                {
                    ActorCardImg.texture = result.CreateTextureFromReceivedData();
                    ActorCardImg.enabled = true;
                }
            });
        }

        ActorPrice.text = info.paymentInfo?.price.ToString() ?? "0";

        if (info.avatarClothes == null) return;
        for (int i = 0; i < info.avatarClothes.Count && i < WardrobeList.Count; i++)
        {
            WardrobeList[i].gameObject.SetActive(true);
            WardrobeList[i].SetData(info.avatarClothes[i], false);
            WardrobeList[i].SetSelected(false);
            WardrobeList[i].OnSelectionChanged = OnWardrobeSelectionChanged;
            int capturedIdx = i;
            WardrobeList[i].OnItemClick = () => OnClothesItemClick?.Invoke(capturedIdx);
            WardrobeList[i].OnDirectBuyOfficialItem = id => OnDirectBuyOfficialItem?.Invoke(id);
        }
    }

    public void SetSelected(bool selected)
    {
        ActorSelect.SetActive(selected);
    }

    public void SetClothSelected(int clothIndex)
    {
        for (int i = 0; i < WardrobeList.Count; i++)
            if (WardrobeList[i].gameObject.activeSelf)
                WardrobeList[i].SetSelected(i == clothIndex);
    }

    public void SelectAll()
    {
        if (!IsActorOwned())
        {
            _isActorSelected = true;
            GridSelect.SetActive(true);
        }
        foreach (var item in WardrobeList)
            if (item.gameObject.activeSelf) item.SelectAll();
    }

    public void DeselectAll()
    {
        _isActorSelected = false;
        GridSelect.SetActive(false);
        foreach (var item in WardrobeList)
            if (item.gameObject.activeSelf) item.DeselectAll();
    }

    private bool IsActorOwned() =>
        !string.IsNullOrEmpty(_data?.id) && AssetsDataManager.IsOwned(_data.id);

    public long GetTotalSelectedPrice()
    {
        long total = 0;
        foreach (var item in WardrobeList)
        {
            if (!item.gameObject.activeSelf) continue;
            foreach (var part in item.GetSelectedItems())
                total += item.GetItemPrice(part);
        }
        return total;
    }
    public long GetActorSelectedPrice()
    {
        if (!_isActorSelected || _data?.paymentInfo == null)
            return 0;
        return _data.paymentInfo.price;
    }

    public bool HasPurchasableItems()
    {
        if (!IsActorOwned()) return true;
        return WardrobeList.Any(item => item.gameObject.activeSelf && item.HasPurchasableItems());
    }

    public void GetSelectedUgcIds(List<string> result)
    {
        if (_isActorSelected && !string.IsNullOrEmpty(_data?.id))
            result.Add(_data.id);

        foreach (var wardrobeItem in WardrobeList)
        {
            if (!wardrobeItem.gameObject.activeSelf) continue;
            foreach (var part in wardrobeItem.GetSelectedItems())
                if (!string.IsNullOrEmpty(part.UId))
                    result.Add(part.UId);
        }
    }

    public void CollectRewardItems(List<CommonRewardItemData> result)
    {
        if (_isActorSelected && _data != null)
            result.Add(new CommonRewardItemData { UgcCover = _data.cover, rewardName = _data.name, RewardAmount = 1, rewardType = (int)BUDRewardType.RewardUgcResource });
        foreach (var wardrobeItem in WardrobeList)
        {
            if (!wardrobeItem.gameObject.activeSelf) continue;
            foreach (var part in wardrobeItem.GetSelectedItems())
            {
                var rewardItem = new CommonRewardItemData { RewardAmount = 1, rewardType = (int)BUDRewardType.RewardUgcResource };
                if (!string.IsNullOrEmpty(part.UId))
                {
                    string uid = part.UId;
                    AssetsDataManager.GetUgcInfo(uid, serverData =>
                    {
                        rewardItem.UgcCover = serverData?.UgcInfo?.cover ?? string.Empty;
                    });
                }
                else
                {
                    rewardItem.pgcId = part.Id;
                }
                result.Add(rewardItem);
            }
        }
    }

    public void RefreshOwnedState()
    {
        if (_isActorSelected && IsActorOwned())
        {
            _isActorSelected = false;
            GridSelect.SetActive(false);
        }
        foreach (var item in WardrobeList)
            if (item.gameObject.activeSelf) item.RefreshOwnedState();
    }
}
