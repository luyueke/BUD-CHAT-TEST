using System.Collections.Generic;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.Config;
using Newtonsoft.Json;
using GameData.GameSync;
using GameSync.Manager;
using Message;
using Pb.Game;
using UIAgent;
using UnityEngine;

public class TheatreGameManager : GameInstance<TheatreGameManager>, IGameMono
{
    private const string PrefabPath = "Assets/Loadable/Model3D/Editor_Props/Theatre/interact_effect/eff/OC_Filmreel.prefab";

    public string CurrentRoomId { get; private set; }
    public string HostUid { get; private set; }
    public List<string> InRoomPlayers { get; private set; } = new List<string>();
    public OCTheatreInfo CurrentTheatreInfo { get; private set; }

    public List<TheatreActorSaveItem> RoomActorAssignment { get; set; }

    // 收到 op=2 邀请但用户尚未接受时，先把剧场信息缓存在这里。
    // key = roomId。接受时由 AcceptInvite 取出并写入 CurrentTheatreInfo。
    // 不要在 op=2 时污染 CurrentRoomId/HostUid/CurrentTheatreInfo，否则
    // 被邀请者不接受、自己直接进剧场会被误判为已在他人房间内 → 无法成为 host。
    // 记录 HostUid 是为了 op=4 能识别"是否房主退出，需要清理 pending"。
    private struct PendingInvite
    {
        public string HostUid;
        public OCTheatreInfo TheatreInfo;
    }
    private readonly Dictionary<string, PendingInvite> _pendingInvites = new Dictionary<string, PendingInvite>();

    private GameObject _theatrePrefabInstance;

    public TheatreGameManager() { }

    public void Init()
    {
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.CallTheatre, OnRecvCallTheatreNetData);
        MessageHelper.AddListener<string>(MessageName.PlayerLeave, OnPlayerLeaveMap);
    }

    public override void Release()
    {
        base.Release();
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.CallTheatre, OnRecvCallTheatreNetData);
        MessageHelper.RemoveListener<string>(MessageName.PlayerLeave, OnPlayerLeaveMap);
        DestroyPrefab();
        ClearRoomState();
        _pendingInvites.Clear();
    }

    public void Update() { }
    public void FixedUpdate() { }

    public bool IsHost => AccountDataManager.Inst.Uid == HostUid;
    public bool IsInRoom => !string.IsNullOrEmpty(CurrentRoomId);

    public void CreateRoom(OCTheatreInfo theatreInfo)
    {
        CurrentTheatreInfo = theatreInfo;
        // Set optimistically so IsHost is immediately true before the op=1 echo returns
        HostUid = AccountDataManager.Inst.Uid;
        var rot = AvatarController.Inst.SelfController.transform.rotation;
        var pos = AvatarController.Inst.SelfController.transform.position + rot * Vector3.forward;
        var data = new CallTheatreNetData
        {
            Op = 1,
            Host = AccountDataManager.Inst.Uid,
            RoomId = System.Guid.NewGuid().ToString("N"),
            TheatreId = PackTheatreId(theatreInfo),
            Position = new PB_Vector3 { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = new PB_Quaternion { X = rot.x, Y = rot.y, Z = rot.z, W = rot.w }
        };
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallTheatre, data);
    }

    public void InvitePlayer(string targetUid)
    {
        if (!IsInRoom) return;
        // Include TheatreId so late-joiners (who missed op=1) can reconstruct CurrentTheatreInfo on accept.
        var data = new CallTheatreNetData
        {
            Op = 2,
            Host = AccountDataManager.Inst.Uid,
            RoomId = CurrentRoomId,
            TheatreId = PackTheatreId(CurrentTheatreInfo),
        };
        data.InvitedPlayers.Add(targetUid);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallTheatre, data);
    }

    public void JoinRoom(string roomId, string hostUid)
    {
        var data = new CallTheatreNetData
        {
            Op = 3,
            Host = AccountDataManager.Inst.Uid,
            RoomId = roomId,
        };
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallTheatre, data);
    }

    // 被邀请者点击"接受"时调用。
    // 把 op=2 缓存的剧场信息正式写入主 state，再广播 op=3 加入房间。
    // 返回 false 表示邀请已失效（房间已关闭、已在其他房间内）。
    public bool AcceptInvite(string roomId, string hostUid)
    {
        if (IsInRoom) return false;
        if (!_pendingInvites.TryGetValue(roomId, out var pend)) return false;

        CurrentTheatreInfo = pend.TheatreInfo;
        _pendingInvites.Remove(roomId);
        CurrentRoomId = roomId;
        HostUid = hostUid;
        if (!InRoomPlayers.Contains(hostUid))
            InRoomPlayers.Add(hostUid);
        // 自己会通过 op=3 的回环被加入 InRoomPlayers（见 OnRecvCallTheatreNetData case 3）

        var data = new CallTheatreNetData
        {
            Op = 3,
            Host = AccountDataManager.Inst.Uid,
            RoomId = roomId,
        };
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallTheatre, data);
        return true;
    }

    public void LeaveRoom()
    {
        if (!IsInRoom) return;
        var data = new CallTheatreNetData
        {
            Op = 4,
            Host = AccountDataManager.Inst.Uid,
            RoomId = CurrentRoomId,
        };
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallTheatre, data);
        // 不在这里 ClearRoomState，等自身收到 op=4 广播时统一处理
        // 确保 HostUid 在广播回来时仍有值，DestroyPrefab 能正确走 host 分支
    }

    private static string PackTheatreId(OCTheatreInfo info)
    {
        if (info == null) return string.Empty;
        return JsonConvert.SerializeObject(info);
    }

    private static OCTheatreInfo ParseTheatreInfo(string packed)
    {
        if (string.IsNullOrEmpty(packed)) return null;
        try { return JsonConvert.DeserializeObject<OCTheatreInfo>(packed); }
        catch { return null; }
    }

    private void OnRecvCallTheatreNetData(CommonSyncClientData netData)
    {
        var d = (CallTheatreNetData)netData.Body;
        switch (d.Op)
        {
            case 1:
                // Only the host processes their own echo; bystanders must not adopt foreign room state
                if (d.Host != AccountDataManager.Inst.Uid) break;
                CurrentRoomId = d.RoomId;
                HostUid = d.Host;
                if (!InRoomPlayers.Contains(d.Host))
                    InRoomPlayers.Add(d.Host);
                if (!string.IsNullOrEmpty(d.TheatreId))
                    CurrentTheatreInfo = ParseTheatreInfo(d.TheatreId);
                SpawnPrefab(new Vector3(d.Position.X, d.Position.Y, d.Position.Z));
                MessageHelper.Broadcast(MessageName.TheatreRoomPlayersChanged);
                break;
            case 2:
                if (d.InvitedPlayers.Contains(AccountDataManager.Inst.Uid))
                {
                    // 不污染主 state：仅把剧场信息存进 _pendingInvites，留到 AcceptInvite 写入。
                    // 这样未接受邀请的 B 自己进剧场仍能走 CreateRoom 成为 host。
                    OCTheatreInfo invitedInfo = !string.IsNullOrEmpty(d.TheatreId) ? ParseTheatreInfo(d.TheatreId) : null;
                    if (invitedInfo != null)
                        _pendingInvites[d.RoomId] = new PendingInvite { HostUid = d.Host, TheatreInfo = invitedInfo };
                    UIAgentManager.Inst.OpenPanel(PanelId.TheatreInviteNotifiactionPanel);
                    UIAgentManager.Inst.CallPanelMethod(PanelId.TheatreInviteNotifiactionPanel,
                        "AddNotification", d.Host, d.RoomId, invitedInfo?.name ?? string.Empty);
                }
                break;
            case 3:
                if (d.RoomId != CurrentRoomId) break;
                if (!InRoomPlayers.Contains(d.Host))
                    InRoomPlayers.Add(d.Host);
                MessageHelper.Broadcast(MessageName.TheatreRoomPlayersChanged);
                break;
            case 4:
                // 即使本端未加入该房间，若退出者是已知 pending 邀请的房主，
                // 也要清理 pending，使后续 AcceptInvite 能正确返回"房间已关闭"。
                if (_pendingInvites.TryGetValue(d.RoomId, out var pendOnLeave) && pendOnLeave.HostUid == d.Host)
                    _pendingInvites.Remove(d.RoomId);
                if (d.RoomId != CurrentRoomId) break;
                InRoomPlayers.Remove(d.Host);
                if (d.Host == HostUid)
                {
                    DestroyPrefab();
                    ClearRoomState();
                    MessageHelper.Broadcast(MessageName.TheatreRoomHostLeft);
                }
                else
                {
                    MessageHelper.Broadcast(MessageName.TheatreRoomPlayersChanged);
                }
                break;
        }
    }

    private void OnPlayerLeaveMap(string uid)
    {
        if (!InRoomPlayers.Contains(uid)) return;
        InRoomPlayers.Remove(uid);
        if (uid == HostUid)
        {
            DestroyPrefab();
            ClearRoomState();
            MessageHelper.Broadcast(MessageName.TheatreRoomHostLeft);
        }
        else
        {
            MessageHelper.Broadcast(MessageName.TheatreRoomPlayersChanged);
        }
    }

    private void SpawnPrefab(Vector3 position)
    {
        DestroyPrefab();
        var wrapper = Loader.Load<GameObject>(PrefabPath);
        if (wrapper == null) return;
        _theatrePrefabInstance = wrapper.Instantiate();
        _theatrePrefabInstance.transform.position = position;
    }

    private void DestroyPrefab()
    {
        if (_theatrePrefabInstance == null) return;
        Object.Destroy(_theatrePrefabInstance);
        _theatrePrefabInstance = null;
    }

    private void ClearRoomState()
    {
        CurrentRoomId = null;
        HostUid = null;
        InRoomPlayers.Clear();
        CurrentTheatreInfo = null;
        RoomActorAssignment = null;
    }
}
