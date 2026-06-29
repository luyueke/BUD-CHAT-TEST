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
    
    private readonly List<GameObject> bgItems = new();
    private Action<int> onSelected;

    public Action OnClosed;

    public void Show(TheatreEditorDataCenter dataCenter, Action<int> onBGSelected)
    {
        onSelected = onBGSelected;
        gameObject.SetActive(true);
        ClearItems();

        if (dataCenter == null || bgListRoot == null || bgItemPrefab == null) return;

        bool isEmpty = dataCenter.AllBackgrounds.Count == 0;
        emptyHint?.SetActive(isEmpty);

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
