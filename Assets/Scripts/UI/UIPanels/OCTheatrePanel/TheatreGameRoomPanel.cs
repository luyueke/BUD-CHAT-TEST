using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using GameData.BaseInfo;
using GameData.Config;
using GameData.TheatreData;
using Message;
using Network;
using Network.Http;
using Basic.Utils;
using GameData.UGCData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class TheatreGameRoomPanel : BasePanel<TheatreGameRoomPanel>
{
    [Header("按钮")]
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button inviteBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button changeActorBtn;

    [Header("角色")]
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private GameObject avatarItemPrefab;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("剧本")]
    [SerializeField] private Text theatreTitle;
    [SerializeField] private Text theatreDescription;
    [SerializeField] private RemoteImageBehaviour cover;

    [Header("商城数据")]
    [SerializeField] private Text designCodeText;
    [SerializeField] private Text dialogueNumText;
    [SerializeField] private Text salesNumText;
    [SerializeField] private Button designCodeCopyBtn;

    [Header("退出二确认")]
    [SerializeField] private GameObject closeConfirmBoard;
    [SerializeField] private Button closeConfirmBtn;
    [SerializeField] private Button closeCancelBtn;

    private OCTheatreInfo _theatreInfo;
    private readonly Dictionary<string, AccountUserInfo> _playerInfoCache = new();
    private MessageHandler _playersChangedHandler;
    private MessageHandler _hostLeftHandler;
    private MessageHandler _actorAssignmentChangedHandler;
    private int _refreshSeq;

    public override void OnCreate()
    {
        base.OnCreate();
        newGameBtn.onClick.AddListener(OnNewGameBtnClick);
        inviteBtn.onClick.AddListener(OnInviteBtnClick);
        changeActorBtn.onClick.AddListener(OnChangeActorBtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);
        closeConfirmBtn.onClick.AddListener(OnConfirmClose);
        closeCancelBtn.onClick.AddListener(() => closeConfirmBoard.SetActive(false));
        designCodeCopyBtn.onClick.AddListener(() =>
        {
            GUIUtility.systemCopyBuffer = _theatreInfo?.designCode ?? string.Empty;
            TipPanel.ShowToast("已复制，去分享给好友吧");
        });
        closeConfirmBoard.SetActive(false);
        _playersChangedHandler = RefreshPlayerList;
        // 房主退出时其他成员面板不强制关闭，刷新玩家列表即可（房间状态已被 ClearRoomState 清除，列表会变空）
        // 不要改回 CloseSelf —— 设计意图是让其他玩家自行决定是否离开，而非被动弹出
        _hostLeftHandler = RefreshPlayerList;
        _actorAssignmentChangedHandler = LoadTheatreAvatars;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0 && args[0] is OCTheatreInfo info) _theatreInfo = info;
        if (_theatreInfo == null) _theatreInfo = TheatreGameManager.Inst.CurrentTheatreInfo;
        // 第二参数：true=房主入口(EmoMenu), false=受邀入口(InviteNotification)。默认 true 保持旧调用兼容
        bool asHost = true;
        if (args.Length > 1 && args[1] is bool h) asHost = h;
        closeConfirmBoard.SetActive(false);
        MessageHelper.AddListener(MessageName.TheatreRoomPlayersChanged, _playersChangedHandler);
        MessageHelper.AddListener(MessageName.TheatreRoomHostLeft, _hostLeftHandler);
        MessageHelper.AddListener(MessageName.TheatreRoomActorAssignmentChanged, _actorAssignmentChangedHandler);

        // Race guard: 受邀路径下若房间已被清理（房主在 accept 与 OnShow 之间退出），不要让 B 误成为新房主
        if (!asHost && !TheatreGameManager.Inst.IsInRoom)
        {
            TipPanel.ShowToast("房间已关闭");
            CloseSelf();
            return;
        }

        if (_theatreInfo != null)
        {
            InitTheatreInfo();
            if (asHost && !TheatreGameManager.Inst.IsInRoom)
                TheatreGameManager.Inst.CreateRoom(_theatreInfo);
        }

        inviteBtn.gameObject.SetActive(TheatreGameManager.Inst.IsHost);
        // Only refresh immediately when already in a room (e.g. invited player after accept).
        // For the host who just called CreateRoom, op=1 echo will arrive shortly and
        // fire TheatreRoomPlayersChanged → RefreshPlayerList — no need to send a duplicate request now.
        if (TheatreGameManager.Inst.IsInRoom)
            RefreshPlayerList();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        MessageHelper.RemoveListener(MessageName.TheatreRoomPlayersChanged, _playersChangedHandler);
        MessageHelper.RemoveListener(MessageName.TheatreRoomHostLeft, _hostLeftHandler);
        MessageHelper.RemoveListener(MessageName.TheatreRoomActorAssignmentChanged, _actorAssignmentChangedHandler);
    }

    private void InitTheatreInfo()
    {
        if (_theatreInfo == null) return;
        theatreTitle.text = _theatreInfo.name;
        theatreDescription.text = _theatreInfo.desc;
        cover.Load(_theatreInfo.cover);

        bool hasCode = !string.IsNullOrEmpty(_theatreInfo.designCode);
        designCodeText.gameObject.SetActive(hasCode);
        if (hasCode) designCodeText.text = _theatreInfo.designCode;
        designCodeCopyBtn.gameObject.SetActive(hasCode);

        LoadTheatreAvatars();

        if (dialogueNumText != null)
            dialogueNumText.text = _theatreInfo.textCount.ToString();

        if (salesNumText != null)
            salesNumText.gameObject.SetActive(false);
        FetchSalesCount(_theatreInfo.id);
    }

    private void FetchSalesCount(string theatreId)
    {
        if (salesNumText == null) return;
        var jb = new JObject { ["id"] = theatreId };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.TheatreInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (_theatreInfo?.id != theatreId) return;
                var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                if (rsp?.interactInfo == null) return;
                if (salesNumText != null)
                {
                    salesNumText.text = GameUtils.ToBudCommonNumString(rsp.interactInfo.consumeAmount);
                    salesNumText.gameObject.SetActive(true);
                }
            },
            error => Debug.LogError($"TheatreGameRoomPanel fetch sales failed: {error}"));
    }

    private void LoadTheatreAvatars()
    {
        foreach (Transform child in avatarRoot) Destroy(child.gameObject);

        var assignment = TheatreGameManager.Inst.RoomActorAssignment;
        if (assignment != null && assignment.Count > 0)
        {
            foreach (var si in assignment)
            {
                var go = Instantiate(avatarItemPrefab, avatarRoot);
                go.SetActive(true);
                go.GetComponent<TheatreAvatarItem>()?.SetData(si.avatarName, si.avatarURL);
            }
            return;
        }

        if (_theatreInfo?.avatarList == null) return;
        foreach (var avatar in _theatreInfo.avatarList)
        {
            var go = Instantiate(avatarItemPrefab, avatarRoot);
            go.SetActive(true);
            go.GetComponent<TheatreAvatarItem>()?.SetData(avatar);
        }
    }

    private void RefreshPlayerList()
    {
        inviteBtn.gameObject.SetActive(TheatreGameManager.Inst.IsHost);
        foreach (Transform child in playerRoot) Destroy(child.gameObject);
        var players = TheatreGameManager.Inst.InRoomPlayers;
        if (players == null || players.Count == 0) return;

        int seq = ++_refreshSeq;
        var req = new BatchInfoReq { uidList = string.Join(",", players) };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.batchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                if (seq != _refreshSeq) return;
                var resp = JsonConvert.DeserializeObject<BatchInfoResponse>(msg);
                if (resp?.list == null) return;
                foreach (Transform child in playerRoot) Destroy(child.gameObject);
                foreach (var entry in resp.list)
                {
                    var info = entry.userInfo;
                    if (info == null) continue;
                    _playerInfoCache[info.uid] = info;
                    var go = Instantiate(playerItemPrefab, playerRoot);
                    go.SetActive(true);
                    go.GetComponentInChildren<RemoteImageBehaviour>()?.Load(info.portraitUrl);
                    var txt = go.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = info.nickname;
                }
            },
            onFail: null);
    }

    private List<TheatreActorSaveItem> BuildRoomPlayerSaveItems()
    {
        var players = TheatreGameManager.Inst.InRoomPlayers;
        if (players == null || players.Count == 0) return null;
        var items = new List<TheatreActorSaveItem>();
        for (int i = 0; i < players.Count; i++)
        {
            string uid = players[i];
            _playerInfoCache.TryGetValue(uid, out var info);
            items.Add(new TheatreActorSaveItem
            {
                playerId = uid,
                avatarName = info?.nickname ?? uid,
                avatarURL = info?.portraitUrl ?? string.Empty,
                clothesIndex = 0,
                origin = 2,
                siblingIndex = i
            });
        }
        return items;
    }

    private void OnNewGameBtnClick()
    {
        // 只把"用户在 GameSetPanel 显式保存过的演员替换映射"传给游戏面板。
        // 未保存（RoomActorAssignment == null）→ 传 null → TheatreGamePanel 不替换 → 保留剧本原 actor。
        // 注意：不能用 BuildRoomPlayerSaveItems()，那是给 GameSetPanel 当"可拖拽源"用的，siblingIndex 语义不同。
        UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, _theatreInfo, (int)TheatreEnterType.Room, 1, TheatreGameManager.Inst.RoomActorAssignment);
    }

    private void OnInviteBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreGameInvitePanel);
    }

    private void OnChangeActorBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreGameSetPanel, BuildRoomPlayerSaveItems(), _theatreInfo);
    }

    private void OnBackBtnClick()
    {
        closeConfirmBoard.SetActive(true);
    }

    private void OnConfirmClose()
    {
        TheatreGameManager.Inst.LeaveRoom();
        closeConfirmBoard.SetActive(false);
        CloseSelf();
    }
}
