using Game.KinematicCharacter;

namespace Game.KinematicCharacter
{
    public interface IMotorGetter
    {
        KinematicCharacterMotor KCMotor { get; }
    }
}
