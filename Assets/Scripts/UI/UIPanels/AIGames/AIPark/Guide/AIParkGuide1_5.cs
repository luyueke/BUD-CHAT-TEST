using System;
using Game.Props;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Message;
using UnityEngine;

public class AIParkGuide1_5 : AIParkGuideBase
{
    private AIPark_CharacterBehaviour _tiliaNpc;

    public override void RunGuide()
    {
        base.RunGuide();
        _tiliaNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Tilia).ToString());

        TimerManager.Inst.RunOnce("AIParkGuide1_5_StartTalk", 4f, () =>
        {
            //tilia讲话
            MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, _tiliaNpc.GetNpcID(), "这里的生活虽然每天都不一样，但却很少有外人来，大家都很期待和你一起交流玩耍呢！", () =>

            // _dialogManager.ShowNpcDialog(_tiliaNpc, "这里的生活每天都不同，大家很高兴乐园里来了新人，很期待和你一起玩呢～", true, true, true, () =>
            {
                //锡兵跑过来，然后讲话
                var xibingNpc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Casper).ToString());
                AIParkPropsManager.Inst.ExitAction(xibingNpc, ActionType.SeeSaw);
                xibingNpc.MoveToPositionNear(_tiliaNpc.transform.position, (canMove) =>
                {
                    if (canMove)
                    {
                        MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, xibingNpc.GetNpcID(), "提莉亚！！！出事了！！！又有新的事件出现了！！！", () =>

                        // _dialogManager.ShowNpcDialog(xibingNpc, "提莉亚！！！出事了！！！又有新的事件出现了！！！", true, true, true, () =>
                        {
                            TimerManager.Inst.RunOnce("AIParkGuide1_4_TriggerNextStep", 1.5f, () =>
                            {
                                TriggerNextStep();
                            });
                        });
                    }
                    else
                    {
                        Debug.LogError("乐园锡兵跑不过来");
                    }
                }, 0, 1f);
            });
        });

    }

    public override void TriggerNextStep()
    {
        base.TriggerNextStep();
    }
}