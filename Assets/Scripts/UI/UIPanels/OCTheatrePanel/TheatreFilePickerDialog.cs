// 临时测试用文件选择器 —— 后续移除
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纯代码构建的文件选择弹窗，无需 Prefab / PanelId 注册。
/// 仅供实机测试 JSON 导入使用，后续功能稳定后可整体删除。
/// 用法：TheatreFilePickerDialog.Show(onActors, onDetail)
/// </summary>
public class TheatreFilePickerDialog : MonoBehaviour
{
    // ── 公开入口 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 弹出文件选择器。用户依次选择 actors.json 和 detail.json 后触发回调。
    /// </summary>
    /// <param name="onConfirm">两个文件绝对路径 (actorsPath, detailPath)</param>
    public static void Show(Action<string, string> onConfirm)
    {
        var go = new GameObject("[TheatreFilePickerDialog]");
        DontDestroyOnLoad(go);
        var dialog = go.AddComponent<TheatreFilePickerDialog>();
        dialog._onConfirm = onConfirm;
        dialog.Build();
    }

    // ── 内部状态 ──────────────────────────────────────────────────────────────

    private Action<string, string> _onConfirm;

    // 当前选定的两个文件路径
    private string _actorsPath;
    private string _detailPath;

    // 分两步选择：先 actors，再 detail
    private enum Step { PickActors, PickDetail, Done }
    private Step _step = Step.PickActors;

    // 当前展示的目录
    private string _currentDir;

    // 文件列表区
    private Transform _fileListContent;
    private Text _stepLabel;
    private Text _selectedActorsLabel;
    private Text _selectedDetailLabel;
    private Button _confirmBtn;

    // ── 构建 UI ───────────────────────────────────────────────────────────────

    private void Build()
    {
        _currentDir = Application.persistentDataPath;

        // 全屏遮罩 Canvas
        var canvas = CreateCanvas();

        // 背景遮罩
        var overlay = CreatePanel(canvas.transform, new Color(0, 0, 0, 0.6f));
        overlay.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        AddClickToClose(overlay);

        // 主卡片（居中）
        var card = CreatePanel(canvas.transform, new Color(0.12f, 0.12f, 0.15f, 1f));
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.08f, 0.1f);
        cardRt.anchorMax = new Vector2(0.92f, 0.9f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;

        // 阻止点击穿透
        card.AddComponent<Button>().onClick.AddListener(() => { });

        BuildCardContent(cardRt);
        RefreshFileList();
    }

    private void BuildCardContent(RectTransform parent)
    {
        // 标题栏
        var titleBar = CreatePanel(parent, new Color(0.08f, 0.08f, 0.1f, 1f));
        var tbRt = titleBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0, 1);
        tbRt.anchorMax = new Vector2(1, 1);
        tbRt.pivot = new Vector2(0.5f, 1);
        tbRt.sizeDelta = new Vector2(0, 90);
        tbRt.anchoredPosition = Vector2.zero;

        var titleText = CreateText(tbRt, "选择 JSON 文件", 28, TextAnchor.MiddleCenter, Color.white);
        StretchFill(titleText.GetComponent<RectTransform>(), new Vector2(8, 0), new Vector2(-60, 0));

        // 关闭按钮
        var closeBtn = CreateButton(tbRt, "✕", 28, new Color(0.8f, 0.2f, 0.2f));
        var closeBtnRt = closeBtn.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1, 0.5f);
        closeBtnRt.anchorMax = new Vector2(1, 0.5f);
        closeBtnRt.pivot = new Vector2(1, 0.5f);
        closeBtnRt.sizeDelta = new Vector2(90, 90);
        closeBtnRt.anchoredPosition = Vector2.zero;
        closeBtn.onClick.AddListener(CloseSelf);

        // 步骤提示
        var stepBar = CreatePanel(parent, new Color(0.15f, 0.15f, 0.18f, 1f));
        var sbRt = stepBar.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0, 1);
        sbRt.anchorMax = new Vector2(1, 1);
        sbRt.pivot = new Vector2(0.5f, 1);
        sbRt.sizeDelta = new Vector2(0, 64);
        sbRt.anchoredPosition = new Vector2(0, -90);
        _stepLabel = CreateText(sbRt, "", 24, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
        StretchFill(_stepLabel.GetComponent<RectTransform>(), new Vector2(12, 0), new Vector2(-12, 0));

        // 已选信息栏
        var infoBar = CreatePanel(parent, new Color(0.1f, 0.14f, 0.1f, 1f));
        var ibRt = infoBar.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0, 1);
        ibRt.anchorMax = new Vector2(1, 1);
        ibRt.pivot = new Vector2(0.5f, 1);
        ibRt.sizeDelta = new Vector2(0, 100);
        ibRt.anchoredPosition = new Vector2(0, -154);

        _selectedActorsLabel = CreateText(ibRt, "actors.json : —", 22, TextAnchor.MiddleLeft, new Color(0.7f, 1f, 0.7f));
        var alRt = _selectedActorsLabel.GetComponent<RectTransform>();
        alRt.anchorMin = new Vector2(0, 0.5f);
        alRt.anchorMax = new Vector2(1, 1);
        alRt.offsetMin = new Vector2(12, 0);
        alRt.offsetMax = new Vector2(-12, 0);

        _selectedDetailLabel = CreateText(ibRt, "detail.json  : —", 22, TextAnchor.MiddleLeft, new Color(0.7f, 0.9f, 1f));
        var dlRt = _selectedDetailLabel.GetComponent<RectTransform>();
        dlRt.anchorMin = new Vector2(0, 0);
        dlRt.anchorMax = new Vector2(1, 0.5f);
        dlRt.offsetMin = new Vector2(12, 0);
        dlRt.offsetMax = new Vector2(-12, 0);

        // 路径导航栏
        var navBar = CreatePanel(parent, new Color(0.1f, 0.1f, 0.12f, 1f));
        var nbRt = navBar.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(0, 1);
        nbRt.anchorMax = new Vector2(1, 1);
        nbRt.pivot = new Vector2(0.5f, 1);
        nbRt.sizeDelta = new Vector2(0, 64);
        nbRt.anchoredPosition = new Vector2(0, -254);

        var upBtn = CreateButton(nbRt, "↑ 上级", 24, new Color(0.3f, 0.5f, 0.8f));
        var ubRt = upBtn.GetComponent<RectTransform>();
        ubRt.anchorMin = new Vector2(0, 0);
        ubRt.anchorMax = new Vector2(0, 1);
        ubRt.pivot = new Vector2(0, 0.5f);
        ubRt.sizeDelta = new Vector2(130, 0);
        ubRt.anchoredPosition = new Vector2(8, 0);
        upBtn.onClick.AddListener(GoUp);

        // 文件滚动列表
        float topOffset = 90 + 64 + 100 + 64; // title+step+info+nav
        float bottomOffset = 80;
        var scrollView = new GameObject("ScrollView").AddComponent<ScrollRect>();
        var svRt = scrollView.GetComponent<RectTransform>();
        svRt.SetParent(parent, false);
        svRt.anchorMin = new Vector2(0, 0);
        svRt.anchorMax = new Vector2(1, 1);
        svRt.offsetMin = new Vector2(0, bottomOffset);
        svRt.offsetMax = new Vector2(0, -topOffset);
        scrollView.vertical = true;
        scrollView.horizontal = false;

        // Viewport（负责裁剪）
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.SetParent(scrollView.transform, false);
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        scrollView.viewport = vpRt;

        var content = new GameObject("Content").AddComponent<RectTransform>();
        content.SetParent(viewport.transform, false);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, 0);
        scrollView.content = content;

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _fileListContent = content;

        // 确认按钮
        var confirmBar = CreatePanel(parent, new Color(0.08f, 0.08f, 0.1f, 1f));
        var cbRt = confirmBar.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0, 0);
        cbRt.anchorMax = new Vector2(1, 0);
        cbRt.pivot = new Vector2(0.5f, 0);
        cbRt.sizeDelta = new Vector2(0, bottomOffset);
        cbRt.anchoredPosition = Vector2.zero;

        _confirmBtn = CreateButton(cbRt, "导入", 26, new Color(0.15f, 0.6f, 0.25f));
        var confRt = _confirmBtn.GetComponent<RectTransform>();
        confRt.anchorMin = new Vector2(0.5f, 0.1f);
        confRt.anchorMax = new Vector2(0.5f, 0.9f);
        confRt.pivot = new Vector2(0.5f, 0.5f);
        confRt.sizeDelta = new Vector2(180, 0);
        confRt.anchoredPosition = Vector2.zero;
        _confirmBtn.onClick.AddListener(OnConfirm);
        _confirmBtn.interactable = false;

        UpdateLabels();
    }

    // ── 文件列表刷新 ──────────────────────────────────────────────────────────

    private void RefreshFileList()
    {
        // 清空旧列表
        foreach (Transform child in _fileListContent)
            Destroy(child.gameObject);

        // 子目录
        try
        {
            var dirs = Directory.GetDirectories(_currentDir);
            foreach (var dir in dirs)
                AddRow(Path.GetFileName(dir), true, dir);
        }
        catch { }

        // .json 文件
        try
        {
            var files = Directory.GetFiles(_currentDir, "*.json");
            foreach (var file in files)
                AddRow(Path.GetFileName(file), false, file);
        }
        catch { }
    }

    private void AddRow(string label, bool isDir, string fullPath)
    {
        var row = new GameObject("Row_" + label);
        row.transform.SetParent(_fileListContent, false);

        var rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 72);

        var bg = row.AddComponent<Image>();
        bg.color = isDir ? new Color(0.18f, 0.18f, 0.22f) : new Color(0.14f, 0.2f, 0.14f);

        var btn = row.AddComponent<Button>();

        var txt = CreateText(rt, (isDir ? "[目录] " : "[JSON] ") + label, 24, TextAnchor.MiddleLeft, Color.white);
        StretchFill(txt.GetComponent<RectTransform>(), new Vector2(16, 4), new Vector2(-8, -4));

        if (isDir)
        {
            btn.onClick.AddListener(() => { _currentDir = fullPath; RefreshFileList(); });
        }
        else
        {
            btn.onClick.AddListener(() => OnFileTap(fullPath));

            // 高亮已选
            bool isActors = fullPath == _actorsPath;
            bool isDetail = fullPath == _detailPath;
            if (isActors) bg.color = new Color(0.2f, 0.45f, 0.2f);
            if (isDetail) bg.color = new Color(0.15f, 0.35f, 0.5f);
        }
    }

    private void GoUp()
    {
        var parent = Directory.GetParent(_currentDir);
        if (parent != null)
        {
            _currentDir = parent.FullName;
            RefreshFileList();
        }
    }

    // ── 文件选择逻辑 ──────────────────────────────────────────────────────────

    private void OnFileTap(string path)
    {
        if (_step == Step.PickActors)
        {
            _actorsPath = path;
            _step = Step.PickDetail;
        }
        else if (_step == Step.PickDetail)
        {
            _detailPath = path;
            _step = Step.Done;
        }

        _confirmBtn.interactable = !string.IsNullOrEmpty(_actorsPath) && !string.IsNullOrEmpty(_detailPath);
        UpdateLabels();
        RefreshFileList();
    }

    private void UpdateLabels()
    {
        if (_stepLabel == null) return;

        switch (_step)
        {
            case Step.PickActors:
                _stepLabel.text = "第 1 步：点击选择 actors.json";
                break;
            case Step.PickDetail:
                _stepLabel.text = "第 2 步：点击选择 detail.json";
                break;
            case Step.Done:
                _stepLabel.text = "✓ 已选择完毕，点击「导入」";
                break;
        }

        _selectedActorsLabel.text = "actors.json : " +
            (string.IsNullOrEmpty(_actorsPath) ? "—" : Path.GetFileName(_actorsPath));
        _selectedDetailLabel.text = "detail.json  : " +
            (string.IsNullOrEmpty(_detailPath) ? "—" : Path.GetFileName(_detailPath));
    }

    private void OnConfirm()
    {
        if (string.IsNullOrEmpty(_actorsPath) || string.IsNullOrEmpty(_detailPath)) return;
        var actors = _actorsPath;
        var detail = _detailPath;
        CloseSelf();
        _onConfirm?.Invoke(actors, detail);
    }

    private void CloseSelf()
    {
        Destroy(gameObject);
    }

    // ── UI 辅助工厂 ──────────────────────────────────────────────────────────

    private Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9999;
        var cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight = 0.5f;
        return c;
    }

    private GameObject CreatePanel(Transform parent, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private void AddClickToClose(GameObject go)
    {
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(CloseSelf);
    }

    private Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = color;
        t.raycastTarget = false;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    private Button CreateButton(Transform parent, string label, int fontSize, Color bgColor)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var txt = CreateText(go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        StretchFill(txt.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        return btn;
    }

    private void StretchFill(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
