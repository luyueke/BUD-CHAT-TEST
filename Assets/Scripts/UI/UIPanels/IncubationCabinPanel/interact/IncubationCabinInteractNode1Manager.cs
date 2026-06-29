using System.Collections.Generic;
using Game.Avatar;
using GameData.PgcData;
using UI.Manager;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 孵化舱互动节点1管理器，负责待机动作（循环/非循环）的列表展示与添加删除。
/// </summary>
public class IncubationCabinInteractNode1Manager : MonoBehaviour
{
    [SerializeField] internal CabinBtnToggleParent interact_node1_toggleParent;
    [SerializeField] internal ScrollRect interact_node1_scrollRect;
    [SerializeField] internal GameObject interact_node1_itemPrefab;
    [SerializeField] internal GameObject interact_node1_addActionItemPrefab;

    /// <summary>左下角剩余可添加数量文本，需在预制体中绑定</summary>
    [SerializeField] internal Text remainingCountText;

    private IncubationCabinPanel _panel;
    private IncubationCabinInteractNode _interactNode;
    private InteractNode1RoleItem _currentSelectedNode1Item;
    private int _currentLoopType = 0; // 0:循环动画 1:非循环动画
    private bool _pendingAutoSelectLast = false;

    internal void Init(IncubationCabinPanel panel, IncubationCabinInteractNode interactNode)
    {
        _panel = panel;
        _interactNode = interactNode;
    }

    public void SetLoopType(int index)
    {
        _currentLoopType = index;
    }

    public void Refresh()
    {
        _currentSelectedNode1Item = null;
        int childCount = interact_node1_scrollRect.content.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Destroy(interact_node1_scrollRect.content.transform.GetChild(i).gameObject);
        }
        interact_node1_addActionItemPrefab.SetActive(false);
        interact_node1_itemPrefab.SetActive(false);

        var addItem = Instantiate(interact_node1_addActionItemPrefab, interact_node1_scrollRect.content);
        addItem.SetActive(true);
        var addActionItem = addItem.GetComponent<InteractNode1AddActionItem>();
        addActionItem.Init();
        addActionItem.onAddAction = () =>
        {
            var caracterData = CharacterData.DeserializeObject(_panel?.curSkinPack?.avatarJson);
            bool isLoopAni = _currentLoopType == 0;
            var panel = UIManager.Inst.OpenPanel<IncubationCabinEmotePopPanel>(PanelId.IncubationCabinEmotePopPanel, isLoopAni, EmoteTabType.Official | EmoteTabType.Community, caracterData, false, isLoopAni ? 1 : 2);

            // 注入当前已添加的动作列表，让弹窗将已有动作显示为"已添加"状态，防止重复添加
            var cabinInfo = _panel?.info;
            var currentList = isLoopAni
                ? cabinInfo?.pendingEmote?.loopEmoteList
                : cabinInfo?.pendingEmote?.emoteList;
            //panel.SetAlreadyAddedEmotes(currentList);

            panel.onConfirmAction = (goodsDatas, onDone) =>
            {
                var goodsData = goodsDatas[0];
                if (goodsData.IsAdded)
                {
                    // 反选：从待机列表中删除与该商品匹配的动作
                    bool isLoop = _currentLoopType == 0;
                    var animList = isLoop
                        ? _panel?.info?.pendingEmote?.loopEmoteList
                        : _panel?.info?.pendingEmote?.emoteList;

                    // UGC 用 animInfo.id 匹配 pEmoteData.ugcData.id，PGC 用 GoodsData.Id 匹配 pEmoteData.emoteId
                    string ugcAnimId = goodsData.Assets?.Count > 0 ? goodsData.Assets[0]?.UgcInfo?.animInfo?.id : null;
                    bool isUgc = !string.IsNullOrEmpty(ugcAnimId);
                    string matchId = isUgc ? ugcAnimId : goodsData.Id;

                    pEmoteData toRemove = null;
                    if (animList != null && !string.IsNullOrEmpty(matchId))
                    {
                        for (int i = 0; i < animList.Count; i++)
                        {
                            bool matches = isUgc
                                ? animList[i].ugcData?.id == matchId
                                : animList[i].emoteId == matchId;
                            if (matches)
                            {
                                toRemove = animList[i];
                                break;
                            }
                        }
                    }

                    if (toRemove != null)
                    {
                        CabinNetManager.Inst.RemovePendingEmoteLocal(_panel?.info, toRemove, isLoop);
                    }
                }
                else
                {
                    // 添加：并自动选中最后添加的动作
                    LoggerUtils.Log("选择了动作: " + goodsData.Id);
                    _pendingAutoSelectLast = true;
                    CabinNetManager.Inst.AddPendingEmoteLocal(_panel?.info, goodsData, _currentLoopType == 0);
                    _pendingAutoSelectLast = false;
                }
                onDone();
            };
        };
        var cabinInfo = _panel?.info;

        var animList = _currentLoopType == 0
            ? cabinInfo.pendingEmote?.loopEmoteList
            : cabinInfo.pendingEmote?.emoteList;
        animList ??= new List<pEmoteData>();

        int showCount = animList.Count;
        InteractNode1RoleItem lastRoleItem = null;
        for (int i = 0; i < showCount; i++)
        {
            var item = Instantiate(interact_node1_itemPrefab, interact_node1_scrollRect.content);
            item.SetActive(true);
            item.GetComponent<InteractNode1RoleItem>().Init(animList[i]);
            var roleItem = item.GetComponent<InteractNode1RoleItem>();
            roleItem.onAddAction = (pEmoteData) =>
            {
                _currentSelectedNode1Item?.SetSelected(false);
                _currentSelectedNode1Item = roleItem;
                _currentSelectedNode1Item.SetSelected(true);
                _interactNode.PlayEmote(pEmoteData, _currentLoopType == 0);
                GlobalFuncExtensions.RefreshLayout(interact_node1_scrollRect.content);
            };
            roleItem.onDeleteAction = (pEmoteData) =>
            {
                LoggerUtils.LogError("onDeleteAction: " + pEmoteData);
                _interactNode.CancelAnim();
                CabinNetManager.Inst.RemovePendingEmoteLocal(_panel?.info, pEmoteData, _currentLoopType == 0);
                GlobalFuncExtensions.RefreshLayout(interact_node1_scrollRect.content);
            };
            lastRoleItem = roleItem;
        }
        if (_pendingAutoSelectLast && lastRoleItem != null)
        {
            _currentSelectedNode1Item = lastRoleItem;
            _currentSelectedNode1Item.SetSelected(true);
            _interactNode.PlayEmote(animList[showCount - 1], _currentLoopType == 0);
        }
        GlobalFuncExtensions.RefreshLayout(interact_node1_scrollRect.content);

        RefreshRemainingCount();
    }

    /// <summary>
    /// 刷新左下角剩余可添加数量文本。
    /// 根据当前 Tab 类型（循环/非循环）以及角色类型（本体/皮肤）计算剩余数量。
    /// </summary>
    private void RefreshRemainingCount()
    {
        if (remainingCountText == null)
            return;

        var cabinInfo = _panel?.info;

        if (cabinInfo == null)
        {
            remainingCountText.text = string.Empty;
            return;
        }

        bool isLoop = _currentLoopType == 0;

        // 根据角色类型取对应上限
        int maxCount = isLoop
            ? CabinConfig.GetLoopEmoteMax(cabinInfo)
            : CabinConfig.GetNonLoopEmoteMax(cabinInfo);

        // 计算当前已添加数量
        var animList = isLoop
            ? cabinInfo.pendingEmote?.loopEmoteList
            : cabinInfo.pendingEmote?.emoteList;
        int currentCount = animList?.Count ?? 0;

        int remaining = maxCount - currentCount;
        remainingCountText.text = $"剩余可添加：{remaining}";
    }
}
