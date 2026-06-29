using Es;
using Game.Avatar;
using GameData.PgcData;
using Pb.Base;
using System;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Game.Props.PropsManagers;
using AIGame.Base;

public class BuddyEmoConentItem : MonoBehaviour
{
    [SerializeField] protected CButton emoBtn;
    [SerializeField] protected Text emoName;
    [SerializeField] protected Image emoIcon;

    protected EmoUIConfig emoteData;

    private Action onEmoBtnClick;

    private string _selectBudddyID;
    bool isAIGmaeLink;
    // 地图共享 buddy 的确定性 key（AINpcInMap_{entityUid}）；非空=对地图 buddy 发起，空=本人 buddy
    private string _mapBuddyKey;
    public virtual void InitData(EmoUIConfig emoteData, Action onEmoBtnClick, string selectBuddyID = "", string mapBuddyKey = "")
    {
        this.emoteData = emoteData;
        this.emoName.SetLocalText(emoteData.name);
        this.onEmoBtnClick = onEmoBtnClick;
        _selectBudddyID = selectBuddyID;
        isAIGmaeLink = !string.IsNullOrEmpty(_selectBudddyID);
        _mapBuddyKey = mapBuddyKey;

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
        if (AIBuddyAvatarController.Inst.SelfStateController == null&&!isAIGmaeLink&&string.IsNullOrEmpty(_mapBuddyKey))
        {
            TipPanel.ShowToast("未召唤BUD伙伴");
            return;
        }

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

        PlayerState stateID = Enum.Parse<PlayerState>(emoteData.playerState);

        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteData.pgcId);

        if (emoAniDataList.Count > 0 && AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
        {   
            int random = emoAniDataList[0].randomCount;
            int randomResult = random == 0 ? 0 : Random.Range(1, random + 1);

            if ((EmoteType)emoteData.emoAniType == EmoteType.DoubleLoop ||
                (EmoteType)emoteData.emoAniType == EmoteType.DoubleOnce)
            {
                EventCenterDataManager.Inst.ReportTask(PostEventId.UseDoubleEmoteInMap);
                EventCenterDataManager.Inst.ReportTask(PostEventId.EmoteInGame);
            }
            GameAIBuddyManager.Inst.SendBuddyEmoteReq(emoteData.pgcId, (EmoteType)emoteData.emoAniType, InteractType.Start, randomResult, _mapBuddyKey);
        }
    }
    
    private void EnterLinkEmote()
    {
        if (AIBuddyAvatarController.Inst.SelfStateController == null&&!isAIGmaeLink&&string.IsNullOrEmpty(_mapBuddyKey))
        {
            TipPanel.ShowToast("未召唤BUD伙伴");
            return;
        }

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

        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteData.pgcId);

        if (emoAniDataList.Count > 0)
        {
            if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.LinkEmoteStart))
            {
                if (isAIGmaeLink)
                {
                    var bev = AIHospital_CharacterManager.Inst.GetNpc(_selectBudddyID);
                    if (bev)
                    {
                        var state = bev.EnterInterruptState<Game.Props.PropsManagers.AIGames.AIHospital.FSM.LinkEmoteState>();
                        state.StartLinkEmote(emoteData.pgcId,true);
                        AIHospitalUtils.Inst.OnSendLinkEmoteReq();
                    }
                }
                else
                    GameAIBuddyManager.Inst.SendBuddyLinkReq(emoteData.pgcId, EmoteType.BuddyLinkEmote, InteractType.Start, 0, false, _mapBuddyKey);
            }
        }
    }
}

