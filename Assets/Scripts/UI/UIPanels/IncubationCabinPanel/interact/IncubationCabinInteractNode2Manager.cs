using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 孵化舱互动节点2管理器，负责唤醒动作的列表展示与添加删除。
/// 唤醒动作上限根据角色类型（本体/皮肤）由 CabinConfig 决定。
/// </summary>
public class IncubationCabinInteractNode2Manager : MonoBehaviour
{
    [SerializeField] internal Transform interact_node2_content;
    [SerializeField] internal GameObject interact_node2_emptyTipGo;
    [SerializeField] internal GameObject interact_node2_itemPrefab;
    [SerializeField] internal Button AddNullNodeBtn;
    [SerializeField] internal Button AddDefNodeBtn;

    /// <summary>左下角剩余可添加数量文本，需在预制体中绑定</summary>
    [SerializeField] internal Text remainingCountText;

    private IncubationCabinPanel _panel;
    private IncubationCabinInteractNode _interactNode;
    private List<bool> _isActivationOpenList = new List<bool>();

    internal void Init(IncubationCabinPanel panel, IncubationCabinInteractNode interactNode)
    {
        _panel = panel;
        _interactNode = interactNode;

        AddNullNodeBtn.onClick.AddListener(() =>
        {
            var characterInfo = _panel?.info;
            var list = characterInfo?.activation;
            int maxCount = CabinConfig.GetActivationMax(characterInfo);

            if (list != null && list.Count >= maxCount)
            {
                TipPanel.ShowToast($"唤醒动作最多{maxCount}个");
                return;
            }

            CabinNetManager.Inst.AddEmptyActivationLocal(characterInfo);
        });

        AddDefNodeBtn.onClick.AddListener(() =>
        {
            var characterInfo = _panel?.info;
            var tokenID = _panel?._tokenID;
            var list = characterInfo?.activation;
            int maxCount = CabinConfig.GetActivationMax(characterInfo);

            if (list != null && list.Count >= maxCount)
            {
                TipPanel.ShowToast($"唤醒动作最多{maxCount}个");
                return;
            }

            var pop = UIManager.Inst.OpenPanel<CabinDefActionPop>(PanelId.CabinDefActionPop, CabinDefActionType.Activation);
            pop.onConfirmAction = (configs, callback) =>
            {
                // 重新计算剩余可添加数量（弹窗打开期间数量可能已变化）
                int maxCount = CabinConfig.GetActivationMax(characterInfo);
                int currentCount = characterInfo?.activation?.Count ?? 0;
                int remaining = maxCount - currentCount;

                if (configs.Count > remaining)
                {
                    configs = configs.GetRange(0, Mathf.Max(0, remaining));
                    TipPanel.ShowToast($"最多添加「{maxCount}」个「唤醒动作」");
                }

                if (configs.Count == 0)
                {
                    callback?.Invoke(true);
                    return;
                }

                CabinNetManager.Inst.AddDefActivationsLocal(characterInfo, tokenID, configs, callback);
            };
        });
    }

    public void Refresh()
    {
        int childCount = interact_node2_content.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Destroy(interact_node2_content.GetChild(i).gameObject);
        }

        interact_node2_itemPrefab.gameObject.SetActive(false);

        var characterInfo = _panel?.info;
        var token = _panel?._tokenID;

        var activationList = characterInfo?.activation;
        interact_node2_emptyTipGo.SetActive(activationList.Count == 0);
        remainingCountText.gameObject.SetActive(activationList.Count > 0);
        for (int i = 0; i < activationList.Count; i++)
        {
            int idx = i;
            if (idx >= _isActivationOpenList.Count)
            {
                _isActivationOpenList.Add(true);
            }
            var item = Instantiate(interact_node2_itemPrefab, interact_node2_content);
            item.SetActive(true);
            var roleItem2 = item.GetComponent<InteractNode2RoleItem>();
            roleItem2.Init(characterInfo, token, activationList[i], i, _isActivationOpenList[i]);
            var capturedItem2 = roleItem2;
            var activation = activationList[i];
            bool hasPgcEmote2 = activation.isPgc == 1 && !string.IsNullOrEmpty(activation.emoteId);
            bool hasUgcEmote2 = activation.isPgc != 1 && activation.ugcData != null;
            if (hasPgcEmote2 || hasUgcEmote2)
            {
                _interactNode.PgcUgcController.FetchAnimMaxDelaySecond(activation, max =>
                {
                    if (capturedItem2 != null)
                    {
                        capturedItem2.SetMaxDelaySecond(max);
                    }
                });
            }
            roleItem2.onSelectEmote = (goodsData, onDone) =>
            {
                CabinNetManager.Inst.ModifyActivationEmoteLocal(characterInfo, idx, goodsData);
                if (idx < characterInfo.activation.Count)
                {
                    CabinNetManager.Inst.PreviewActivation(characterInfo.activation[idx]);
                }
                onDone();
            };
            roleItem2.onCommonSelectBtnClick = () =>
            {
                _isActivationOpenList[idx] = !_isActivationOpenList[idx];
                GlobalFuncExtensions.RefreshLayout(interact_node2_content);
            };
        }

        GlobalFuncExtensions.RefreshLayout(interact_node2_content);

        RefreshRemainingCount();
    }

    /// <summary>
    /// 刷新左下角剩余可添加唤醒动作数量文本。
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

        int maxCount = CabinConfig.GetActivationMax(characterInfo);
        int currentCount = characterInfo.activation?.Count ?? 0;
        int remaining = maxCount - currentCount;
        remainingCountText.text = $"剩余可添加：{remaining}";
    }
}
