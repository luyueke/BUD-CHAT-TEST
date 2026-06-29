using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;
using Pb.Base;
using System;
using AIGame.Base;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Game.Vehicle.PGCVehicle;

public class EmoContentItem : MonoBehaviour
{
    [SerializeField] protected CButton emoBtn;
    [SerializeField] protected Text emoName;
    [SerializeField] protected Image emoIcon;

    protected EmoUIConfig emoteData;

    private Action onEmoBtnClick;

    public virtual void InitData(EmoUIConfig emoteData, Action onEmoBtnClick)
    {
        this.emoteData = emoteData;
        var pgcName = Es.DataTables.GetPgcNameData(emoteData.pgcId);
        this.emoName.SetLocalText(pgcName != null ? pgcName.Name : emoteData.name);
        this.onEmoBtnClick = onEmoBtnClick;

        emoBtn.onClick.RemoveAllListeners();
        emoBtn.onClick.AddListener(OnEmoBtnClick);
        emoIcon.sprite = PgcUtils.GetIconSpriteByPgcId(emoteData.pgcId, gameObject);
    }

    protected void OnEmoBtnClick()
    {
        switch ((EmoteSubType)emoteData.emoType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                EnterEmote();
                break;
            case EmoteSubType.PetSingle:
            case EmoteSubType.PetSingleLoop:
            case EmoteSubType.PetWithPlayer:
            case EmoteSubType.PetWithPlayerLoop:
                EnterPetEmote();
                break;
            case EmoteSubType.LinkEmote:
                EnterLinkEmote();
                break;
            default:
                break;
        }

        onEmoBtnClick?.Invoke();
    }

    private void EnterEmote()
    {
        if (AvatarController.Inst.SelfStateController.IsInLinkEmote() && (emoteData.emoType == (int)EmoteSubType.Double || emoteData.emoType == (int)EmoteSubType.DoubleLoop))
        {
            TipPanel.ShowToast("牵手状态下不可以做双人动作哦");
            return;
        }
        
        if (AvatarController.Inst.SelfStateController.IsInLinkAIBuddy() && (emoteData.emoType == (int)EmoteSubType.Double || emoteData.emoType == (int)EmoteSubType.DoubleLoop))
        {
            TipPanel.ShowToast("牵手状态下不可以做双人动作哦");
            return;
        }

        if (LinkEmoteManager.Inst.IsPlayerB(AccountDataManager.Inst.Uid))
        {
            TipPanel.ShowToast("被牵手状态下不可发起动作哦");
            return;
        }

        //载具也不行
        if( UIManager.Inst.FindPanel<CameraModePanel>(PanelId.CameraModePanel) != null && (
            GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.Uid)
            || GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.Uid)
        )){
            TipPanel.ShowToast("您正在驾驶载具，请先下车再发起动作");
            return;
        }
        
        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteData.pgcId);

        if (emoAniDataList.Count > 0)
        {
            if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
            {
                TryExitSelfieStateKeepOcPose();
                int random = emoAniDataList[0].randomCount;
                int randomResult = random == 0 ? 0 : Random.Range(1, random + 1);

                AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteData.pgcId, randomResult);

                if ((EmoteType)emoteData.emoAniType == EmoteType.DoubleLoop ||
                    (EmoteType)emoteData.emoAniType == EmoteType.DoubleOnce)
                {
                    EventCenterDataManager.Inst.ReportTask(PostEventId.UseDoubleEmoteInMap);
                }
                else if ((EmoteType)emoteData.emoAniType == EmoteType.SingleLoop || (EmoteType)emoteData.emoAniType == EmoteType.SingleOnce)
                {
                    if (AIGameController.Inst.GetCurAIGame<AIHospitalGame>() != null)
                    {
                        AIHospitalUtils.Inst.OnSendSingleEmoteReq();
                    }
                    if (AIGameController.Inst.GetCurAIGame<AIParkGame>() != null)
                    {
                        AIParkUtils.Inst.OnSendSingleEmoteReq();
                    }
                }
                EventCenterDataManager.Inst.ReportTask(PostEventId.EmoteInGame);
                EmoteNetManager.Inst.SendEmoteReq(emoteData.pgcId, (EmoteType)emoteData.emoAniType, InteractType.Start, randomResult);
            }
        }
    }

    private void EnterPetEmote()
    {
        if (AvatarController.Inst.SelfStateController.IsInLinkEmote() && (emoteData.emoType == (int)EmoteSubType.PetWithPlayer || emoteData.emoType == (int)EmoteSubType.PetWithPlayerLoop))
        {
            TipPanel.ShowToast("牵手状态下不可以做宠物交互动作哦");
            return;
        }
        
        if (AvatarController.Inst.SelfStateController.IsInLinkAIBuddy() && (emoteData.emoType == (int)EmoteSubType.PetWithPlayer || emoteData.emoType == (int)EmoteSubType.PetWithPlayerLoop))
        {
            TipPanel.ShowToast("牵手状态下不可以做宠物交互动作哦");
            return;
        }

        //在相机模式下检查
        if( UIManager.Inst.FindPanel<CameraModePanel>(PanelId.CameraModePanel) != null && (
            GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.Uid)
            || GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.Uid)
        )){
            TipPanel.ShowToast("您正在驾驶载具，请先下车再发起动作");
            return;
        }
        
        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteData.pgcId);

        if (emoAniDataList.Count > 0)
        {
            if (emoteData.playerState == "PetEmote")
            {
                int random = emoAniDataList[0].randomCount;
                int randomResult = random == 0 ? 0 : Random.Range(1, random + 1);

                StateEventManager.Inst.TriggerStateEvent(AccountDataManager.Inst.Uid, StateEvent.PetSingleEmote);
                AvatarController.Inst.SelfStateController.PetEmoState.PlayEmote(emoteData.pgcId, randomResult);
                EmoteNetManager.Inst.SendEmoteReq(emoteData.pgcId, (EmoteType)emoteData.emoAniType, InteractType.Start, randomResult);
            }
            else
            {
                if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    TryExitSelfieStateKeepOcPose();
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : Random.Range(1, random + 1);

                    if (emoteData.emoType == (int) EmoteSubType.PetWithPlayer || emoteData.emoType == (int)EmoteSubType.PetWithPlayerLoop)
                    {
                        StateEventManager.Inst.TriggerStateEvent<bool>(AccountDataManager.Inst.Uid, StateEvent.PetBeginFollow, true);
                    }

                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteData.pgcId, randomResult);

                    EmoteNetManager.Inst.SendEmoteReq(emoteData.pgcId, (EmoteType)emoteData.emoAniType, InteractType.Start, randomResult);
                }
            }
        }
    }
    
    private void EnterLinkEmote()
    {
        if (AvatarController.Inst.SelfStateController.IsInSpecialAnim())
        {
            TipPanel.ShowToast("请先解除特殊动作道具再进行牵手哦");
            return;
        }
        
        if (AvatarController.Inst.SelfStateController.IsInLinkEmote())
        {
            TipPanel.ShowToast("您已经处于双人牵手状态哦");
            return;
        }
        
        if (AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
        {
            TipPanel.ShowToast("您已经处于双人牵手状态哦");
            return;
        }

        if( UIManager.Inst.FindPanel<CameraModePanel>(PanelId.CameraModePanel) != null && (
            GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.Uid)
            || GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.Uid)
        )){
            TipPanel.ShowToast("您正在驾驶载具，请先下车再发起动作");
            return;
        }

        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteData.pgcId);

        if (emoAniDataList.Count > 0)
        {
            if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.LinkEmoteStart))
            {
                TryExitSelfieStateKeepOcPose();
                AvatarController.Inst.SelfStateController.EnterState(PlayerState.LinkEmoteStart, emoteData.pgcId);
                string sendEmoteId = emoteData.pgcId;
                EmoteNetManager.Inst.SendEmoteReq(sendEmoteId, EmoteType.LinkEmote, InteractType.Start);
            }
        }
    }

    /// <summary>
    /// 动作触发前：若当前在自拍状态，先退出自拍状态机并保留当前镜头位姿切入 OC。
    /// 这样可避免动作结束后从缓存恢复回 CameraMode。
    /// </summary>
    private static void TryExitSelfieStateKeepOcPose()
    {
        var avatarCtrl = AvatarController.Inst;
        var statePlayer = avatarCtrl != null ? avatarCtrl.SelfStateController : null;
        if (statePlayer == null || !statePlayer.ContainsCurrentState(PlayerState.CameraMode)) return;

        CameraModeConotroller.Inst.ExitSelfieModeToOcKeepCurrentPose();
        statePlayer.ExitState(PlayerState.CameraMode, false);
        MessageHelper.Broadcast(MessageName.SelfieMode, false);
    }
}
