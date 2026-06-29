using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    /// <summary>
    /// AI章节处理器，负责管理章节状态的切换和处理玩家交互
    /// </summary>
    public class AIChapterHandler
    {
        private List<AIHospital_ChapterData> chapters;
        private int currentChapterIndex = -1;
        private AIHospital_CharacterBehaviour character;
        private AIChapterStateMachine stateMachine;
        private bool isInterruptedByPlayer = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapters">章节数据列表</param>
        /// <param name="character">角色行为控制器</param>
        public AIChapterHandler(List<AIHospital_ChapterData> chapters, AIHospital_CharacterBehaviour character)
        {
            this.chapters = chapters;
            this.character = character;
            stateMachine = new AIChapterStateMachine();
            LoggerUtils.Log($"[AIChapterHandler] 初始化完成，章节数量: {chapters.Count}");
        }

        /// <summary>
        /// 更新章节状态
        /// </summary>
        /// <param name="currentTime">当前游戏时间</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float currentTime, float deltaTime)
        {
            //还没初始化
            if(!this.character._isInitialized)
                return;
            
            // 如果被玩家打断，不进行章节切换
            if (isInterruptedByPlayer)
                return;

            // 根据当前时间切换章节
            if (currentChapterIndex + 1 < chapters.Count && currentTime >= chapters[currentChapterIndex + 1].startTime)
            {
                currentChapterIndex++;
                var chapter = chapters[currentChapterIndex];
                var newState = CreateState(chapter);
                stateMachine.ChangeState(newState);
                LoggerUtils.Log($"[AIChapterHandler] 切换到章节 {currentChapterIndex}: {chapter}");
            }

            stateMachine.Update(deltaTime);
        }

        /// <summary>
        /// 根据章节数据创建对应的状态
        /// </summary>
        /// <param name="chapter">章节数据</param>
        /// <returns>章节状态</returns>
        private AIChapterState CreateState(AIHospital_ChapterData chapter)
        {
            switch ((ActionType)chapter.action)
            {
                case ActionType.OperatingEquipment:
                    return new OperatingEquipmentState(character, chapter);
                case ActionType.InjectionsToPatients:
                    return new InjectionsToPatientsState(character, chapter);
                case ActionType.UsingRestroom:
                    return new ToiletState(character, chapter);
                case ActionType.OrganizingDocuments:
                    return new OrganizingDocumentsState(character, chapter);
                case ActionType.Working:
                    return new WorkingState(character, chapter);
                case ActionType.Sitting:
                    return new SittingState(character, chapter);
                case ActionType.Cleaning:
                    return new CleaningState(character, chapter);
                case ActionType.TakingMedication:
                    return new TakingMedicationState(character, chapter);
                case ActionType.ConductingRounds:
                    return new ConductingRoundsState(character, chapter);
                case ActionType.LyingDown:
                    return new LyingDownState(character, chapter);
                case ActionType.DispensingMedication:
                    return new DispensingMedicationState(character, chapter);
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
            if(!this.character._isInitialized)
                return;
            
            for (int i = 0; i < chapters.Count; i++)
            {
                if (currentTime >= chapters[i].startTime && currentTime < chapters[i].endTime)
                {
                    isInterruptedByPlayer = false; 
                    currentChapterIndex = i;
                    var chapter = chapters[currentChapterIndex];
                    var newState = CreateState(chapter);
                    stateMachine.ChangeState(newState);
                    LoggerUtils.Log($"[AIChapterHandler] 重新计算章节，切换到章节 {currentChapterIndex}: {chapter}");
                    break;
                }
            }
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