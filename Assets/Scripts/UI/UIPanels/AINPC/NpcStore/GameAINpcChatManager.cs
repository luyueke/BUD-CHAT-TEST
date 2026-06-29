using Game.Avatar;
using Game.Props;
using System.Collections.Generic;
using System;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using Game.Props.PropsManagers;
using Es;
using UnityEngine;
using AIGame.Base;

/// <summary>
/// 主要服务于S9AINpc,区别于普通伙伴
/// </summary>
public class GameAINpcChatManager : GlobalInstance<GameAINpcChatManager>
{
    private Dictionary<string, AIHospital_CharacterBehaviour> _allNpcDic = new Dictionary<string, AIHospital_CharacterBehaviour>();
    private Dictionary<string, string> _npcConversitionDic = new Dictionary<string, string>();
    private string _strCurrentInteractNpc;

    /// <summary>
    /// 客户端自己随机的ID
    /// </summary>
    private string _sessionID = string.Empty;

    private AIHospital_CharacterBehaviour _currentChatTarget;

    private AIHospitalGuestPanel _aiHospitalGuestPanel;

    private bool _hideGuide=false;

    public void StartChatToAIBuddy(bool withEmote = false, string npcID = "")
    {
        _allNpcDic = AIHospital_CharacterManager.Inst.GetNpcDic();
        _strCurrentInteractNpc = AIBuddyAvatarController.Inst.CurrentInteractNpcID;
        _aiHospitalGuestPanel = UIManager.Inst.FindPanel<AIHospitalGuestPanel>(PanelId.AIHospitalGuestPanel);
        if (_aiHospitalGuestPanel == null)
        {
            LoggerUtils.LogError("找不到s9UI界面");
            return;
        }
        bool bUnFinishGuide = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId,-1) == (int)AIGameHospitalConfig.EPgcGuideID.SendMsgTips - 2;
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>().isPgcEnter;
        bool bShowGuide = bUnFinishGuide && isPgcGame;
        if (_allNpcDic.TryGetValue(_strCurrentInteractNpc, out AIHospital_CharacterBehaviour characterBehaviour))
        {
            _currentChatTarget = characterBehaviour;
            _aiHospitalGuestPanel.PopKeyBoardByNpcInteract(characterBehaviour, npcID);
            var panel = UIManager.Inst.OpenPanel(PanelId.AIHospitalQuickEmotePanel, npcID, bShowGuide);
            panel.gameObject.SetActive(!bShowGuide);
        }
        if (bShowGuide)
        {
            UIManager.Inst.OpenPanel<AIHospitalStrongGuide>(PanelId.AIHospitalStrongGuidePanel, AIGameHospitalConfig.EPgcGuideID.SendMsgTips);
        }
    }

    public void SetNpcConversitionInfo(string npcId, string conversitionID)
    {
        if (_npcConversitionDic.ContainsKey(npcId))
        {
            _npcConversitionDic[npcId] = conversitionID;
        }
        else
            _npcConversitionDic.Add(npcId, conversitionID);
    }

    public string GetNpcConversitionID(string npcId)
    {
        if (_npcConversitionDic.TryGetValue(npcId,out string conversitionID))
        {
            return conversitionID;
        }
        return String.Empty;
    }

    /// <summary>
    /// 如果先和一个未完成任务的npc对话，会生成临时会话ID,完成当前任务后需要clear一下
    /// </summary>
    public void ClearTempConversitionID(int step)
    {
        for (int i = step; i < AIGameHospitalConfig.taskTarget.Count - 1; i++)
        {
            HospitalNpcRoleType role = AIGameHospitalConfig.taskTarget[i].targetRole;
            var targetNpcBev = AIHospital_CharacterManager.Inst.GetNpc(role);
            SetNpcConversitionInfo(targetNpcBev.GetNpcID(), String.Empty);
            var npc = AIHospital_CharacterManager.Inst.GetNpc(role);
            if (npc != null)
            {
                AIHospital_CharacterManager.Inst.UpdateNpcData(npc.GetNpcID(), AIGameHospitalConfig.defaultDecision);
            }
        }
    }

    public void ClearAllConversitionID()
    {
        _sessionID = string.Empty;
        _npcConversitionDic.Clear();
    }

    public void ResetChatTargetBehaviour()
    {
        if (_currentChatTarget)
        {
            _currentChatTarget.RecalculateChapter(5f);
        }
    }

    public void ClearGuideCache()
    {
        _hideGuide = false;
    }

    public string GetSessionID()
    {
        //todo如果空，则随机一个字符串作为id
        if (string.IsNullOrEmpty(_sessionID))
        {
            _sessionID = Guid.NewGuid().ToString("N");
        }
        return _sessionID;
    }
}
