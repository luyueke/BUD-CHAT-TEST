using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class MDPropUsedItem : MonoBehaviour, IRefreshable<MapInfo>
{
    public Button actionBtn;
    public SellPriceTag priceTag;
    public RemoteImageBehaviour remoteImg;

    public MapInfo MMapInfo { get; private set; }

    protected void Start()
    {
        actionBtn.onClick.AddListener(OnClick);
    }

    public void Refresh(MapInfo data)
    {
        MMapInfo = data;
        if (priceTag && data != null)
        {
            // priceTag.SetPrice(data.paymentInfo, data.paymentInfo.price);
        }
        remoteImg.Load(data.cover);
    }

    public void SetPool(IPool pool)
    {
        remoteImg.InitializeWithPool(pool);
    }

    private void OnClick()
    {
        MapInfo mapInfo = MMapInfo;
        if (mapInfo == null) return;
        if (string.IsNullOrEmpty(mapInfo.id)) return;

        // if (mapInfo.dataType == 5)
        // {
        //     OutfitDetailPanel.Show();
        //     OutfitDetailPanel.Instance.setNpcData(mapInfo);
        // }
        // else
        // {
        //     OutfitDetailPanel.Show();
        //     var itemId = mapInfo.dcInfo?.itemId;
        //     if (string.IsNullOrEmpty(itemId))
        //     {
        //         itemId = "";
        //     }
        //     var budActId = mapInfo.dcInfo?.budActId;
        //     if (string.IsNullOrEmpty(budActId))
        //     {
        //         budActId = "";
        //     }
        //     OutfitDetailPanel.Instance.setCurrentMapId(mapInfo.mapId, mapInfo.isDC, itemId, budActId, cache: mapInfo);
        // }
    }
}
