using Pb.Base;
using Game.Avatar;
using Game.KinematicCharacter;
using NetEngine;
using Pb.Map;
using System.Collections.Generic;
using UnityEngine;
using Game.Scene.ModeController;
using Message;
using NetEngine.src;

namespace GameSync.Manager
{

    public class FrameDataManager : GameInstance<FrameDataManager>, IGameMono
    {
        private readonly int MaxSelfDataCount = 15;
        private readonly int MaxOtherDataCount = 15;
        private readonly int ChaseToFrame = 5;
        private const int Offest = 2;        // 距离的平方

        private IMotorGetter m_SelfKCC;

        private List<FrameData> m_SelfFrameDataList;
        private List<string> m_OtherPlayerIDList;
        private Dictionary<string, List<FrameData>> m_OtherFrameDataDic;

        public void Init()
        {
            m_SelfFrameDataList = new List<FrameData>();
            m_OtherPlayerIDList = new List<string>();
            m_OtherFrameDataDic = new Dictionary<string, List<FrameData>>();

            m_SelfKCC = AvatarController.Inst.SelfController;
        }

        public FrameDataManager()
        {
            MessageHelper.AddListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        }

        public override void Release()
        {
            base.Release();

            MessageHelper.RemoveListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        }

        public void SetSelfMotorGetter(IMotorGetter motorGetter)
        {
            if(motorGetter == null){
                LoggerUtils.LogError("SetSelfMotorGetter is null, use default KinematicCharacterController");
                m_SelfKCC = AvatarController.Inst.SelfController;
                return;
            }
            m_SelfKCC = motorGetter;
        }

        private void OnSyncMapInfoComplete()
        {
            var players = Global.Map.ClientData.Players;

            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].PlayerInfo.Uid.Equals(AccountDataManager.Inst.Uid))
                {
                    AddOtherPlayer(players[i].PlayerInfo.Uid);
                }
            }
        }

        private bool changeInput = false;

        public void FixedUpdate()
        {
            if(Inst.IsEdit())
            {
                return;
            }

            var inputData = MobileJoystick.Inst.GetCharacterIputs();

            if (Global.Room == null || Sdk.Instance == null || Global.Room.GetNetworkState(ConnectionType.Frame) == false)
            {
                // 没有进入房间，跑本地位移
                MobileJoystick.Inst.SetInputs(inputData);
                if((inputData.MoveAxisForward != 0 && inputData.MoveAxisRight != 0) && changeInput == false)
                {
                    changeInput = true;
                    MessageHelper.Broadcast(MessageName.OnVehicleEditDriverAudioPlay);
                }
                else if((inputData.MoveAxisForward == 0 && inputData.MoveAxisRight == 0) && changeInput == true)
                {
                    changeInput = false;
                    MessageHelper.Broadcast(MessageName.OnVehicleEditDriverAudioStop);
                }
                return;
            }

            AddSelfFrameData(inputData);

            // 自身预表现
            MobileJoystick.Inst.SetInputs(inputData);
            // 输入其他玩家命令
            OtherSetInputs();
        }

        public void Update()
        {
        }


        private PlayerCharacterInputs lastInputs = new PlayerCharacterInputs();
        private float lastPostionSendCD = 5;
        private void AddSelfFrameData(PlayerCharacterInputs inputData)
        {
            if (!inputData.Equals(lastInputs))
            {
                FrameData data = new FrameData();
                data.MoveForward = inputData.MoveAxisForward;
                data.MoveRight = inputData.MoveAxisRight;
                data.CameraRotation = inputData.CameraRotation.ToFixPB();
                data.PressJoystickTime = inputData.PressJoystickTime;

                data.Postion = m_SelfKCC.KCMotor.TransientPosition.ToFixPB();
                data.Rotation = m_SelfKCC.KCMotor.TransientRotation.ToFixPB();

                data.OpCmd = inputData.JumpDown ? 1 : 0;

                m_SelfFrameDataList.Add(data);
                lastInputs.CopyFrom(inputData);
                return;
            }

            lastPostionSendCD -= Time.fixedDeltaTime;
            if (lastPostionSendCD <= 0)
            {
                FrameData data = new FrameData();
                data.MoveForward = lastInputs.MoveAxisForward;
                data.MoveRight = lastInputs.MoveAxisRight;
                data.CameraRotation = lastInputs.CameraRotation.ToFixPB();
                data.PressJoystickTime = lastInputs.PressJoystickTime;

                data.Postion = m_SelfKCC.KCMotor.TransientPosition.ToFixPB();
                data.Rotation = m_SelfKCC.KCMotor.TransientRotation.ToFixPB();

                data.OpCmd = lastInputs.JumpDown ? 1 : 0;

                m_SelfFrameDataList.Add(data);
                lastPostionSendCD = 5;
            }
        }

        public void SetFrameData(FrameItem frameItem)
        {
            if (m_SelfFrameDataList.Count > MaxSelfDataCount)
            {
                frameItem.SyncType = (int)SyncType.Force;
                m_SelfFrameDataList.RemoveRange(MaxSelfDataCount, m_SelfFrameDataList.Count - m_SelfFrameDataList.Count);
            }

            frameItem.Frames.AddRange(m_SelfFrameDataList);
            m_SelfFrameDataList.Clear();
        }

        public void OtherFrameDataHandler(FrameItem frameItem)
        {
            if (frameItem == null || !m_OtherFrameDataDic.ContainsKey(frameItem.PlayerId)) return;

            m_OtherFrameDataDic[frameItem.PlayerId].AddRange(frameItem.Frames);

            ForceSetPositionAndRotation(frameItem);

            CheckOutOfMaxCount(frameItem);
        }

        public void AddOtherPlayer(string playerId)
        {
            if (!m_OtherFrameDataDic.ContainsKey(playerId))
            {
                m_OtherPlayerIDList.Add(playerId);
                m_OtherFrameDataDic.Add(playerId, new List<FrameData>());
            }
        }

        public void RemoveOtherPlayer(string playerId)
        {
            if (m_OtherFrameDataDic.ContainsKey(playerId))
            {
                m_OtherFrameDataDic.Remove(playerId);
                m_OtherPlayerIDList.Remove(playerId);
            }
        }

        /// <summary>
        /// 强制设置位置
        /// </summary>
        private void ForceSetPositionAndRotation(FrameItem frameItem)
        {
            if (frameItem.SyncType == (int)SyncType.Force)
            {
                LoggerUtils.Log($"因丢弃部分数据，强制设置位置。PlayerID : {frameItem.PlayerId}");

                // 强制设置位置
                var otherPlayer = AvatarController.Inst.GetPlayerStateCtrl(frameItem.PlayerId);
                if(otherPlayer.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle) && otherPlayer.PGCVehicleKinematicCtrl != null){

                    otherPlayer.PGCVehicleKinematicCtrl.Motor.SetPositionAndRotation(frameItem.Frames[0].Postion.ToFixVector3(), frameItem.Frames[0].Rotation.ToFixQuaternion());
                
                }else if(otherPlayer.stateMachine.ContainsCurrentState(PlayerState.Passenger)){
                    //乘客状态不强制同步位置
                }else if(otherPlayer.stateMachine.ContainsCurrentState(PlayerState.CameraMode) 
                && GameVehicleManager.Inst.IsPassenger(frameItem.PlayerId)){
                    //相机自拍模式乘客状态不强制同步位置
                }
                else
                {
                    otherPlayer.PlayerKCCtrl.Motor.SetPositionAndRotation(frameItem.Frames[0].Postion.ToFixVector3(), frameItem.Frames[0].Rotation.ToFixQuaternion());
                }
                frameItem.Frames.RemoveAt(0);
            }
        }

        /// <summary>
        /// 检查是否超出 MaxOtherDataCount 数量
        /// </summary>
        private void CheckOutOfMaxCount(FrameItem frameItem)
        {
            var otherFrameDataList = m_OtherFrameDataDic[frameItem.PlayerId];

            if (otherFrameDataList.Count > MaxOtherDataCount)
            {
                var otherPlayer = AvatarController.Inst.GetPlayerStateCtrl(frameItem.PlayerId);

                //判断是否在PGC载具状态
                if(otherPlayer.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle) && otherPlayer.PGCVehicleKinematicCtrl != null)
                    KinematicCharacterSystem.SinglePreSimulationInterpolationUpdate(otherPlayer.PGCVehicleKinematicCtrl.Motor);
                else
                    KinematicCharacterSystem.SinglePreSimulationInterpolationUpdate(otherPlayer.PlayerKCCtrl.Motor);

                for (int i = 0; i < otherFrameDataList.Count - ChaseToFrame; i++)
                {
                    FrameData data = otherFrameDataList[i];
                    if (data == null) continue;

                    PlayerCharacterInputs otherInputs = new PlayerCharacterInputs();
                    otherInputs.MoveAxisForward = data.MoveForward;
                    otherInputs.MoveAxisRight = data.MoveRight;
                    otherInputs.CameraRotation = data.CameraRotation.ToFixQuaternion();
                    otherInputs.PressJoystickTime = data.PressJoystickTime;
                    otherInputs.JumpDown = data.OpCmd == 1;

                    if (otherPlayer.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle) && otherPlayer.PGCVehicleKinematicCtrl != null)
                    {
                        otherPlayer.PGCVehicleKinematicCtrl.SetCalculatePos(data.Postion.ToFixVector3());
                        otherPlayer.PGCVehicleKinematicCtrl.SetInput(new Vector2(otherInputs.MoveAxisRight, otherInputs.MoveAxisForward), otherInputs.JumpDown);
                        otherPlayer.PGCVehicleKinematicCtrl.SetDriverCameraRotation(otherInputs.CameraRotation);
                        KinematicCharacterSystem.SingleSimulate(otherPlayer.PGCVehicleKinematicCtrl.Motor, Time.fixedDeltaTime);
                    }
                    else
                    {
                        otherPlayer.PlayerKCCtrl.SetCalculatePos(data.Postion.ToFixVector3());
                        otherPlayer.PlayerKCCtrl.SetInputs(ref otherInputs);
                        KinematicCharacterSystem.SingleSimulate(otherPlayer.PlayerKCCtrl.Motor, Time.fixedDeltaTime);
                    }
                }

                //判断是否在PGC载具状态
                if(otherPlayer.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle) && otherPlayer.PGCVehicleKinematicCtrl != null)
                    KinematicCharacterSystem.SinglePostSimulationInterpolationUpdate(otherPlayer.PGCVehicleKinematicCtrl.Motor);
                else
                    KinematicCharacterSystem.SinglePostSimulationInterpolationUpdate(otherPlayer.PlayerKCCtrl.Motor);

                otherFrameDataList.RemoveRange(0, otherFrameDataList.Count - ChaseToFrame);
            }
        }

        private void OtherSetInputs()
        {
            for (int i = 0; i < m_OtherPlayerIDList.Count; i++)
            {
                var otherFrameDataList = m_OtherFrameDataDic[m_OtherPlayerIDList[i]];

                if (otherFrameDataList.Count > 0)
                {
                    FrameData data = otherFrameDataList[0];
                    if (data == null)
                    {
                        otherFrameDataList.RemoveAt(0);
                        continue;
                    }

                    PlayerCharacterInputs otherInputs = new PlayerCharacterInputs();
                    otherInputs.MoveAxisForward = data.MoveForward;
                    otherInputs.MoveAxisRight = data.MoveRight;
                    otherInputs.CameraRotation = data.CameraRotation.ToFixQuaternion();
                    otherInputs.PressJoystickTime = data.PressJoystickTime;
                    otherInputs.JumpDown = data.OpCmd == 1;

                    var otherPlayer = AvatarController.Inst.GetPlayerStateCtrl(m_OtherPlayerIDList[i]);
                    if (otherPlayer != null)
                    {
                        if (otherPlayer.stateMachine.ContainsCurrentState(PlayerState.PGCVehicle) && otherPlayer.PGCVehicleKinematicCtrl != null)
                        {
                            if ((otherPlayer.PGCVehicleKinematicCtrl.Motor.TransientPosition - data.Postion.ToFixVector3()).magnitude >= Offest)
                            {
                                otherPlayer.PGCVehicleKinematicCtrl.SetCalculatePos(data.Postion.ToFixVector3());
                            }
                            otherPlayer.PGCVehicleKinematicCtrl.SetInput(new Vector2(otherInputs.MoveAxisRight, otherInputs.MoveAxisForward), otherInputs.JumpDown);
                            otherPlayer.PGCVehicleKinematicCtrl.SetDriverCameraRotation(otherInputs.CameraRotation);
                        }
                        else
                        {
                            if ((otherPlayer.PlayerKCCtrl.Motor.TransientPosition - data.Postion.ToFixVector3()).magnitude >= Offest)
                            {
                                otherPlayer.PlayerKCCtrl.SetCalculatePos(data.Postion.ToFixVector3());
                            }

                            otherPlayer.PlayerKCCtrl.SetInputs(ref otherInputs);
                        }
                    }
                    otherFrameDataList.RemoveAt(0);
                }
            }
        }
    }

    public enum SyncType
    {
        Normal = 0,
        Force = 1,  // 强制同步位置
    }
}
