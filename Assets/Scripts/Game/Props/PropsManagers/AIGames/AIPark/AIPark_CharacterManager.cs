using System;
using System.Collections.Generic;
using AIGame.Prop;
using Game.Avatar;
using UnityEngine;
using Game.Base;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData.BaseInfo;
using System.Text.RegularExpressions;
using Pb.Game;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AIGame.Base;
using Game.KinematicCharacter;
using Google.Protobuf;
using Message;
using Game.Props.PropsBehaviours;
using Basic.Utils;
using System.Linq;

namespace Game.Props.PropsManagers
{
    public class AIPark_CharacterManager : GlobalInstance<AIPark_CharacterManager>
    {
        // NPC字典，存储NPC ID与NPC行为控制器的映射
        private Dictionary<string, AIPark_CharacterBehaviour> _npcDicts = new Dictionary<string, AIPark_CharacterBehaviour>();

        // AIPark_CharacterBehaviour _selfPlayer;
        // 已使用的角色类型集合，用于确保随机不重复
        private HashSet<ParkNpcRoleType> _usedRoleTypes = new HashSet<ParkNpcRoleType>();
        //begin 预想是history的步骤演绎完成，才能触发下一个history
        private List<History> _curHistorys; //当前演绎的剧情列表
        private History _curHistory; //当前演绎剧情内容

        private Dictionary<string, bool> _historyCompleteStepDict = new();//记录history里各个npc步骤完成情况
                                                                          // private int _historyCompleteStep = 0;//剧情某个history的步骤
                                                                          // private int _historyTotalStep = 0; //剧情某个history的总步骤
                                                                          //end
        private Dictionary<AIPark_CharacterInitPosType, AIPark_CharacterInitPosData> _8PosData = new Dictionary<AIPark_CharacterInitPosType, AIPark_CharacterInitPosData>();

        List<AIPark_CharacterInitPosData> _8PosDataList = new List<AIPark_CharacterInitPosData>();

        public DoubleSearchDictionary<string, string> sitEmote2SpeakEmoteDoubleDic = new();
        private bool _triggerHistoryState = false;
        public bool TriggerHistoryState
        {
            get
            {
                return _triggerHistoryState;
            }
            set
            {
                _triggerHistoryState = value;
                TriggerNextHistory();
            }
        }
        public bool IsPgcEnter = false;
        /// <summary>
        /// 注册NPC
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <param name="character">NPC行为控制器</param>
        public void RegisterNpc(string id, AIPark_CharacterBehaviour character)
        {
            _npcDicts[id] = character;
            LoggerUtils.Log($"[AIPark_CharacterManager] 注册NPC: ID={id}, Name={character.GetNpcName()}");
        }

        public Dictionary<string, AIPark_CharacterBehaviour> GetNpcDic()
        {
            return _npcDicts;
        }

        /// <summary>
        /// 获取NPC位置
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <returns>NPC位置</returns>
        public Vector3 GetNpcPosition(string id)
        {
            if (_npcDicts.TryGetValue(id, out AIPark_CharacterBehaviour character))
            {
                return character.transform.position;
            }

            LoggerUtils.LogError($"乐园[AIPark_CharacterManager.GetNpcPosition] 未找到NPC: ID={id}");
            return Vector3.zero;
        }

        /// <summary>
        /// 获取NPC
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <returns>NPC行为控制器</returns>
        public AIPark_CharacterBehaviour GetNpc(string id)
        {
            if (_npcDicts.TryGetValue(id, out AIPark_CharacterBehaviour character))
            {
                return character;
            }
            // ParkNpcRoleType result = ParkNpcRoleType.Default;
            // try
            // {
            //     // 尝试解析枚举名称
            //     result = (ParkNpcRoleType)Enum.Parse(typeof(ParkNpcRoleType), id, true);
            // }
            // catch (ArgumentException)
            // {
            // }
            // if (_npcDicts.TryGetValue(((int)result).ToString(), out AIPark_CharacterBehaviour character2))
            // {
            //     return character2;
            // }

            LoggerUtils.Log($"乐园[AIPark_CharacterManager.GetNpc] 未找到NPC: ID={id}");
            return null;
        }


        /// <summary>
        /// 获取不重复的随机角色类型
        /// </summary>
        /// <returns>未使用过的随机角色类型</returns>
        private ParkNpcTransData GetRandomSpawnPoint()
        {
            //目标元数据
            var spawnPoint = AIPark_CharacterUtils.Inst.UGCGameSpawnPoint;

            // 创建可用出生点列表（排除已使用的）
            List<ParkNpcTransData> availableSpawnPoints = new List<ParkNpcTransData>();

            // 遍历所有出生点
            foreach (var point in spawnPoint)
            {
                // 检查此出生点是否已被使用
                bool isUsed = false;
                foreach (var npc in _npcDicts.Values)
                {
                    Vector3 npcPos = npc.transform.position;
                    // 判断位置是否接近（考虑一定误差范围）
                    if (Vector3.Distance(npcPos, point.pos) < 0.5f)
                    {
                        isUsed = true;
                        break;
                    }
                }

                // 如果未被使用，则添加到可用列表
                if (!isUsed)
                {
                    availableSpawnPoints.Add(point);
                }
            }

            // 如果没有可用出生点，则返回一个随机出生点（虽然可能重复）
            if (availableSpawnPoints.Count == 0)
            {
                LoggerUtils.LogError("乐园[AIPark_CharacterManager] 没有可用的不重复出生点，将使用随机出生点");
                return spawnPoint[UnityEngine.Random.Range(0, spawnPoint.Count)];
            }

            // 返回一个随机的可用出生点
            ParkNpcTransData resultType = availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
            LoggerUtils.Log($"乐园[AIPark_CharacterManager] 选择出生点: pos={resultType.pos}");

            return resultType;
        }

        public void InitSceneAICharacter(List<string> parkRoleIdList, List<string> avatarJsonList, List<string> nameList)
        {
            _npcDicts.Clear();
            _usedRoleTypes.Clear();

            for (int i = 0; i < parkRoleIdList.Count; i++)
            {
                string roleId = parkRoleIdList[i];
                if (roleId == ((int)ParkNpcRoleType.self).ToString())
                {
                    var npcBev = AvatarController.Inst.SelfController.gameObject.AddComponent<AIPark_CharacterBehaviour>();
                    npcBev.gameObject.AddComponent<NpcAvatarTrigger>();
                    npcBev.InitData(new()
                    {
                        npcId = roleId,
                        name = nameList[i],
                    });
                    RegisterNpc(roleId, npcBev);
                }
                else
                {
                    string avatarJson = avatarJsonList[i];
                    var npcKcc = AIBuddyAvatarController.Inst.CreateAIGameAINpc(nameList[i], roleId, avatarJson);
                    var npcBev = npcKcc.gameObject.AddComponent<AIPark_CharacterBehaviour>();
                    npcBev.gameObject.AddComponent<NpcAvatarTrigger>();
                    // 初始化NPC
                    npcBev.InitData(new()
                    {
                        npcId = roleId,
                        name = nameList[i],
                    });

                    // 添加到列表
                    RegisterNpc(roleId, npcBev);
                }
            }

            LoggerUtils.Log($"[AIPark_CharacterManager] 初始化完成，NPC数量: {_npcDicts.Count}");
        }


        public void ResetNpc2DiscussPoint(LocationType locationType = LocationType.Park)
        {
            int cnt = _npcDicts.Values.Count;
            var spawnPoints = AIPark_CharacterUtils.Inst.GetAllSpawnPointByLocationType(locationType, cnt, 0.3f);
            int idx = 0;

            var selfSpawnPoint = AIPark_CharacterUtils.Inst.GetSelfSpawnPointByLocationType(locationType);

            foreach (var npc in _npcDicts.Values)
            {
                // var roleType = npc.GetNpcRoleType();
                // if (roleType == ParkNpcRoleType.self)
                // {
                //     npc.GetComponent<KinematicCharacterController>().Motor.SetPositionAndRotation(selfSpawnPoint.pos, Quaternion.Euler(selfSpawnPoint.rot));
                //     continue;
                // }
                ParkNpcTransData SpawnPointData = spawnPoints[idx];

                var firstPos = SpawnPointData.pos;
                var firstRot = SpawnPointData.rot;
                npc.GetComponent<KinematicCharacterController>().Motor.SetPositionAndRotation(firstPos, Quaternion.Euler(firstRot));
                idx++;
            }
            var mainCameraTarget = GameObject.Find("CameraTarget");
            var trans = AvatarController.Inst.SelfController.gameObject.transform;
            mainCameraTarget.transform.localEulerAngles = trans.localEulerAngles + new Vector3(20, 0, 0);
        }



        /// <summary>
        /// 示例：使用GetAllSpawnPointByLocationType2方法在指定半径的圆上重置NPC位置
        /// </summary>
        /// <param name="locationType">位置类型</param>
        /// <param name="radius">圆的半径</param>
        public void ResetNpc2DiscussPointOnCircle(LocationType locationType = LocationType.Park, float radius = 5f)
        {
            int cnt = _npcDicts.Values.Count;

            // 使用新的方法获取圆上的有效行走点
            var spawnPoints = AIPark_CharacterUtils.Inst.GetAllSpawnPointByLocationType2(locationType, cnt, radius, 1f);

            if (spawnPoints.Count == 0)
            {
                LoggerUtils.LogError($"ResetNpc2DiscussPointOnCircle: 在半径 {radius} 的圆上未找到有效点，使用默认方法");
                ResetNpc2DiscussPoint(locationType);
                return;
            }

            int idx = 0;
            foreach (var npc in _npcDicts.Values)
            {
                if (idx < spawnPoints.Count)
                {
                    ParkNpcTransData SpawnPointData = spawnPoints[idx];
                    var firstPos = SpawnPointData.pos;
                    var firstRot = SpawnPointData.rot;
                    npc.GetComponent<KinematicCharacterController>().Motor.SetPositionAndRotation(firstPos, Quaternion.Euler(firstRot));
                    idx++;
                }
                else
                {
                    LoggerUtils.Log($"ResetNpc2DiscussPointOnCircle: NPC数量超过可用点数量，跳过剩余NPC");
                    break;
                }
            }

            LoggerUtils.Log($"ResetNpc2DiscussPointOnCircle: 成功将 {idx} 个NPC重置到半径 {radius} 的圆上");
        }

        public void ResetNpc2Idle()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.StopMoving();
                AIPark_ChapterData aIPark_ChapterData = new()
                {
                    action = (int)ActionType.Idle,
                    location = (int)LocationType.None,
                    participants = new List<string>() { npc.GetNpcID() },
                };
                npc.GetChapterHandler()?.ChangeState(aIPark_ChapterData);
            }
        }

        public void InsertHistory(string npcId, History history)
        {
            var npc = GetNpc(npcId);
            if (npc != null)
            {
                //行为
                if (history.Quotes?.Count > 0)
                {
                    // Debug.Log("乐园开始讲话:" + history.Quotes[0].Speaker);
                    //纯讲话
                    MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, history.Quotes[0].Speaker, history.Quotes[0].Content, () =>
                    {
                        // Debug.Log("乐园开始讲话结束:" + history.Quotes[0].Speaker);
                        MessageHelper.Broadcast<string>(MessageName.ParkNpcTalkOver, history.Quotes[0].Speaker);
                        UpdateHistoryCompleteStep(npcId, true);
                    });
                }
                else
                {
                    // history.Action = (int)ActionType.Idle; //默认只有idle 暂时只有群聊 没有行动
                    npc.InsertHistory(history);
                    // UpdateHistoryCompleteStep(npcId, true);
                }
            }
            else
            {
                UpdateHistoryCompleteStep(npcId, true);
            }
        }

        public void InsertHistoryImmediate(string npcId, History history)
        {
            var npc = GetNpc(npcId);
            if (npc != null)
            {
                //行为
                if (history.Quotes?.Count > 0)
                {
                    //纯讲话
                    MessageHelper.Broadcast<string, string, Action>(MessageName.OnParkBeginNpcTalk, history.Quotes[0].Speaker, history.Quotes[0].Content, () =>
                    {
                        MessageHelper.Broadcast<string>(MessageName.ParkNpcTalkOver, history.Quotes[0].Speaker);
                    });
                }
                else
                {
                    // history.Action = (int)ActionType.Idle; //默认只有idle 暂时只有群聊 没有行动
                    npc.InsertHistory(history);
                }
            }
            else
            {
            }
        }

        public void EnableCharacterDetection(string npcId)
        {
            var npc = GetNpc(npcId);
            if (npc != null)
            {
                npc.EnableCharacterDetection();
            }
        }

        public void DisableCharacterDetection(string npcId)
        {
            var npc = GetNpc(npcId);
            if (npc != null)
            {
                npc.DisableCharacterDetection();
            }
        }

        public void EnableAllCharacterDetection()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.EnableCharacterDetection();
            }
        }

        public void DisableAllCharacterDetection()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.DisableCharacterDetection();
            }
        }

        public void EnableSelfCharacterDetection()
        {
            AvatarController.Inst.SelfController.gameObject.GetComponent<AIPark_CharacterBehaviour>().EnableCharacterDetection();
        }

        public void DisableSelfCharacterDetection()
        {
            AvatarController.Inst.SelfController.gameObject.GetComponent<AIPark_CharacterBehaviour>().DisableCharacterDetection();
        }

        public void ResetSceneAICharacter()
        {

        }
        //剧情演绎begin
        //停止当前的history
        public void StopCurHistory()
        {
            //todo 停止当前的history 找到对应的npc
            _curHistory = null;
        }

        public void ReInitSceneAICharacter(List<History> actions)
        {
            if (actions?.Count == 0)
            {
                LoggerUtils.LogError($"乐园[AIPark_CharacterManager] 新的剧情列表为空");
                return;
            }
            _curHistorys ??= new();
            _curHistorys.Clear();
            _curHistorys.AddRange(actions);

            TriggerNextHistory();
        }

        public void AddSceneAICharacter(List<History> actions)
        {
            if (actions?.Count == 0)
            {
                LoggerUtils.LogError($"乐园[AIPark_CharacterManager] 新的剧情列表为空");
                return;
            }
            _curHistorys ??= new();
            _curHistorys.AddRange(actions);

            TriggerNextHistory();
        }

        public void AddSceneAICharacter(History action)
        {
            if (action == null)
            {
                LoggerUtils.LogError($"乐园[AIPark_CharacterManager] 新的剧情为空");
                return;
            }
            _curHistorys ??= new();
            _curHistorys.Add(action);

            TriggerNextHistory();
        }

        public void SceneAIDoRandomActions()
        {
            var availableNodeCount = AIParkPropsManager.Inst.GetAvailableNodeCount();
            int i = 0;
            foreach (var npc in _npcDicts.Values)
            {
                if (npc.GetNpcID() != ((int)ParkNpcRoleType.self).ToString() && i < 10)
                {
                    i++;
                    //由后端推动
                    AIParkPropsManager.Inst.CheckDoCustomAction(npc.GetNpcID(), 100);
                }
            }
        }

        void SceneAICharacter2DoThing(History history)
        {
            foreach (var npcId in history.Participants)
            {
                InsertHistory(npcId, history);
            }
        }

        //npc完成步骤时调用这个
        public void UpdateHistoryCompleteStep(string npcId, bool isComplete)
        {
            if (_historyCompleteStepDict.ContainsKey(npcId))
            {
                Debug.Log("乐园111更新步骤:" + npcId + " 是否完成:" + isComplete);
                _historyCompleteStepDict[npcId] = isComplete;
            }
            else
            {
                Debug.Log("乐园更新步骤错误:" + npcId + " 是否完成:" + isComplete);
                LoggerUtils.Log($"乐园[AIPark_CharacterManager.UpdateHistoryCompleteStep] 未找到npcId: {npcId}");
            }
            if (_CheckHistoryComplete())
            {
                TriggerNextHistory();
            }
        }

        bool _CheckHistoryComplete()
        {
            if (_curHistory == null)
            {
                return true;
            }
            foreach (var npcId in _curHistory.Participants)
            {
                if (_historyCompleteStepDict[npcId] == false)
                {
                    return false;
                }
            }
            return true;
        }

        public void ResetAllHistoryState(){
            _triggerHistoryState = false;
            _curHistorys?.Clear();
            _curHistory = null;
            _historyCompleteStepDict?.Clear();
        }

        public void TriggerNextHistory()
        {
            if (_curHistorys == null || _curHistorys?.Count == 0)
            {
                return;
            }
            if (!_triggerHistoryState)
            {
                return;
            }
            if (!_CheckHistoryComplete())
            {
                return;
            }
            _curHistory = _curHistorys[0];
            _curHistorys.RemoveAt(0);
            LoggerUtils.Log("乐园剧情开始:" + JsonConvert.SerializeObject(_curHistory));
            if (_curHistory.Participants?.Count == 0)
            {
                LoggerUtils.LogError($"乐园[AIPark_CharacterManager] 剧情没有行动者");
                TriggerNextHistory();
                return;
            }

            _historyCompleteStepDict ??= new();
            _historyCompleteStepDict.Clear();
            Debug.Log("乐园111清理步骤");

            foreach (var npcId in _curHistory.Participants)
            {
                Debug.Log("乐园111添加步骤:" + npcId + " 是否完成:false");

                _historyCompleteStepDict[npcId] = false;
            }

            MessageHelper.Broadcast<History>(MessageName.ParkTriggerNextHistory, _curHistory);
            SceneAICharacter2DoThing(_curHistory);
        }


        //剧情演绎end
        public void StartPatrol()
        {
            foreach (var npcBev in _npcDicts.Values)
            {
                npcBev.StartPortal();
                npcBev.IsNpcTalking = false;
                npcBev.RecalculateChapter(3f);
            }
        }

        #region 更新NPC信息
        public void UpdateNpcData(string npcId, float decisionRate)
        {
            var curBev = GetNpc(npcId);
            curBev?.UpdateNpcData(decisionRate / AIGameParkConfig.maxDecisionNum);
        }

        #endregion

        #region 获取状态相关信息
        /// <summary>
        /// 获取NPC当前状态
        /// </summary>
        /// <param name="npcId">NPC ID</param>
        /// <returns>NPC当前状态，如果未找到NPC或状态为空则返回null</returns>
        public AIChapterState GetNpcCurrentState(string npcId)
        {
            if (_npcDicts.TryGetValue(npcId, out AIPark_CharacterBehaviour character))
            {
                // 获取NPC的章节处理器
                var chapterHandler = character.GetChapterHandler();
                if (chapterHandler != null)
                {
                    // 获取状态机
                    var stateMachine = chapterHandler.GetStateMachine();
                    if (stateMachine != null)
                    {
                        // 获取当前状态
                        var currentState = stateMachine.GetCurrentState();
                        LoggerUtils.Log($"[AIPark_CharacterManager] NPC: ID={npcId}, Name={character.GetNpcName()}, 当前状态: {currentState?.GetType().Name ?? "无状态"}");
                        return currentState;
                    }
                }

                LoggerUtils.LogError($"乐园[AIPark_CharacterManager.GetNpcCurrentState] NPC: ID={npcId}, Name={character.GetNpcName()}, 无法获取状态");
                return null;
            }

            LoggerUtils.LogError($"乐园[AIPark_CharacterManager.GetNpcCurrentState] 未找到NPC: ID={npcId}");
            return null;
        }

        /// <summary>
        /// 获取NPC当前状态类型名称
        /// </summary>
        /// <param name="npcId">NPC ID</param>
        /// <returns>NPC当前状态类型名称，如果未找到NPC或状态为空则返回"无状态"</returns>
        public string GetNpcCurrentStateName(string npcId)
        {
            var state = GetNpcCurrentState(npcId);
            return state != null ? state.GetType().Name : "无状态";
        }

        /// <summary>
        /// 查找处于特定状态类型的NPC
        /// </summary>
        /// <typeparam name="T">状态类型，必须是AIChapterState的子类</typeparam>
        /// <returns>找到的状态对象，如果没有找到则返回null</returns>
        public T FindNpcWithState<T>() where T : AIChapterState
        {
            foreach (var npcPair in _npcDicts)
            {
                var character = npcPair.Value;
                var state = character.GetCurrentState();

                if (state != null && state is T typedState)
                {
                    LoggerUtils.Log($"[AIPark_CharacterManager] 找到处于 {typeof(T).Name} 状态的NPC: ID={npcPair.Key}, Name={character.GetNpcName()}");
                    return typedState;
                }
            }

            LoggerUtils.Log($"[AIPark_CharacterManager] 未找到处于 {typeof(T).Name} 状态的NPC");
            return null;
        }

        /// <summary>
        /// 查找处于特定状态类型的NPC，并返回NPC ID和状态对象
        /// </summary>
        /// <typeparam name="T">状态类型，必须是AIChapterState的子类</typeparam>
        /// <param name="npcId">输出参数，找到的NPC ID</param>
        /// <returns>找到的状态对象，如果没有找到则返回null</returns>
        public T FindNpcWithState<T>(out string npcId) where T : AIChapterState
        {
            npcId = null;

            foreach (var npcPair in _npcDicts)
            {
                var character = npcPair.Value;
                var state = character.GetCurrentState();

                if (state != null && state is T typedState)
                {
                    npcId = npcPair.Key;
                    LoggerUtils.Log($"[AIPark_CharacterManager] 找到处于 {typeof(T).Name} 状态的NPC: ID={npcId}, Name={character.GetNpcName()}");
                    return typedState;
                }
            }

            LoggerUtils.Log($"[AIPark_CharacterManager] 未找到处于 {typeof(T).Name} 状态的NPC");
            return null;
        }

        /// <summary>
        /// 查找所有处于特定状态类型的NPC
        /// </summary>
        /// <typeparam name="T">状态类型，必须是AIChapterState的子类</typeparam>
        /// <returns>找到的NPC ID和状态对象的字典</returns>
        public Dictionary<string, T> FindAllNpcsWithState<T>() where T : AIChapterState
        {
            Dictionary<string, T> result = new Dictionary<string, T>();

            foreach (var npcPair in _npcDicts)
            {
                var character = npcPair.Value;
                var state = character.GetCurrentState();

                if (state != null && state is T typedState)
                {
                    result.Add(npcPair.Key, typedState);
                }
            }

            LoggerUtils.Log($"[AIPark_CharacterManager] 找到 {result.Count} 个处于 {typeof(T).Name} 状态的NPC");
            return result;
        }

        public void SetAllNpcEnable(bool isEnable)
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.gameObject.SetActive(isEnable);
            }
        }
        #endregion
        public void ExitInterruptState()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.bIsEnterInterruptState = false;
            }
        }
        public void PauseAllNpcChapter()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.GetChapterHandler()?.PauseChapter();
            }
        }

        public void ResumeAllNpcChapter()
        {
            foreach (var npc in _npcDicts.Values)
            {
                npc.GetChapterHandler()?.ResumeChapter();
            }
        }


        public override void Release()
        {
            var com1 = AvatarController.Inst.SelfController?.gameObject?.GetComponent<AIPark_CharacterBehaviour>();
            if (com1 != null)
            {
                GameObject.DestroyImmediate(com1);
            }
            var com2 = AvatarController.Inst.SelfController?.gameObject?.GetComponent<NpcAvatarTrigger>();
            if (com2 != null)
            {
                GameObject.DestroyImmediate(com2);
            }
            _curHistorys?.Clear();
            _curHistory = null;
            _npcDicts?.Clear();

        }



        public void Init8PosData()
        {
            _8PosData.Clear();
            var data1 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Swing_1, new Vector3(33.55f, 0.874f, 2.834f), new Vector3(0, -90, 0), AIParkConfig.Anim_FeetOut_Sit);
            var data2 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Swing_2, new Vector3(33.55f, 0.874f, 1.88f), new Vector3(0, -90, 0), AIParkConfig.Anim_FeetOut_Sit);
            var data3 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Bench_1, new Vector3(34.198f, -0.18f, -2.025f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Sit);
            var data4 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Bench_2, new Vector3(34.198f, -0.18f, -3f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Sit);
            var data5 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Seesaw_1, new Vector3(31.08f, 0.466f, 6.426f), new Vector3(0, 180, 0), AIParkConfig.Anim_StateLy_Sit);
            var data6 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Seesaw_2, new Vector3(27.417f, 0.466f, 6.426f), new Vector3(0, 180, 0), AIParkConfig.Anim_StateLy_Sit);
            var data7 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Slide_1, new Vector3(28.25f, 0, -1.6f), new Vector3(0, 0, 0), AIParkConfig.Anim_FeetOut_Sit);
            var data8 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Slide_2, new Vector3(26.75f, 0, -1.6f), new Vector3(0, 0, 0), AIParkConfig.Anim_FeetOut_Sit);
            var data9 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Park_Stand_1, new Vector3(33.55f, 0.874f, 2.834f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Stand);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Swing_1, data1);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Swing_2, data2);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Bench_1, data3);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Bench_2, data4);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Seesaw_1, data5);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Seesaw_2, data6);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Slide_1, data7);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Slide_2, data8);
            _8PosData.Add(AIPark_CharacterInitPosType.Park_Stand_1, data9);

            data1 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_1, new Vector3(3.622f, 0.3f, -10.871f), new Vector3(0, 0, 0), AIParkConfig.Anim_FeetOut_Sit);
            data2 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_2, new Vector3(5f, 0.3f, -10.871f), new Vector3(0, 0, 0), AIParkConfig.Anim_FeetOut_Sit);
            data3 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_3, new Vector3(6.44f, 0.3f, -10.871f), new Vector3(0, 0, 0), AIParkConfig.Anim_FeetOut_Sit);
            data4 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Stand_1, new Vector3(7.9f, 0, -5.95f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Stand);
            data5 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Stand_2, new Vector3(1.95f, 0, -6.97f), new Vector3(0, 90, 0), AIParkConfig.Anim_StateLy_Stand);
            data6 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Stand_3, new Vector3(1.95f, 0, -4.05f), new Vector3(0, 90, 0), AIParkConfig.Anim_StateLy_Stand);
            data7 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Bench_1, new Vector3(4.634f, -0.21f, -1.923f), new Vector3(0, 180, 0), AIParkConfig.Anim_StateLy_Sit);
            data8 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Bench_2, new Vector3(5.529f, -0.21f, -1.923f), new Vector3(0, 180, 0), AIParkConfig.Anim_StateLy_Sit);
            data9 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Stage_Stand_4, new Vector3(25.9f, 0, 3.44f), new Vector3(0, 90, 0), AIParkConfig.Anim_StateLy_Stand);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_1, data1);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_2, data2);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_3, data3);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Stand_1, data4);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Stand_2, data5);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Stand_3, data6);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Bench_1, data7);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Bench_2, data8);
            _8PosData.Add(AIPark_CharacterInitPosType.Stage_Stand_4, data9);
            data1 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Bench_1, new Vector3(-0.074f, -0.265f, 3.574f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Sit);
            data2 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Bench_2, new Vector3(-0.074f, -0.265f, 2.744f), new Vector3(0, -90, 0), AIParkConfig.Anim_StateLy_Sit);
            data3 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Stand_1, new Vector3(-3.4f, 0, -0.65f), new Vector3(0, 0, 0), AIParkConfig.Anim_StateLy_Stand);
            data4 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Stand_2, new Vector3(-2.18f, 0, 5.5f), new Vector3(0, 180, 0), AIParkConfig.Anim_StateLy_Stand);
            data5 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Stand_3, new Vector3(-5.17f, 0, 5.62f), new Vector3(0, 174, 0), AIParkConfig.Anim_StateLy_Stand);
            data6 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Horse_1, new Vector3(-7.328f, -0.265f, 4.15f), new Vector3(0, 90, 0), AIParkConfig.Anim_StateLy_Sit);
            data7 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Horse_2, new Vector3(-7.13f, -0.265f, 2.562f), new Vector3(0, 83, 0), AIParkConfig.Anim_StateLy_Sit);
            data8 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Horse_3, new Vector3(-7.3f, -0.265f, 0.868f), new Vector3(0, 100, 0), AIParkConfig.Anim_StateLy_Sit);
            data9 = new AIPark_CharacterInitPosData(AIPark_CharacterInitPosType.Horse_Stand_4, new Vector3(-5.7f, 0, -0.27f), new Vector3(0, 10, 0), AIParkConfig.Anim_StateLy_Stand);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Bench_1, data1);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Bench_2, data2);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Stand_1, data3);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Stand_2, data4);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Stand_3, data5);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Horse_1, data6);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Horse_2, data7);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Horse_3, data8);
            _8PosData.Add(AIPark_CharacterInitPosType.Horse_Stand_4, data9);
        }
        public void NewResetNpc2DiscussPoint(LocationType locationType = LocationType.Park)
        {
            // Init8PosData();//test
            sitEmote2SpeakEmoteDoubleDic ??= new();
            sitEmote2SpeakEmoteDoubleDic.Add(AIParkConfig.Anim_StateLy_Sit, AIParkConfig.Anim_StateLy_Sit_Speak);
            sitEmote2SpeakEmoteDoubleDic.Add(AIParkConfig.Anim_StateLy_Stand, AIParkConfig.Anim_StateLy_Stand_Speak);
            sitEmote2SpeakEmoteDoubleDic.Add(AIParkConfig.Anim_FeetOut_Sit, AIParkConfig.Anim_FeetOut_Sit_Speak);
            _8PosDataList.Clear();
            // locationType = LocationType.Stage;//test
            //有特殊站位
            if (locationType == LocationType.Park)
            {
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Swing_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Swing_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Bench_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Bench_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Seesaw_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Seesaw_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Slide_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Slide_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Park_Stand_1]);
            }
            else if (locationType == LocationType.Stage)
            {
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_3]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Stand_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Stand_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Stand_3]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Bench_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Bench_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Stage_Stand_4]);
            }
            else
            {
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Bench_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Bench_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Stand_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Stand_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Stand_3]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Horse_1]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Horse_2]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Horse_3]);
                _8PosDataList.Add(_8PosData[AIPark_CharacterInitPosType.Horse_Stand_4]);
            }
            var allNpc = GetNpcDic().Values.ToList();
            int cnt = allNpc.Count;
            if (cnt > _8PosDataList.Count)
            {
                LoggerUtils.LogError($"NewResetNpc2DiscussPoint: NPC数量超过可用点数量，跳过剩余NPC");
                return;
            }
            for (int i = 0; i < cnt; i++)
            {
                var npc = allNpc[i];
                _8PosDataList[i].npcId = npc.GetNpcID();
                allNpc[i]._npcKccCtr.Motor.SetPositionAndRotation(_8PosDataList[i].pos, Quaternion.Euler(_8PosDataList[i].rot));
                allNpc[i]._npcKccCtr.Motor.enabled = false;
                // if (allNpc[i]._npcKccCtr.Motor != AvatarController.Inst.SelfController.Motor)
                {
                    allNpc[i]._npcKccCtr.Motor.GetComponent<Rigidbody>().isKinematic = true;
                }
                allNpc[i].playerStateControllerState.EnterState(PlayerState.SingleEmote, _8PosDataList[i].animName, 0);
            }
        }
        public string GetOriginStateWhenDiscuss(string npcId)
        {
            for (int i = 0; i < _8PosDataList.Count; i++)
            {
                if (_8PosDataList[i].npcId != npcId)
                {
                    continue;
                }
                return _8PosDataList[i].animName;
            }
            return "";
        }
        public string GetSpeakStateWhenDiscuss(string npcId, string emoteId)
        {
            if (CanEnterServerStateWhenDiscuss(npcId))
            {
                var npc = GetNpc(npcId);
                // npc.PlayAnim(emoteId);
                if (string.IsNullOrEmpty(emoteId))
                {
                    emoteId = AIParkConfig.Anim_StateLy_Stand_Speak;
                }
                // npc.playerStateControllerState.EnterState(PlayerState.SingleEmote, emoteId, 0);
                return emoteId;
            }
            for (int i = 0; i < _8PosDataList.Count; i++)
            {
                if (_8PosDataList[i].npcId != npcId)
                {
                    continue;
                }
                var npc = GetNpc(_8PosDataList[i].npcId);
                if (sitEmote2SpeakEmoteDoubleDic.TryGetOther(_8PosDataList[i].animName, out string otherEmoteId))
                {
                    return otherEmoteId;
                    // npc.PlayAnim(otherEmoteId);
                    // npc.playerStateControllerState.EnterState(PlayerState.SingleEmote, otherEmoteId, 0);
                }
            }
            return "";
        }
        /// <summary>
        /// 只有AIParkConfig.Anim_StateLy_Stand可以进入服务器推的emote状态
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        public bool CanEnterServerStateWhenDiscuss(string npcId)
        {
            for (int i = 0; i < _8PosDataList.Count; i++)
            {
                if (_8PosDataList[i].npcId != npcId)
                {
                    continue;
                }
                if (_8PosDataList[i].animName == AIParkConfig.Anim_StateLy_Stand)
                {
                    return true;
                }
            }
            return false;
        }

        public void ExitAllFromDiscussPoint()
        {
            var allNpc = GetNpcDic().Values.ToList();
            for (int i = 0; i < _8PosDataList.Count; i++)
            {
                var npc = GetNpc(_8PosDataList[i].npcId);
                if (npc == null)
                {
                    continue;
                }
                npc.playerStateControllerState.ExitState(PlayerState.SingleEmote);
                npc._npcAnimController.SetPlayerAniState(PlayerAniState.Idle, true);
                // npc.PlayAnim(AIParkConfig.Anim_StateLy_Stand);
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(npc.transform.position, 0.2f, 1.8f);

                npc.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
                npc.StopMoving();
                npc._npcKccCtr.Motor.enabled = true;
                if (npc._npcKccCtr.Motor != AvatarController.Inst.SelfController.Motor)
                {
                    npc.GetComponent<KinematicCharacterController>().Motor.GetComponent<Rigidbody>().isKinematic = true;
                }
                npc.SetPositionAndRotation(_npcTransData.pos, Quaternion.Euler(_npcTransData.rot));
            }
        }

        public void RevertChatNpcToLastChapter(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }
            var npc = GetNpc(npcId);
            if (npc == null)
            {
                return;
            }
            npc.CheckReEnterLastChapter(false);
        }
        public void RevertChatNpcToCurChapter()
        {
            var npcId = AIBuddyAvatarController.Inst.CurrentInteractNpcID;
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }
            var npc = GetNpc(npcId);
            if (npc == null)
            {
                return;
            }
            npc.CheckReEnterCurChapter(false);
        }
        
    }

    #region DataStruct

    /// <summary>
    /// 后端返回的结构
    /// </summary>
    public class ParkGameDataRsp
    {
        public string sessionId;
        public List<AIGameAmusementParkSyncReply.Types.StoryEvent> events;

        public AIGameAmusementParkSyncReply.Types.StoryEvent summaryEvent;
        public List<History> actions = new();

        public AICommonGameConfig aICommonGameConfig;

        public string Toast; //飘字
        public string Summary = "";//总结

        public List<string> EventSummaryList = new List<string>(); // 根据下标对应每个事件的结局

        public List<History> NetHistoryList = new();//客户端维护一份完整的后台返回的history列表

        private History _curHistory;
        public History curHistory
        {
            get
            {
                return _curHistory;
            }
            set
            {
                _curHistory = value;
            }
        } //当前演绎的history

        public bool isPgcEnter = false;

        public List<string> GetAllNpcs()
        {
            List<string> allNpcs = new();

            if (isPgcEnter)
            {
                allNpcs.Add(((int)ParkNpcRoleType.self).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Elise).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Casper).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Pio).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Teddy).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Vivien).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Rowland).ToString());
                allNpcs.Add(((int)ParkNpcRoleType.Tilia).ToString());
            }
            else
            {
                allNpcs.Add(((int)ParkNpcRoleType.self).ToString());
                aICommonGameConfig.npcData.ForEach(npc => allNpcs.Add(npc.id));
            }
            return allNpcs;
        }

        public void ParseNewHistoryList(List<History> historyList)
        {
            //测试用
            // actions?.Clear();
            // actions.AddRange(historyList);
            // AIPark_CharacterManager.Inst.ReInitSceneAICharacter(actions);
        }

        public void AddNewHistory(History history)
        {
            //需要维护一份完整的history列表
            NetHistoryList.Add(history);
            //
            List<History> histories = new();
            histories.Add(history);
            AIPark_CharacterManager.Inst.AddSceneAICharacter(histories);
        }
        //过去某个npc过去几条信息
        public History GetParticipantsLastHistory(string participants)
        {
            //默认curHistory不应该为空
            int speakCount = 2;  //2条讲话条目
            int idx = NetHistoryList.FindIndex(h => h == curHistory);
            if (idx == -1)
            {
                LoggerUtils.LogError("乐园GetParticipantsLastHistory curHistory is null");
                return null;
            }
            History lastHistory = new();
            lastHistory.Participants.Add(participants);
            int addCnt = 0;
            for (int i = idx; i >= 0; i--)
            {
                if (NetHistoryList[i].Participants.Contains(participants) && addCnt < speakCount)
                {
                    lastHistory.Location = NetHistoryList[i].Location;
                    lastHistory.Action = NetHistoryList[i].Action;
                    lastHistory.Quotes.AddRange(NetHistoryList[i].Quotes);
                    addCnt++;
                    if (addCnt == speakCount)
                    {
                        break;
                    }
                }
            }
            return lastHistory;
        }
        public void ParsePbData(string json)
        {
            var jObject = JObject.Parse(json);
            events?.Clear();
            if (jObject["events"] != null && (jObject["events"] is JArray))
            {
                JArray jArray = jObject["events"] as JArray;
                foreach (var ev in jArray)
                {
                    try
                    {
                        var cleanedEv = Basic.Utils.JObject2PbUtil.CleanUnknownFields(ev as JObject, new AIGameAmusementParkSyncReply.Types.StoryEvent());
                        var ev2 = AIGameAmusementParkSyncReply.Types.StoryEvent.Parser.ParseJson(cleanedEv.ToString());
                        events.Add(ev2);
                    }
                    catch (System.Exception ex)
                    {
                        LoggerUtils.LogError($"ev解析事件数据失败: {ex.Message}");
                        LoggerUtils.LogError($"ev原始JSON: {ev}");
                    }
                }
            }
            actions?.Clear();
            if (jObject["actions"] != null && (jObject["actions"] is JArray))
            {
                JArray jArray = jObject["actions"] as JArray;
                foreach (var ev in jArray)
                {
                    try
                    {
                        var cleanedEv = Basic.Utils.JObject2PbUtil.CleanUnknownFields(ev as JObject, new History());
                        var ev2 = History.Parser.ParseJson(cleanedEv.ToString());
                        actions.Add(ev2);
                    }
                    catch (System.Exception ex)
                    {
                        LoggerUtils.LogError($"ac解析事件数据失败: {ex.Message}");
                        LoggerUtils.LogError($"ac原始JSON: {ev}");
                    }
                }
            }
            CompatibleStage();
        }

        /// <summary>
        /// 因为舞台的action有问题 需要兼容  暂时使用这个方法(替换成舞台旁白的长椅动作)
        /// </summary>
        public void CompatibleStage()
        {
            for (int i = 0; i < actions.Count; i++){
                if(actions[i].Location == (int)LocationType.Stage && actions[i].Action == (int)ActionType.PerformOnStage){
                    actions[i].Action = (int)ActionType.SitOnBench;
                }
            }
        }

    }
    /// <summary>
    /// 自定义数据
    /// </summary>
    public class ParkCustomData
    {
        public int SceneIndex; //场景索引 1 2 3
        public bool isSummaryScene = false; //是否是总结场景
    }

    public class ParkEvents
    {
        public ParkEventType eventType;
        public List<ParkEventOption> eventOpts;
        public List<ParkEventDiscuss> eventDiscuss;
        public string eventStory;
    }
    public class ParkEventOption
    {
        public string eventName; //事件名称
        public string eventDesc; //事件描述
        public int eventDuration; //事件持续时间
    }
    public class ParkEventDiscuss
    {
        public string speaker; //说话人
        public string content; //说话内容
    }

    public enum ParkEventType
    {
        None = -1,
        Normal, //普通事件
        Choose, //选择事件
    }

    public class ParkFirstAction
    {
        public List<string> participants;
        public string location;
        public string action;
        public List<ParkEventDiscuss> quotes;
    }
    /// <summary>
    /// 角色剧本数据
    /// </summary>
    [Serializable]
    public class ParkCharacterScriptData
    {
        public string npcId;
        public string name;
        public int npcType;
        public int npcRole; // PGC角色
        public string avatarJson;
    }

    public class ParkNpcTransData
    {
        public Vector3 pos = new Vector3();
        public Vector3 rot = new Vector3();
    }

    public class RecordNpcHistory
    {
        public string npcId;
        public History lastHistory;
        public History curHistory;
        public void InsertHistory(History history)
        {
            if (curHistory != null)
            {
                lastHistory = curHistory;
            }
            curHistory = history;
        }
    }

    public class AIPark_CharacterInitPosData
    {
        public AIPark_CharacterInitPosType type;
        public Vector3 pos;
        public Vector3 rot;
        public string animName;
        public string npcId;
        public AIPark_CharacterInitPosData(AIPark_CharacterInitPosType type, Vector3 pos, Vector3 rot, string animName)
        {
            this.type = type;
            this.pos = pos;
            this.rot = rot;
            this.animName = animName;
            this.npcId = "";
        }
    }
    public enum AIPark_CharacterInitPosType
    {
        Park_Swing_1,
        Park_Swing_2,
        Park_Bench_1,
        Park_Bench_2,
        Park_Seesaw_1,
        Park_Seesaw_2,
        Park_Slide_1,
        Park_Slide_2,
        Park_Stand_1,


        Stage_1,
        Stage_2,
        Stage_3,
        Stage_Stand_1,
        Stage_Stand_2,
        Stage_Stand_3,
        Stage_Stand_4,
        Stage_Bench_1,
        Stage_Bench_2,

        Horse_Bench_1,
        Horse_Bench_2,
        Horse_Stand_1,
        Horse_Stand_2,
        Horse_Stand_3,
        Horse_Stand_4,
        Horse_Horse_1,
        Horse_Horse_2,
        Horse_Horse_3,
    }
    #endregion
}
