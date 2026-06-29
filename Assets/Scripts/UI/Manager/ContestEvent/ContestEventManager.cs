using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ContestEventManager : GlobalInstance<ContestEventManager>
{
    public void OpenContestPage(string id, bool isBanner = false)
    {
        var contestInfo = ContestDataManager.Inst.GetContestInfo(id);
        if (contestInfo == null)
        {
            return;
        }

        OpenPanel(contestInfo);
    }

    public void OpenContestSelectPage(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        var info = LobbyInfoManager.Inst.GetContestInfo(id);
        if (info != null && info.CurrentContestType == BUDContestType.OC)
        {
            OcCompetitionSystem.Inst.OpenPanel(info);
            return;
        }

        UIManager.Inst.OpenPanel(PanelId.ContestSelectPanel, WindowId.ActivityCenterWindow, id);
    }

    public void OpenContestPage()
    {
        var ls = ContestDataManager.Inst.CurrentLobbyInfo?.contestList;
        if (ls != null) {
            foreach (var item in ls)
            {
                if (item != null && item.CurrentContestType != BUDContestType.OC) 
                {
                    OpenContestSelectPage(item.contestId);
                    break;
                }
            }
        }
    }

    private void OpenPanel(ContestInfo info)
    {
        if (!GameController.IsInHallScene())
        {
            TipPanel.ShowToast("您正在游戏中，请稍后再试");
            return;
        }

        if (info.CurrentContestType == BUDContestType.OC)
        {
            OcCompetitionSystem.Inst.OpenPanel(info);
            return;
        }

        UIManager.Inst.SwapPanel(PanelId.ClothContestPanel, info);
    }

    public void JoinContest(List<string> idList, string creationId)
    {
        JoinContestInfo info = new JoinContestInfo();
        info.creationId = creationId;
        info.idList = idList;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestJoin, HttpMethod.POST,
            JsonConvert.SerializeObject(info), (msg) => { },
            (error) => { LoggerUtils.LogError("JoinContestInfo fail "); });
    }

    public void SetRawImage(RemoteImageBehaviour rawImage, string url, Action<bool, bool> onSucc = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        if (!rawImage) return;
        rawImage.Load(url, true, (isSucc, fromCache) => onSucc?.Invoke(isSucc, fromCache));
    }

    public void SetCustomBg(Transform bgParent, List<string> bgIconUrlList, string bgColor)
    {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/RemoteIconBg.prefab")
            .Instantiate(bgParent);
        var item = itemObj.GetComponent<RemoteIconBgPanel>();
        item.InitCustomBgItem(bgColor, bgIconUrlList);
        item.HideSolidBg();
       
        item.gameObject.SetActive(true);
    }

    public static string GetLeftTimeStr(ContestInfo contest)
    {
        if (contest == null)
        {
            return "";
        }

        if (contest.status == (int)ContestStatus.Completed)
        {
            return LocalizationManager.Inst.GetLocalizedText("活动已结束");
        }
        else
        {
            return $"{LocalizationManager.Inst.GetLocalizedText("距离活动结束:")}{contest.leftTime ?? ""}";
        }
    }

    public static void ShowDetailView(ContestInfo contestInfo, ContestEntryInfo entryInfo, Action<int> voteNumDidChange = null)
    {
        if (entryInfo == null)
        {
            return;
        }

        var ugcId = entryInfo?.creationInfo?.creationId;
        if (string.IsNullOrEmpty(ugcId))
        {
            return;
        }

        switch ((BUDContestType)entryInfo.creationInfo.creationType)
        {
            case BUDContestType.Skin:
            case BUDContestType.PetSkin:
                var assetDetailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, ugcId) as AssetDetailPanel;
                assetDetailPanel.likeNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.OC:
            case BUDContestType.PetOC:
                var votePanel = UIManager.Inst.OpenPanel<OcVotePanel>(PanelId.OcVotePanel, contestInfo, entryInfo.creationInfo);
                votePanel.voteNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.Instrument:
                var instrumentDetailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Instrument, ugcId) as AssetDetailPanel;
                instrumentDetailPanel.likeNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.Vehicle:
                var veDetailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Vehicle, ugcId) as AssetDetailPanel;
                veDetailPanel.likeNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.MusicScore:
                var MusicScoreDetailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicScore, ugcId) as AssetDetailPanel;
                MusicScoreDetailPanel.likeNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.Bundle:
            case BUDContestType.PetBundle:
                var bundleDetailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcBundle, ugcId) as AssetDetailPanel;
                bundleDetailPanel.likeNumDidChange = voteNumDidChange;
                break;
            case BUDContestType.Camera:
                RequestAlbumInfo(ugcId);
                break;
  
        }
    }

    public static void RequestAlbumInfo(string id)
    {
        var jb = new JObject
        {
            ["id"] = id,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AlbumInfo, HttpMethod.GET, reqParam, (content) =>
        {
            try
            {
                var res = JsonConvert.DeserializeObject<AlbumPhotoInfo>(content);
                UIManager.Inst.OpenPanel(PanelId.MapPhotoShowPanel,res);

            }
            catch (Exception e)
            {
            
            }
        }, (error) =>
        {
       
        });
    }
}

public enum ContestListType
{
    AllEntries = 1,
    MyEntry = 2,
    Top100 = 3,
}
