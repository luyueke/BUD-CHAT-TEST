using Game.Audio;
using UnityEngine;

public class SelfStateController : PlayerStateController
{
    //private PlayerBaseControl playerBaseCtrl;

    public override bool IsSelf => true;
    public override bool IsSelfAIBuddy => false;

    protected override void Awake()
    {
        base.Awake();

        //playerBaseCtrl = transform.parent.GetComponent<PlayerBaseControl>();

        //MessageHelper.AddListener<GameMode>(MessageName.ChangeMode, OnChangeMode);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        //MessageHelper.RemoveListener<GameMode>(MessageName.ChangeMode, OnChangeMode);

        UnBindPlayerID();
    }

    public override void MoveJoystick(Vector3 screenOffset)
    {
        //base.MoveJoystick(screenOffset);

        //playerBaseCtrl.Move(screenOffset);
    }


    //private void OnChangeMode(GameMode mode)
    //{
    //    if (mode == GameMode.Edit)
    //        ResetToDefaultState();
    //}
}
