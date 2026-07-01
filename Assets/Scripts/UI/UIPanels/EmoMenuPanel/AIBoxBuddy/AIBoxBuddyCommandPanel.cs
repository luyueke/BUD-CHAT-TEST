using System.Collections.Generic;
using Game.Avatar;
using Game.Props.PropsBehaviours;
using GameData.Account;
using Message;
using UI.Base;
using UI.UIPanels.AINPC.AIBuddyInMap;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 已召唤 AI 伙伴的口令列表面板。
/// 由 UIOperationOnWorldPanel 点击场景中的 AI 伙伴打开。
/// 点击口令后，场景中的伙伴播放对应动作并播放语音（与 Cabin 编辑预览同一套时序规则）。
/// </summary>
public class AIBoxBuddyCommandPanel : BasePanel<AIBoxBuddyCommandPanel>
{
    [SerializeField] private Transform commandItemRoot;
    [SerializeField] private AIBoxBuddyCommandItem commandItem;
    [SerializeField] private Button closeBgBtn; //和closeBtn同一个效果
    [SerializeField] private Button closeBtn;

    private List<AIBoxBuddyCommandItem> _itemList = new List<AIBoxBuddyCommandItem>();

    // 地图放置伙伴（非本人 self-buddy）：OnShow 传入则口令读该伙伴本体、动作播在该伙伴身上
    private AIBuddyInMapBehaviour _mapBuddy;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        closeBgBtn.onClick.AddListener(CloseSelf);
        commandItem.gameObject.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _mapBuddy = (args != null && args.Length > 0) ? args[0] as AIBuddyInMapBehaviour : null;
        RefreshItems();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        ClearItems();
    }

    public override void OnWindowBeFocused() { }
    public override void OnWindowPop() { }

    private void RefreshItems()
    {
        ClearItems();

        List<voiceCommands> commands;
        if (_mapBuddy != null)
        {
            // 地图伙伴：只展示本体口令（不含皮肤卡口令）
            commands = AIBuddyInMapUIManager.Inst.GetBuddyInfo(_mapBuddy)?.voiceCommands;
        }
        else
        {
            // self-buddy：优先用当前装备皮肤的口令覆盖；无覆盖时回退到主体的口令
            commands = AIBoxBuddyCallPanel.ActiveSkinVoiceCommands
                       ?? AIBoxBuddyCallPanel.CurrentSummonedInfo?.voiceCommands;
        }
        if (commands == null) return;

        foreach (var cmd in commands)
        {
            if (cmd == null || cmd.disabled == 1 || string.IsNullOrEmpty(cmd.command))
                continue;

            var item = Instantiate(commandItem, commandItemRoot);
            item.gameObject.SetActive(true);
            item.SetData(cmd, OnCommandClicked);
            _itemList.Add(item);
        }
    }

    private void OnCommandClicked(voiceCommands cmd)
    {
        if (_mapBuddy != null)
        {
            // 地图放置伙伴：动作 + 语音播在该伙伴身上（本地表现）
            var ctrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(_mapBuddy.LocalBuddyId);
            if (ctrl == null) return;
            var go = ctrl.gameObject;
            var name = AIBuddyInMapUIManager.Inst.GetBuddyInfo(_mapBuddy)?.GetName();
            var mapData = AIBoxBuddyCallPanel.BuildInteractSyncData(cmd, name);
            AIBoxBuddyCallPanel.PlayActivation(ctrl, go, mapData);
            // 不广播 OnBuddyCommandChat：该消息用 selfUid 作 key 会命中自己召唤的 buddy 并冒出气泡，
            // 地图 buddy 无需在自己 buddy 头顶显示台词。
            // TODO: 地图伙伴的房间内联机同步需独立通道（self-buddy 的 SendBuddyInteract 针对本人 buddy）
            CloseSelf();
            return;
        }

        var buddyCtrl = AIBuddyAvatarController.Inst.SelfStateController;
        var buddyGo = AIBuddyAvatarController.Inst.SelfController != null
            ? AIBuddyAvatarController.Inst.SelfController.gameObject
            : null;
        if (buddyCtrl == null) return;

        // 复用召唤面板的互动播放逻辑（动作 + 语音按 delaySecond 协调）
        var buddyName = AIBoxBuddyCallPanel.CurrentSummonedInfo?.name;
        var data = AIBoxBuddyCallPanel.BuildInteractSyncData(cmd, buddyName);
        AIBoxBuddyCallPanel.PlayActivation(buddyCtrl, buddyGo, data);                              // 本地动作 + 语音
        MessageHelper.Broadcast(MessageName.OnBuddyCommandChat, AccountDataManager.Inst.Uid, data); // 本地聊天 + 气泡
        GameAIBuddyManager.Inst.SendBuddyInteract(data);                                           // 同步给房间内其他玩家
        CloseSelf();
    }

    private void ClearItems()
    {
        foreach (var item in _itemList)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _itemList.Clear();
    }
}
