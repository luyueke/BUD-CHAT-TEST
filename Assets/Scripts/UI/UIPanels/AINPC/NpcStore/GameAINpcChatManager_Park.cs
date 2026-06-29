using Game.Avatar;
using Game.Props;
using System.Collections.Generic;
using System;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Props.PropsManagers;
using Es;
using UnityEngine;
using AIGame.Base;

/// <summary>
/// 主要服务于S11AINpc,区别于普通伙伴
/// </summary>
public class GameAINpcChatManager_Park : GlobalInstance<GameAINpcChatManager_Park>
{
    private Dictionary<string, AIPark_CharacterBehaviour> _allNpcDic = new Dictionary<string, AIPark_CharacterBehaviour>();
    private Dictionary<string, string> _npcConversitionDic = new Dictionary<string, string>();
    private string _strCurrentInteractNpc;

    /// <summary>
    /// 客户端自己随机的ID
    /// </summary>
    private string _sessionID = string.Empty;

    private AIPark_CharacterBehaviour _currentChatTarget;

    private AIParkGuestPanel _aiParkGuestPanel;

    private bool _hideGuide=false;

    public void StartChatToAIBuddy(bool withEmote = false, string npcID = "")
    {
        _strCurrentInteractNpc = AIBuddyAvatarController.Inst.CurrentInteractNpcID;
        _aiParkGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
        if (_aiParkGuestPanel == null)
        {
            LoggerUtils.LogError("找不到s11UI界面");
            return;
        }
        bool bUnFinishGuide = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + AIGameParkConfig.pgcGuideId,-1) == (int)AIGameParkConfig.EPgcGuideID.SendMsgTips - 2;
        bool isPgcGame = AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter;
        bool bShowGuide = bUnFinishGuide && isPgcGame;

        // 将字符串ID转换为ParkNpcRoleType枚举
        // ParkNpcRoleType npcRoleType = ParkNpcRoleType.Default;
        // if (int.TryParse(_strCurrentInteractNpc, out int npcId))
        // {
        //     if (Enum.IsDefined(typeof(ParkNpcRoleType), npcId))
        //     {
        //         npcRoleType = (ParkNpcRoleType)npcId;
        //     }
        // }
        AIPark_CharacterBehaviour characterBehaviour=AIPark_CharacterManager.Inst.GetNpc(_strCurrentInteractNpc);
        if (characterBehaviour!=null)
        {
            _currentChatTarget = characterBehaviour;
            _aiParkGuestPanel.PopKeyBoardByNpcInteract(characterBehaviour, npcID);
            var panel = UIManager.Inst.OpenPanel(PanelId.AIParkQuickEmotePanel, npcID, bShowGuide);
            panel.gameObject.SetActive(!bShowGuide);
            panel.gameObject.SetActive(true);
            (panel as AIParkQuickEmotePanel).QuickKeyBoard();
        }
        if (bShowGuide)
        {
            //暂时注释 待补充
            // UIManager.Inst.OpenPanel<AIParkStrongGuide>(PanelId.AIParkStrongGuidePanel, AIGameParkConfig.EPgcGuideID.SendMsgTips);
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
        for (int i = step; i < AIGameParkConfig.taskTarget.Count - 1; i++)
        {
            string roleId = AIGameParkConfig.taskTarget[i].targetRoleId;
            var targetNpcBev = AIPark_CharacterManager.Inst.GetNpc(roleId);
            SetNpcConversitionInfo(targetNpcBev.GetNpcID(), String.Empty);
            var npc = AIPark_CharacterManager.Inst.GetNpc(roleId);
            if (npc != null)
            {
                AIPark_CharacterManager.Inst.UpdateNpcData(npc.GetNpcID(), AIGameParkConfig.defaultDecision);
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
