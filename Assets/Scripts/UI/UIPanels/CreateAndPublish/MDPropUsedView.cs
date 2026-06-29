using GameData.BaseInfo;
using NetCoreServer;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.UI;


public class MDPropUsedView : MonoBehaviour
{
    [SerializeField] private Transform itemParent;
    [SerializeField] private MDPropUsedItem itemSrc;
    [SerializeField] private Button viewAllBtn;
    private string mapId;
    private MapInfo mapInfo;
    private int maxCount = 10;

    private DynamicItemList<MDPropUsedItem, MapInfo> itemList;
    private int allDataCount;
    
    private void Awake()
    {
        itemList = new DynamicItemList<MDPropUsedItem, MapInfo>(CreateItem);
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
        JObject jo = new JObject()
        {
            ["mapId"] = mapId,
        };
        // HttpUtils.MakeHttpRequest("/ugcmap/quoteList", (int)HTTP_METHOD.GET, JsonConvert.SerializeObject(jo), (content) =>
        // {
        //     if (this!=null && gameObject)
        //     {
        //         HttpResponse response = JsonConvert.DeserializeObject<HttpResponse>(content);
        //         var quoteRsp = JsonConvert.DeserializeObject<QuoteListRsp>(response.data);
        //         itemList.AllDatas.Clear();
        //         if (quoteRsp?.mapInfos != null)
        //         {
        //             itemList.AllDatas.AddRange(DataUtils.SaveGetRange(quoteRsp.mapInfos, 0, maxCount));
        //             allDataCount = quoteRsp.mapInfos.Count;
        //             RefreshView();
        //         }
        //
        //     }
        // }, onFail: null);
    }

    private void RefreshView()
    {
        itemList.Refresh();
        bool hasData = itemList.AllDatas.Count > 0;
        viewAllBtn.transform.SetAsLastSibling();
        viewAllBtn.gameObject.SetActive(allDataCount > maxCount);
        Show(hasData);
    }

    private MDPropUsedItem CreateItem()
    {
        MDPropUsedItem newItem = Instantiate(itemSrc, itemParent);
        newItem.gameObject.SetActive(true);
        return newItem;
    }

    private void ToNative()
    {
        if (mapInfo == null) return;
        // OpenDetailParams openDetailParams = new OpenDetailParams()
        // {
        //     mapInfo = mapInfo,
        //     openTab = (int)OpenDetailOpenTab.PropUsed,
        // };
        // MobileBackManager.Inst.OpenNativePage(MobileInterface.openDetailPage, JsonConvert.SerializeObject(openDetailParams));
    }
}
