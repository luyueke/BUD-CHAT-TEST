using AIGame.Base;
using Game.Avatar;
using Game.Props;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Utils;
using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using AIGame.Prop;
using GameData.BaseInfo;
using Game.KinematicCharacter;
using Cinemachine;
using Basic;
using System.Collections;
using Game.Props.PropsBehaviours;

public class AIParkGuest_GmPanel : MonoBehaviour
{
    private CinemachineBrain cinemachineBrain;

    [SerializeField] private CButton btn1;
    [SerializeField] private CButton btn2;
    [SerializeField] private CButton btn3;
    [SerializeField] private CButton btn4;
    [SerializeField] private CButton btn5;
    [SerializeField] private CButton btn6;
    [SerializeField] private CButton btn7;
    public List<KinematicCharacterController> npcList = new List<KinematicCharacterController>();
    void Awake()
    {
        btn1.onClick.AddListener(OnClick_Gm1_NextScene);
        btn2.onClick.AddListener(OnClick_Gm2_NextScene);
        btn3.onClick.AddListener(OnClick_Gm3_NextScene);
        btn4.onClick.AddListener(OnClick_Gm4_NextScene);
        btn5.onClick.AddListener(OnClick_Gm5_NextScene);
        // btn6.onClick.AddListener(OnClick_Gm6_NextScene);
        // btn7.onClick.AddListener(OnClick_Gm7_NextScene);
        transform.localScale = new Vector3(0, 0, 0);
        // transform.localScale = Vector3.one;
    }

    void Start()
    {
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
    }

    public void OnClick_Gm1_NextScene()
    {
            //        AIGameCameraUtils.Inst.SetMoveCameraPosAndRotation(new(26.97f,2.366f,0.607f), new(6.86f,85f, 0));
            // AIGameCameraUtils.Inst.SetCamFOV(50);
            AIParkGuideMgr.Inst.OnStepChange(S11GuideStep.Guide_FirstInGame_Guide_2_3);
return;
            AvatarController.Inst.SelfController.SetFreezeCharacter(true);return;

        AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_1_1);
        //  var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(((int)(ParkNpcRoleType.Elise)).ToString());

        //                 npcBehaviour.PlayAnim("40200053");
        //                 return;
        // AIParkTcpNetMgr.Instance.testt5();
        // return;
        //       AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        // aiGame.OnGameEndRsp();
// AIPark_CharacterManager.Inst.GetNpc(ParkNpcRoleType.Pio)._npcAnimController.SetPlayerAniState(PlayerAniState.Idle,true);
        // AIPark_CharacterManager.Inst.ResetNpc2DiscussPoint();
        // AIPark_CharacterManager.Inst.InitSceneAICharacter(new List<string> { "Elise", "Tilia", "Casper", "Pio", "Teddy", "Vivien", "Rowland" });
     //   AIParkPropsManager.Inst.ExitAction(ParkNpcRoleType.Elise, ActionType.SeeSaw);
        return;
        OnClick_Gm6_NextScene(); return;
        var selfTrans = AvatarController.Inst.SelfController.gameObject.transform;
        var parkRoleTypes = new List<string> { "Elise", "Tilia", "Casper", "Pio", "Teddy", "Vivien", "Rowland" };
        npcList.Clear();
        // var parkRoleTypes = new List<string> { "Elise"};
        var points = AIPark_RandomPointUtil.Inst.RandomPoints(selfTrans.position, selfTrans.forward, 4, parkRoleTypes.Count);
        List<ParkNpcTransData> spawnPoints = new List<ParkNpcTransData>();
        for (int i = 0; i < parkRoleTypes.Count; i++)
        {
            var spawnPoint = points[i];
            var direction = (selfTrans.position - spawnPoint).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var euler = new Vector3(0, angle, 0);
            spawnPoints.Add(new ParkNpcTransData()
            {
                pos = spawnPoint,
                rot = euler,
            });
        }
        int idx = 0;
        // foreach (var tempParkRoleType in parkRoleTypes)
        // {
        //     ParkNpcRoleType parkNpcRoleType = (ParkNpcRoleType)Enum.Parse(typeof(ParkNpcRoleType), tempParkRoleType);
        //     string avatarJson = AIPark_CharacterManager.Inst.CreateAvatarJsonByParkNpcType(parkNpcRoleType);
        //     var npcKcc = AIBuddyAvatarController.Inst.CreateAIGameAINpc(tempParkRoleType, tempParkRoleType, avatarJson);

        //     // 初始位置
        //     ParkNpcTransData SpawnPointData = new ParkNpcTransData();
        //     //黑幕后初始位置是聚在一起的
        //     SpawnPointData = AIPark_CharacterUtils.Inst.GetSpawnPointByRoleType(parkNpcRoleType);
        //     SpawnPointData = spawnPoints[idx];

        //     idx++;
        //     var firstPos = SpawnPointData.pos;
        //     var firstRot = SpawnPointData.rot;
        //     var npcBev = npcKcc.gameObject.AddComponent<AIPark_CharacterBehaviour>();
        //     npcBev.gameObject.AddComponent<NpcAvatarTrigger>();
        //     npcKcc.Motor.SetPositionAndRotation(firstPos, Quaternion.Euler(firstRot));
        //     npcBev.InitData(new()
        //     {
        //         npcId = ((int)parkNpcRoleType).ToString(),
        //         name = parkNpcRoleType.ToString().ToLower(),
        //         npcType = (int)ParkNPCType.Default,
        //         npcRole = (int)parkNpcRoleType,
        //         avatarJson = avatarJson,
        //     });
        //     npcList.Add(npcKcc);
        //     // LoggerUtils.Log($"[AIPark_CharacterManager] 创建NPC: ID={scriptData.npcId}, Name={scriptData.name}");
        // }
    }


    void OnClick_Gm2_NextScene()
    {
        AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_1_3);
return;
         var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(((int)(ParkNpcRoleType.Elise)).ToString());
                        // npcBehaviour.PlayAnim("40200505");
                        // return;
             AIParkTcpNetMgr.Instance.testt6();
        return;
                var transform = AIPark_CharacterManager.Inst.GetNpc("1").transform;

              AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        aiGame.SetDiscussCamera(transform,1,null);
        return;
        // AIPark_CharacterManager.Inst.SceneAIDoActionWhenNpcDiscussEnd();return;
        // AIParkTcpNetMgr.Instance.SendAIParkSyncReq_InferAction("1");return;
        // OnClick_Gm7_NextScene(); return;

        var selfTrans = AvatarController.Inst.SelfController.gameObject.transform;
        var parkRoleTypes = new List<string> { "Elise", "Tilia", "Casper", "Pio", "Teddy", "Vivien", "Rowland" };
        // var parkRoleTypes = new List<string> { "Elise"};
        var points = AIPark_RandomPointUtil.Inst.RandomPoints(selfTrans.position, selfTrans.forward, 3, parkRoleTypes.Count);
        List<ParkNpcTransData> spawnPoints = new List<ParkNpcTransData>();
        for (int i = 0; i < parkRoleTypes.Count; i++)
        {
            var spawnPoint = points[i];
            var direction = (selfTrans.position - spawnPoint).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // 返回欧拉角
            var euler = new Vector3(0, angle, 0);
            spawnPoints.Add(new ParkNpcTransData()
            {
                pos = spawnPoint,
                rot = euler,
            });
        }
        int idx = 0;

        spawnPoints = AIPark_CharacterUtils.Inst.GetAllSpawnPointByLocationType(LocationType.Park, parkRoleTypes.Count);

        foreach (var tempParkRoleType in parkRoleTypes)
        {
            var npcKcc = npcList[idx];
            var SpawnPointData = spawnPoints[idx];
            idx++;
            var firstPos = SpawnPointData.pos;
            var firstRot = SpawnPointData.rot;
            npcKcc.Motor.SetPositionAndRotation(firstPos, Quaternion.Euler(firstRot));
        }
    }


    void OnClick_Gm3_NextScene()
    {
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
                guestPanel.DirectUnlockInput();

        var target = GameObject.Find("CameraTarget");
        var trans = AvatarController.Inst.SelfController.transform;
        // target.transform.position = trans.position;
        return; 
        Debug.LogError(111);
                 var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(((int)(ParkNpcRoleType.Elise)).ToString());
                        // npcBehaviour.PlayAnim("40200505");
                            npcBehaviour._npcAnimController.SetPlayerAniState(PlayerAniState.Idle, false);
                            npcBehaviour.playerStateControllerState.ExitState(PlayerState.SingleEmote);
                        return;
        //   var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc(((int)(ParkNpcRoleType.Elise)).ToString());

                        // npcBehaviour.PlayAnim("40200053");
                        return;
          AIParkTcpNetMgr.Instance.testt7();
        return;
          var transform = AvatarController.Inst.SelfController.transform;
         

            CinemachineTouchParam param;
            param.target = transform;
            param.followOff = new Vector3(0, 1, 2);
            param.fov = 30;
            param.rotateSpeed = 1;
            param.zoomSpeed = 1;
            param.mouseRotateSpeed = 2;
            param.mouseZoomSpeed = 2;
            param.minZoom = 5;
            param.maxZoom = 15;

             var playerCamera = GameCameraUtils.Inst.GetPlayVirtualCamera();
            var camera = GameCameraUtils.Inst.GetCustomVirtualCamera();
            GameCameraUtils.Inst.SetCinemachineTouchController(true, param);
            camera.Follow = param.target;
            camera.LookAt = param.target;
            camera.m_Lens.FieldOfView = param.fov;
            camera.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = param.followOff;

            playerCamera.enabled = false;
            if (!camera.enabled)
            {
                camera.enabled = true;
            }
        // AIPark_CharacterManager.Inst.SceneAIDoActionWhenNpcDiscussEnd();
        // return;
        // AIParkTcpNetMgr.Instance.testt4();
        return;
 
        var selfTrans = AvatarController.Inst.SelfController.gameObject.transform;
        var selfSpawnPoint = AIPark_CharacterUtils.Inst.GetSelfSpawnPointByLocationType(LocationType.Park);
        selfTrans.GetComponent<KinematicCharacterMotor>().enabled = true;
        selfTrans.GetComponent<KinematicCharacterMotor>().SetPositionAndRotation(selfSpawnPoint.pos, Quaternion.Euler(selfSpawnPoint.rot));
    }
    void OnClick_Gm4_NextScene()
    {
        AIParkGuideMgr.Inst.RunGuide(S11GuideStep.Guide_FirstInGame_Guide_1_4);
        return;
        Debug.LogError(11121);
            var npc2 = AIPark_CharacterManager.Inst.GetNpc("2");
 Vector3 npcPosition = npc2.transform.position;
            Vector3 npcForward = npc2.transform.forward;
            Vector3 npcUp = npc2.transform.up;
            
            // 使用AIGameCameraUtils设置相机朝向NPC，过渡时间1秒，距离3米
            AIGameCameraUtils.Inst.SetCamLookAt(npcPosition+new Vector3(0,0.5f,0), -(npcForward*2.5f+npcUp), 0, 5f); 
        return;
        var t  =GameObject.Find("AIHospitalHUDPanel").transform.Find("AIHospitalNpcHudItem(Clone)/TargetView/Btn_ChatToNpc");
        Debug.LogError(t.position);
        Debug.LogError(t.GetComponent<RectTransform>().position);
        return;
        AIParkTcpNetMgr.Instance.testt8();
        return;
         GameCameraUtils.Inst.GetCustomVirtualCamera().enabled = false;
            GameCameraUtils.Inst.SetCinemachineTouchController(false, new CinemachineTouchParam());
            GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = true;
        // AIParkTcpNetMgr.Instance.testt5();
        return;
        var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());
        if (npc != null)
        {
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev != null)
            {
                // AIParkPropsManager.Inst.EnterSeeSaw(npcBev, null, null);
                return;
            }
        }
        return;
        Debug.LogError(42121);
        // AvatarController.Inst.SelfController.UnbindCameraTarget();
        var aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        var cinemachineBrain = GlobalCameraManager.Inst.GlobalMainCamera.GetComponent<CinemachineBrain>();
        cinemachineBrain.enabled = false;

        var mainCamera = GameCameraUtils.Inst.GetMainCamera();
        var cameraTrans = mainCamera.transform;
        var targetPos = new Vector3(19.8f, 2, -3);

        // 计算endPos为targetPos绕指定向量旋转一定角度后的位置点
        Vector3 endPos = CalculateRotatedPositionAroundCenter(cameraTrans.position, new(targetPos.x, cameraTrans.position.y, targetPos.z), Vector3.up, 45f); // 绕Y轴旋转45度

        // GameCameraUtils.Inst.StartCameraMovement(
        //     cinemachineBrain.transform.position, 
        //     endPos, 
        //     targetPos, 
        //     3f, 
        //     null
        // );
        Debug.LogError(endPos);

        var cameraTarget = GameObject.Find("CameraTarget").transform;
        // var cameraTarget = AvatarController.Inst.SelfController.CameraTarget;
        var cameraTargetOffset = cameraTarget.position - cameraTrans.position;




    }

    /// <summary>
    /// 计算点绕指定轴旋转后的位置
    /// </summary>
    /// <param name="point">要旋转的点</param>
    /// <param name="axis">旋转轴（单位向量）</param>
    /// <param name="angle">旋转角度（度）</param>
    /// <returns>旋转后的位置</returns>
    private Vector3 CalculateRotatedPosition(Vector3 point, Vector3 axis, float angle)
    {
        // 将角度转换为弧度
        float radians = angle * Mathf.Deg2Rad;

        // 创建旋转四元数
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);

        // 应用旋转
        return rotation * point;
    }

    private IEnumerator SmoothCameraTransition(
        Vector3 startPos, Vector3 endPos,
        Quaternion startRotation, Quaternion endRotation,
        float duration,
        System.Action onComplete = null)
    {
        float elapsedTime = 0;
        var mainCamera = GameCameraUtils.Inst.GetMainCamera();
        var cameraTrans = mainCamera.transform;

        // 在过渡开始前确保 Cinemachine 是禁用的
        cinemachineBrain.enabled = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 使用平滑的插值曲线
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // 同时插值位置和旋转
            cameraTrans.position = Vector3.Lerp(startPos, endPos, smoothT);
            cameraTrans.rotation = Quaternion.Slerp(startRotation, endRotation, smoothT);

            yield return null;
        }

        // 确保最终位置和旋转精确
        cameraTrans.position = endPos;
        cameraTrans.rotation = endRotation;

        // 在启用 Cinemachine 之前，先重新绑定相机目标
        // AvatarController.Inst.SelfController.RebindCameraTarget();

        // 获取当前的 Cinemachine 虚拟相机
        var virtualCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
        if (virtualCamera != null)
        {
            // 将虚拟相机的位置和旋转设置为当前相机的位置和旋转
            virtualCamera.transform.position = cameraTrans.position;
            virtualCamera.transform.rotation = cameraTrans.rotation;

            // 如果虚拟相机有 Body 组件，同步其位置
            if (virtualCamera.GetCinemachineComponent<CinemachineTransposer>() is CinemachineTransposer transposer)
            {
                transposer.m_FollowOffset = virtualCamera.transform.InverseTransformPoint(cameraTrans.position);
            }
        }

        // 现在可以安全地启用 Cinemachine
        cinemachineBrain.enabled = true;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 计算点绕指定轴旋转后的位置（相对于中心点）
    /// </summary>
    /// <param name="point">要旋转的点</param>
    /// <param name="center">旋转中心点</param>
    /// <param name="axis">旋转轴（单位向量）</param>
    /// <param name="angle">旋转角度（度）</param>
    /// <returns>旋转后的位置</returns>
    private Vector3 CalculateRotatedPositionAroundCenter(Vector3 point, Vector3 center, Vector3 axis, float angle)
    {
        // 计算相对于中心点的位置
        Vector3 relativePoint = point - center;

        // 旋转相对位置
        Vector3 rotatedRelative = CalculateRotatedPosition(relativePoint, axis, angle);

        // 返回绝对位置
        return center + rotatedRelative;
    }
    public int offset = 3;
    void OnClick_Gm5_NextScene()
    {

                var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc("3");
                        npcBehaviour.PlayAnim("40200505");
        AIParkTcpNetMgr.Instance.testt6();
        return;
        var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());
        if (npc != null)
        {
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev != null)
            {
                Debug.LogError("custom");
                // AIParkPropsManager.Inst.ExitSeeSaw(npcBev, null, null);
                return;
            }
        }
        return;

        GameObject go = GameObject.Find("AI_Park_building_fountain_01_05");
        var targetPos = new Vector3(19.8f, 2, -3);
        Debug.LogError(offset);
        AIGameCameraUtils.Inst.SetCamToPos(go.transform, 1, offset);
        TimerManager.Inst.RunOnce("WaitCameraMove1", 2f, () =>
        {
            AIGameCameraUtils.Inst.BackToPlayer();
        });

        return;
        var mainCamera = GameCameraUtils.Inst.GetMainCamera();
        var selfTrans = mainCamera.transform;

    }

    void OnClick_Gm6_NextScene()
    {
        var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Casper).ToString());
        if (npc != null)
        {
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev != null)
            {
                Debug.LogError("custom");
                AIParkPropsManager.Inst.EnterAction(npcBev, ActionType.Swinging);
                return;
            }
        }
        return;
    }
    void OnClick_Gm7_NextScene()
    {

        // var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.Casper).ToString());
        // if (npc != null)
        // {
        //     var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
        //     if (npcBev != null)
        //     {
        //         Debug.LogError("custom");
        //         // AIParkPropsManager.Inst.ExitSeeSaw(npcBev, null, null);
        //         AIParkPropsManager.Inst.ExitAction(npcBev, ActionType.Swinging);

        //         return;
        //     }
        // }
        // return;
    }
}


