using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIHospital.FSM;
using Message;
using System;
using System.Collections.Generic;
using AIGame.Base;
using Game.Event;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Game.Base;

public class AIHospitalQuickEmotePanel : BasePanel<AIHospitalQuickEmotePanel>
{
    public Button BgBtn;
    public Button InputBtn;

    public class TEmoteInfo
    {
        public string id;
        public string name;
        public string desc;
    }

    private List<TEmoteInfo> _quickEmoteList = new()    
    {
        new TEmoteInfo{id = "40100401", name = "礼貌问好", desc = "您好！"},
        new TEmoteInfo{id = "40300111", name = "扇耳光", desc = "看我揍你！"},
        new TEmoteInfo{id = "40300486", name = "吃薯片", desc = "我请你吃薯片呀！"},
        new TEmoteInfo{id = "40300472", name = "体温计爆炸", desc = "我病好重啊！！！"},
        new TEmoteInfo{id = "40400376", name = "长椅贴贴", desc = "我想和你贴贴！"},
        new TEmoteInfo{id = "40100262", name = "摇摆舞", desc = "我们一起跳舞！"},
        new TEmoteInfo{id = "40300106", name = "亲额头", desc = "亲亲你～"},
        new TEmoteInfo{id = "40300468", name = "偷袭", desc = "偷袭你～"},
        new TEmoteInfo{id = "40300128", name = "击倒", desc = "揍扁你！"}
    };

    private string _currentInteractNpcID = "";

    [SerializeField] private CButton _singleEmoteBtn;
    [SerializeField] private CButton _doubleEmoteBtn;

    [SerializeField] private GameObject _emoteScroll;

    [SerializeField] private GameObject _emoteItemPrefab;

    KeyBoardInfo keyBoardInfo;

    private AIHospitalGuestPanel _aIHospitalGuestPanel;

    public AIHospitalQuickMsgView _quickMsgView;

    public CButton _quickMsgViewStateBtn;

    public AIHospitalInputAnimation _moveAnimation;

    private bool _bInGuide;

    public override void OnCreate()
    {
        keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入消息..."),
            inputMode = 2,
            maxLength = 60,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send,
            source = (int)KeyboardSource.RoomChat
        };
        _aIHospitalGuestPanel = UIManager.Inst.FindPanel<AIHospitalGuestPanel>(PanelId.AIHospitalGuestPanel);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        bool _bInGuide = false;
        if (args != null && args.Length > 1)
        {
            if (args[0] is string npcID)
            {
                _currentInteractNpcID = npcID;
            }
            else
                _currentInteractNpcID = "";
            if (args[1] is bool bSkip)
            {
                _bInGuide = bSkip;
            }
        }

        // 播放进入动画
        _moveAnimation.PlayEnterAnimation(_bInGuide);
        
        InitUI();
        _quickMsgView.InitQuickMsgScrollView(this,_bInGuide);
    }

    private void InitUI()
    {
        InitEmoteScroll();
        BgBtn.onClick.AddListener(OnBgClick);
        _doubleEmoteBtn.onClick.AddListener(OnDoubleEmoteClick);
        _singleEmoteBtn.onClick.AddListener(OnSingleEmoteClick);
        InputBtn.onClick.AddListener(OnInputClick);
        _quickMsgViewStateBtn.onClick.AddListener(OnShowQuickMsgClick);
    }

    private void InitEmoteScroll()
    {
        for (int i = 0; i < _quickEmoteList.Count; i++)
        {
            var item = Instantiate(_emoteItemPrefab, _emoteItemPrefab.transform.parent);
            var btn = item.GetComponent<CButton>();
            btn.SetLocalText(_quickEmoteList[i].name);
            
            int index = i;
            btn.onClick.AddListener(() =>
            {
                OnQuickEmoteBtnClick(index);
            });
            item.SetActive(true);
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        if (!string.IsNullOrEmpty(_currentInteractNpcID))
        {
            var curNpcBev = AIHospital_CharacterManager.Inst.GetNpc(_currentInteractNpcID);
            if (curNpcBev != null)
            {
                curNpcBev.RecalculateChapter(3);
            }
        }
    }

    protected override void OnDestroy()
    {
        _moveAnimation.StopAnimation();
        base.OnDestroy();
    }

    private void OnInputClick()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.hideKeyboard, OnHideKeyboard);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnHideKeyboard(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.hideKeyboard);
    }

    private void OnKeyboard(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _aIHospitalGuestPanel.SendInput(msg);
        if (!string.IsNullOrEmpty(msg))
        {
            AIHospitalUtils.Inst.OnSendConversationReq();
        }
    }

    private void OnSingleEmoteClick()
    {
        //这里隐藏emote界面的tab
        bool bHideTab = true;
        UIManager.Inst.OpenPanel(PanelId.EmoMenuPanel_AIGame,bHideTab);
    }

    private void OnDoubleEmoteClick()
    {
        _emoteScroll.gameObject.SetActive(!_emoteScroll.gameObject.activeSelf);
    }

    private void OnBgClick()
    {
        LoggerUtils.Log("AIHospitalQuickEmotePanel OnBgClick");
        GameAINpcChatManager.Inst.ResetChatTargetBehaviour();
        CloseSelf();
    }

    public void OnQuickEmoteBtnClick(int index)
    {
        string emoteID = _quickEmoteList[index].id;
        if (String.IsNullOrEmpty(emoteID))
        {
            return;
        }

#if UNITY_EDITOR || UNITY_STANDARD_BUILD
        if (MobileInterface.Instance.onClientRespose.ContainsKey(MobileInterfaceDefine.quickEmote))
        {
            MobileInterface.Instance.onClientRespose[MobileInterfaceDefine.quickEmote]?.Invoke(emoteID);
        }
#endif
        LoggerUtils.Log("DebugInputPanel QuickEmoteClick:" + emoteID);
        MessageHelper.Broadcast(MessageName.OnS9ChatEmoteClick, emoteID, _quickEmoteList[index].desc);
        EventCenterDataManager.Inst.ReportTask(PostEventId.UseDoubleEmoteInMap);
        AIHospitalUtils.Inst.OnSendDoubleEmoteReq();
        OnPlayEmote(emoteID);

        CloseSelf();
    }

    private void OnPlayEmote(string emoteID)
    {
        var npcBehaviour = AIHospital_CharacterManager.Inst.GetNpc(_currentInteractNpcID);
        if (npcBehaviour)
        {
            var curState = npcBehaviour.GetCurrentState();
            if (curState is TalkWithPlayerState talkState)
            {
                talkState.StartPlayNpcEmote(emoteID, true);
            }
            else
            {
                LoggerUtils.LogError("当前点击了弹窗交互，但是npc尚不处于交谈状态中");
            }
        }
    }

    private void OnShowQuickMsgClick()
    {
        bool active = _quickMsgView.gameObject.activeSelf;
        _quickMsgView.gameObject.SetActive(!active);
        _quickMsgViewStateBtn.GetComponent<CommonSpriteSwitch>().Switch((uint)(active?0:1));
    }
}
