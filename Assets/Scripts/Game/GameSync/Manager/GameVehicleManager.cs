using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Base;
using Game.Pet;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Vehicle.PGCVehicle;
using GameData.BaseInfo;
using GameData.GameSync;
using GameData.MapData;
using GameData.PgcData;
using GameSync.Manager;
using Message;
using NetEngine;
using Newtonsoft.Json;
using Pb.Game;
using UIAgent;
using UnityEngine;

public class GameVehicleManager : GameInstance<GameVehicleManager>, IGameMono
{
    /// <summary>
    /// 玩家当前乘坐的载具信息，key是主驾驶人uid
    /// </summary>
    private Dictionary<string, VehicleGameController> playerCurVehicles = new Dictionary<string, VehicleGameController>();
    /// <summary>
    /// 乘客记录表，key为乘客uid，value为主驾驶人uid
    /// </summary>
    private Dictionary<string, string> passengerDict = new Dictionary<string, string>();

    /// <summary>
    /// 大厅是否展示载具或特殊道具，如果为false，则不展示载具
    /// </summary>
    public bool isShowHallVehicle = false;

    public GameVehicleManager()
    {
    }

    public void Init() { 
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.CallVehicle, OnRecvCallVehicleNetData);
        MessageHelper.AddListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        MessageHelper.AddListener<string>(MessageName.OnVehicleTryHonking, OnVehicleTryHonking);
        MessageHelper.AddListener<string>(MessageName.OnPlayerTryGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.AddListener<int, bool>(MessageName.OnPlayerUseSkill, OnPlayerUseSkill);
        MessageHelper.AddListener<string>(MessageName.PlayerLeave, OnPlayerLeaveMap);
        MessageHelper.AddListener<string, string>(MessageName.OnWawajiTryGrab, OnWawajiTryGrab);
        MessageHelper.AddListener<string>(MessageName.OnWawajiSelfLanded, OnWawajiSelfLanded);
        MessageHelper.AddListener<string>(MessageName.OnWawajiClawLocked, OnWawajiClawLocked);
    }

    public override void Release()
    {
        base.Release();
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.CallVehicle, OnRecvCallVehicleNetData);
        foreach(var vehicleGame in playerCurVehicles){
            vehicleGame.Value.Release();
        }
        playerCurVehicles.Clear();
        passengerDict.Clear();
        MessageHelper.RemoveListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleTryHonking, OnVehicleTryHonking);
        MessageHelper.RemoveListener<string>(MessageName.OnPlayerTryGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.RemoveListener<int, bool>(MessageName.OnPlayerUseSkill, OnPlayerUseSkill);
        MessageHelper.RemoveListener<string>(MessageName.PlayerLeave, OnPlayerLeaveMap);
        MessageHelper.RemoveListener<string, string>(MessageName.OnWawajiTryGrab, OnWawajiTryGrab);
        MessageHelper.RemoveListener<string>(MessageName.OnWawajiSelfLanded, OnWawajiSelfLanded);
        MessageHelper.RemoveListener<string>(MessageName.OnWawajiClawLocked, OnWawajiClawLocked);
        _boundByDriver.Clear();
        _capturedByDriver.Clear();
    }

    public void Update()
    {
        if(Inst.IsEdit()){
            return;
        }
        
        if(playerCurVehicles.Count > 0){
            for(int i = 0; i < playerCurVehicles.Count; i++){
                playerCurVehicles.ElementAt(i).Value.Update();
            }
        }
    }

    public void FixedUpdate()
    {
        if(Inst.IsEdit()){
            return;
        }

        if(playerCurVehicles.Count > 0){
            //使用for来循环
            for(int i = 0; i < playerCurVehicles.Count; i++){
                playerCurVehicles.ElementAt(i).Value.FixedUpdate();
            }
        }
    }

    public bool IsAnyVehicleOnMap(){
        return playerCurVehicles.Count > 0;
    }

    public bool IsDriverAndHasSeat(string driverUid){
        if(playerCurVehicles.TryGetValue(driverUid, out var vehicleGameController)){
            int seatCount = vehicleGameController.VehicleInfo.vehicleType == (int)VehicleType.Single ? 1 : 2;
            if(seatCount < 2) return false;
            int onSeatCount = passengerDict.Where(x => x.Value == driverUid).Select(x => x.Key).Count();
            onSeatCount += 1; //加上司机自己;
            return onSeatCount < seatCount; //小于才是有空位
        }
        return false;
    }

    public bool IsPassenger(string uid){
        return passengerDict.ContainsKey(uid);
    }

    public bool IsDriver(string uid){
        return playerCurVehicles.ContainsKey(uid);
    }

    public VehicleInfo GetPlayerCurVehicle(string uid){
        if(playerCurVehicles.TryGetValue(uid, out var vehicleGameController)){
            return vehicleGameController.VehicleInfo;
        }
        return null;
    }

    private void OnSyncMapInfoComplete()
    {
        //非常之不优雅，目前没找到其他位置调用这个合适，如果一触发就调用，玩家状态为初始化完毕，会导致人车分离
        CoroutineManager.Inst.StartCoroutine(LoadAllPlayerVehicles());
    }

    private IEnumerator LoadAllPlayerVehicles(){

        //勉强解决一个加载顺序的问题，要查问题
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForEndOfFrame();

        var players = Global.Map.ClientData.Players;

        //要循环2次，因为需要先加载出载具，才能让乘客上车
        for (int i = 0; i < players.Count; i++)
        {

            if(AccountDataManager.Inst.IsMySelf(players[i].PlayerInfo.Uid)){
                continue;
            }
            var playerStatus = players[i];
            if(playerStatus != null
            && playerStatus.VehicleStatus != null 
            && playerStatus.VehicleStatus.Driver == players[i].PlayerInfo.Uid
            && !playerCurVehicles.ContainsKey(players[i].PlayerInfo.Uid)
            ){
                if(playerStatus.VehicleStatus.VehicleJson == null){
                    LoggerUtils.Log("LoadAllPlayerVehicles - VehicleJson is null - " + players[i].PlayerInfo.Uid);
                    continue;
                }
                var vehicleInfo = JsonConvert.DeserializeObject<VehicleInfo>(playerStatus.VehicleStatus.VehicleJson);
                string id = vehicleInfo.id;
                if(string.IsNullOrEmpty(id)){
                    id = vehicleInfo.templateId;
                }
                CreateVehicle(players[i].PlayerInfo.Uid, id, playerStatus.VehicleStatus.VehicleJson, isReconstruction: true);
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            if(AccountDataManager.Inst.IsMySelf(players[i].PlayerInfo.Uid)){
                continue;
            }
            var playerStatus = players[i];
            if(playerStatus.VehicleStatus != null 
            && playerStatus.VehicleStatus.Driver != players[i].PlayerInfo.Uid
            && !IsPassenger(players[i].PlayerInfo.Uid)){ //只给没上车的乘客执行上车逻辑
                if(playerStatus.VehicleStatus.VehicleJson == null){
                    LoggerUtils.Log("LoadAllPlayerVehicles - VehicleJson is null - " + players[i].PlayerInfo.Uid);
                    continue;
                }
                GetInVehicle(players[i].PlayerInfo.Uid, playerStatus.VehicleStatus.Driver);
            }
            var vehicleBannerInfo = JsonConvert.DeserializeObject<VehicleBannerInfo>(playerStatus.VehicleStatus.BannerJson);
            if(vehicleBannerInfo != null)
            {
                TriggerVehicleSkill(playerStatus.VehicleStatus.Driver, new VehicleSkillData { SkillId = (int)SkillType.Skill5, IsPress = vehicleBannerInfo.bannerStatus });
                PlayerChangeBannerText(playerStatus.VehicleStatus.Driver,vehicleBannerInfo.bannerText);
            }
        }

        // 重建娃娃机抓取关系：仅司机本人的 VehicleStatus.BannerJson 才持有 capturedSlots（车主持久化）。
        // 须在载具重建之后执行，CapturedState 才能找到 catch-point 锚点。
        for (int i = 0; i < players.Count; i++)
        {
            var ps = players[i];
            if(ps.VehicleStatus == null || ps.VehicleStatus.Driver != ps.PlayerInfo.Uid) continue;
            if(string.IsNullOrEmpty(ps.VehicleStatus.BannerJson)) continue;
            VehicleBannerInfo info = null;
            try { info = JsonConvert.DeserializeObject<VehicleBannerInfo>(ps.VehicleStatus.BannerJson); } catch { }
            if(info != null && info.capturedSlots != null)
                RestoreCapturedSlots(ps.PlayerInfo.Uid, info.capturedSlots);
        }
    }

    private void OnRecvCallVehicleNetData(CommonSyncClientData netData)
    {
        var callVehicleNetData = (CallVehicleNetData)netData.Body;
        switch(callVehicleNetData.Op){
            case 0: //收起载具
                DiscardVehicle(callVehicleNetData.Uid, callVehicleNetData.VehicleId);
                break;
            case 1: //创建载具
                CreateVehicle(callVehicleNetData.Driver, callVehicleNetData.VehicleId, callVehicleNetData.VehicleJson);
                break;
            case 2: //乘客上车
                GetInVehicle(callVehicleNetData.Uid, callVehicleNetData.Driver);
                break;
            case 3: //乘客下车
                GetOutVehicle(callVehicleNetData.Uid, callVehicleNetData.Driver);
                break;
            case 4: //触发载具鸣笛
                TriggerVehicleSound(callVehicleNetData.Driver);
                break;
            case 5: //控制PGC载具
                PlayerControlPGCVehicle(callVehicleNetData.Uid, true);
                break;
            case 6: //脱离控制
                PlayerControlPGCVehicle(callVehicleNetData.Uid, false);
                break;
            case 7:  //技能1按下 (旧版兼容)
            case 8:  //技能1释放 (旧版兼容)
            case 9:  //技能2按下 (旧版兼容)
            case 10: //技能2释放 (旧版兼容)
            case 11: //技能3按下 (旧版兼容)
            case 12: //技能3释放 (旧版兼容)
            case 13: //技能4按下 (旧版兼容)
            case 14: //技能4释放 (旧版兼容)
            case 15: //技能5按下 (旧版兼容)
            case 16: //技能5释放 (旧版兼容)
            {
                int skillIndex = (callVehicleNetData.Op - 7) / 2;
                bool oldIsPress = (callVehicleNetData.Op - 7) % 2 == 0;
                TriggerVehicleSkill(callVehicleNetData.Driver, new VehicleSkillData { SkillId = (int)SkillType.Skill1 + skillIndex, IsPress = oldIsPress });
                break;
            }
            case 19: //技能操作（技能ID与按下/释放由skill_data字段携带）
                TriggerVehicleSkill(callVehicleNetData.Driver, callVehicleNetData.SkillData);
                break;
            case 17: //修改横幅
                if(callVehicleNetData.BannerJson == null)
                {
                    return;
                }
                var vehicleBannerInfo = JsonConvert.DeserializeObject<VehicleBannerInfo>(callVehicleNetData.BannerJson);
                PlayerChangeBannerText(callVehicleNetData.Driver,vehicleBannerInfo.bannerText);
                break;
            case 20: //娃娃机抓取玩家：VehicleJson = 被抓玩家uid
                OnRecvWawajiGrab(callVehicleNetData.Driver, callVehicleNetData.VehicleJson);
                break;
            case 21: //挣脱成功：VehicleJson = 被抓玩家uid
                OnRecvWawajiEscape(callVehicleNetData.Driver, callVehicleNetData.VehicleJson);
                break;
            case 22: //挣扎失败被劫持：VehicleJson = "被抓玩家uid,slot"（slot=-1为请求，>=0为车主分配结果）
                OnRecvWawajiCaptured(callVehicleNetData.Driver, callVehicleNetData.VehicleJson);
                break;
            case 23: //挣扎动作同步：VehicleJson = 被抓玩家uid
                OnRecvWawajiStruggleTap(callVehicleNetData.VehicleJson);
                break;
            case 24: //本人落地：VehicleJson = 落地玩家uid → 远端退出 Falling
                OnRecvWawajiLanded(callVehicleNetData.VehicleJson);
                break;
        }
    }

    #region 娃娃机抓人玩法

    // driver -> 正在被束缚(挣扎中)的玩家uid集合
    private Dictionary<string, HashSet<string>> _boundByDriver = new Dictionary<string, HashSet<string>>();
    // driver -> 已被劫持的玩家uid列表（下标即锚点slot，车主权威分配）
    private Dictionary<string, List<string>> _capturedByDriver = new Dictionary<string, List<string>>();

    /// <summary>A的娃娃机选定目标B后由WawajiKVC广播，转成op=20全房广播。</summary>
    private void OnWawajiTryGrab(string driverUid, string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = driverUid,
            Driver = driverUid,
            Op = 20,
            VehicleJson = targetUid,
        });
    }

    /// <summary>本人挣脱成功时由UI调用：广播op=21。</summary>
    public void SendStruggleEscape(string driverUid)
    {
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = AccountDataManager.Inst.Uid,
            Driver = driverUid,
            Op = 21,
            VehicleJson = AccountDataManager.Inst.Uid,
        });
    }

    /// <summary>本人QTE挣扎失败时由UI调用：广播op=22请求(slot=-1)，由车主分配锚点。</summary>
    public void SendStruggleFailCaptured(string driverUid)
    {
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = AccountDataManager.Inst.Uid,
            Driver = driverUid,
            Op = 22,
            VehicleJson = AccountDataManager.Inst.Uid + ",-1",
        });
    }

    /// <summary>本人每点一次挣脱：广播 struggle 动作，让其他端的本人副本也播 struggle。</summary>
    public void SendStruggleTap(string driverUid)
    {
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = AccountDataManager.Inst.Uid,
            Driver = driverUid,
            Op = 23,
            VehicleJson = AccountDataManager.Inst.Uid,
        });
    }

    private void OnRecvWawajiStruggleTap(string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;
        // 本人已在点击时本地即时播放，跳过回环；仅其他端的该玩家副本播 struggle
        if(AccountDataManager.Inst.IsMySelf(targetUid)) return;
        MessageHelper.Broadcast(MessageName.OnWawajiStruggleTap, targetUid);
    }

    /// <summary>本人落地(FallingState.OnLand 广播) → 转 op=24 全房，让远端副本退出 Falling。</summary>
    private void OnWawajiSelfLanded(string uid)
    {
        if(string.IsNullOrEmpty(uid)) return;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = uid,
            Driver = uid,
            Op = 24,
            VehicleJson = uid,
        });
    }

    private void OnRecvWawajiLanded(string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;
        // 本人客户端已在本地落地检测中退出 Falling，跳过回环；仅远端副本据此切回 Default。
        if(AccountDataManager.Inst.IsMySelf(targetUid)) return;
        var b = AvatarController.Inst.GetPlayerStateCtrl(targetUid);
        if(b != null && b.ContainsCurrentState(PlayerState.Falling))
        {
            b.ExitState(PlayerState.Falling);
        }
    }

    private void OnRecvWawajiGrab(string driver, string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;

        var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
        vc?.VehicleKinematic?.GrabPlayer(targetUid);

        // 先登记 bound 关系（供 escape/captured/discard 清理）；实际进 Bound 不再用墙钟定时，
        // 改由 WawajiKVC 在爪子 IK 拉满那一刻广播 OnWawajiClawLocked → OnWawajiClawLocked 处理，
        // 保证"进 Bound 时 weight 已满、爪头已抓到人"，BoundState 届时才开始跟随 Bone_WWJ_60。
        if(!_boundByDriver.TryGetValue(driver, out var set))
        {
            set = new HashSet<string>();
            _boundByDriver[driver] = set;
        }
        set.Add(targetUid);
    }

    /// <summary>WawajiKVC 爪子 IK 拉满锁定目标时本地广播 → 该被抓玩家进 Bound。
    /// 反查持有 targetUid 的 driver（op=20 时已登记到 _boundByDriver）。</summary>
    private void OnWawajiClawLocked(string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;
        foreach(var kv in _boundByDriver)
        {
            if(!kv.Value.Contains(targetUid)) continue;
            var b = AvatarController.Inst.GetPlayerStateCtrl(targetUid);
            if(b != null && !b.ContainsCurrentState(PlayerState.Bound))
            {
                b.EnterState(PlayerState.Bound, kv.Key);
            }
            RefreshDriverIdleFloating(kv.Key);
            return;
        }
    }

    /// <summary>按 driver 当前【已劫持(Captured)】人数刷新其娃娃机 float：≥1 改播 float、0 还原。
    /// 只数 Captured（不含 Bound/挣扎中）——float 须等被抓玩家进入 Captured 才进入，与设计一致。
    /// 各客户端独立执行（_capturedByDriver 已逐端同步）。</summary>
    private void RefreshDriverIdleFloating(string driver)
    {
        int captured = 0;
        if(_capturedByDriver.TryGetValue(driver, out var clist))
        {
            foreach(var u in clist) if(!string.IsNullOrEmpty(u)) captured++;
        }
        var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
        vc?.VehicleKinematic?.SetIdleFloating(captured >= 1);
    }

    #region 自身载具持久化（借 VehicleStatus.BannerJson 给晚加入玩家补偿：横幅 + 娃娃机抓取）
    // 单一真源 _selfBannerPersist 同时持有横幅字段与抓取关系；所有写 BannerJson 的入口都经它合并发送，
    // 避免“改横幅清掉抓取 / 抓取清掉横幅”的互相覆盖。横幅与抓取各用分离的命名函数维护各自字段。

    private readonly VehicleBannerInfo _selfBannerPersist = new VehicleBannerInfo();

    /// <summary>自己修改横幅文字：更新真源并持久化(op=17)。供横幅 UI 调用，替代直接构造发送。
    /// 横幅字段复刻原行为（原 op=17 只带 bannerText，bannerStatus 默认 false），仅额外保留 capturedSlots。</summary>
    public void SyncSelfBannerText(string bannerText)
    {
        _selfBannerPersist.bannerText = bannerText;
        _selfBannerPersist.bannerStatus = false;
        PushSelfBannerPersist(17);
    }

    /// <summary>自己显隐横幅：更新真源并持久化(op=15显/16隐，保持原 op 语义)。供横幅 UI 调用。
    /// 横幅字段复刻原行为（原 op=15/16 带 bannerStatus + bannerText=""），仅额外保留 capturedSlots。</summary>
    public void SyncSelfBannerShow(bool show)
    {
        _selfBannerPersist.bannerStatus = show;
        _selfBannerPersist.bannerText = "";
        PushSelfBannerPersist(show ? 15 : 16);
    }

    /// <summary>娃娃机抓取关系变化时（仅车主自己）把当前 Captured 列表写入真源并持久化(op=17)。</summary>
    private void PersistSelfCapturedSlots()
    {
        string self = AccountDataManager.Inst.Uid;
        if(_capturedByDriver.TryGetValue(self, out var clist))
            _selfBannerPersist.capturedSlots = new List<string>(clist);
        else
            _selfBannerPersist.capturedSlots = new List<string>();
        PushSelfBannerPersist(17);
    }

    /// <summary>把真源（横幅+抓取）合并序列化进 BannerJson 全房广播，服务器据此持久化 VehicleStatus.BannerJson。</summary>
    private void PushSelfBannerPersist(int op)
    {
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = AccountDataManager.Inst.Uid,
            Driver = AccountDataManager.Inst.Uid,
            Op = op,
            BannerJson = JsonConvert.SerializeObject(_selfBannerPersist),
        });
    }

    /// <summary>晚加入时按某司机持久化的 capturedSlots 重建抓取关系（载具已在 LoadAllPlayerVehicles 重建）。</summary>
    private void RestoreCapturedSlots(string driver, List<string> capturedSlots)
    {
        if(string.IsNullOrEmpty(driver) || capturedSlots == null) return;
        if(!_capturedByDriver.TryGetValue(driver, out var clist))
        {
            clist = new List<string>();
            _capturedByDriver[driver] = clist;
        }
        for(int slot = 0; slot < capturedSlots.Count; slot++)
        {
            string uid = capturedSlots[slot];
            if(string.IsNullOrEmpty(uid)) continue;
            // 自己重进房间不把自己恢复成被抓（被抓者退房应已脱离；防离开清理未及时传播的竞态）
            if(AccountDataManager.Inst.IsMySelf(uid)) continue;
            while(clist.Count <= slot) clist.Add(null);
            clist[slot] = uid;
            var b = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if(b != null && !b.ContainsCurrentState(PlayerState.Captured))
                b.EnterState(PlayerState.Captured, driver, slot);
        }
        RefreshDriverIdleFloating(driver);
    }

    /// <summary>被抓(Bound)/被劫持(Captured)的玩家离开房间：从其捕获者记录中清除该玩家，
    /// 释放占用的 slot；若捕获者是自己则重新持久化 capturedSlots，使其重进时不再被错误恢复。</summary>
    private void CleanupCaptiveOnLeave(string uid)
    {
        if(string.IsNullOrEmpty(uid)) return;
        // Bound（挣扎中）记录清理（Bound 不持久化，仅清运行时）
        foreach(var kv in _boundByDriver)
            kv.Value.Remove(uid);
        // Captured（被劫持）记录清理 + 车主端重新持久化
        foreach(var kv in _capturedByDriver)
        {
            int idx = kv.Value.IndexOf(uid);
            if(idx < 0) continue;
            kv.Value[idx] = null;            // 置空保留 slot 序号
            RefreshDriverIdleFloating(kv.Key);
            if(AccountDataManager.Inst.IsMySelf(kv.Key)) PersistSelfCapturedSlots();
        }
    }

    #endregion

    private void OnRecvWawajiEscape(string driver, string targetUid)
    {
        if(string.IsNullOrEmpty(targetUid)) return;

        var b = AvatarController.Inst.GetPlayerStateCtrl(targetUid);
        if(b != null && b.ContainsCurrentState(PlayerState.Bound))
        {
            // 在爪子上(Bound)挣脱：释放爪子让 B 脱离
            var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
            vc?.VehicleKinematic?.ReleasePlayer();
            b.ExitState(PlayerState.Bound);
            b.EnterState(PlayerState.Falling); // 挣脱后下落
        }
        else if(b != null && b.ContainsCurrentState(PlayerState.Captured))
        {
            // 被劫持(catch-point)态单击挣脱：不碰爪子（爪子可能正在抓新目标）
            b.ExitState(PlayerState.Captured);
            b.EnterState(PlayerState.Falling);
        }

        if(_boundByDriver.TryGetValue(driver, out var set))
        {
            set.Remove(targetUid);
        }
        // 清掉被劫持锚点占用（置 null 保留 slot 序号，避免影响其他被劫持者）
        if(_capturedByDriver.TryGetValue(driver, out var clist))
        {
            int idx = clist.IndexOf(targetUid);
            if(idx >= 0) clist[idx] = null;
        }
        RefreshDriverIdleFloating(driver);
        if(AccountDataManager.Inst.IsMySelf(driver)) PersistSelfCapturedSlots(); // 车主持久化抓取变更
    }

    /// <summary>娃娃机 catch 挂点容量（按车型ID）：160100003~005 用 2 个，160100006~008 用 3 个。</summary>
    private int GetWawajiCaptureCapacity(string driver)
    {
        var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
        int id = vc != null ? vc.VehicleConfigId : 0;
        switch(id)
        {
            case 160100006:
            case 160100007:
            case 160100008:
                return 3;
            case 160100003:
            case 160100004:
            case 160100005:
            default:
                return 2;
        }
    }

    private void OnRecvWawajiCaptured(string driver, string payload)
    {
        if(string.IsNullOrEmpty(payload)) return;
        var parts = payload.Split(',');
        if(parts.Length < 2) return;
        string targetUid = parts[0];
        if(!int.TryParse(parts[1], out int slot)) return;

        if(slot < 0)
        {
            // 仅车主权威分配锚点
            if(!AccountDataManager.Inst.IsMySelf(driver)) return;
            if(!_capturedByDriver.TryGetValue(driver, out var list))
            {
                list = new List<string>();
                _capturedByDriver[driver] = list;
            }
            int assigned = list.IndexOf(targetUid);
            if(assigned < 0)
            {
                // 在 [0,容量) 内复用第一个空 slot（挣脱后 slot 置 null 可复用）；全占满则容量已满
                int capacity = GetWawajiCaptureCapacity(driver);
                for(int i = 0; i < capacity; i++)
                {
                    if(i >= list.Count) list.Add(null);
                    if(string.IsNullOrEmpty(list[i])) { assigned = i; break; }
                }
                if(assigned < 0)
                {
                    // 容量已满：无 catch 挂点可用 → 让该玩家挣脱掉落，不劫持
                    NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
                        Uid = driver, Driver = driver, Op = 21, VehicleJson = targetUid,
                    });
                    return;
                }
                list[assigned] = targetUid;
            }
            NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
                Uid = driver,
                Driver = driver,
                Op = 22,
                VehicleJson = targetUid + "," + assigned,
            });
            return;
        }

        // slot >= 0：所有客户端执行
        if(!_capturedByDriver.TryGetValue(driver, out var clist))
        {
            clist = new List<string>();
            _capturedByDriver[driver] = clist;
        }
        while(clist.Count <= slot) clist.Add(null);
        clist[slot] = targetUid;

        if(_boundByDriver.TryGetValue(driver, out var bset))
        {
            bset.Remove(targetUid);
        }

        // 爪子松开（B从GrabHook转挂到CapturePoint由CapturedState接管）
        var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
        vc?.VehicleKinematic?.ReleasePlayer();

        var b = AvatarController.Inst.GetPlayerStateCtrl(targetUid);
        if(b != null)
        {
            if(b.ContainsCurrentState(PlayerState.Bound)) b.ExitState(PlayerState.Bound);
            if(!b.ContainsCurrentState(PlayerState.Captured)) b.EnterState(PlayerState.Captured, driver, slot);
        }
        RefreshDriverIdleFloating(driver);
        if(AccountDataManager.Inst.IsMySelf(driver)) PersistSelfCapturedSlots(); // 车主持久化抓取，供晚加入补偿
    }

    /// <summary>车主收纳载具时释放其抓取/劫持的所有玩家（A放车大家才能下来）。</summary>
    private void ReleaseAllGrabbedByDriver(string driver)
    {
        if(_boundByDriver.TryGetValue(driver, out var bset))
        {
            foreach(var uid in bset.ToList())
            {
                var b = AvatarController.Inst.GetPlayerStateCtrl(uid);
                if(b != null && b.ContainsCurrentState(PlayerState.Bound))
                {
                    b.ExitState(PlayerState.Bound);
                    b.EnterState(PlayerState.Falling); // 放车后下落
                }
            }
            bset.Clear();
        }
        if(_capturedByDriver.TryGetValue(driver, out var clist))
        {
            foreach(var uid in clist)
            {
                if(string.IsNullOrEmpty(uid)) continue;
                var b = AvatarController.Inst.GetPlayerStateCtrl(uid);
                if(b != null && b.ContainsCurrentState(PlayerState.Captured))
                {
                    b.ExitState(PlayerState.Captured);
                    b.EnterState(PlayerState.Falling); // 放车后下落
                }
            }
            clist.Clear();
        }
        var vc = PGCVehicleManager.Inst.GetPGCVehicleController(driver);
        vc?.VehicleKinematic?.ReleasePlayer();
        RefreshDriverIdleFloating(driver); // 全部释放，held=0 → 还原 idle
        if(AccountDataManager.Inst.IsMySelf(driver)) PersistSelfCapturedSlots(); // 车主持久化（清空抓取）
    }

    #endregion

    private void OnPlayerLeaveMap(string uid){

        // 被抓/被劫持的玩家离开房间：从捕获者记录里清除（车主端并重新持久化），
        // 否则其重进时会被残留的 capturedSlots 错误恢复成被抓（自己挂在载具上 / 卡死）。
        CleanupCaptiveOnLeave(uid);

        //当司机离开地图
        if(playerCurVehicles.ContainsKey(uid)){
            var passengers = playerCurVehicles[uid].GetPassengers();
            foreach(var passenger in passengers){
                if(passenger == uid) continue; //司机不处理
                StopPlayerActionsForVehicle(passenger); //强制停止乘客的动作状态，避免残留
                GetOutVehicle(passenger, uid);
            }
            DiscardVehicle(uid, playerCurVehicles[uid].VehicleInfo.id);
            //playerCurVehicles.Remove(uid);
        }

        //当乘客离开地图
        if(passengerDict.ContainsKey(uid)){
            if(playerCurVehicles.TryGetValue(passengerDict[uid], out var vehicleGameController)){
                vehicleGameController.RemovePassenger(uid);
            }
            passengerDict.Remove(uid);
        }
    }

    private void CreateVehicle(string driver, string vehicleId, string vehicleJson, bool isReconstruction = false){

        if (playerCurVehicles.ContainsKey(driver) && playerCurVehicles[driver].VehicleInfo.id == vehicleId){
            //LoggerUtils.Log("CreateVehicle - Driver already has vehicle - " + driver);
            return;
        }

        VehicleInfo vehicleInfo = JsonConvert.DeserializeObject<VehicleInfo>(vehicleJson);

        if(vehicleInfo == null){
            Debug.LogWarning("CreateVehicle - VehicleInfo is null - " + vehicleId);
            return;
        }

        int passengerCount = vehicleInfo.vehicleType == (int)VehicleType.Single ? 1 : 2;
        bool isPgc = false;

        if(!string.IsNullOrEmpty(vehicleInfo.templateId) && int.TryParse(vehicleInfo.templateId, out int tmp_id)){
            if (UniqueType.ResourceType(tmp_id / 1000) == ResourceType.UgcVehicle){
                isPgc = false;
            }else if(UniqueType.ResourceType(tmp_id / 1000) == ResourceType.Vehicle){
                isPgc = true;
            }
            else if(Es.DataTables.GetGameResData(vehicleInfo.templateId)?.ResourceType == (int)ResourceType.Vehicle)
            {
                isPgc = true;
            }
        }else{
            isPgc = false;
        }

        var player = AvatarController.Inst.GetPlayerStateCtrl(driver);
        if(player == null){
            Debug.LogWarning("CreateVehicle - Player not found - " + driver);
            return;
        }

        // 创建载具前：强制停止人物正在播放的动作/表情（避免“带动作上车/动作残留”）
        StopPlayerActionsForVehicle(driver);

        if(isPgc){
            //留给PGC载具用
            int pgcId = int.Parse(vehicleInfo.id);
            PGCVehicleManager.Inst.CreatePGCVehicle(pgcId, player, isReconstruction: isReconstruction, callback: (vehicleController)=>{
                if(vehicleController == null){
                    Debug.LogWarning("CreateVehicle - PGCVehicleController not found - " + pgcId);
                    return;
                }
                var config = DataTables.GetPgcVehicleConfig(pgcId);
                vehicleInfo.vehicleType = config.type == VehicleType.Double.ToString() ? 2 : 1;

                var vehicleGameController = new VehicleGameController(
                    new VehicleGameInfo(vehicleId, player.Wrap.Avatar, isPgc:true, vehicleInfo, driver, passengerCount),
                    new VehicleAudioInfo(), 
                    isSelfVehicle:AccountDataManager.Inst.IsMySelf(driver),
                    isPgcVehicle:true);
                vehicleGameController.AddPassengerOnPos(driver, 0); //司机位置
                playerCurVehicles.TryAdd(driver, vehicleGameController);

                //将同步目标设置为PGC载具的KinematicMotor
                player.SetPGCVehicleKinematicCtrl(vehicleController.VehicleKinematic);
                if(AccountDataManager.Inst.IsMySelf(driver))
                {
                    FrameDataManager.Inst.SetSelfMotorGetter(vehicleController.VehicleKinematic);
                    MessageHelper.Broadcast(MessageName.OnSelfPgcVehicleCreated, driver, config.inputPrefab, config.gameCameraOffset);
                }

                vehicleController.EnterDrive(driver); //直接上车

                // if(AccountDataManager.Inst.IsMySelf(driver)){
                //     MessageHelper.Broadcast(
                //         MessageName.OnSelfVehicleCreated, 
                //         driver, 
                //         vehicleController.GetSeatConfig().inputPrefab);
                // }
            });
        }
        else{
            if(vehicleInfo != null)
            {
                AddPropData(vehicleInfo);
            }

            player.Wrap.ChangeUGCVehicle(player.PlayerID, vehicleInfo, ()=>{
                var vehicleGameController = new VehicleGameController(
                    new VehicleGameInfo(vehicleId, player.Wrap.Avatar, isPgc:false, vehicleInfo, driver, passengerCount),
                    new VehicleAudioInfo(), 
                    isSelfVehicle:AccountDataManager.Inst.IsMySelf(driver),
                    isPgcVehicle:false);
                vehicleGameController.AddPassengerOnPos(driver, 0); //司机位置

                playerCurVehicles.TryAdd(driver, vehicleGameController);
                //播放载具启动声音 TODO
                if(AccountDataManager.Inst.IsMySelf(driver)){
                    MessageHelper.Broadcast(MessageName.OnSelfVehicleCreated, driver, "");
                }
            });
        }
    }

    private void StopPlayerActionsForVehicle(string uid)
    {
        var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (stateCtrl == null)
        {
            return;
        }

        // 先处理牵手/联动：需要同时清理绑定数据，避免仅退出状态但逻辑仍认为在牵手
        if (LinkEmoteManager.Inst.IsPlayerLinking(uid))
        {
            if (LinkEmoteManager.Inst.IsPlayerA(uid))
            {
                var playerBId = LinkEmoteManager.Inst.GetPlayerBId(uid);
                if (!string.IsNullOrEmpty(playerBId))
                {
                    LinkEmoteManager.Inst.StopBind(uid, playerBId);
                }
            }
            else
            {
                var playerAId = LinkEmoteManager.Inst.GetPlayerAId(uid);
                if (!string.IsNullOrEmpty(playerAId))
                {
                    LinkEmoteManager.Inst.StopBind(playerAId, uid);
                }
            }
        }

        if (BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(uid))
        {
            BuddyLinkEmoteManager.Inst.StopBind(uid);
        }

        // 再兜底退出各类动作状态（不播退出动画，直接切断）
        PlayerState[] statesToExit =
        {
            PlayerState.SingleEmote,
            PlayerState.DoubleEmote,
            PlayerState.UgcEmote,
            PlayerState.UgcDoubleEmote,
            PlayerState.LinkEmoteStart,
            PlayerState.LinkEmote,
            PlayerState.CameraMode,
            PlayerState.MusicInstrumentPlay,
        };
        foreach (var s in statesToExit)
        {
            if (stateCtrl.ContainsCurrentState(s))
            {
                stateCtrl.ExitState(s, isPlayExitAni: false);
            }
        }

        // 最后强制复位到 idle（包含停止表情协程/音效）
        stateCtrl.PlayerAnimCtrl?.ResetEmoteAnimation();
    }

    private void GetInVehicle(string uid, string driverUid){
        if(playerCurVehicles.TryGetValue(driverUid, out var vehicleGameContainer)){
            
            if(vehicleGameContainer.IsPgcVehicle){
                //有个成功回传，失败就是没有座位的情况，防止有人同时上最后座位的情况，
                // 服务器那边没处理，本地先这么处理
                if(!PGCVehicleManager.Inst.EnterPassenger(uid, driverUid)){
                    return;
                }
            }
            else{
                var selfPlayer = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
                if (AccountDataManager.Inst.Uid == uid)
                {
                    var player = AvatarController.Inst.GetPlayerStateCtrl(driverUid);
                    if (player == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    player.Wrap.GetInVehicle(selfPlayer.Wrap, vehicleGameContainer.VehicleInfo,uid);
                }
                else if(AccountDataManager.Inst.Uid == driverUid)
                {
                    var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
                    if (player == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    selfPlayer.Wrap.GetInVehicle(player.Wrap, vehicleGameContainer.VehicleInfo,uid);
                }
                else
                {
                    var driverPlayer = AvatarController.Inst.GetPlayerStateCtrl(driverUid);
                    if (driverPlayer == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    var targetPlayer = AvatarController.Inst.GetPlayerStateCtrl(uid);
                    if (targetPlayer == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + uid);
                        return;
                    }
                    driverPlayer.Wrap.GetInVehicle(targetPlayer.Wrap, vehicleGameContainer.VehicleInfo,uid);
                }
            }

            vehicleGameContainer.AddPassenger(uid);
            if(passengerDict.ContainsKey(uid))
            {
                return;
            }
            passengerDict.Add(uid, driverUid);

            //需要乘客也有PGC载具状态的相机偏移
            Vector3 cameraOffset = Vector3.zero;
            if(vehicleGameContainer.IsPgcVehicle){
                if(int.TryParse(vehicleGameContainer.VehicleInfo.id, out int id)){
                    var config = DataTables.GetPgcVehicleConfig(id);
                    if(config != null){
                        cameraOffset = config.gameCameraOffset;
                    }
                }
            }

            MessageHelper.Broadcast(MessageName.OnSelfGetInVehicle, AccountDataManager.Inst.IsMySelf(uid), vehicleGameContainer.IsPgcVehicle, cameraOffset);
        }
        
    }

    private void GetOutVehicle(string uid, string driverUid,bool isDiscardVehicle = false){
        if(playerCurVehicles.TryGetValue(driverUid, out var vehicleGameContainer)){
            vehicleGameContainer.RemovePassenger(uid);

            passengerDict.Remove(uid);
            if(vehicleGameContainer.IsPgcVehicle){
                PGCVehicleManager.Inst.CancelPassenger(uid, driverUid);
            }else{
                var selfPlayer = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
                if (AccountDataManager.Inst.Uid == uid)
                {
                    var player = AvatarController.Inst.GetPlayerStateCtrl(driverUid);
                    if (player == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    player.Wrap.GetOutVehicle(selfPlayer.Wrap,uid,isDiscardVehicle);
                }
                else if(AccountDataManager.Inst.Uid == driverUid)
                {
                    var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
                    if (player == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    selfPlayer.Wrap.GetOutVehicle(player.Wrap,uid,isDiscardVehicle);
                }
                else
                {
                    //好像只需要这段逻辑就可以进行判断，上面的if有点多余
                    var driverPlayer = AvatarController.Inst.GetPlayerStateCtrl(driverUid);
                    if (driverPlayer == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + driverUid);
                        return;
                    }
                    var targetPlayer = AvatarController.Inst.GetPlayerStateCtrl(uid);
                    if (targetPlayer == null)
                    {
                        Debug.LogWarning("GetInVehicle - Player not found - " + uid);
                        return;
                    }
                    driverPlayer.Wrap.GetOutVehicle(targetPlayer.Wrap,uid,isDiscardVehicle);
                }

            }
            
            if(AccountDataManager.Inst.IsMySelf(uid)){
                MessageHelper.Broadcast(MessageName.OnPlayerGetOutVehicle, uid);
            }
        }else
        {
            Debug.LogWarning("GetOutVehicle - Driver not found =" + driverUid);
        }
    }

    private void DiscardVehicle(string uid, string vehicleId){

        if(!playerCurVehicles.ContainsKey(uid)){
            LoggerUtils.Log("DiscardVehicle - Vehicle not found - " + uid);
            return;
        }

        // 收纳载具前，释放该车主抓取/劫持的所有玩家（A放车大家才能下来）
        ReleaseAllGrabbedByDriver(uid);
        
        var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if(player == null){
            LoggerUtils.Log("DiscardVehicle - Player not found - " + uid);
            return;
        }

        if(playerCurVehicles.TryGetValue(uid, out var vehicleGameContainer)){
            var passengers = vehicleGameContainer.GetPassengers().Clone() as string[];
            foreach (var passenger in passengers)
            {
                if(passenger == uid) continue;
                GetOutVehicle(passenger, uid, true);
            }

            if(vehicleGameContainer.IsPgcVehicle){
                PGCVehicleManager.Inst.RemovePGCVehicle(uid);
                if(AccountDataManager.Inst.IsMySelf(uid)){
                    FrameDataManager.Inst.SetSelfMotorGetter(AvatarController.Inst.SelfController);
                }
            }else{
                player.Wrap.GetOutSelfVehicle(uid);
            }


        }
        
        if(AccountDataManager.Inst.IsMySelf(uid)){
            MessageHelper.Broadcast(MessageName.OnPlayerGetOutVehicle, uid);
        }
        if(playerCurVehicles.ContainsKey(uid)){
            playerCurVehicles.Remove(uid);
        }

    }

    private void TriggerVehicleSound(string uid){
        if(playerCurVehicles.TryGetValue(uid, out var vehicleGameController)){
            vehicleGameController.TriggerVehicleHonking();
        }
    }

    private void PlayerControlPGCVehicle(string uid, bool isControl){
        if(AccountDataManager.Inst.IsMySelf(uid)){
            return; //自己的情况本地处理
        }
        
        if(playerCurVehicles.TryGetValue(uid, out var vehicleGameContainer)){
            if(vehicleGameContainer.IsPgcVehicle){
                if(isControl){
                    PGCVehicleManager.Inst.EnterDrive(uid);
                }
                else{
                    PGCVehicleManager.Inst.CancelDrive(uid);
                }
            }
        }
    }
    
    /// <summary>
    /// 触发载具技能
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="op"></param>
    private void TriggerVehicleSkill(string uid, VehicleSkillData skillData){
        if(AccountDataManager.Inst.IsMySelf(uid)){
            return;  //自己技能本地表现
        }
        if(skillData == null) return;
        PGCVehicleManager.Inst.TriggerVehicleSkill(uid, skillData.SkillId, skillData.IsPress, skillData.ExtraJson ?? string.Empty);
    }

    public void PlayerChangeBannerText(string uid,string bannerText)
    {
        if(playerCurVehicles.TryGetValue(uid, out var vehicleGameContainer)){
            if(vehicleGameContainer.IsPgcVehicle){
                PGCVehicleManager.Inst.ChangeBannerText(uid,bannerText);
            }
        }
    }
    
    #region 发送载具网络数据

    /// <summary>
    /// 创建载具
    /// </summary>
    /// <param name="driver">司机uid</param>
    /// <param name="vehicleId">载具id</param>
    /// <param name="vehicleJson">载具数据json</param>
    /// <param name="position">载具位置</param>
    /// <param name="rotation">载具旋转</param>
    public void SendCreateVehicle(string driver, string vehicleId, string vehicleJson, Vector3 position, Quaternion rotation, Action<bool> onCall){

        // 兜底：牵手等待/牵手中/双人动作时禁止召唤载具（避免状态机残留导致异常）
        if (AccountDataManager.Inst.IsMySelf(driver))
        {
            var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(driver);
            if (stateCtrl != null && !stateCtrl.CanCallVehicle())
            {
                onCall?.Invoke(false);
                return;
            }
        }

        if(playerCurVehicles.ContainsKey(driver)){
            onCall?.Invoke(false);
            return;
        }

        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = driver,
            Driver = driver,
            VehicleId = vehicleId,
            VehicleJson = vehicleJson,
            Postion = new PB_Vector3(){
                X = position.x,
                Y = position.y,
                Z = position.z,
            },
            Rotation = new PB_Quaternion(){
                X = rotation.x,
                Y = rotation.y,
                Z = rotation.z,
                W = rotation.w,
            },
            Op = 1,
        });
        onCall?.Invoke(true);
        if(AccountDataManager.Inst.PetInfo.isGameHidden == 0){
            var stateCtr = PetAvatarController.Inst.GetPetKCCtrl(AccountDataManager.Inst.Uid);
            stateCtr.kinematicCharacterController.gameObject.SetActive(false);
            AccountDataManager.Inst.SyncGamePetData(1);
            var netData = new HiddenPetNetData
            {
                Op = 1
            };
            NetSyncManager.Inst.SendAllRoom(SubCmdType.HiddenPet, netData);
        }
    }

    /// <summary>
    /// 试玩态（本地无房间）本地直造载具：正式多人下载具实体由 CallVehicle 网络回包在接收端 CreateVehicle 创建，
    /// 而试玩态该 sync 被丢弃（无房间），实体永不生成。此方法直接本地创建（CreateVehicle 内部会让司机就位），
    /// 仅供本地预览，不发任何同步。守卫与 SendCreateVehicle 一致。
    /// </summary>
    /// <returns>是否成功发起创建</returns>
    public bool CreateVehicleLocalOnly(string driver, string vehicleId, string vehicleJson)
    {
        if (AccountDataManager.Inst.IsMySelf(driver))
        {
            var stateCtrl = AvatarController.Inst.GetPlayerStateCtrl(driver);
            if (stateCtrl != null && !stateCtrl.CanCallVehicle())
            {
                return false;
            }
        }
        if (playerCurVehicles.ContainsKey(driver))
        {
            return false;
        }
        CreateVehicle(driver, vehicleId, vehicleJson);
        return true;
    }

    /// <summary>
    /// 乘客上车
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="driverUid"></param>
    public void SendGetInVehicle(string uid, string driverUid){
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = uid,
            Driver = driverUid,
            Op = 2,
        });

        if(AccountDataManager.Inst.PetInfo.isGameHidden == 0){
            var stateCtr = PetAvatarController.Inst.GetPetKCCtrl(AccountDataManager.Inst.Uid);
            stateCtr.kinematicCharacterController.gameObject.SetActive(false);
            AccountDataManager.Inst.SyncGamePetData(1);
            var netData = new HiddenPetNetData
            {
                Op = 1
            };
            NetSyncManager.Inst.SendAllRoom(SubCmdType.HiddenPet, netData);
        }
    }

    private void OnPlayerUseSkill(int skillId, bool isPress){
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = AccountDataManager.Inst.Uid,
            Driver = AccountDataManager.Inst.Uid,
            Op = 19,
            SkillData = new VehicleSkillData { SkillId = skillId, IsPress = isPress },
        });
    }


    /// <summary>
    /// 只有自己是司机的情况下触发
    /// </summary>
    private void OnVehicleTryHonking(string uid){
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = uid,
            Driver = uid,
            Op = 4,
        });

    }

    /// <summary>
    /// 乘客下车
    /// </summary>
    /// <param name="uid"></param>
    private void OnPlayerGetOutVehicle(string uid){

        if(AccountDataManager.Inst.IsMySelf(uid) && playerCurVehicles.ContainsKey(uid)){
            //当自己是司机时
            NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
                Uid = uid,
                Driver = uid,
                Op = 0,
            });
        }
        else if(AccountDataManager.Inst.IsMySelf(uid) && passengerDict.ContainsKey(uid)){
            //当自己是乘客时
            NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
                Uid = uid,
                Driver = passengerDict[uid],
                Op = 3,
            });
        }
    }

    /// <summary>
    /// 更新载具位置和旋转, 只能司机自己更新
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void SendUpdateVehiclePosRot(string driver, Vector3 position, Quaternion rotation){
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = driver,
            Driver = driver,
            Op = 5,
            Postion = new PB_Vector3(){
                X = position.x,
                Y = position.y,
                Z = position.z,
            },
            Rotation = new PB_Quaternion(){
                X = rotation.x,
                Y = rotation.y,
                Z = rotation.z,
                W = rotation.w,
            },
        });
    }

    /// <summary>
    /// 控制PGC载具, 例如热气球，玩家可以脱离控制在平台上活动
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="isControl"></param>
    public void SendControlPGCVehicle(string uid, bool isControl){
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallVehicle, new CallVehicleNetData(){
            Uid = uid,
            Driver = uid,
            Op = isControl ? 5 : 6, //5是控制，6是脱离控制
        });
    }

    #endregion


    public void AddPropData(VehicleInfo vehicleInfo)
    {
        //var metaDataBytes = xasset.Asset.LoadRemoteAssetSync(vehicleInfo.mapUrl);
        //var mapData = MapPbDataTool.ParseMapPb(metaDataBytes);
        //GlobalNodeManager.Inst.Get<PropManager>().InitPropData(mapData.UgcItemData); //带有素材的，需要初始化
        if (!string.IsNullOrEmpty(vehicleInfo.mapUrl))
        {
            var metaDataBytes = xasset.Asset.LoadRemoteAssetSync(vehicleInfo.mapUrl);
            var mapData = MapPbDataTool.ParseMapPb(metaDataBytes);
            GlobalNodeManager.Inst.Get<PropManager>().InitPropData(mapData.UgcItemData); //带有素材的，需要初始化
        }

    }

}