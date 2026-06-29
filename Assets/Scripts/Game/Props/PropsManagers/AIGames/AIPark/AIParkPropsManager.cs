using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData.BaseInfo;
using Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIParkPropsManager : GlobalInstance<AIParkPropsManager>
    {

        private List<NodeBaseBehaviour> _sceneNodeBevs = new List<NodeBaseBehaviour>();
        private List<NodeBaseBehaviour> _canClickNodeBevs = new List<NodeBaseBehaviour>();
        private List<ActionType> _availableActionTypes = new List<ActionType>();
        public ActionType currentMyActionType = ActionType.Idle;

        public int selfUsePropId = -1;
        private bool _bBanDoCustomAction = false;
        private bool bGuideSwinging = false; //是否是引导荡秋千

        public bool bBanTillaAction = false; //是否禁止蒂莉动作(事件引导禁止tilia动作)
        public void SetBanDoCustomAction(bool bBan)
        {
            _bBanDoCustomAction = bBan;
        }
        public bool GetBanDoCustomAction()
        {
            return _bBanDoCustomAction;
        }

        public void SetGuideSwinging(bool bSwinging)
        {
            bGuideSwinging = bSwinging;
        }
        public void InitData()
        {
            _sceneNodeBevs.Clear();
            _canClickNodeBevs.Clear();
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_SeesawMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_SlideMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_SwingMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_TrojanhorseMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_SeesawChairMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_SwingChairMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_TrojanhorseChairMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_BenchMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_BenchChairMgr>().GetBehaviours());
            _sceneNodeBevs.AddRange(GlobalNodeManager.Inst.Get<AIPark_StageManager>().GetBehaviours());
        }
        /// <summary>
        /// 到达目标点发现道具被占用/寻路超时 检查是否进行其他自定义交互
        /// </summary>
        public void CheckDoCustomActionWhenPropBePlaced(string roleType, ActionType actionType)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(roleType);
            if (npc == null)
            {
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                return;
            }
            ExitAction(npcBev, actionType);
            if (CheckDoCustomAction(npcBev.GetNpcID(), 100))
            {
            }
            else
            {
                //
                LoggerUtils.LogError("地图道具没有可用的行为 尽可能加入一些动作避免进入到这里来");
                npcBev.GetChapterHandler().ForceChangeState(ActionType.Idle);
            }
        }

        public int GetAvailableNodeCount()
        {
            _canClickNodeBevs.Clear();
            _sceneNodeBevs.ForEach(x =>
            {
                if (x.IsCanClick)
                {
                    _canClickNodeBevs.Add(x);
                }
            });
            return _canClickNodeBevs.Count;
        }

        public void SetAvailableActionTypes(List<ActionType> actionTypes)
        {
            _availableActionTypes = actionTypes;
        }

        public bool IsAvailableActionType(List<ActionType> actionTypes, NodeBaseBehaviour nodeBaseBehaviour)
        {
            if (actionTypes == null)
            {
                return false;
            }
            foreach (var actionType in actionTypes)
            {
                if (actionType != ActionType.None && actionType == GetActionType(nodeBaseBehaviour))
                {
                    return true;
                }
            }
            return false;
        }

        public NodeBaseBehaviour GetRandomAvailableNodeBehaviour()
        {
            //需要修改随机的逻辑 先按照取出行为可用
            _canClickNodeBevs.Clear();
            _sceneNodeBevs.ForEach(x =>
            {
                if (x.IsCanClick && IsAvailableActionType(_availableActionTypes, x))
                {
                    if (bGuideSwinging)
                    {
                        if (x is AIPark_SwingBehaviour)
                        {
                            return;
                        }
                    }
                    _canClickNodeBevs.Add(x);
                }
            });



            // 检查是否有可用的节点
            if (_canClickNodeBevs.Count == 0)
            {
                LoggerUtils.Log("GetRandomAvailableNodeBehaviour: 没有可用的可点击节点");
                return null;
            }

            // 随机选择一个可用的节点
            int randomIndex = UnityEngine.Random.Range(0, _canClickNodeBevs.Count);
            return _canClickNodeBevs[randomIndex];
        }

        /// <summary>
        /// 玩家对话后 检查是否进行自定义交互
        /// </summary>
        /// <param name="nodeBaseBehaviour"></param>
        public bool CheckDoCustomAction(string roleId, int percent = 20)
        {
            if (_bBanDoCustomAction)
            {
                return false;
            }
            if (bGuideSwinging && roleId == ((int)ParkNpcRoleType.Tilia).ToString())
            {
                return false;
            }
            var npc = AIPark_CharacterManager.Inst.GetNpc(roleId);
            var currentState = npc.GetCurrentState();
            if (currentState != null && !(currentState is IdleState))
            {
                //非idle状态 不去改变行为
                return false;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            var random = UnityEngine.Random.Range(0, 100);
            if (random <= percent)
            {
                var nodeBaseBehaviour = GetRandomAvailableNodeBehaviour();
                if (nodeBaseBehaviour != null)
                {
                    var actionType = GetActionType(nodeBaseBehaviour);
                    if (actionType == ActionType.None)
                    {
                        LoggerUtils.LogError("乐园自定义交互CheckDoCustomAction: 节点类型为空");
                        return false;
                    }
                    LoggerUtils.Log("乐园自定义交互CheckDoCustomAction:" + npcBev._curNpcName + " 进入节点:" + actionType);
                    EnterAction(npcBev, actionType);
                    MessageHelper.Broadcast(MessageName.OnSyncNpcLocation, npcBev.GetNpcID(), npcBev.GetChapterLocation(), npcBev.GetChapterLocation());
                    return true;
                }
            }
            return false;
        }

        public void SelfEnterProp(NodeBaseBehaviour nodeBaseBehaviour)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());
            if (npc == null)
            {
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                return;
            }
            selfUsePropId = nodeBaseBehaviour?.entity?.Id ?? -1;
            currentMyActionType = GetActionType(nodeBaseBehaviour);
            if (nodeBaseBehaviour is AIPark_SeesawBehaviour seesaw)
            {
                EnterAction(npcBev, ActionType.SeeSaw, nodeBaseBehaviour, true);
            }
            else if (nodeBaseBehaviour is AIPark_SlideBehaviour slide)
            {
                EnterAction(npcBev, ActionType.SlideSlides, nodeBaseBehaviour, true);
            }
            else if (nodeBaseBehaviour is AIPark_SwingBehaviour swing)
            {
                EnterAction(npcBev, ActionType.Swinging, nodeBaseBehaviour, true);
            }
            else if (nodeBaseBehaviour is AIPark_TrojanhorseBehaviour trojanhorse)
            {
                EnterAction(npcBev, ActionType.TrojanHorse, nodeBaseBehaviour, true);
            }
            else if (nodeBaseBehaviour is AIPark_BenchBehaviour benchChair)
            {
                EnterAction(npcBev, ActionType.SitOnBench, nodeBaseBehaviour, true);
            }
            else if (nodeBaseBehaviour is AIPark_StageBehaviour stage)
            {
                var panel = UIAgentManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.AIParkGameStagePanel);
                if (!panel)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.AIParkGameStagePanel, WindowId.GuestWindow, stage, true);
                }
                //EnterAction(npcBev, ActionType.PerformOnStage, nodeBaseBehaviour, false);
                //先用老方案进入
                //stage.OnTouchClick();
            }
            else if (nodeBaseBehaviour is AIPark_InstrumentBehaviour instrument)
            {
                //先用老方案进入
                instrument._playerStateController = npc.playerStateControllerState;
                instrument.OnTouchClick();
            }
        }
       
        public void SelfEnterIdle()
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());
            if (npc == null)
            {
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                return;
            }
            selfUsePropId = -1;
            currentMyActionType = ActionType.Idle;
            if (!(npcBev.GetCurrentState() is IdleState))
            {
                npcBev.GetChapterHandler().ForceChangeState(ActionType.Idle);
                // npcBev.GetChapterHandler().CheckChangeState(ActionType.Idle,null,false);
            }
        }

        public ActionType GetActionType(AIChapterState state)
        {
            if (state is SeeSawState)
            {
                return ActionType.SeeSaw;
            }
            else if (state is SlideSlidesState)
            {
                return ActionType.SlideSlides;
            }
            else if (state is SwingingState)
            {
                return ActionType.Swinging;
            }
            else if (state is TrojanHorseState)
            {
                return ActionType.TrojanHorse;
            }
            else if (state is SitOnBenchState)
            {
                return ActionType.SitOnBench;
            }
            return ActionType.None;
        }

        public ActionType GetActionType(NodeBaseBehaviour nodeBaseBehaviour)
        {
            if (nodeBaseBehaviour is AIPark_SeesawBehaviour seesaw)
            {
                return ActionType.SeeSaw;
            }
            else if (nodeBaseBehaviour is AIPark_SlideBehaviour slide)
            {
                return ActionType.SlideSlides;
            }
            else if (nodeBaseBehaviour is AIPark_SwingBehaviour swing)
            {
                return ActionType.Swinging;
            }
            else if (nodeBaseBehaviour is AIPark_TrojanhorseBehaviour trojanhorse)
            {
                return ActionType.TrojanHorse;
            }
            else if (nodeBaseBehaviour is AIPark_StageBehaviour stage)
            {
                return ActionType.PerformOnStage;
            }
            else if (nodeBaseBehaviour is AIPark_BenchBehaviour benchChair)
            {
                return ActionType.SitOnBench;
            }
            return ActionType.None;
        }
        public void EnterAction(AIPark_CharacterBehaviour character, NodeBaseBehaviour nodeBaseBehaviour)
        {
            var actionType = GetActionType(nodeBaseBehaviour);
            if (actionType == ActionType.None)
            {
                return;
            }
            EnterAction(character, actionType, nodeBaseBehaviour);
        }

        public void EnterAction(ParkNpcRoleType roleType, ActionType actionType, NodeBaseBehaviour nodeBaseBehaviour = null, bool byServer = true)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(((int)roleType).ToString());
            if (npc == null)
            {
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                return;
            }
            EnterAction(npcBev, actionType, nodeBaseBehaviour, byServer);
        }
        public void EnterAction(AIPark_CharacterBehaviour character, ActionType actionType, NodeBaseBehaviour nodeBaseBehaviour = null, bool byServer = true)
        {

            if (actionType == ActionType.SeeSaw)
            {
                if (character.GetCurrentState() is SeeSawState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.SeeSaw, nodeBaseBehaviour, byServer);
            }
            else if (actionType == ActionType.SitOnBench)
            {
                if (character.GetCurrentState() is SitOnBenchState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.SitOnBench, nodeBaseBehaviour, byServer);
            }
            else if (actionType == ActionType.SlideSlides)
            {
                if (character.GetCurrentState() is SlideSlidesState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.SlideSlides, nodeBaseBehaviour, byServer);
            }
            else if (actionType == ActionType.Swinging)
            {
                if (character.GetCurrentState() is SwingingState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.Swinging, nodeBaseBehaviour, byServer);
            }
            else if (actionType == ActionType.TrojanHorse)
            {
                if (character.GetCurrentState() is TrojanHorseState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.TrojanHorse, nodeBaseBehaviour, byServer);
            }
            else if (actionType == ActionType.PerformOnStage)
            {
                if (character.GetCurrentState() is PerformOnStageState)
                {
                    return;
                }
                character.GetChapterHandler()?.CheckChangeState(ActionType.PerformOnStage, nodeBaseBehaviour, byServer);
            }
        }

        public void ExitAction(string roleId, ActionType actionType)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(roleId);
            if (npc == null)
            {
                Debug.LogError("11乐园ExitAction: 未找到npc:" + roleId);
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                Debug.LogError("22乐园ExitAction: 未找到npc:" + roleId);
                return;
            }
            ExitAction(npcBev, actionType);
        }

        public void ExitAction(string roleId)
        {
            var npc = AIPark_CharacterManager.Inst.GetNpc(roleId);
            if (npc == null)
            {
                return;
            }
            var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
            if (npcBev == null)
            {
                return;
            }
            var actionType = GetActionType(npcBev.GetCurrentState());
            ExitAction(npcBev, actionType);
        }

        public void ExitAction(AIPark_CharacterBehaviour character, ActionType actionType)
        {
            // Debug.LogError(character.transform.name + "退出动作:" + actionType);
            if (actionType == ActionType.SeeSaw)
            {
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
            else if (actionType == ActionType.SlideSlides)
            {
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
            else if (actionType == ActionType.Swinging)
            {
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
            else if (actionType == ActionType.TrojanHorse)
            {
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
            else if(actionType == ActionType.PerformOnStage){
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
            else
            {
                character.GetChapterHandler()?.ForceChangeState(ActionType.Idle);
            }
        }

        public void ExitGameFreeAllProps() {
            var npcDic = AIPark_CharacterManager.Inst.GetNpcDic();
            foreach (var npc in npcDic)
            {
                Debug.LogError("ExitGameFreeAllProps:" + npc.Value.GetNpcID());
                ExitAction(npc.Value.GetNpcID());
            }
        }

        public void InitPhoto(AICommonGameConfig gameconfig)
        {
            var photoMgr = GlobalNodeManager.Inst.Get<AIPark_PhotoManager>();
            var photos = photoMgr.GetBevs();
            GameObject photo = GameObject.Find("photo");
            // 1. 按location分组
            Dictionary<int, string> locationGroups = new Dictionary<int, string>();
            var stageUrls = gameconfig.stage.backgroundUrls;
            for(int i=0;i<stageUrls.Count;i++)
            {
                locationGroups[i] = stageUrls[i];
            }
           
            var woodenhorseUrls = gameconfig.billboard.woodenhorseUrls;
            for (int i = 0; i < woodenhorseUrls.Count; i++)
            {
                locationGroups[i+4] = woodenhorseUrls[i];
            }
            var parkUrls = gameconfig.billboard.parkUrls;
            for (int i = 0; i < parkUrls.Count; i++)
            {
                locationGroups[i + 8] = parkUrls[i];
            }
            var fountainUrls = gameconfig.billboard.fountainUrls;
            for (int i = 0; i < fountainUrls.Count; i++)
            {
                locationGroups[i + 9] = fountainUrls[i];
            }
            //foreach (var data in datas)
            //{
            //    if (!locationGroups.ContainsKey(data.location))
            //    {
            //        locationGroups[data.location] = data.urls;
            //    }
            //    //locationGroups[data.location].Add(data.urls);
            //}

            foreach (var item in photos)
            {
                item.gameObject.SetActive(false);
            }
            // 2. 遍历每个location组，找到对应的ShotPhotoBehaviour并初始化
            foreach (var locationGroup in locationGroups)
            {
                int location = locationGroup.Key;
                var photoDataList = locationGroup.Value;

                // 找到该location下的所有照片行为组件
                var locationPhotos = photos.Where(p => p.location == location).ToList();

                var photoBev = locationPhotos[0];
                var url = photoDataList;
                var pgcPhoto = photo.transform.Find($"photo{location}");
                if (photoBev != null && !string.IsNullOrEmpty(url))
                {
                    pgcPhoto?.gameObject.SetActive(false);
                    photoBev.gameObject.SetActive(true);
                    photoBev.Load(url);
                }
                else
                {
                    pgcPhoto?.gameObject.SetActive(true);
                    photoBev.gameObject.SetActive(false);
                }

            }
        }
    }

    #region 通用的泛型管理器基类
    public abstract class BaseTypedNodeManager<T> : BaseNodeManager where T : AIPropBaseBehaviour
    {
        protected Dictionary<int, T> _behaviourDict = new();

        public List<T> GetBehaviours()
        {
            List<T> list = new List<T>();
            entities.ToList().ForEach(x =>
            {
                if (x is T behaviour)
                {
                    list.Add(behaviour);
                    _behaviourDict[behaviour.Uid] = behaviour;
                }
            });
            return list;
        }

        public T GetBehaviour()
        {
            foreach (var x in entities)
            {
                if (x is T behaviour)
                {
                    return x as T;
                }
            }
            return null;
        }

        public T GetBehaviourByUid(int uid)
        {
            return _behaviourDict.TryGetValue(uid, out var behaviour) ? behaviour : null;
        }

        // 通用的获取可用对象方法
        public T GetAvailableBehaviour(Func<T, bool> condition)
        {
            List<T> list = new List<T>();
            entities.ForEach(x =>
            {
                if (x is T behaviour && condition(behaviour))
                {
                    list.Add(behaviour);
                }
            });
            if (list.Count > 0)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }
            return null;
        }
    }
    #endregion


    #region 具体的道具管理类实现
    [NodeBehaviourAttribute(typeof(AIPark_SeesawBehaviour))]
    public class AIPark_SeesawMgr : BaseTypedNodeManager<AIPark_SeesawBehaviour>
    {

        public AIPark_SeesawBehaviour GetEmptySeesaw()
        {
            foreach (var bev in entities)
            {
                var seesawBev = bev as AIPark_SeesawBehaviour;
                if (seesawBev.IsCanClick)
                {
                    return seesawBev;
                }
            }
            return null;
        }
    }

    [NodeBehaviourAttribute(typeof(AIPark_SlideBehaviour))]
    public class AIPark_SlideMgr : BaseTypedNodeManager<AIPark_SlideBehaviour>
    {

    }

    [NodeBehaviourAttribute(typeof(AIPark_SwingBehaviour))]
    public class AIPark_SwingMgr : BaseTypedNodeManager<AIPark_SwingBehaviour>
    {

    }

    [NodeBehaviourAttribute(typeof(AIPark_TrojanhorseBehaviour))]
    public class AIPark_TrojanhorseMgr : BaseTypedNodeManager<AIPark_TrojanhorseBehaviour>
    {

    }

    [NodeBehaviourAttribute(typeof(AIPark_SeesawChairBehaviour))]
    public class AIPark_SeesawChairMgr : BaseTypedNodeManager<AIPark_SeesawChairBehaviour>
    {
    }

    [NodeBehaviourAttribute(typeof(AIPark_BenchBehaviour))]
    public class AIPark_BenchMgr : BaseTypedNodeManager<AIPark_BenchBehaviour>
    {
        Dictionary<LocationType, List<int>> _locationType2Id = new(){  //长椅对应位置id
            {LocationType.Park, new List<int>(){67,70,91}},
            {LocationType.Stage, new List<int>(){73}},
            {LocationType.Fountain, new List<int>(){79,82,85,88}},
            {LocationType.TrojanHorse, new List<int>(){76}},
        };
        public AIPark_BenchBehaviour GetAvailableBehaviour(LocationType locationType)
        {
            if (!_locationType2Id.ContainsKey(locationType))
            {
                return null;
            }
            var idList = _locationType2Id[locationType];
            List<AIPark_BenchBehaviour> list = new List<AIPark_BenchBehaviour>();
            entities.ForEach(x =>
            {
                //寻找对应区域的长椅
                if (x is AIPark_BenchBehaviour behaviour && idList.Contains(behaviour.Uid))
                {
                    if (behaviour.IsCanClick)
                    {

                        list.Add(behaviour);
                    }
                }
            });
            if (list.Count > 0)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }
            return null;
        }
    }
    [NodeBehaviourAttribute(typeof(AIPark_StageBehaviour))]
    public class AIPark_StageMgr : BaseTypedNodeManager<AIPark_StageBehaviour>
    {

    }

    [NodeBehaviourAttribute(typeof(AIPark_BenchChairBehaviour))]
    public class AIPark_BenchChairMgr : BaseTypedNodeManager<AIPark_BenchChairBehaviour>
    {
    }

    [NodeBehaviourAttribute(typeof(AIPark_SwingChairBehaviour))]
    public class AIPark_SwingChairMgr : BaseTypedNodeManager<AIPark_SwingChairBehaviour>
    {
    }

    [NodeBehaviourAttribute(typeof(AIPark_TrojanhorseChairBehaviour))]
    public class AIPark_TrojanhorseChairMgr : BaseTypedNodeManager<AIPark_TrojanhorseChairBehaviour>
    {
    }
    [NodeBehaviourAttribute(typeof(AIPark_ChairBehaviour))]
    public class AIPark_ChairManager : BaseNodeManager
    {
        Dictionary<int, AIPark_ChairBehaviour> _bevDic = new();
        public List<AIPark_ChairBehaviour> GetBevs()
        {
            List<AIPark_ChairBehaviour> list = new List<AIPark_ChairBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                if (x is AIPark_ChairBehaviour chair)
                {
                    list.Add(chair);
                    _bevDic.Add(chair.Uid, chair);
                }
            });
            return list;
        }
        public AIPark_ChairBehaviour GetChairByType(AIPark_ChairBehaviour.Park_ChairType type)
        {
            foreach (var bev in entities)
            {
                var chairBev = bev as AIPark_ChairBehaviour;
                if (chairBev._chairType == type)
                    return chairBev;
            }
            return null;
        }

        public AIPark_ChairBehaviour GetEmptyCorridorChair()
        {
            foreach (var bev in entities)
            {
                var chairBev = bev as AIPark_ChairBehaviour;
                if ((chairBev._chairType == AIPark_ChairBehaviour.Park_ChairType.WaitingRoom))
                {
                    if (chairBev.IsCanClick)
                        return chairBev;
                }
            }
            return null;
        }

        public AIPark_ChairBehaviour GetBevsByUID(int uid)
        {
            if (_bevDic.TryGetValue(uid, out AIPark_ChairBehaviour chair))
            {
                return chair;
            }
            return null;
        }
    }
    [NodeBehaviourAttribute(typeof(AIPark_PhotoBehaviour))]
    public class AIPark_PhotoManager : BaseNodeManager
    {
        public List<AIPark_PhotoBehaviour> GetBevs()
        {
            List<AIPark_PhotoBehaviour> list = new List<AIPark_PhotoBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIPark_PhotoBehaviour);
            });
            return list;
        }
    }
    #endregion
}