using Es;
using FSM;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEmoteState : PlayerStateTemplate<PlayerStateController>
{
    protected List<EmoAniConfig> emoAniDataList;
    protected List<GameObject> expressionGameObject;

    protected BaseEmoteState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);

        string emoteID = (string)args[0];
        owner.emoteData = Es.DataTables.GetEmoUIConfig(emoteID);
        emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
    }

    public override void ReleaseData()
    {
        base.ReleaseData();

        emoAniDataList?.Clear();
        emoAniDataList = null;
        if (owner != null)
        {
            owner.emoteData = null;
        }
    }

    public override void OnExit()
    {
        base.OnExit();

        //UnRegisterStateEvent();

        //owner.animationController.StopAudio();
        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
    }

    protected void ExitState()
    {
        owner.ExitState(stateID);
    }
}
