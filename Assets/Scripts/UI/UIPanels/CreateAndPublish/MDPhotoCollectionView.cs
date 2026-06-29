using GameData.BaseInfo;
using Network;
using Network.Http;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.UI;

public class MDPhotoCollectionView : MonoBehaviour
{
    [SerializeField] private Transform itemParent;
    [SerializeField] private MDPhotoCollectionItem itemSrc;
    [SerializeField] private Button viewAllBtn;
    private string mapId;
    private MapInfo mapInfo;
    private int maxCount = 6;

    private DynamicItemList<MDPhotoCollectionItem, PhotoListItemInfo> itemList;
    private int allDataCount;

    private void Awake()
    {
        itemList = new DynamicItemList<MDPhotoCollectionItem, PhotoListItemInfo>(CreateItem);
        viewAllBtn.onClick.AddListener(ToNative);
        viewAllBtn.gameObject.SetActive(false);
    }

    public void Show(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    public void AttachMapInfo(MapInfo mapInfo)
    {
        this.mapInfo = mapInfo;
    }

    public void Pull(string mapId)
    {
        this.mapId = mapId;
        JObject req = new JObject()
        {
            ["mapId"] = mapId,
        };
        
        NetworkManager.Inst.SendHttpRequest("", HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this == null || !gameObject)
                {
                    return;
                }
                var photoListRsp = JsonConvert.DeserializeObject<PhotoListRsp>(content);
                itemList.AllDatas.Clear();
                if(photoListRsp?.photos != null)
                {
                    itemList.AllDatas.AddRange(DataUtil.SaveGetRange(photoListRsp.photos, 0, maxCount));
                    allDataCount = photoListRsp.photos.Count;
                    RefreshView();
                }
            },null);
    }

    private void RefreshView()
    {
        itemList.Refresh();
        bool hasData = itemList.AllDatas.Count > 0;
        viewAllBtn.transform.SetAsLastSibling();
        viewAllBtn.gameObject.SetActive(allDataCount > maxCount);
        Show(hasData);
    }

    private void OnItemClick(MDPhotoCollectionItem item)
    {
        ToNative();
    }

    private MDPhotoCollectionItem CreateItem()
    {
        MDPhotoCollectionItem newItem = Instantiate(itemSrc, itemParent);
        newItem.gameObject.SetActive(true);
        newItem.SetOnClick(OnItemClick);
        return newItem;
    }

    private void ToNative()
    {
        if (mapInfo == null) return;
        // OpenDetailParams openDetailParams = new OpenDetailParams()
        // {
        //     mapInfo = mapInfo,
        //     openTab = (int)OpenDetailOpenTab.PhotoCollection,
        // };
        // MobileBackManager.Inst.OpenNativePage(MobileInterface.openDetailPage, JsonConvert.SerializeObject(openDetailParams));
    }
}
