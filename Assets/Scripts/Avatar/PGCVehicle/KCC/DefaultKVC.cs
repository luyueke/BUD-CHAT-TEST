using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;

public class DefaultKVC : PGCVehicleBaseKVC
{
    public override void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
    {
        base.OnInit(motor, isSelf, isReconstruction);
    }

    public override void OnSkill(int skillId, bool isPress, string extraJson = null)
    {
        //throw new System.NotImplementedException();
    }

    public override void ChangeAirBanner(string strParam)
    {
        //throw new System.NotImplementedException();
    }
}