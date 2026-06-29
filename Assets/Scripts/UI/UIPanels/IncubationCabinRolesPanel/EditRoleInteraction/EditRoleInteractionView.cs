using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色互动编辑面板（EditRoleInteractionView）。
/// 支持待机动作（主/表演）、唤醒动作、口令互动的查看与编辑。
/// 待机动作在本地副本上操作，保存时统一提交服务器。
/// </summary>
public class EditRoleInteractionView : BasePanel<EditRoleInteractionView>
{
    public Button btn_clock;
    public Toggle tog_standby;
    public Toggle tog_activate;
    public Toggle tog_watchword;
    public Transform standbyView;
    public Transform activeView;
    public Transform watchwordView;
    public LoadingButton btn_save;

    #region standbyView

    public EditRoleInteractionStandbyItem standbyItem;
    public Transform standbyItemParent;
    public Toggle tog_main_anim;
    public Toggle tog_performance_anim;
    public Button btn_standby_add;

    /// <summary>待机动作左下角剩余可添加数量文本，需在预制体中绑定</summary>
    public Text remainingCountText;

    #endregion

    #region activateView
    public EditRoleInteractionActivateItem activateItem;
    public Transform activateItemParent;

    #endregion

    #region watchwordView
    public EditRoleInteractionWatchwordItem watchwordItem;
    public Transform watchwordItemParent;
    #endregion

    // 面板打开时的深拷贝副本，所有待机动作的增删操作均基于此副本，保存时才提交服务器
    private CabinCharacterUgcInfo _localCabinInfo;

    // 当前激活的主页签索引：0=待机 1=唤醒 2=指令
    private int _currentTabIndex = 0;

    // 初始化所有按钮与 Toggle 的事件绑定，并加载首屏内容
    public override void OnCreate()
    {
        base.OnCreate();

        // 深拷贝当前角色数据，确保编辑操作不影响原始数据（只有保存时才发服务器）
        var original = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();
        _localCabinInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(
            Newtonsoft.Json.JsonConvert.SerializeObject(original));

        // 保存按钮：将本地副本提交到服务器，成功后刷新父面板并关闭此视图
        btn_save.onClick.AddListener(() =>
        {
            btn_save.ShowLoading();

            CabinRolesNetManager.Inst.SaveCharacterInfo(_localCabinInfo, (isSuccess) =>
            {
                btn_save.HideLoading();
                if (isSuccess)
                {
                    LoggerUtils.Log("保存角色互动数据成功");
                    // 保存成功后同步刷新 IncubationCabinRolesPanel 的互动内容
                    var rolesPanel = UIManager.Inst.FindPanel<IncubationCabinRolesPanel>(PanelId.IncubationCabinRolesPanel);
                    rolesPanel?.RefreshInteract();
                    CloseSelf();
                    // 关闭编辑面板后检测伙伴是否在 BOX 中，若是则提示前往控制台同步
                    rolesPanel?.CheckIsInBoxThenDo(null);
                }
                else
                {
                    LoggerUtils.LogError("保存角色互动数据失败");
                    CloseSelf();
                }
            });

        });
        tog_standby.onValueChanged.AddListener(OnTogStandbyValueChanged);
        tog_activate.onValueChanged.AddListener(OnTogActivateValueChanged);
        tog_watchword.onValueChanged.AddListener(OnTogWatchwordValueChanged);

        btn_standby_add.onClick.AddListener(OnStandbyAddBtnClick);
        tog_main_anim.onValueChanged.AddListener(isOn => { if (isOn) RefreshStandbyByCurrentTog(); });
        tog_performance_anim.onValueChanged.AddListener(isOn => { if (isOn) RefreshStandbyByCurrentTog(); });
        btn_clock.onClick.AddListener(CloseSelf);
        // 默认显示 standbyView
        SwitchToView(0);
        RefreshContent();
    }

    // 选中待机 Tab 时切换到待机视图
    void OnTogStandbyValueChanged(bool isOn)
    {
        if (isOn)
        {
            SwitchToView(0);
        }
    }

    // 选中激活 Tab 时切换到激活视图
    void OnTogActivateValueChanged(bool isOn)
    {
        if (isOn)
        {
            SwitchToView(1);
        }
    }

    // 选中口令互动 Tab 时切换到口令互动视图
    void OnTogWatchwordValueChanged(bool isOn)
    {
        if (isOn)
        {
            SwitchToView(2);
        }
    }

    // 打开表情选择弹窗，确认后将所选动作本地添加到副本的待机列表（不发网络，保存时统一提交）
    void OnStandbyAddBtnClick()
    {
        // 传入当前是否为主动作 Tab，让弹窗过滤对应类型的表情
        bool isLoopAni = tog_main_anim.isOn;

        var panel = UIManager.Inst.OpenPanel<IncubationCabinEmotePopPanel>(PanelId.IncubationCabinEmotePopPanel, tog_main_anim.isOn, EmoteTabType.Box | EmoteTabType.Official | EmoteTabType.Community, null, true, isLoopAni ? 1 : 2);

        // 注入当前已添加的动作列表（来自本地副本），让弹窗将已有动作显示为"已添加"状态，防止重复添加
        var currentList = isLoopAni
            ? _localCabinInfo?.usingEmote?.loopEmoteList
            : _localCabinInfo?.usingEmote?.emoteList;
        panel.SetAlreadyAddedEmotes(currentList);

        panel.onConfirmAction = (goodsDatas, onDone) =>
        {
            // 本地操作副本，不发服务器；保存按钮触发时统一提交
            bool anyChanged = false;
            bool isMain = tog_main_anim.isOn;
            var targetList = isMain
                ? _localCabinInfo?.usingEmote?.loopEmoteList
                : _localCabinInfo?.usingEmote?.emoteList;

            foreach (var gd in goodsDatas)
            {
                if (gd.IsAdded)
                {
                    // 反选：从本地副本的待机列表中删除对应动作
                    if (targetList == null)
                    {
                        continue;
                    }

                    // UGC 用 animInfo.id 匹配 pEmoteData.ugcData.id，PGC 用 GoodsData.Id 匹配 pEmoteData.emoteId
                    string ugcAnimId = gd.Assets?.Count > 0 ? gd.Assets[0]?.UgcInfo?.animInfo?.id : null;
                    bool isUgc = !string.IsNullOrEmpty(ugcAnimId);
                    string matchId = isUgc ? ugcAnimId : gd.Id;

                    if (string.IsNullOrEmpty(matchId))
                    {
                        continue;
                    }

                    pEmoteData toRemove = null;
                    for (int i = 0; i < targetList.Count; i++)
                    {
                        bool matches = isUgc
                            ? targetList[i].ugcData?.id == matchId
                            : targetList[i].emoteId == matchId;
                        if (matches)
                        {
                            toRemove = targetList[i];
                            break;
                        }
                    }

                    if (toRemove != null)
                    {
                        targetList.Remove(toRemove);
                        anyChanged = true;
                    }
                }
                else
                {
                    // 添加到本地副本
                    bool added = CabinRolesNetManager.Inst.TryAddPendingEmoteToInfo(_localCabinInfo, gd, isMain);
                    if (added)
                    {
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged)
            {
                RefreshStandbyByCurrentTog();
            }
            onDone();
        };
    }

    void SwitchToView(int index)
    {
        _currentTabIndex = index;
        standbyView.gameObject.SetActive(index == 0);
        activeView.gameObject.SetActive(index == 1);
        watchwordView.gameObject.SetActive(index == 2);
        GameObjectEx.FindComponentByName<Text>(tog_standby.transform, "Label").gameObject.SetActive(index == 0);
        GameObjectEx.FindComponentByName<Text>(tog_activate.transform, "Label").gameObject.SetActive(index == 1);
        GameObjectEx.FindComponentByName<Text>(tog_watchword.transform, "Label").gameObject.SetActive(index == 2);
        RefreshRemainingCount();
    }

    public void RefreshContent()
    {
        RefreshActivateItems();
        RefreshStandbyByCurrentTog();
        RefreshWatchwordItems();
    }

    /// <summary>
    /// 刷新待机动作列表。按添加时间从新到旧排列（倒序），并更新剩余可添加数量文本。
    /// </summary>
    void RefreshStandbyByCurrentTog()
    {
        if (_localCabinInfo == null)
            return;

        bool isMain = tog_main_anim.isOn;
        var list = isMain
            ? _localCabinInfo.usingEmote?.loopEmoteList ?? new List<pEmoteData>()
            : _localCabinInfo.usingEmote?.emoteList ?? new List<pEmoteData>();
        GameObjectEx.FindComponentByName<Text>(tog_main_anim.transform, "Label").gameObject.SetActive(isMain);
        GameObjectEx.FindComponentByName<Text>(tog_performance_anim.transform, "Label").gameObject.SetActive(!isMain);

        for (int i = standbyItemParent.childCount - 1; i >= 0; i--)
        {
            var child = standbyItemParent.GetChild(i);
            if (child.gameObject != btn_standby_add.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
        standbyItem.gameObject.SetActive(false);

        // 按添加时间从新到旧排列：列表尾部为最新添加，倒序遍历
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var data = list[i];
            var item = Instantiate(standbyItem, standbyItemParent);
            item.gameObject.SetActive(true);
            item.Init(data, isMain);
            var capturedData = data;
            var capturedIsLoop = isMain;
            item.onDeleteSuccess = () =>
            {
                if (_localCabinInfo?.usingEmote == null)
                    return;

                var targetList = capturedIsLoop
                    ? _localCabinInfo.usingEmote.loopEmoteList
                    : _localCabinInfo.usingEmote.emoteList;
                targetList?.Remove(capturedData);

                RefreshStandbyByCurrentTog();
            };
        }
        btn_standby_add.transform.SetAsFirstSibling();
        GlobalFuncExtensions.RefreshLayout(standbyItemParent);

        RefreshRemainingCount();
    }

    /// <summary>
    /// 刷新左下角剩余数量文本。
    /// 待机页签：根据主动作/表演动作子页签计算剩余可添加数量。
    /// 唤醒页签：统计已启用（disabled != 1）的唤醒动作数，与 EditViewActivationEnabledMax 比较。
    /// 指令页签：统计已启用的口令数，与 EditViewVoiceCommandEnabledMax 比较。
    /// </summary>
    void RefreshRemainingCount()
    {
        if (remainingCountText == null)
            return;

        if (_localCabinInfo == null)
        {
            remainingCountText.text = string.Empty;
            return;
        }

        if (_currentTabIndex == 0)
        {
            // 待机：按子页签（主动作/表演动作）计算剩余可添加数量
            bool isMain = tog_main_anim.isOn;
            int maxCount = isMain ? CabinConfig.EditViewLoopEmoteMax : CabinConfig.EditViewNonLoopEmoteMax;
            var list = isMain
                ? _localCabinInfo.usingEmote?.loopEmoteList
                : _localCabinInfo.usingEmote?.emoteList;
            int currentCount = list?.Count ?? 0;
            remainingCountText.text = $"剩余可添加：{maxCount - currentCount}";
        }
        else if (_currentTabIndex == 1)
        {
            // 唤醒：统计已启用条目数，与上限比较
            int enabledCount = 0;
            if (_localCabinInfo?.usingActivation != null)
            {
                foreach (var a in _localCabinInfo.usingActivation)
                {
                    if (a.disabled != 1)
                    {
                        enabledCount++;
                    }
                }
            }
            remainingCountText.text = $"剩余可启用：{CabinConfig.EditViewActivationEnabledMax - enabledCount}";
        }
        else if (_currentTabIndex == 2)
        {
            // 指令：统计已启用条目数，与上限比较
            int enabledCount = 0;
            if (_localCabinInfo?.usingVoiceCommands != null)
            {
                foreach (var vc in _localCabinInfo.usingVoiceCommands)
                {
                    if (vc.disabled != 1)
                    {
                        enabledCount++;
                    }
                }
            }
            remainingCountText.text = $"剩余可启用：{CabinConfig.EditViewVoiceCommandEnabledMax - enabledCount}";
        }
    }

    void RefreshActivateItems()
    {
        if (_localCabinInfo == null)
        {
            return;
        }
        List<characterInteraction> dataList = _localCabinInfo.usingActivation;
        if (dataList == null)
        {
            return;
        }
        var cabinInfo = _localCabinInfo;

        ClearChildren(activateItemParent);
        activateItem.gameObject.SetActive(false);
        for (int i = 0; i < dataList.Count; i++)
        {
            var item = Instantiate(activateItem, activateItemParent);
            item.gameObject.SetActive(true);
            item.Init(cabinInfo, dataList[i], i, false);
            item.onCommonSelectBtnClick = () =>
            {
                GlobalFuncExtensions.RefreshLayout(activateItemParent);
                RefreshRemainingCount();
            };
            item.onEnabledChanged = RefreshRemainingCount;
        }
        GlobalFuncExtensions.RefreshLayout(activateItemParent);
    }

    void RefreshWatchwordItems()
    {
        if (_localCabinInfo == null)
        {
            return;
        }
        List<voiceCommands> dataList = _localCabinInfo.usingVoiceCommands;
        if (dataList == null)
        {
            return;
        }
        var cabinInfo = _localCabinInfo;
        ClearChildren(watchwordItemParent);
        watchwordItem.gameObject.SetActive(false);
        for (int i = 0; i < dataList.Count; i++)
        {
            var item = Instantiate(watchwordItem, watchwordItemParent);
            item.gameObject.SetActive(true);
            item.Init(cabinInfo, dataList[i], i, false);
            item.onCommonSelectBtnClick = () =>
            {
                GlobalFuncExtensions.RefreshLayout(watchwordItemParent);
                RefreshRemainingCount();
            };
            item.onEnabledChanged = RefreshRemainingCount;
        }
        GlobalFuncExtensions.RefreshLayout(watchwordItemParent);
    }

    void ClearChildren(Transform parent)
    {
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
