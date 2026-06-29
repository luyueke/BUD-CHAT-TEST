using System;
using System.Collections.Generic;
using AIGame.Prop;
using Game.Avatar;
using UnityEngine;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;

namespace Game.Props.PropsManagers
{
    public class AIHospital_CharacterManager : GlobalInstance<AIHospital_CharacterManager>
    {
        // NPC字典，存储NPC ID与NPC行为控制器的映射
        private Dictionary<string, AIHospital_CharacterBehaviour> _npcDicts = new Dictionary<string, AIHospital_CharacterBehaviour>();
        // 已使用的角色类型集合，用于确保随机不重复
        private HashSet<HospitalNpcRoleType> _usedRoleTypes = new HashSet<HospitalNpcRoleType>();
        
        /// <summary>
        /// 注册NPC
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <param name="character">NPC行为控制器</param>
        public void RegisterNpc(string id, AIHospital_CharacterBehaviour character)
        {
            _npcDicts[id] = character;
            LoggerUtils.Log($"[AIHospital_CharacterManager] 注册NPC: ID={id}, Name={character.GetNpcName()}");
        }

        public Dictionary<string,AIHospital_CharacterBehaviour> GetNpcDic()
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
            if (_npcDicts.TryGetValue(id, out AIHospital_CharacterBehaviour character))
            {
                return character.transform.position;
            }
            
            LoggerUtils.LogError($"[AIHospital_CharacterManager] 未找到NPC: ID={id}");
            return Vector3.zero;
        }
        
        /// <summary>
        /// 获取NPC
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <returns>NPC行为控制器</returns>
        public AIHospital_CharacterBehaviour GetNpc(string id)
        {
            if (_npcDicts.TryGetValue(id, out AIHospital_CharacterBehaviour character))
            {
                return character;
            }
            
            LoggerUtils.LogError($"[AIHospital_CharacterManager] 未找到NPC: ID={id}");
            return null;
        }
        
        public AIHospital_CharacterBehaviour GetNpc(HospitalNpcRoleType roleType)
        {
            foreach (var npcBev in _npcDicts.Values)
            {
                if (npcBev.GetNpcRoleType() == roleType)
                    return npcBev;
            }

            LoggerUtils.LogError($"[AIHospital_CharacterManager] 未找到NPC: roleType={roleType}");
            return null;
        }

        /// <summary>
        /// 获取不重复的随机角色类型
        /// </summary>
        /// <returns>未使用过的随机角色类型</returns>
        private HospitalNpcTransData GetRandomSpawnPoint()
        {
            //目标元数据
            var spawnPoint = AIHospital_CharacterUtils.Inst.UGCGameSpawnPoint;
            
            // 创建可用出生点列表（排除已使用的）
            List<HospitalNpcTransData> availableSpawnPoints = new List<HospitalNpcTransData>();
            
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
                LoggerUtils.LogError("[AIHospital_CharacterManager] 没有可用的不重复出生点，将使用随机出生点");
                return spawnPoint[UnityEngine.Random.Range(0, spawnPoint.Count)];
            }
            
            // 返回一个随机的可用出生点
            HospitalNpcTransData resultType = availableSpawnPoints[UnityEngine.Random.Range(0, availableSpawnPoints.Count)];
            LoggerUtils.Log($"[AIHospital_CharacterManager] 选择出生点: pos={resultType.pos}");
            
            return resultType;
        }

        /// <summary>
        /// 初始化场景中的AI角色
        /// </summary>
        /// <param name="characterScriptDatas">角色剧本数据列表</param>
        public void InitSceneAICharacter(List<HospitalCharacterScriptData> characterScriptDatas, bool isPgcEnter = true)
        {
            _npcDicts.Clear();
            _usedRoleTypes.Clear();
            
            foreach (var scriptData in characterScriptDatas)
            {
                var npcKcc = AIBuddyAvatarController.Inst.CreateAIGameAINpc(scriptData.name, scriptData.npcId, scriptData.avatarJson);

                scriptData.npcId = scriptData.npcId;
                scriptData.plan.ForEach(x =>
                {
                    if(!string.IsNullOrEmpty(x.talkTo))
                        x.talkTo = x.talkTo;
                });
                
                // 初始位置
                HospitalNpcTransData SpawnPointData = new HospitalNpcTransData();
                if (isPgcEnter)
                {
                    SpawnPointData = AIHospital_CharacterUtils.Inst.GetSpawnPointByRoleType((HospitalNpcRoleType)scriptData.npcRole);
                }
                else
                {
                    SpawnPointData = GetRandomSpawnPoint();
                }

                var firstPos = SpawnPointData.pos;
                var firstRot = SpawnPointData.rot;
                var npcBev = npcKcc.gameObject.AddComponent<AIHospital_CharacterBehaviour>();
                npcBev.gameObject.AddComponent<NpcAvatarTrigger>();
                // npcKcc.Motor.SetCapsuleCollisionsActivation(false);
                npcKcc.Motor.SetPositionAndRotation(firstPos, Quaternion.Euler(firstRot));

                // 初始化NPC
                npcBev.InitData(scriptData);
                
                // 添加到列表
                RegisterNpc(scriptData.npcId, npcBev);
                
                LoggerUtils.Log($"[AIHospital_CharacterManager] 创建NPC: ID={scriptData.npcId}, Name={scriptData.name}");
            }
            
            LoggerUtils.Log($"[AIHospital_CharacterManager] 初始化完成，NPC数量: {_npcDicts.Count}");
        }

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
            curBev?.UpdateNpcData(decisionRate/AIGameHospitalConfig.maxDecisionNum);
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
            if (_npcDicts.TryGetValue(npcId, out AIHospital_CharacterBehaviour character))
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
                        LoggerUtils.Log($"[AIHospital_CharacterManager] NPC: ID={npcId}, Name={character.GetNpcName()}, 当前状态: {currentState?.GetType().Name ?? "无状态"}");
                        return currentState;
                    }
                }
                
                LoggerUtils.LogError($"[AIHospital_CharacterManager] NPC: ID={npcId}, Name={character.GetNpcName()}, 无法获取状态");
                return null;
            }
            
            LoggerUtils.LogError($"[AIHospital_CharacterManager] 未找到NPC: ID={npcId}");
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
                    LoggerUtils.Log($"[AIHospital_CharacterManager] 找到处于 {typeof(T).Name} 状态的NPC: ID={npcPair.Key}, Name={character.GetNpcName()}");
                    return typedState;
                }
            }
            
            LoggerUtils.Log($"[AIHospital_CharacterManager] 未找到处于 {typeof(T).Name} 状态的NPC");
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
                    LoggerUtils.Log($"[AIHospital_CharacterManager] 找到处于 {typeof(T).Name} 状态的NPC: ID={npcId}, Name={character.GetNpcName()}");
                    return typedState;
                }
            }
            
            LoggerUtils.Log($"[AIHospital_CharacterManager] 未找到处于 {typeof(T).Name} 状态的NPC");
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
            
            LoggerUtils.Log($"[AIHospital_CharacterManager] 找到 {result.Count} 个处于 {typeof(T).Name} 状态的NPC");
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
    }
    
    #region DataStruct

    /// <summary>
    /// 后端返回的结构
    /// </summary>
    public class HospitalGameDataRsp
    {
        public List<HospitalCharacterScriptData> hospitalScript;
    }
    
    /// <summary>
    /// 角色剧本数据
    /// </summary>
    [Serializable]
    public class HospitalCharacterScriptData
    {
        public string npcId;
        public string name;
        public int npcType;
        public int npcRole; // PGC角色
        public string avatarJson;
        public List<AIHospital_ChapterData> plan;
    }

    public class HospitalNpcTransData
    {
        public Vector3 pos = new Vector3();
        public Vector3 rot = new Vector3();
    }
    #endregion
}
