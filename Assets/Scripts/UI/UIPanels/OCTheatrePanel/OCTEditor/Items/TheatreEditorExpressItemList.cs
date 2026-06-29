using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorExpressItemList : MonoBehaviour
{
    [SerializeField] private Text avatarNameText;
    [SerializeField] private ScrollRect expressListScroll;
    [SerializeField] private Transform expressRoot;
    [SerializeField] private TheatreEditorAvatarExpressItem itemPrefab;

    private readonly List<TheatreEditorAvatarExpressItem> items = new();

    public void Init(string avatarId, OCTheatreAvatarInfo avatarInfo,
        string selectedAvatarId, string selectedExpressType,
        Action<string, string> onSelected)
    {
        if (avatarNameText != null)
            avatarNameText.text = avatarInfo?.name ?? "";

        ClearItems();
        if (avatarInfo?.expressions == null || itemPrefab == null || expressRoot == null) return;

        foreach (var expr in avatarInfo.expressions)
        {
            if (string.IsNullOrEmpty(expr.expressionURL)) continue;

            var obj = Instantiate(itemPrefab.gameObject, expressRoot);
            var item = obj.GetComponent<TheatreEditorAvatarExpressItem>();
            if (item == null) { Destroy(obj); continue; }

            string exprKey = $"{expr.expressionName}|{expr.mType}";
            item.Init(avatarId, exprKey, expr.expressionName, expr.expressionURL, avatarInfo.name ?? "", onSelected);

            bool preSelected = avatarId == selectedAvatarId
                && exprKey == selectedExpressType
                && !string.IsNullOrEmpty(expr.expressionURL);
            if (preSelected) item.SetSelected(true);

            items.Add(item);
            obj.SetActive(true);
        }
    }

    private const string NarratorImagePath = "Assets/Loadable/UI/UIPanel/OCTheatrePanel/Theatre_narration.png";

    public void InitAsNarrator(bool isPreSelected, Action<string, string> onSelected)
    {
        if (avatarNameText != null)
            avatarNameText.text = "旁白";

        ClearItems();
        if (itemPrefab == null || expressRoot == null) return;

        var obj = Instantiate(itemPrefab.gameObject, expressRoot);
        var item = obj.GetComponent<TheatreEditorAvatarExpressItem>();
        if (item == null) { Destroy(obj); return; }

        item.Init(TheatreEditorDataCenter.NarratorAvatarId, "", "旁白", NarratorImagePath, "旁白", onSelected);
        if (isPreSelected) item.SetSelected(true);
        obj.SetActive(true);
    }

    private void ClearItems()
    {
        foreach (var item in items)
            if (item != null) Destroy(item.gameObject);
        items.Clear();
    }
}
