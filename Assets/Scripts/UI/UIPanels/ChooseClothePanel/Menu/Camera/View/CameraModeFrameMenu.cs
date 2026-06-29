using UnityEngine;
using System;
using System.Collections.Generic;
using Es;
using Message;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class CameraModeFrameMenu : CameraModeMenuBase
{
    // 供拍照流程读取：当前选中的 Frame 资源路径（为空表示“无”）
    public static string CurrentSelectedFramePath { get; private set; }

    /// <summary>
    /// 外部若需要应用/清空 Frame，可监听该消息。
    /// - arg1: 选中 id，0 表示“无”
    /// - arg2: 选中资源（可能为 null，表示“无”）
    /// </summary>
    public const string Message_CameraModeFrameSelected = "CameraModeFrameSelected";
    private const string CameraModeAtlasPath = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";

    [Header("List Components")]
    [FormerlySerializedAs("emoContentPrefab")]
    [SerializeField] private GameObject cameraResItemPrefab; // 列表 item prefab（建议挂 CameraModeToggle + 文案/图标节点）
    [SerializeField] private ScrollRect frameScrollRect;
    [SerializeField] private Transform frameContent;
    [SerializeField] private Text emptyTip;
    [SerializeField] private RawImage frameItemBg;

    private sealed class ResItemEntry
    {
        public GameObject go;
        public CameraModeToggle toggle;
        public Text nameText;
        public Image iconImage;
        public int id;
        public CameraModeResource cfg;
    }

    private readonly List<ResItemEntry> _activeItems = new List<ResItemEntry>();
    private readonly List<ResItemEntry> _inactiveItems = new List<ResItemEntry>();
    private ToggleGroup _toggleGroup;
    private bool _internalToggleChange;

    private int _selectedFrameId;
    private CameraModeResource _selectedFrameCfg;
    
    protected override void OnInit(){
        // 尝试自动绑定（如果 prefab 没拖引用，也能尽量跑起来）
        if (frameScrollRect == null)
        {
            frameScrollRect = GetComponentByName<ScrollRect>("FrameScrollView");
            frameScrollRect ??= GetComponentByName<ScrollRect>("EmoScrollView");
            frameScrollRect ??= GetComponentInChildren<ScrollRect>(true);
        }
        if (frameContent == null && frameScrollRect != null && frameScrollRect.content != null)
        {
            frameContent = frameScrollRect.content;
        }
        if (emptyTip == null && frameScrollRect != null)
        {
            emptyTip = GameObjectEx.FindComponentByName<Text>(frameScrollRect.transform, "EmptyTip");
        }

        // 默认：选中“无”
        _selectedFrameId = 0;
        _selectedFrameCfg = null;
        CurrentSelectedFramePath = null;
        ApplyFrameBg(null);

        gameObject.SetActive(false); //默认隐藏
    }


    protected override void OnShow()
    {
        RefreshFrameContent();
        // 进入页面时恢复显示当前选中 Frame
        ApplyFrameBg(_selectedFrameCfg);
    }

    protected override void OnHide()
    {
        // 仅隐藏列表（保留缓存，避免频繁 Instantiate）
        SetAllItemUnActive();
    }

    /// <summary>
    /// 还原到默认边框（“无”）：用于退出相机模式时清理显示。
    /// </summary>
    public void ResetToDefault(bool applyNow = true)
    {
        _selectedFrameId = 0;
        _selectedFrameCfg = null;
        CurrentSelectedFramePath = null;

        if (applyNow)
        {
            ApplyFrameBg(null);
        }

        // 如果当前列表已生成过，则同步 Toggle 选中态
        if (_activeItems != null && _activeItems.Count > 0)
        {
            RefreshSelectedToggleState();
        }
    }

    private void RefreshFrameContent()
    {
        SetAllItemUnActive();

        EnsureToggleGroup();

        // 先添加第 0 个“无”
        {
            var entry = GetResItemEntry();
            if (entry != null)
            {
                entry.go.transform.SetSiblingIndex(0);
                BindItem(entry, id: 0, cfg: null, displayName: "无", icon: null);
            }
        }

        List<CameraModeResource> frameList = null;
        try
        {
            var all = DataTables.GetCameraModeResourceList();
            if (all != null)
            {
                frameList = all.FindAll(r => r != null && string.Equals(r.type, "Frame", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception)
        {
            frameList = null;
        }

        if (frameList != null && frameList.Count > 0)
        {
            // id 排序稳定一下（如果配置已经有顺序，也不会乱）
            frameList.Sort((a, b) => a.id.CompareTo(b.id));

            for (int i = 0; i < frameList.Count; i++)
            {
                var cfg = frameList[i];
                var entry = GetResItemEntry();
                if (entry == null) continue;

                // 第 0 个被“无”占了，所以 +1
                entry.go.transform.SetSiblingIndex(i + 1);

                var icon = TryLoadFrameIcon(cfg, entry.go);
                BindItem(entry, cfg.id, cfg, cfg.name, icon);
            }
        }

        if (emptyTip != null)
        {
            // 注意：即使没配置，也会有“无”这一项，所以这里默认隐藏 emptyTip。
            emptyTip.gameObject.SetActive(false);
        }
    }

    private void SelectFrame(int id, CameraModeResource cfg)
    {
        _selectedFrameId = id;
        _selectedFrameCfg = cfg;
        CurrentSelectedFramePath = (cfg != null && !string.IsNullOrEmpty(cfg.resourcePath)) ? cfg.resourcePath : null;

        // 应用/清空 Frame 显示
        ApplyFrameBg(cfg);
        RefreshSelectedToggleState();

        MessageHelper.Broadcast(Message_CameraModeFrameSelected, id, cfg);
    }

    private void SetAllItemUnActive()
    {
        foreach (var entry in _activeItems)
        {
            _inactiveItems.Add(entry);
            if (entry?.go != null) entry.go.SetActive(false);
        }
        _activeItems.Clear();
    }

    private void EnsureToggleGroup()
    {
        if (frameContent == null) return;
        if (_toggleGroup != null) return;
        _toggleGroup = frameContent.GetComponent<ToggleGroup>();
        if (_toggleGroup == null) _toggleGroup = frameContent.gameObject.AddComponent<ToggleGroup>();
        _toggleGroup.allowSwitchOff = false;
    }

    private ResItemEntry GetResItemEntry()
    {
        ResItemEntry entry;
        if (_inactiveItems.Count == 0)
        {
            if (cameraResItemPrefab == null || frameContent == null) return null;
            var go = GameObject.Instantiate(cameraResItemPrefab, frameContent);
            entry = BuildEntry(go);
        }
        else
        {
            entry = _inactiveItems[0];
            _inactiveItems.RemoveAt(0);
            if (entry?.go != null) entry.go.SetActive(true);
        }

        _activeItems.Add(entry);
        return entry;
    }

    private ResItemEntry BuildEntry(GameObject go)
    {
        var entry = new ResItemEntry();
        entry.go = go;
        entry.toggle = go.GetComponentInChildren<CameraModeToggle>(true);
        entry.nameText = GameObjectEx.FindComponentByName<Text>(go.transform, "emoName");
        entry.nameText ??= GameObjectEx.FindComponentByName<Text>(go.transform, "Name");
        entry.nameText ??= go.GetComponentInChildren<Text>(true);

        entry.iconImage = GameObjectEx.FindComponentByName<Image>(go.transform, "emoIcon");
        entry.iconImage ??= GameObjectEx.FindComponentByName<Image>(go.transform, "Icon");
        if (entry.iconImage == null)
        {
            // 兜底：找第一个 Image（尽量避开 On/Off 节点内部的底图）
            var images = go.GetComponentsInChildren<Image>(true);
            if (images != null && images.Length > 0) entry.iconImage = images[0];
        }
        return entry;
        
    }

    private void BindItem(ResItemEntry entry, int id, CameraModeResource cfg, string displayName, Sprite icon)
    {
        if (entry == null || entry.go == null) return;

        entry.id = id;
        entry.cfg = cfg;

        if (entry.nameText != null) entry.nameText.text = string.IsNullOrEmpty(displayName) ? string.Empty : displayName;
        if (entry.iconImage != null)
        {
            entry.iconImage.sprite = icon;
            entry.iconImage.enabled = icon != null;
        }

        // 用 CameraModeToggle 的 On/Off 节点控制表现
        if (entry.toggle != null)
        {
            entry.toggle.onValueChanged.RemoveAllListeners();
            entry.toggle.Init(); // 重新绑定 On/Off 表现

            entry.toggle.group = _toggleGroup;
            entry.toggle.onValueChanged.AddListener(isOn =>
            {
                if (_internalToggleChange) return;
                if (!isOn) return;
                if (!CanUseFrame(entry.cfg))
                {
                    // 非 VIP 用户点击了 VIP 资源：回滚到之前选中项
                    RefreshSelectedToggleState();
                    return;
                }
                SelectFrame(entry.id, entry.cfg);
            });

            // 同步当前选中
            _internalToggleChange = true;
            entry.toggle.isOn = (entry.id == _selectedFrameId);
            _internalToggleChange = false;
        }
    }

    private void RefreshSelectedToggleState()
    {
        _internalToggleChange = true;
        for (int i = 0; i < _activeItems.Count; i++)
        {
            var entry = _activeItems[i];
            if (entry?.toggle == null) continue;
            entry.toggle.isOn = (entry.id == _selectedFrameId);
        }
        _internalToggleChange = false;
    }

    private static bool CanUseFrame(CameraModeResource cfg)
    {
        if (cfg == null || !cfg.isVIP) return true;
        if (VipDataManager.Inst.isVip) return true;

        var joinVipType = new List<JoinVipType> { JoinVipType.VIP_CameraFilter };
        var joinVipTitle = "您正在使用的VIP功能：相机滤镜";
        UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        return false;
    }

    /// <summary>
    /// 将当前选中的 Frame 资源显示到 RawImage 上。
    /// cfg == null 表示“无”，需要隐藏。
    /// </summary>
    private void ApplyFrameBg(CameraModeResource cfg)
    {
        if (frameItemBg == null) return;

        if (cfg == null || string.IsNullOrEmpty(cfg.resourcePath))
        {
            frameItemBg.texture = null;
            frameItemBg.gameObject.SetActive(false);
            return;
        }

        // 按你的要求：必须使用 Loader.Load 加载
        Texture tex = null;
        try
        {
            tex = Loader.Load<Texture>(cfg.resourcePath, frameItemBg.gameObject);
            if (tex == null)
            {
                // 兜底：部分资源可能是 Sprite（仍然用 Loader.Load）
                var sp = Loader.Load<Sprite>(cfg.resourcePath, frameItemBg.gameObject);
                if (sp != null) tex = sp.texture;
            }
        }
        catch (Exception)
        {
            tex = null;
        }

        frameItemBg.texture = tex;
        frameItemBg.gameObject.SetActive(tex != null);
    }

    private static Sprite TryLoadFrameIcon(CameraModeResource cfg, GameObject refObj)
    {
        if (cfg == null || refObj == null) return null;

        // 1) 按需求：iconString 作为 spriteName，从指定图集里取
        if (!string.IsNullOrEmpty(cfg.iconString))
        {
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CameraModeAtlasPath, cfg.iconString, refObj);
            if (sp != null) return sp;

            // 兜底：如果有人仍然填了“资源路径”，则继续兼容
            if (cfg.iconString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.iconString, refObj);
                if (sp != null) return sp;
            }
        }

        // 2) 兜底：如果 resourcePath 是 sprite 路径，也尝试加载
        if (!string.IsNullOrEmpty(cfg.resourcePath) && cfg.resourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            var sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.resourcePath, refObj);
            if (sp != null) return sp;
        }

        return null;
    }
}
