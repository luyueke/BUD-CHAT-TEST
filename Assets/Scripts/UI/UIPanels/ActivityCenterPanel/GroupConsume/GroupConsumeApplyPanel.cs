using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Audio;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;


public enum GroupConsumeApplyType {
    None,
    Friends, // 好友列表
    Invited, // 已经邀请列表
    Applied, // 已经申请列表
    Leaved, // 退出列表
}


public class GroupConsumeApplyArgs {
    public GroupConsumeApplyType applyType;
    public GroupConsumeInfo groupConsumeInfo;
    public string activityId;
    public Action backCallBack;
}


public class GroupConsumeApplyPanel : BasePanel<GroupConsumeApplyPanel> {
    public GroupConsumeApplyAdapter adapter;
    public Text emptyText;
    public Text titleText;

    #region Friends

    public TextInputView searchInput;
    private CButton deleteBtn;

    #endregion

    #region Apply

    public GameObject sessionContainer;

    #endregion


    private RectTransform viewRectTransform;
    private GroupConsumeApplyType applyType = GroupConsumeApplyType.Invited;
    private string activityId = "";
    private Action backCallBack;
    private GroupConsumeInfo groupConsumeInfo;

    private string cookie = "";
    private bool isEnd = false;


    public override void OnCreate() {
        base.OnCreate();
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);

        viewRectTransform = adapter.transform.parent.GetComponent<RectTransform>();
        GameObjectEx.FindComponentByName<CButton>(searchInput.transform, "SearchBtn").onClick.AddListener(() => {
            OnSearchClicked(searchInput.Input);
        });
        deleteBtn = GameObjectEx.FindComponentByName<CButton>(searchInput.transform, "DeleteBtn");
        deleteBtn.onClick.AddListener(() => {
            emptyText.SetLocalText("");
            searchInput.SetInputWithoutNotify("");
            deleteBtn.gameObject.SetActive(false);
            GetFirstPageFriendList();
        });
        searchInput.SetOnInput(OnSearchClicked);
        GameObjectEx.FindComponentByName<CButton>(transform, "ContainerView/Close").onClick.AddListener(() => {
            CloseSelf();
            backCallBack?.Invoke();
        });
        foreach (var tmpToggle in sessionContainer.GetComponentsInChildren<Toggle>()) {
            tmpToggle.onValueChanged.AddListener((isOn) => {
                if (isOn) {
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    OnSessionToggleChanged(tmpToggle.name);
                }
            });
        }

        MessageHelper.RemoveListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
        MessageHelper.AddListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.RefreshGroupConsumeApplyData, OnRefreshGroupConsumeAppleData);
    }


    public override void OnShow(params object[] args) {
        base.OnShow(args);
        if (args.Length > 0 && args[0] is GroupConsumeApplyArgs applyArgs) {
            applyType = applyArgs.applyType;
            activityId = applyArgs.activityId;
            backCallBack = applyArgs.backCallBack;
            groupConsumeInfo = applyArgs.groupConsumeInfo;
        }

        searchInput.gameObject.SetActive(false);
        sessionContainer.SetActive(false);
        var offset = viewRectTransform.offsetMax;
        if (applyType == GroupConsumeApplyType.Friends) {
            searchInput.gameObject.SetActive(true);
            titleText.SetLocalText("邀请好友");
            adapter.SetType(applyType);
            GetFirstPageFriendList();
            offset.y = -161;
        } else {
            sessionContainer.SetActive(true);
            titleText.SetLocalText("队伍申请");
            OnSessionToggleChanged(applyType.ToString());
            offset.y = -142;
        }
        //viewRectTransform.offsetMax = offset;
    }

    private void OnRefreshGroupConsumeAppleData() {
        if (this == null) {
            return;
        }

        if (applyType == GroupConsumeApplyType.Friends) {
            GetFirstPageFriendList();
        } else {
            GetFirstPageApplyList();
        }
    }


    private void OnPullReleased(float sign) {
        switch (applyType) {
            case GroupConsumeApplyType.Friends:
                HandleFriendPullReleased(sign);
                break;
            default:
                HandleApplyPullReleased(sign);
                break;
        }
    }

    #region Friend

    private void OnSearchClicked(string input) {
        if (string.IsNullOrEmpty(input)) {
            deleteBtn.gameObject.SetActive(false);
            return;
        }

        deleteBtn.gameObject.SetActive(true);
        var req = new JObject() {
            ["businessId"] = activityId,
            ["cookie"] = cookie,
            ["searchWord"] = input,
        };
        emptyText.SetLocalText("");
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GroupInviteSearchUser,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg => {
                if (this == null || gameObject == null) {
                    return;
                }

                GroupApplyListRsp<GroupInviteUserInfo> resourceInfo =
                    JsonConvert.DeserializeObject<GroupApplyListRsp<GroupInviteUserInfo>>(msg);
                isEnd = resourceInfo.isEnd == 1;
                cookie = resourceInfo.cookie;
                var newModels = resourceInfo.list != null
                    ? resourceInfo.list.Convert<GroupConsumeBaseInfo>(tmp => {
                        var tmpUserInfo = tmp as GroupInviteUserInfo;
                        if (tmpUserInfo != null) {
                            tmpUserInfo.groupId = groupConsumeInfo?.groupInfo?.groupId;
                        }

                        return tmpUserInfo;
                    }).ToList()
                    : new List<GroupConsumeBaseInfo>();
                adapter.Data.ResetItems(newModels, false);
                adapter.OnItemsUpdated?.Invoke();
                if (newModels.Count == 0) {
                    emptyText.SetLocalText("没有找到用户");
                }
            }, onFail: arg0 => {
                emptyText.SetLocalText("没有找到用户");
            });
    }


    private void HandleFriendPullReleased(float sign) {
        if (sign < 0) {
            RequestFriendList(OnReceivedFriendNewModelsForInsert);
        } else if (sign > 0) {
            GetFirstPageFriendList();
        }
    }

    private void HandleApplyPullReleased(float sign) {
        if (sign < 0) {
            RequestApplyList(OnReceivedApplyNewModelsForInsert);
        } else if (sign > 0) {
            GetFirstPageApplyList();
        }
    }

    private void RequestFriendList(Action<List<GroupConsumeBaseInfo>> onResult) {
        if (isEnd) {
            onResult?.Invoke(new List<GroupConsumeBaseInfo>());
            return;
        }

        var req = new JObject() {
            ["businessId"] = activityId,
            ["cookie"] = cookie,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GroupInviteUserList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg => {
                if (this == null || gameObject == null) {
                    return;
                }

                GroupApplyListRsp<GroupInviteUserInfo> resourceInfo =
                    JsonConvert.DeserializeObject<GroupApplyListRsp<GroupInviteUserInfo>>(msg);
                isEnd = resourceInfo.isEnd == 1;
                cookie = resourceInfo.cookie;
                onResult?.Invoke(resourceInfo.list != null
                    ? resourceInfo.list.Convert<GroupConsumeBaseInfo>(tmp => {
                        var tmpUserInfo = tmp as GroupInviteUserInfo;
                        if (tmpUserInfo != null) {
                            tmpUserInfo.groupId = groupConsumeInfo?.groupInfo?.groupId;
                        }

                        return tmpUserInfo;
                    }).ToList()
                    : new List<GroupConsumeBaseInfo>());
            }, onFail: arg0 => {
                var baseInfos = new List<GroupConsumeBaseInfo>();
#if UNITY_EDITOR && LOCAL_TEST
                baseInfos.Add(new GroupInviteUserInfo() {
                        groupId = groupConsumeInfo?.groupInfo?.groupId,
                        isInvited = 1,
                        userInfo = new AccountUserInfo() {
                            uid = "1815335630306512896",
                            username = "XN16S4",
                            nickname = "哎呦喂",
                            portraitUrl =
                                "https://cdn.budapp.cn/Account/characterInfo/1815335630306512896/1815335630306512896_1722078081.png?imageMogr2/thumbnail/256x256/Account/characterInfo/1815335630306512896/1815335630306512896_1722078081.png",
                        }
                    }
                );
                baseInfos.Add(new GroupInviteUserInfo() {
                        groupId = groupConsumeInfo?.groupInfo?.groupId,
                        isInvited = 0,
                        userInfo = new AccountUserInfo() {
                            uid = "1815575295634673664",
                            username = "78L1W4",
                            nickname = "han_han9",
                            portraitUrl = "https://cdn.budapp.cn/Static/head_nor.png?imageMogr2/thumbnail/256x256/Static/head_nor.png",
                        }
                    }
                );
#endif
                onResult?.Invoke(baseInfos);
            });
    }

    public void GetFirstPageFriendList() {
        isEnd = false;
        cookie = "";
        RequestFriendList(newModels => {
            adapter.Data.ResetItems(newModels, false);
            adapter.OnItemsUpdated?.Invoke();
            if (newModels == null || newModels.Count == 0) {
                emptyText.SetLocalText("你还没有好友");
            }
        });
    }


    void OnReceivedFriendNewModelsForInsert(List<GroupConsumeBaseInfo> newModels) {
        if (newModels == null || newModels.Count == 0) {
            adapter.OnItemsUpdated?.Invoke();
            return;
        }

        adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
        adapter.OnItemsUpdated?.Invoke();
        if (adapter.Data == null || adapter.Data.Count == 0) {
            emptyText.SetLocalText("你还没有好友");
        } else {
            emptyText.SetLocalText("");
        }
    }

    #endregion


    #region Apply

    private void OnSessionToggleChanged(string toggleName) {
        applyType = Enum.Parse<GroupConsumeApplyType>(toggleName);
        isEnd = false;
        cookie = "";
        adapter.SetType(applyType);
        if ((applyType == GroupConsumeApplyType.Applied) && groupConsumeInfo?.captainId != AccountDataManager.Inst.Uid) {
            // 自己不是队长 不显示列表
            adapter.Data.ResetItems(new List<GroupConsumeBaseInfo>(), false);
            adapter.OnItemsUpdated?.Invoke();
            emptyText.SetLocalText("只有队长才能查看队伍申请");
            return;
        }
        emptyText.SetLocalText("");
        GetFirstPageApplyList();
    }


    private void RequestApplyList(Action<List<GroupConsumeBaseInfo>> onResult) {
        if (isEnd) {
            onResult?.Invoke(new List<GroupConsumeBaseInfo>());
            return;
        }

        var req = new JObject() {
            ["cookie"] = cookie,
        };
        var url = HttpUrlDefine.GroupInviteList;

        switch (applyType) {
            case GroupConsumeApplyType.Invited:
                req["activityId"] = activityId;
                url = HttpUrlDefine.GroupInviteList;
                break;
            case GroupConsumeApplyType.Applied:
                req["groupId"] = groupConsumeInfo?.groupInfo?.groupId;
                url = HttpUrlDefine.GroupApplyList;
                break;
            case GroupConsumeApplyType.Leaved:
                req["groupId"] = groupConsumeInfo?.groupInfo?.groupId;
                url = HttpUrlDefine.GroupLeaveList;
                break;
        }

        NetworkManager.Inst.SendHttpRequest(url,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg => {
                if (this == null || gameObject == null) {
                    return;
                }

                GroupApplyListRsp<GroupConsumeApplyInfo> resourceInfo =
                    JsonConvert.DeserializeObject<GroupApplyListRsp<GroupConsumeApplyInfo>>(msg);
                isEnd = resourceInfo.isEnd == 1;
                cookie = resourceInfo.cookie;
                onResult?.Invoke(resourceInfo.list != null
                    ? resourceInfo.list.Convert(tmp => tmp as GroupConsumeBaseInfo).ToList()
                    : new List<GroupConsumeBaseInfo>());
            }, onFail: arg0 => {
                var baseInfos = new List<GroupConsumeBaseInfo>();
#if UNITY_EDITOR && LOCAL_TEST
                baseInfos.Add(new GroupConsumeApplyInfo() {
                        recordId = "1",
                        userInfo = new AccountUserInfo() {
                            uid = "1815335630306512896",
                            username = "XN16S4",
                            nickname = "哎呦喂",
                            portraitUrl =
                                "https://cdn.budapp.cn/Account/characterInfo/1815335630306512896/1815335630306512896_1722078081.png?imageMogr2/thumbnail/256x256/Account/characterInfo/1815335630306512896/1815335630306512896_1722078081.png",
                        }
                    }
                );
                baseInfos.Add(new GroupConsumeApplyInfo() {
                        recordId = "2",
                        userInfo = new AccountUserInfo() {
                            uid = "1815575295634673664",
                            username = "78L1W4",
                            nickname = "han_han9",
                            portraitUrl = "https://cdn.budapp.cn/Static/head_nor.png?imageMogr2/thumbnail/256x256/Static/head_nor.png",
                        }
                    }
                );
#endif


                onResult?.Invoke(baseInfos);
            });
    }


    public void GetFirstPageApplyList() {
        isEnd = false;
        cookie = "";
        RequestApplyList(newModels => {
            adapter.Data.ResetItems(newModels, false);
            adapter.OnItemsUpdated?.Invoke();
            if (newModels == null || newModels.Count == 0) {
                emptyText.SetLocalText(GetEmptyText());
            }
        });
    }


    void OnReceivedApplyNewModelsForInsert(List<GroupConsumeBaseInfo> newModels) {
        if (newModels == null || newModels.Count == 0) {
            adapter.OnItemsUpdated?.Invoke();
            return;
        }

        adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
        adapter.OnItemsUpdated?.Invoke();
        if (adapter.Data == null || adapter.Data.Count == 0) {
            emptyText.SetLocalText(GetEmptyText());
        } else {
            emptyText.SetLocalText("");
        }
    }

    private string GetEmptyText() {
        switch (applyType) {
            case GroupConsumeApplyType.Invited:
                return "你还没有消息";
            case GroupConsumeApplyType.Applied:
                return "你还没有消息";
            case GroupConsumeApplyType.Leaved:
                return "你还没有消息";
        }

        return "";
    }

    #endregion


    private void ResetData() {
        isEnd = false;
        cookie = "";
        adapter.Data.ResetItems(new List<GroupConsumeBaseInfo>());
        adapter.OnItemsUpdated?.Invoke();
    }


    public class GroupInviteUserInfo : GroupConsumeBaseInfo
    {
        public int isInvited;
        public string groupId;
        public int isInGroup;
    }

    public class GroupApplyListRsp<T> where T : GroupConsumeBaseInfo {
        public int isEnd;
        public string cookie;
        public List<T> list;
    }

    public class GroupConsumeApplyInfo : GroupConsumeBaseInfo {
        public string recordId;
    }
}
