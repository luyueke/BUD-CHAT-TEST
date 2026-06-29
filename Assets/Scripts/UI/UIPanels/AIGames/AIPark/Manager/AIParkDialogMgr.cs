using System;
using System.Collections.Generic;
using UnityEngine;
using Basic.Utils;
using Game.Props;
using Message;
using Game.Utils;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using AIGame.Base;

public class AIParkDialogMgr 
{
    private Dictionary<string, NpcDialogBox> _npcDialogBoxes = new Dictionary<string, NpcDialogBox>();
    private SelfNpcDialogBox _selfDialogBox;
    private Transform _dialogRoot;
    private GameObject _selfDialogNode;

    public void Init(Transform dialogRoot, GameObject selfDialogNode)
    {
        _dialogRoot = dialogRoot;
        _selfDialogNode = selfDialogNode;
        InitSelfDialogBox();
        
        // 注册事件
        MessageHelper.AddListener<string,string,Action>(MessageName.OnParkBeginNpcTalk, OnParkBeginNpcTalk);
    }

    public void Release()
    {
        MessageHelper.RemoveListener<string,string,Action>(MessageName.OnParkBeginNpcTalk, OnParkBeginNpcTalk);
        
        // 清理所有对话框
        foreach(var dialogBox in _npcDialogBoxes.Values)
        {
            if(dialogBox != null)
            {
                GameObject.Destroy(dialogBox.gameObject);
            }
        }
        _npcDialogBoxes.Clear();
        
        if(_selfDialogBox != null)
        {
            GameObject.Destroy(_selfDialogBox.gameObject);
            _selfDialogBox = null;
        }
    }

    private void InitSelfDialogBox()
    {
        if(_selfDialogBox == null && _selfDialogNode != null)
        {
            _selfDialogBox = SelfNpcDialogBox.CreatePark(_selfDialogNode, new Vector3(0, 1.8f, 0));
            _selfDialogBox.SetCamera(GameCameraUtils.Inst.GetMainCamera());
            _selfDialogBox.transform.SetParent(_dialogRoot);
            _selfDialogBox.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 获取或创建NPC的对话框
    /// </summary>
    public NpcDialogBox GetOrCreateNpcDialogBox(AIPark_CharacterBehaviour npcBehaviour)
    {
        string npcId = npcBehaviour.GetNpcID();
        
        if(!_npcDialogBoxes.ContainsKey(npcId))
        {
            var npcWrap = npcBehaviour._npcKccCtr.PlayerAnimCtrl.Wrap;
            var npcNode = GameUtils.FindChildByName(npcWrap.Avatar.transform, "Bip001 Head").gameObject;
            
            var dialogBox = NpcDialogBox.CreatePark(npcNode, new Vector3(0, 1.8f, 0));
            dialogBox.SetCamera(GameCameraUtils.Inst.GetMainCamera());
            dialogBox.transform.SetParent(_dialogRoot);
            
            _npcDialogBoxes[npcId] = dialogBox;
        }
        
        return _npcDialogBoxes[npcId];
    }

    /// <summary>
    /// 显示NPC对话
    /// </summary>
    public void ShowNpcDialog(AIPark_CharacterBehaviour npcBehaviour, string text, bool isEnd = true, bool needAni = true, bool isFirstContent = true, Action onComplete = null)
    {
        if(string.IsNullOrEmpty(text)) return;

        var dialogBox = GetOrCreateNpcDialogBox(npcBehaviour);
        
        if(isFirstContent)
        {
            dialogBox.ResetContent();
        }
        
        dialogBox.SetTextAndSpeak(
            YandereDataManager.Inst.TextAnimDuration,
            text,
            needAni,
            isFirstContent,
            isEnd,
            onComplete
        );
    }

    /// <summary>
    /// 显示玩家对话
    /// </summary>
    public void ShowSelfDialog(string text)
    {
        if(string.IsNullOrEmpty(text)) return;
        InitSelfDialogBox();
        _selfDialogBox.gameObject.SetActive(true);
        _selfDialogBox.SetText(text);
    }

    /// <summary>
    /// 隐藏指定NPC的对话框
    /// </summary>
    public void HideNpcDialog(string npcId)
    {
        if(_npcDialogBoxes.TryGetValue(npcId, out var dialogBox))
        {
            dialogBox.ForceHide();
        }
    }

    /// <summary>
    /// 隐藏玩家对话框
    /// </summary>
    public void HideSelfDialog()
    {
        _selfDialogBox?.ForceHide();
    }

    /// <summary>
    /// 处理NPC对话
    /// </summary>
    private void OnParkBeginNpcTalk(string npcId,string content,Action onComplete)
    {
        var npc= AIPark_CharacterManager.Inst.GetNpc(npcId);
        if(null == npc) return;
        ShowNpcDialog(npc, content, true, true, true,()=>{
            onComplete?.Invoke();
            // AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
            // aiGame.TriggerNpcNextDiscuss();
        });
    }
}
