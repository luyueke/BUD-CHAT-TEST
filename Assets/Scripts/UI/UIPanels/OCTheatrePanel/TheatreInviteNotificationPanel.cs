using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;

public class TheatreInviteNotificationPanel : BasePanel<TheatreInviteNotificationPanel>
{
    [SerializeField] private Transform notificationRoot;
    [SerializeField] private GameObject inviteItemPrefab;

    private const int MaxNotifications = 10;
    private readonly HashSet<string> _activeKeys = new HashSet<string>();

    public void AddNotification(string hostUid, string roomId, string theatreName)
    {
        string key = $"{hostUid}:{roomId}";
        if (_activeKeys.Contains(key)) return;
        _activeKeys.Add(key);

        if (notificationRoot.childCount >= MaxNotifications)
        {
            var oldest = notificationRoot.GetChild(0).gameObject;
            var oldKey = oldest.GetComponent<TheatreAccpetInviteItem>()?.Key;
            if (oldKey != null) _activeKeys.Remove(oldKey);
            Destroy(oldest);
        }

        var go = Instantiate(inviteItemPrefab, notificationRoot);
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        var item = go.GetComponent<TheatreAccpetInviteItem>();
        item?.SetData(hostUid, theatreName, roomId,
            (h, r) => RemoveItem(go, key, () => OnAccept(h, r)),
            () => RemoveItem(go, key, null));

        StartCoroutine(AutoDismiss(go, key, 30f));
        RefreshItemColors();
    }

    private void RemoveItem(GameObject go, string key, System.Action onRemoved)
    {
        _activeKeys.Remove(key);
        if (go != null) Destroy(go);
        onRemoved?.Invoke();
        StartCoroutine(CleanupAfterFrame());
    }

    private IEnumerator CleanupAfterFrame()
    {
        yield return null;
        RefreshItemColors();
        if (notificationRoot.childCount == 0) CloseSelf();
    }

    private void OnAccept(string hostUid, string roomId)
    {
        // AcceptInvite 内部把 op=2 缓存的剧场信息写入 CurrentTheatreInfo，
        // 并设置 CurrentRoomId/HostUid，最后广播 op=3。
        // 返回 false 表示邀请已失效（房主已退出 → pending 已被 op=4 清理；或本端已在其他房间）。
        if (!TheatreGameManager.Inst.AcceptInvite(roomId, hostUid))
        {
            TipPanel.ShowToast("房间已关闭");
            CloseSelf();
            return;
        }
        // 第二个参数 false 标识受邀入口：TheatreGameRoomPanel.OnShow 不会再误调用 CreateRoom
        UIManager.Inst.OpenPanel(PanelId.TheatreGameRoomPanel, TheatreGameManager.Inst.CurrentTheatreInfo, false);
        CloseSelf();
    }

    private void RefreshItemColors()
    {
        int count = notificationRoot.childCount;
        for (int i = 0; i < count; i++)
        {
            var item = notificationRoot.GetChild(i).GetComponent<TheatreAccpetInviteItem>();
            item?.SetHighlight(i == count - 1);
        }
    }

    private IEnumerator AutoDismiss(GameObject go, string key, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null)
        {
            _activeKeys.Remove(key);
            Destroy(go);
            yield return null;
            RefreshItemColors();
            if (notificationRoot.childCount == 0) CloseSelf();
        }
    }
}
