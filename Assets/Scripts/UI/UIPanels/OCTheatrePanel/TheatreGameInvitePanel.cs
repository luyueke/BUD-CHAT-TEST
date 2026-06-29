using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using GameSync.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class TheatreGameInvitePanel : BasePanel<TheatreGameInvitePanel>
{
    [SerializeField] private GameObject friendList;
    [SerializeField] private GameObject playerList;
    [SerializeField] private Transform mapPlayerRoot;
    [SerializeField] private Transform friendRoot;
    [SerializeField] private TheatreInviteItem inviteItemPrefab;
    [SerializeField] private Button inviteAllBtn;
    [SerializeField] private Button closeBtn;

    private readonly List<TheatreInviteItem> _allItems = new();
    private MessageHandler _roomPlayersChangedHandler;
    // BatchInfo HTTP 请求序号：每次 LoadMapPlayers 自增，回调时校验，过期请求直接丢弃。
    // 防止 OnShow / OnRoomPlayersChanged 在飞行期重发请求导致回调累积 Instantiate 出重复 item。
    private int _loadSeq;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        inviteAllBtn.onClick.AddListener(OnInviteAllBtnClick);
        _roomPlayersChangedHandler = OnRoomPlayersChanged;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        MessageHelper.AddListener(MessageName.TheatreRoomPlayersChanged, _roomPlayersChangedHandler);
        _allItems.Clear();
        foreach (Transform child in mapPlayerRoot) Destroy(child.gameObject);
        foreach (Transform child in friendRoot) Destroy(child.gameObject);
        friendList.SetActive(false);
        LoadMapPlayers();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        MessageHelper.RemoveListener(MessageName.TheatreRoomPlayersChanged, _roomPlayersChangedHandler);
    }

    private void OnRoomPlayersChanged()
    {
        _allItems.Clear();
        foreach (Transform child in mapPlayerRoot) Destroy(child.gameObject);
        foreach (Transform child in friendRoot) Destroy(child.gameObject);
        friendList.SetActive(false);
        LoadMapPlayers();
    }

    private void LoadMapPlayers()
    {
        int seq = ++_loadSeq;
        var allUids = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoIds();
        var roomPlayers = TheatreGameManager.Inst.InRoomPlayers;
        var myUid = AccountDataManager.Inst.Uid;

        var invitableUids = new List<string>();
        foreach (var uid in allUids)
        {
            if (uid == myUid) continue;
            if (roomPlayers != null && roomPlayers.Contains(uid)) continue;
            invitableUids.Add(uid);
        }

        if (invitableUids.Count == 0) return;

        var req = new BatchInfoReq { uidList = string.Join(",", invitableUids) };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.batchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                // 过期请求（已被新一轮 LoadMapPlayers 覆盖）丢弃，避免重复 Instantiate。
                if (seq != _loadSeq) return;
                var resp = JsonConvert.DeserializeObject<BatchInfoResponse>(msg);
                if (resp?.list == null) return;
                bool hasFriend = false;
                foreach (var entry in resp.list)
                {
                    var info = entry.userInfo;
                    if (info == null) continue;
                    bool isFriend = entry.relationShipInfo?.relationStatus == (int)RelationStatusType.Each;
                    var root = isFriend ? friendRoot : mapPlayerRoot;
                    var item = Instantiate(inviteItemPrefab, root);
                    item.gameObject.SetActive(true);
                    item.SetData(info.uid, info.nickname, info.portraitUrl,
                        uid => TheatreGameManager.Inst.InvitePlayer(uid));
                    _allItems.Add(item);
                    if (isFriend) hasFriend = true;
                }
                friendList.SetActive(hasFriend);
            },
            onFail: null);
    }

    private void OnInviteAllBtnClick()
    {
        foreach (var item in _allItems)
            item.TryInvite();
    }
}
