using UnityEngine;

namespace Game.Vehicle.PGCVehicle
{
    public interface IPGCVehicle
    {
        void Drive(Vector2 input, bool jump);
        Transform GetSeatTransform(string uid);
        void OnPassengerEnter(string uid);
        void OnPassengerExit(string uid);
    }

    /// <summary>
    /// 载具输入接收器（避免强依赖具体实现类，便于你后续替换/扩展KVC式载具控制）
    /// </summary>
    public interface IPGCVehicleInputDriver
    {
        void SetInput(Vector2 moveAxis, bool jump);
    }

    /// <summary>
    /// 驾驶上下文（由 PGCVehicleState 在进入驾驶状态时注入：例如相机朝向）
    /// </summary>
    public interface IPGCVehicleDriverContext
    {
        void SetDriverCameraRotation(Quaternion cameraRotation);
    }
}

