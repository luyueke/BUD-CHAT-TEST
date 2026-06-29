using System;
using AIGame.Base;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Props;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GameData.BaseInfo;
using Message;

public class AIHospitalNpcHudItem : MonoBehaviour
{
    [Header("TargetView")]
    public CButton Btn_ChatToNpc;
    public CButton Btn_LinkToNpc;
    public Image Img_Fill;
    public Text Txt_Name;
    
    [Header("NormalView")]
    public CButton Btn_ChatToNpc_2;
    public CButton Btn_LinkToNpc_2;
    public Text Txt_Name_2;
    
    public CanvasGroup TargetView_c;
    public CanvasGroup NormalView_c;

    [Header("Tween Settings")]
    public float fillTweenDuration = 0.5f;
    public Ease fillTweenEase = Ease.OutQuad;

    private AIHospital_CharacterBehaviour _character;
    private AIHospitalGame aiGame;
    private float curDecisionRate;

    public void Awake()
    {
        MessageHelper.AddListener<bool>(MessageName.LinkEmoteStateChange,SetLinkBtnState);
    }

    public void SetData(AIHospital_CharacterBehaviour characterBev)
    {
        aiGame = aiGame ? aiGame : AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        
        this._character = characterBev;
        Btn_ChatToNpc.onClick.RemoveAllListeners();
        Txt_Name.text = _character._curNpcName;
        curDecisionRate = _character._decisionRate;
        Img_Fill.fillAmount = _character._decisionRate;
        Btn_ChatToNpc.onClick.AddListener(ChatToCurNpc);
        Btn_LinkToNpc.onClick.AddListener(LinkToCurNpc);

        Btn_ChatToNpc_2.onClick.RemoveAllListeners();
        Btn_LinkToNpc_2.onClick.RemoveAllListeners();
        Txt_Name_2.text = _character._curNpcName;
        Btn_ChatToNpc_2.onClick.AddListener(ChatToCurNpc);
        Btn_LinkToNpc_2.onClick.AddListener(LinkToCurNpc);

        if (aiGame!=null && aiGame.isPgcEnter)
        {
            var CurrentStep = aiGame.CurrentStep;
            if (CurrentStep < AIGameHospitalConfig.taskTarget.Count)
            {
                var roleType = AIGameHospitalConfig.taskTarget[CurrentStep].targetRole;
                SetViewState(roleType == _character.GetNpcRoleType() && _character.GetNpcRoleType()!=HospitalNpcRoleType.Dustman);
            }
            
            if (characterBev._decisionRate >= 1)
            {
                SetViewState(false);
            }
        }
        else
        {
            SetViewState(!(characterBev.GetNpcType() == HospitalNPCType.Runagate || characterBev._decisionRate >= 1));
        }
    }

    private void ChatToCurNpc()
    {
        //todo 如果角色的当前状态处于双人emote 需要先退出
        if (AvatarController.Inst.SelfStateController.IsMainState(PlayerState.DoubleEmote))
        {
            AvatarController.Inst.SelfStateController.ExitState(PlayerState.DoubleEmote);
        }
        _character.StartTalkWithPlayer();
        ChatToHospitalNPC(_character);
    }
    
    private void ChatToHospitalNPC(AIHospital_CharacterBehaviour npcBev)
    {
        var playerStateController = npcBev.GetComponentInChildren<KinematicCharacterController>();
        AIBuddyAvatarController.Inst.CurrentInteractNpcID = playerStateController.PlayerID;
        GameAINpcChatManager.Inst.StartChatToAIBuddy(true,playerStateController.PlayerID);
    }

    private void LinkToCurNpc()
    {
        var emotePanel = UIManager.Inst.OpenPanel<AIBuddyOptionPanel>(PanelId.AIBuddyOptionPanel,_character.GetNpcID());
    }

    private void SetLinkBtnState(bool value)
    {
        bool active = !value;
        if (aiGame && aiGame.isPgcEnter)
        {
            active = active && aiGame.CurrentStep > 0;
        }
        Btn_LinkToNpc.gameObject.SetActive(active);
        Btn_LinkToNpc_2.gameObject.SetActive(active);
    }

    /// <summary>
    /// 获取当前关联的NPC角色
    /// </summary>
    public AIHospital_CharacterBehaviour GetCurrentNpc()
    {
        return _character;
    }

    public void UpdateDecisionRate()
    {
        if (curDecisionRate != _character._decisionRate)
        {
            curDecisionRate = _character._decisionRate;
            Img_Fill.DOKill();
            Img_Fill.DOFillAmount(_character._decisionRate, fillTweenDuration)
                .SetEase(fillTweenEase)
                .OnComplete(() => {
                    // 在动画完成时检查决策率
                    if (_character._decisionRate >= 1)
                    {
                        SetViewState(false);
                    }
                });
        }
    }

    public void SetActive(bool value)
    {
        bool isLink = BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid);
        bool active = !isLink;
        if (aiGame&&aiGame.isPgcEnter)
        {
            active = active && aiGame.CurrentStep > 0;
        }
        Btn_LinkToNpc.gameObject.SetActive(active);
        Btn_LinkToNpc_2.gameObject.SetActive(active);
        gameObject.SetActive(value);
    }

    public void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.LinkEmoteStateChange,SetLinkBtnState);
        Btn_ChatToNpc.onClick.RemoveAllListeners();
        Btn_LinkToNpc.onClick.RemoveAllListeners();
        Btn_ChatToNpc_2.onClick.RemoveAllListeners();
        Btn_LinkToNpc_2.onClick.RemoveAllListeners();
    }

    private void SetViewState(bool isTargetView)
    {
        if (isTargetView)
        {
            // 显示目标视图，隐藏普通视图
            TargetView_c.gameObject.SetActive(true);
            NormalView_c.gameObject.SetActive(false);
        }
        else
        {
            // 显示普通视图，隐藏目标视图
            TargetView_c.gameObject.SetActive(false);
            NormalView_c.gameObject.SetActive(true);
        }
    }
}

