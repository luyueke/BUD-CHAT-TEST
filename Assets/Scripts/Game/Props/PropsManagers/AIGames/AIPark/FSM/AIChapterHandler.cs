using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsBehaviours.AIPark;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    /// <summary>
    /// AI章节处理器，负责管理章节状态的切换和处理玩家交互
    /// </summary>
    public class AIChapterHandler
    {
        private AIPark_ChapterData lastChapterData; //记录上一章节数据 以判断位置/行动
        private AIPark_ChapterData chapterData;
        private int currentChapterIndex = -1;
        private AIPark_CharacterBehaviour character;
        private AIChapterStateMachine stateMachine;
        private bool isInterruptedByPlayer = false;
        private float _serverValidActionEndTime = 0;

        float _customNextActionTime = 0;  //客户端下次做的道具行为
        bool _isPause = false;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapters">章节数据列表</param>
        /// <param name="character">角色行为控制器</param>
        public AIChapterHandler(AIPark_ChapterData chapter, AIPark_CharacterBehaviour character)
        {
            lastChapterData = null;
            this.chapterData = chapter;
            CheckChangeState(chapter);
            this.character = character;
            stateMachine = new AIChapterStateMachine();
            LoggerUtils.Log($"[AIChapterHandler] 初始化完成，章节位置: {chapterData?.location}, 章节动作: {chapterData?.action}");
        }

        /// <summary>
        /// 更新章节状态
        /// </summary>
        /// <param name="currentTime">当前游戏时间</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float currentTime, float deltaTime)
        {
            //还没初始化
            if (!this.character._isInitialized)
                return;
            if (_isPause)
                return;
            if (character.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
            {
                return;
            }

            // 如果被玩家打断，不进行章节切换
            if (isInterruptedByPlayer)
                return;

            if (CheckServerActionValid())
            {
                return;
            }
            if (Time.time > _customNextActionTime)
            {
                Debug.Log("乐园AIChapterHandler " + character.GetNpcName() + " " + character.GetNpcID() + ": 长期没有动作，随机下个行为");
                //暂时屏蔽npc自己的行动 下一个版本打开
                // AIParkPropsManager.Inst.ExitAction(character.GetNpcID());
                // AIParkPropsManager.Inst.CheckDoCustomAction(character.GetNpcID(), 100);
                _customNextActionTime = _randomNextActionTime();
            }

            // 根据当前时间切换章节
            // if (currentChapterIndex + 1 < chapters.Count && currentTime >= chapters[currentChapterIndex + 1].startTime)
            // {
            //     currentChapterIndex++;s
            //     var chapter = chapters[currentChapterIndex];
            //     var newState = CreateState(chapter);
            //     stateMachine.ChangeState(newState);
            //     LoggerUtils.Log($"[AIChapterHandler] 切换到章节 {currentChapterIndex}: {chapter}");
            // }

            stateMachine.Update(deltaTime);
        }

        public AIPark_ChapterData GetChapterData()
        {
            return chapterData;
        }

        public AIPark_ChapterData GetLastChapterData()
        {
            return lastChapterData;
        }

        float _randomNextActionTime(float min = 140f, float max = 160f)
        {
            return Time.time + UnityEngine.Random.Range(min, max);
        }

        public void PauseChapter()
        {
            _isPause = true;
        }

        public void ResumeChapter()
        {
            _customNextActionTime = _randomNextActionTime();
            _isPause = false;
        }

        public bool CheckServerActionValid()
        {
            if (_serverValidActionEndTime - Time.time > 0)
            {
                return true;
            }
            return false;
        }

        void CheckValidChapterAction(AIPark_ChapterData chapter)
        {
            if (chapter.action != 0 && (chapter.action < 101 || chapter.action > 108))
            {
                LoggerUtils.Log("乐园[AIChapterHandler] " + character.GetNpcName() + " " + character.GetNpcID() + ": 章节动作:" + chapter.action + " 不合法，设置为Idle");
                chapter.action = (int)ActionType.Idle;
            }
        }

        public bool IsJustIdle()
        {
            if (lastChapterData == null || chapterData == null)
            {
                return false;
            }
            return lastChapterData.location == chapterData.location;

        }
        public void CheckChangeState(AIPark_ChapterData chapter, NodeBaseBehaviour nodeBaseBehaviour = null, bool byServer = true)
        {
            if (chapter == null)
                return;
            if (chapterData == null)
            {
                Debug.Log("乐园上一个章节为空");
            }
            else
            {
                Debug.Log("乐园上一个章节:" + chapterData.location + " " + chapterData.action);
            }
            Debug.Log("乐园尝试切换章节:" + chapter.location + " " + chapter.action + " server:" + byServer);
            CheckValidChapterAction(chapter);
            if (this.chapterData != null)
            {
                if (this.chapterData.location == chapter.location && this.chapterData.action == chapter.action)
                {
                    return;
                }
            }
            if (byServer)
            {
                if (chapter.location == 0 && chapter.action == 0)
                {
                    return;
                }
                _serverValidActionEndTime = _randomNextActionTime();
            }
            else
            {
                if (chapter.location == 0 && chapter.action == 0)
                {
                    //默认的不使用
                    return;
                }
                if (CheckServerActionValid())
                {
                    //不可以切换 服务器的动作有效
                    //return;
                }
                _customNextActionTime = _randomNextActionTime();
            }
            chapter.nodeBaseBehaviour = nodeBaseBehaviour;
            ChangeState(chapter);
        }

        /// <summary>
        /// 仅仅是设置章节数据，不切换状态(每一幕初始时设置npc的位置及状态)
        /// </summary>
        /// <param name="location"></param>
        /// <param name="action"></param>
        public void ResetChapterData()
        {
            AIPark_ChapterData tempChapterData = new AIPark_ChapterData();
            tempChapterData.action = 0;
            tempChapterData.location = 0;
            this.lastChapterData = null;
            this.chapterData = tempChapterData;
        }

        public void ChangeState(AIPark_ChapterData chapter)
        {
            if (stateMachine.GetCurrentState() is PerformOnStageState)
            {
                //舞台不让切状态  只能自己退出走强切
                // return;
            }
            if (chapter == null) return;
            lastChapterData = this.chapterData;
            chapterData = chapter;
            var newState = CreateState(chapterData);
            stateMachine.ChangeState(newState);
            LoggerUtils.Log($"乐园[AIChapterHandler] " + character.GetNpcName() + " " + character.GetNpcID() + ": 切换到章节:" + chapterData.location + ", 章节动作:" + chapterData.action);
        }

        public void ForceChangeState(ActionType actionType)
        {
            AIPark_ChapterData tempChapterData = new AIPark_ChapterData();
            tempChapterData.action = (int)actionType;
            if (this.chapterData != null)
            {
                tempChapterData.location = this.chapterData.location;
            }
            CheckValidChapterAction(tempChapterData);
            lastChapterData = this.chapterData;
            chapterData = tempChapterData;
            var newState = CreateState(chapterData);
            stateMachine.ChangeState(newState);
            LoggerUtils.Log($"状态强切 乐园[AIChapterHandler] " + character.GetNpcName() + " " + character.GetNpcID() + ": 切换到章节:" + chapterData.location + ", 章节动作:" + chapterData.action);
        }

        public void GuideForceQuickEnter(LocationType locationType, ActionType actionType)
        {
            AIPark_ChapterData tempChapterData = new AIPark_ChapterData();
            tempChapterData.action = (int)actionType;
            tempChapterData.bQuickEnter = true;
            tempChapterData.location = (int)locationType;
            chapterData = tempChapterData;
            var newState = CreateState(chapterData);
            stateMachine.ChangeState(newState);
            LoggerUtils.Log($"引导状态强切 乐园[AIChapterHandler] " + character.GetNpcName() + " " + character.GetNpcID() + ": 切换到章节:" + chapterData.location + ", 章节动作:" + chapterData.action);
        }

        public void CheckChangeState(ActionType actionType, NodeBaseBehaviour nodeBaseBehaviour = null, bool byServer = true)
        {
            // if (byServer)
            // {
            //     _serverValidActionEndTime = _randomNextActionTime();
            // }
            // else
            // {
            //     if (CheckServerActionValid())
            //     {
            //         //不可以切换 服务器的动作有效
            //         return;
            //     }
            //     _customNextActionTime = _randomNextActionTime();
            // }
            // AIPark_ChapterData tempChapterData = new AIPark_ChapterData();
            // tempChapterData.action = (int)actionType;
            // if (this.chapterData != null)
            // {
            //     tempChapterData.location = this.chapterData.location;
            // }
            // tempChapterData.nodeBaseBehaviour = nodeBaseBehaviour;
            // CheckValidChapterAction(tempChapterData);
            // ChangeState(tempChapterData);


            AIPark_ChapterData tempChapterData = new AIPark_ChapterData();
            tempChapterData.action = (int)actionType;
            if (actionType == ActionType.PerformOnStage)
            {
                tempChapterData.location = (int)LocationType.Stage;
            }
            else if (actionType == ActionType.TrojanHorse)
            {
                tempChapterData.location = (int)LocationType.TrojanHorse;
            }
            else if (actionType == ActionType.Swinging)
            {
                tempChapterData.location = (int)LocationType.Swinging;
            }
            else if (actionType == ActionType.SeeSaw)
            {
                tempChapterData.location = (int)LocationType.SeeSaw;
            }
            else if (actionType == ActionType.SlideSlides)
            {
                tempChapterData.location = (int)LocationType.SlideSlides;
            }
            else
            {
                if (this.chapterData != null)
                {
                    tempChapterData.location = this.chapterData.location;
                }
            }

            CheckChangeState(tempChapterData, nodeBaseBehaviour, byServer);
        }

        /// <summary>
        /// 根据章节数据创建对应的状态
        /// </summary>
        /// <param name="chapter">章节数据</param>
        /// <returns>章节状态</returns>
        private AIChapterState CreateState(AIPark_ChapterData chapter)
        {
            switch ((ActionType)chapter.action)
            {
                case ActionType.TrojanHorse:
                    return new TrojanHorseState(character, chapter);
                case ActionType.SlideSlides:
                    return new SlideSlidesState(character, chapter);
                case ActionType.SeeSaw:
                    return new SeeSawState(character, chapter);
                case ActionType.Swinging:
                    return new SwingingState(character, chapter);
                case ActionType.Idle:
                    return new IdleState(character, chapter);
                case ActionType.PerformOnStage:
                    return new PerformOnStageState(character, chapter);
                case ActionType.Selfie:
                    return new SelfieState(character, chapter);
                case ActionType.SitOnBench:
                    return new SitOnBenchState(character, chapter);


                case ActionType.Sitting:
                    return new SittingState(character, chapter);
                case ActionType.Cleaning:
                    return new CleaningState(character, chapter);
                case ActionType.Stand:
                    return new StandState(character, chapter);
                case ActionType.Sleep:
                    return new SleepState(character, chapter);
                case ActionType.Talk:
                    return new TalkState(character, chapter);
                case ActionType.TalkWithPlayer:
                    return new TalkWithPlayerState(character, chapter);
                default:
                    LoggerUtils.LogError($"[AIChapterHandler] 未知的动作类型: {chapter.action}");
                    return new IdleState(character, chapter);
            }
        }

        public void InterruptState(AIChapterState state)
        {
            isInterruptedByPlayer = true;
            stateMachine.InterruptState(state);
        }

        /// <summary>
        /// 根据当前时间重新计算章节
        /// </summary>
        /// <param name="currentTime">当前游戏时间</param>
        public void RecalculateChapter(float currentTime)
        {
            //还没初始化
            if (!this.character._isInitialized)
                return;
            // CheckChangeState(chapterData);
            // for (int i = 0; i < chapters.Count; i++)
            // {
            //     if (currentTime >= chapters[i].startTime && currentTime < chapters[i].endTime)
            //     {
            //         isInterruptedByPlayer = false; 
            //         currentChapterIndex = i;
            //         var chapter = chapters[currentChapterIndex];
            //         var newState = CreateState(chapter);
            //         stateMachine.ChangeState(newState);
            //         LoggerUtils.Log($"[AIChapterHandler] 重新计算章节，切换到章节 {currentChapterIndex}: {chapter}");
            //         break;
            //     }
            // }
        }

        /// <summary>
        /// 获取状态机
        /// </summary>
        /// <returns>状态机</returns>
        public AIChapterStateMachine GetStateMachine()
        {
            return stateMachine;
        }
    }
}