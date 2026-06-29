using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponPonyPreviewItemData
{

    public List<GashaponData> gashaponDataList;
    public List<GashaponRewardData> rewardList;
    public string title;
}

public class GashaponPonyPreviewListView : MonoBehaviour
{
    [SerializeField] private Transform cacheNode;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private GashaponPriceItem itemPrefab;
    [SerializeField] private GameObject gashaponPoolItemPrefab;
    [SerializeField] private GameObject contentRootNode;
    [SerializeField] private Button fenjieBtn; //分解按钮
    [SerializeField] private Text fenjieBtnText; //分解按钮文本





    private List<GashaponPriceItem> items = new List<GashaponPriceItem>();
    private LinkedList<GashaponPriceItem> cacheItems = new LinkedList<GashaponPriceItem>();
    private Action<GashaponRewardData> _itemSelectListener;

    [HideInInspector] public GashaponPriceItem CurSelectItem = null;

    private GashaponData _gashaponData;
    public int targetIdx = -1;
    bool afterInit = false;

    void Start()
    {
        InitUI();
        Invoke("AfterInit", 0.2f);
    }

    public void SetDecomposeTextColor()
    {
        if (_gashaponData != null)
        {
            if (_gashaponData.Id == "lottery.musicalPudding.hotAirBalloon")
            {
                fenjieBtnText.color = new Color(0, 0, 0, 1);
            }
            else if (_gashaponData.Id == "lottery.ponyVehicle")
            {
                fenjieBtnText.color = new Color(0,0,0, 1);
            }
        }
    }

    private void InitUI()
    {
        fenjieBtn.onClick.AddListener(OnDecomposeBtnClick);
    }

    private void OnDecomposeBtnClick()
    {
        if (_gashaponData != null)
        {
            if (_gashaponData.Id == "lottery.musicalPudding.hotAirBalloon")
            {
                UIManager.Inst.OpenPanel<MusicNoteDecomponsePanel>(PanelId.MusicNoteDecomposePanel);
            }
            else if (_gashaponData.Id == "lottery.ponyVehicle")
            {
                UIManager.Inst.OpenPanel<PonyDecomponsePanel>(PanelId.PonyDecomposePanel);
            }
            else if (_gashaponData.Id == "lottery.littleTailCatCar")
            {
                UIManager.Inst.OpenPanel<PonyDecomponsePanel>(PanelId.SockDecomposePanel);
            }
            else if(_gashaponData.Id == "lottery.cloudCone")
            {
                UIManager.Inst.OpenPanel<AirDecomposePanel>(PanelId.AirDecomposePanel);
            }
            else if(_gashaponData.Id == "lottery.clawMachine")
            {
                UIManager.Inst.OpenPanel<WawajiDecomposePanel>(PanelId.WawajiDecomposePanel);
            }
             else if(_gashaponData.Id == "lottery.zzz.phantomParty")
            {
                UIManager.Inst.OpenPanel<PhantomSoundPartyDecomposePanel>(PanelId.PhantomSoundPartyDecomposePanel);
            }
        }
    }

    void AfterInit()
    {
        afterInit = true;
        if (targetIdx == -1)
        {
            DefClickFirst();
        }
        else
        {
            items[targetIdx].OnItemClick();
        }
    }

    public void SetGashaponData(GashaponData gashaponData)
    {
        _gashaponData = gashaponData;
        SetDecomposeTextColor();

    }

    public void UpdateListview(List<GashaponPonyPreviewItemData> dataList, GashaponInfoRsp gashaponInfoRsp = null)
    {
        ClearItems();
        if (dataList == null || dataList.Count <= 0) return;
        var childCount = contentRootNode.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentRootNode.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < dataList.Count; i++)
        {
            GashaponPonyPreviewItemData data = dataList[i];
            var item = Instantiate(gashaponPoolItemPrefab, contentRootNode.transform);
            item.SetActive(true);
            GameObjectEx.FindComponentByName<Text>(item, "txt_title").text = data.title;
            var rewardContent = GameObjectEx.FindChildByName(item, "Content");
            var rewardList = data.rewardList;
            for (int j = 0; j < rewardList.Count; j++)
            {
                GashaponRewardData priceData = rewardList[j];
                GashaponPriceItem itemScript = GetItem();
                itemScript.transform.SetParent(rewardContent.transform);
                itemScript.Init(priceData, OnItemClick);
                if (gashaponInfoRsp != null && gashaponInfoRsp.rewardPool != null)
                {
                    var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId == priceData.RewardId);
                    if (drawnInfo != null)
                    {
                        itemScript.SetOwnedStatus(drawnInfo.everDrawn == 1);
                    }
                }
                items.Add(itemScript);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootNode.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRootNode.GetComponent<RectTransform>());

    }


    public void UpdateListview(List<GashaponRewardData> rewardList, GashaponInfoRsp gashaponInfoRsp = null)
    {
        ClearItems();
        if (rewardList == null || rewardList.Count <= 0) return;
        for (int i = 0; i < rewardList.Count; i++)
        {
            GashaponRewardData priceData = rewardList[i];
            GashaponPriceItem itemScript = GetItem();
            itemScript.Init(priceData, OnItemClick);
            if (gashaponInfoRsp != null && gashaponInfoRsp.rewardPool != null)
            {
                var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId == priceData.RewardId);
                if (drawnInfo != null)
                {
                    itemScript.SetOwnedStatus(drawnInfo.everDrawn == 1);
                }
            }
            items.Add(itemScript);
        }
    }

    public void DefClickFirst()
    {
        if (items.Count > 0)
        {
            items[0].OnItemClick();
        }
    }

    public void Turn2Preview(string bundleId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].GetBindData().BundleId == bundleId)
            {
                if (!afterInit)
                {
                    targetIdx = i;
                }
                else
                {
                    items[i].OnItemClick();
                }
                break;
            }
        }

    }

    public void HideItemsLoading()
    {
        foreach (var item in items)
        {
            item.SetLoadingVisible(false);
        }
    }

    public void AddItemClickListener(Action<GashaponRewardData> callback)
    {
        _itemSelectListener += callback;
    }

    public void ClearItemClickListener()
    {
        _itemSelectListener = null;
    }

    #region Item相关

    private GashaponPriceItem GetItem()
    {
        if (cacheItems != null && cacheItems.Count != 0)
        {
            GashaponPriceItem cache = cacheItems.Last.Value;
            cacheItems.RemoveLast();
            cache.gameObject.SetActive(true);
            cache.transform.SetParent(scrollContent);
            return cache;
        }
        GashaponPriceItem newIns = Instantiate(itemPrefab, scrollContent);
        return newIns;
    }
    private void RecycleItem(GashaponPriceItem item)
    {
        if (cacheItems == null)
        {
            cacheItems = new LinkedList<GashaponPriceItem>();
        }

        cacheItems.AddLast(item);
        item.gameObject.SetActive(false);
        item.SetSelectStatus(false);
        item.transform.SetParent(cacheNode);
    }
    private void ClearItems()
    {
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                RecycleItem(items[i]);
            }

            items.Clear();
        }
    }

    private void OnItemClick(GashaponPriceItem item, GashaponRewardData info)
    {
        HideItemsLoading();

        if (GashaponUtils.HasPGCData(info))
        {
            var asset = info.PgcDatas[0];
            item.SetLoadingVisible(asset.ResourceType == ResourceType.Avatar || asset.ResourceType == ResourceType.PGCPetAvatar);
        }

        bool isSelect = false;

        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetSelectStatus(false);
        }
        CurSelectItem = item;
        _itemSelectListener?.Invoke(info);

        if (_gashaponData?.Id == "lottery.zzz.phantomParty")
        {
            bool isOptionalBox = (int)info.RewardType == (int)BUDRewardType.RewardPgcOptionalBox;
            if (isOptionalBox)
            {
                var panel = UIManager.Inst.OpenPanel<PhantomSoundPartyBigRewardShowPanel>(PanelId.PhantomSoundPartyBigRewardShowPanel);
                if (panel != null) panel.SelectFirst();
            }
            else
            {
                UIManager.Inst.ClosePanel(PanelId.PhantomSoundPartyBigRewardShowPanel);
            }
        }
    }
    #endregion

}
