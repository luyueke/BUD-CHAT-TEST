using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.Store;
using GameData;
using Network.Http;
using Network.Message;
using UI.Preview3D.Base;
using xasset;

public class ContestRankView : MonoBehaviour
{
    public ContestRankListView ListView;
    public RawImage renderImg;
    public GameObject rankGo;
    public Image groupBg;
    public Image selfBg;

    private PlayerPodiumController podiumController;
    protected bool _isRegion = true;
    public delegate string GetScoreFormat(int score);

    private string contestId;
    private string emptyTips;
    private GetScoreFormat scoreformat;
    private Color themeColor1 = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color themeColor2 = new Color(0.8f, 0.8f, 0.8f, 1f);

    bool IsInited = false;

    public void Awake()
    {

    }

    public void InitView(ContestInfo contestInfo, string empty, GetScoreFormat func)
    {
        contestId = contestInfo.contestId;
        emptyTips = empty;
        scoreformat = func;
        SyncThemeColor(contestInfo);
        if (IsInited) return;
        IsInited = true;
        InitPodium();
        RefreshColor();
        ListView.InitUI(contestId, OnClickRankItem);
        ListView.RequestDataList(RefreshRankUI);
    }

    private void SyncThemeColor(ContestInfo contestInfo)
    {
        DataUtil.TryGetFromList(contestInfo.themeColorList, 0, out string tc1);
        DataUtil.TryGetFromList(contestInfo.themeColorList, 1, out string tc2);
        if (!string.IsNullOrEmpty(tc1))
        {
            themeColor1 = DataUtil.DeSerializeColorCheckHash(tc1);
        }
        if (!string.IsNullOrEmpty(tc2))
        {
            themeColor2 = DataUtil.DeSerializeColorCheckHash(tc2);
        }
    }

    private void OnEnable()
    {
        // var playerUidList = curRankData.Select(x => x.userInfo.uid).ToList();
        // if (playerUidList != null) podiumController?.ShowPlayers(playerUidList);
    }

    public void OnDestroy()
    {

    }
    
    private void InitPodium()
    {
        var asset = Asset.Load(Preview3DConstant.PATH_PODIUM_PREVIEW_MODEL, typeof(GameObject));
        GameObject prefab = asset.asset as GameObject;
        GameObject podiumInst = UnityEngine.Object.Instantiate(prefab, this.transform);
        podiumController = podiumInst.GetComponent<PlayerPodiumController>();
        renderImg.texture = podiumController.viewCamera.targetTexture;
    }

    private void OnClickRankItem(ContestEntryInfo info)
    {
        
    }

    private void RefreshRankUI(List<ContestEntryInfo> items)
    {
        var playerUidList = items.Select(x => x.creator.uid).ToList();
        if (playerUidList == null)
        {
            return;
        }
        podiumController.ShowPlayers(playerUidList);
    }
    
    private void SetTips(bool flag, string tips)
    {
        // emptyGo.SetActive(flag);
        // emptyTxt.text = tips;
        rankGo.SetActive(!flag);
    }

    private void UpdateList(bool refresh)
    {
        // SetItemValue(selfItem, selfData);
        // selfItem.SetSelfUIStyle(themeColor1, themeColor1);
        // SetTips(curRankData.Count == 0, emptyTips);
        // 展示前3名

        // if (refresh)
        // {
        //     listView.MovePanelToItemByRowColumn(0, 0);
        //     listView.RefreshGridView(curRankData.Count, true);
        // }
        // else listView.SetListItemCount(curRankData.Count, false);
    }

    private void RefreshColor()
    {
        groupBg.color = themeColor1;
        selfBg.color = themeColor2;
    }
}
