using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBGSelector : MonoBehaviour
{
    [SerializeField] private ScrollRect bgScrollRect;
    [SerializeField] private Transform bgListRoot;
    [SerializeField] private GameObject bgItemPrefab;
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private Toggle tog_my;
    [SerializeField] private Toggle tog_pgc;

    private const int DefBGCount = 14;
    private const string DefImagePath = "Assets/Loadable/UI/UIPanel/OCTheatrePanel/TheatreDefaultBG/";

    private static readonly string[] DefBgNames =
    {
        "车站", "废弃医院", "审讯室", "路口", "废弃小屋", "便利店", "海边",
        "小屋", "公园", "天台", "餐馆", "停车场", "客厅", "小院"
    };

    private readonly List<GameObject> bgItems = new();
    private Action<int> onSelected;
    private TheatreEditorDataCenter dataCenter;

    public Action OnClosed;

    private bool IsPgcMode => tog_pgc != null && tog_pgc.isOn;

    public void Show(TheatreEditorDataCenter dc, Action<int> onBGSelected)
    {
        onSelected = onBGSelected;
        dataCenter = dc;
        gameObject.SetActive(true);

        if (tog_my != null)
        {
            tog_my.onValueChanged.RemoveListener(OnMyToggleChanged);
            tog_my.onValueChanged.AddListener(OnMyToggleChanged);
        }
        if (tog_pgc != null)
        {
            tog_pgc.onValueChanged.RemoveListener(OnPgcToggleChanged);
            tog_pgc.onValueChanged.AddListener(OnPgcToggleChanged);
        }

        // 默认开启 tog_my
        if (tog_my != null) tog_my.isOn = true;

        RefreshList();
    }

    private void OnMyToggleChanged(bool isOn)
    {
        if (isOn) RefreshList();
    }

    private void OnPgcToggleChanged(bool isOn)
    {
        if (isOn) RefreshList();
    }

    private void RefreshList()
    {
        ClearItems();

        if (dataCenter == null || bgListRoot == null || bgItemPrefab == null) return;

        // 始终有背景可选，不再显示空状态
        emptyHint?.SetActive(false);

        if (IsPgcMode)
        {
            // tog_pgc：内置默认背景
            for (int i = 0; i < DefBGCount; i++)
            {
                var obj = Instantiate(bgItemPrefab, bgListRoot);
                var item = obj.GetComponent<TheatreEditorBGItem>();
                string defPath = $"{DefImagePath}TheatreDefaultBG{i}.png";
                item?.InitAsDef(defPath, DefBgNames[i]);

                string capturedPath = defPath;
                item?.SetSelectCallback(() =>
                {
                    // 复用已有记录或追加到 AllBackgrounds，保持 int 索引体系不变
                    int existingIdx = dataCenter.AllBackgrounds.IndexOf(capturedPath);
                    int bgIdx;
                    if (existingIdx >= 0)
                    {
                        bgIdx = existingIdx;
                    }
                    else
                    {
                        dataCenter.AddBackground(capturedPath);
                        bgIdx = dataCenter.AllBackgrounds.Count - 1;
                    }
                    onSelected?.Invoke(bgIdx);
                    gameObject.SetActive(false);
                    OnClosed?.Invoke();
                });

                bgItems.Add(obj);
            }
            return;
        }

        // tog_my：后端返回的用户背景
        for (int i = 0; i < dataCenter.AllBackgrounds.Count; i++)
        {
            var obj = Instantiate(bgItemPrefab, bgListRoot);
            var item = obj.GetComponent<TheatreEditorBGItem>();
            item?.InitAsFilled(dataCenter.AllBackgrounds[i]);

            int idx = i;
            item?.SetSelectCallback(() =>
            {
                onSelected?.Invoke(idx);
                gameObject.SetActive(false);
                OnClosed?.Invoke();
            });

            bgItems.Add(obj);
        }
    }

    private void ClearItems()
    {
        foreach (var item in bgItems) { if (item != null) Destroy(item); }
        bgItems.Clear();
    }
}
