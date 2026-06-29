using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 孵化舱互动节点3管理器，负责口令互动的列表展示与添加删除。
/// 口令互动上限根据角色类型（本体/皮肤）由 CabinConfig 决定。
/// </summary>
public class IncubationCabinInteractNode3Manager : MonoBehaviour
{
    [SerializeField] internal Transform interact_node3_content;
    [SerializeField] internal GameObject interact_node3_emptyTipGo;
    [SerializeField] internal GameObject interact_node3_itemPrefab;
    [SerializeField] internal Button AddNullNodeBtn;
    [SerializeField] internal Button AddDefNodeBtn;

    /// <summary>左下角剩余可添加数量文本，需在预制体中绑定</summary>
    [SerializeField] internal Text remainingCountText;

    private IncubationCabinPanel _panel;
    private IncubationCabinInteractNode _interactNode;
    private List<bool> _isVoiceCommandsOpenList = new List<bool>();

    internal void Init(IncubationCabinPanel panel, IncubationCabinInteractNode interactNode)
    {
        _panel = panel;
        _interactNode = interactNode;

        AddNullNodeBtn.onClick.AddListener(() =>
        {
            var characterInfo = _panel?.info;
            var list = characterInfo?.voiceCommands;
            int maxCount = CabinConfig.GetVoiceCommandMax(characterInfo);

            if (list != null && list.Count >= maxCount)
            {
                TipPanel.ShowToast($"指令互动最多{maxCount}个");
                return;
            }

            CabinNetManager.Inst.AddEmptyVoiceCommandsLocal(characterInfo);
        });

        AddDefNodeBtn.onClick.AddListener(() =>
        {
            var characterInfo = _panel?.info;
            var tokenID = _panel?._tokenID;

            // 修复：此处应检查 voiceCommands 而非 activation
            var list = characterInfo?.voiceCommands;
            int maxCount = CabinConfig.GetVoiceCommandMax(characterInfo);

            if (list != null && list.Count >= maxCount)
            {
                TipPanel.ShowToast($"指令互动最多{maxCount}个");
                return;
            }

            var pop = UIManager.Inst.OpenPanel<CabinDefActionPop>(PanelId.CabinDefActionPop, CabinDefActionType.VoiceCommand);
            pop.onConfirmAction = (configs, callback) =>
            {
                // 重新计算剩余可添加数量（弹窗打开期间数量可能已变化）
                int maxCount = CabinConfig.GetVoiceCommandMax(characterInfo);
                int currentCount = characterInfo?.voiceCommands?.Count ?? 0;
                int remaining = maxCount - currentCount;

                if (configs.Count > remaining)
                {
                    configs = configs.GetRange(0, Mathf.Max(0, remaining));
                    TipPanel.ShowToast($"最多添加「{maxCount}」个「指令动作」");
                }

                if (configs.Count == 0)
                {
                    callback?.Invoke(true);
                    return;
                }

                CabinNetManager.Inst.AddDefVoiceCommandsLocalBatch(characterInfo, tokenID, configs, callback);
            };
        });
    }

    public void Refresh()
    {
        int childCount = interact_node3_content.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Destroy(interact_node3_content.GetChild(i).gameObject);
        }

        interact_node3_itemPrefab.gameObject.SetActive(false);

        var characterInfo = _panel?.info;
        var token = _panel?._tokenID;

        var voiceCommandsList = characterInfo?.voiceCommands;

        interact_node3_emptyTipGo.SetActive(voiceCommandsList.Count == 0);
        remainingCountText.gameObject.SetActive(voiceCommandsList.Count > 0);

        for (int i = 0; i < voiceCommandsList.Count; i++)
        {
            int idx = i;
            if (idx >= _isVoiceCommandsOpenList.Count)
            {
                _isVoiceCommandsOpenList.Add(true);
            }
            var item = Instantiate(interact_node3_itemPrefab, interact_node3_content);
            item.SetActive(true);
            var roleItem = item.GetComponent<InteractNode3RoleItem>();
            roleItem.Init(characterInfo, token, voiceCommandsList[i], i, _isVoiceCommandsOpenList[i]);
            var capturedItem3 = roleItem;
            var voiceCommand = voiceCommandsList[i];
            bool hasPgcEmote3 = voiceCommand.isPgc == 1 && !string.IsNullOrEmpty(voiceCommand.emoteId);
            bool hasUgcEmote3 = voiceCommand.isPgc != 1 && voiceCommand.ugcData != null;
            if (hasPgcEmote3 || hasUgcEmote3)
            {
                _interactNode.PgcUgcController.FetchAnimMaxDelaySecond(voiceCommand, max =>
                {
                    if (capturedItem3 != null)
                    {
                        capturedItem3.SetMaxDelaySecond(max);
                    }
                });
            }
            roleItem.onSelectEmote = (goodsData, onDone) =>
            {
                CabinNetManager.Inst.ModifyVoiceCommandsEmoteLocal(characterInfo, idx, goodsData);
                if (idx < characterInfo.voiceCommands.Count)
                {
                    CabinNetManager.Inst.PreviewActivation(characterInfo.voiceCommands[idx]);
                }
                onDone();
            };
            roleItem.onCommonSelectBtnClick = () =>
            {
                _isVoiceCommandsOpenList[idx] = !_isVoiceCommandsOpenList[idx];
                GlobalFuncExtensions.RefreshLayout(interact_node3_content);
            };
        }

        GlobalFuncExtensions.RefreshLayout(interact_node3_content);

        RefreshRemainingCount();
    }

    /// <summary>
    /// 刷新左下角剩余可添加口令互动数量文本。
    /// 根据角色类型（本体/皮肤）计算上限与剩余数量。
    /// </summary>
    private void RefreshRemainingCount()
    {
        if (remainingCountText == null)
            return;

        var characterInfo = _panel?.info;

        if (characterInfo == null)
        {
            remainingCountText.text = string.Empty;
            return;
        }

        int maxCount = CabinConfig.GetVoiceCommandMax(characterInfo);
        int currentCount = characterInfo.voiceCommands?.Count ?? 0;
        int remaining = maxCount - currentCount;
        remainingCountText.text = $"剩余可添加：{remaining}";
    }
}
