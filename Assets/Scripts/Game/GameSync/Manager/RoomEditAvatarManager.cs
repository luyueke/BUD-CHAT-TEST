using Es;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Pet;
using GameData;
using GameData.GameSync;
using Pb.Game;

public class RoomEditAvatarManager : GameInstance<RoomEditAvatarManager>
{
    public enum RoomEditAvatarOP
    {
        Start = 1,
        End = 2,
    }
    public void Init() { }
    public RoomEditAvatarManager()
    {
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.ChangeImage, OnChangeAvatarRecv);
    }

    public override void Release()
    {
        base.Release();
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.ChangeImage, OnChangeAvatarRecv);
    }

    public void OnEnterFittingRoom()
    {
        ChangeImageNetData netData = new ChangeImageNetData();
        netData.Op = (int)RoomEditAvatarOP.Start;
        netData.AvatarJson = "";

        DoChangeAvatarBev(AccountDataManager.Inst.Uid, RoomEditAvatarOP.Start);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.ChangeImage, netData);
    }

    public void OnExitFittingRoom(CharacterData data)
    {
        if(GameController.GetCurrentGameMode() != GameMode.Guest)
            return;

        var avatarJson = CharacterData.SerializeObject(data);
        var curAvatarData = CharacterData.DeserializeObject(avatarJson);
        DoChangeAvatarBev(AccountDataManager.Inst.Uid, RoomEditAvatarOP.End, curAvatarData);

        ChangeImageNetData netData = new ChangeImageNetData();
        netData.Op = (int)RoomEditAvatarOP.End;
        netData.AvatarJson = avatarJson;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.ChangeImage, netData);
    }

    public void OnExitPetFittingRoom(PetData data)
    {
        if(GameController.GetCurrentGameMode() != GameMode.Guest)
            return;

        var petAvatarJson = PetData.SerializeObject(data);
        var curAvatarData = PetData.DeserializeObject(petAvatarJson);
        DoChangeAvatarBev(AccountDataManager.Inst.Uid, RoomEditAvatarOP.End, curAvatarData);

        ChangeImageNetData netData = new ChangeImageNetData();
        netData.Op = (int)RoomEditAvatarOP.End;
        netData.PetAvatarJson = petAvatarJson;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.ChangeImage, netData);
    }

    private void OnChangeAvatarRecv(CommonSyncClientData netData)
    {
        //排除自己
        if(netData.PalyerId == AccountDataManager.Inst.Uid)
            return;

        var changeImageNetData = (ChangeImageNetData)netData.Body;
        BaseAvatarData baseAvatarData = null;
        if (!string.IsNullOrEmpty(changeImageNetData.AvatarJson))
        {
            baseAvatarData = CharacterData.DeserializeObject(changeImageNetData.AvatarJson);
        }
        else if (!string.IsNullOrEmpty(changeImageNetData.PetAvatarJson))
        {
            baseAvatarData = PetData.DeserializeObject(changeImageNetData.PetAvatarJson);
        }
        DoChangeAvatarBev(netData.PalyerId, (RoomEditAvatarOP)changeImageNetData.Op, baseAvatarData);
    }

    private BudTimer changeTimer;
    private void DoChangeAvatarBev(string playerId, RoomEditAvatarOP op, BaseAvatarData baseAvatarData = null)
    {

        var stateCtr = AvatarController.Inst.GetPlayerStateCtrl(playerId);
        if(stateCtr == null)
            return;

        switch (op)
        {
            case RoomEditAvatarOP.Start:
                stateCtr.EnterState(PlayerState.ChangeClothes);
                break;

            case RoomEditAvatarOP.End:
                if (changeTimer != null)
                    TimerManager.Inst.Stop(changeTimer);

                if(baseAvatarData == null)
                    return;

                if (baseAvatarData is CharacterData)
                {
                    var characterData = (CharacterData)baseAvatarData;
                    stateCtr.ExitState(PlayerState.ChangeClothes);

                    changeTimer = TimerManager.Inst.RunOnce("changeOcdelay", 1f, () => {
                        string lastSpecialAnimPgcId = stateCtr.PlayerAnimCtrl.specialAnimPgcId;
                        //stateCtr.Wrap.RefreshAvatar(characterData);
                        AvatarController.Inst.RefreshAvatarByData(playerId, characterData);
                        if (!string.IsNullOrEmpty(stateCtr.PlayerAnimCtrl.specialAnimPgcId)) {
                            stateCtr.PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
                            var specialKCC = DataTables.GetSpecialSkinConfig(stateCtr.PlayerAnimCtrl.specialAnimPgcId);
                            stateCtr.PlayerKCCtrl.ChangeSpecialAnimKCC(specialKCC.KCC);
                        } else if (!string.IsNullOrEmpty(lastSpecialAnimPgcId)) {
                            stateCtr.PlayerAnimCtrl.ClearOverrideSpecialAnim();
                            stateCtr.PlayerKCCtrl.ChangeSpecialAnimKCC("Default");
                        }
                    });
                }
                else if (baseAvatarData is PetData)
                {
                    if (stateCtr.PetAnimCtrl != null)
                    {
                        stateCtr.PetEmoState.OnPetBeginFollow(true);
                        if (stateCtr.PetWrap.Avatar.activeInHierarchy)
                        {
                            stateCtr.PetAnimCtrl.PlayChangeClothAni(null, true, playerId);
                        }
                    }
                    stateCtr.ExitState(PlayerState.ChangeClothes, false);

                    var petData = (PetData)baseAvatarData;
                    changeTimer = TimerManager.Inst.RunOnce("changeOcdelay", 1f, () =>
                    {
                        if (stateCtr != null && stateCtr.PetWrap != null)
                        {
                            stateCtr.PetWrap.RefreshAvatar(petData);
                        }
                    });
                }
                break;
        }
    }

}
