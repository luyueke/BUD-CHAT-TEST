using UnityEngine;
using System;
using System.Collections.Generic;
using Es;
using Game.MapSetting;
using Game.Props.PropsComponents;
using UnityEngine.UI;

public class CameraModeFilterMenu : CameraModeMenuBase
{
    public const string Message_CameraModeFilterSelected = "CameraModeFilterSelected";
    private const string CameraModeAtlasPath = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";

    [Header("List Components")]
    [SerializeField] private GameObject resItemPrefab; // 列表 item prefab（建议挂 CameraModeToggle + 文案/图标节点）
    [SerializeField] private ScrollRect filterScrollRect;
    [SerializeField] private Transform filterContent;
    [SerializeField] private Text emptyTip;

    private sealed class ResItemEntry
    {
        public GameObject go;
        public CameraModeToggle toggle;
        public Text nameText;
        public Image iconImage;
        public GameObject vipGo;
        public int id;
        public CameraModeResource cfg;
    }

    private readonly List<ResItemEntry> _activeItems = new List<ResItemEntry>();
    private readonly List<ResItemEntry> _inactiveItems = new List<ResItemEntry>();
    private ToggleGroup _toggleGroup;
    private bool _internalToggleChange;

    private int _selectedFilterId;
    private CameraModeResource _selectedFilterCfg;
    
    // 进入相机滤镜面板时的默认环境色（用于选择“无”/退出时还原）
    private SkyboxColor _defaultEnvColor;
    private bool _defaultEnvColorInited;

    protected override void OnInit(){
        if (filterScrollRect == null)
        {
            filterScrollRect = GetComponentByName<ScrollRect>("FilterScrollView");
            filterScrollRect ??= GetComponentByName<ScrollRect>("EmoScrollView");
            filterScrollRect ??= GetComponentInChildren<ScrollRect>(true);
        }
        if (filterContent == null && filterScrollRect != null && filterScrollRect.content != null)
        {
            filterContent = filterScrollRect.content;
        }
        if (emptyTip == null && filterScrollRect != null)
        {
            emptyTip = GameObjectEx.FindComponentByName<Text>(filterScrollRect.transform, "EmptyTip");
        }

        _selectedFilterId = 0;
        _selectedFilterCfg = null;
        _defaultEnvColorInited = false;

        gameObject.SetActive(false); //默认隐藏
    }


    protected override void OnShow()
    {
        if (!IsCameraModeFeatureVersionSupported())
        {
            gameObject.SetActive(false);
            return;
        }

        EnsureDefaultEnvColor();
        RefreshFilterContent();
        ApplyFilter(_selectedFilterCfg);
    }

    protected override void OnHide()
    {
        SetAllItemUnActive();
    }

    /// <summary>
    /// 还原到默认滤镜（“无”）：用于退出相机模式时清理效果。
    /// </summary>
    public void ResetToDefault(bool applyNow = true)
    {
        _selectedFilterId = 0;
        _selectedFilterCfg = null;

        if (applyNow)
        {
            // 直接还原默认 Volume
            ApplyFilter(null);
        }
        else
        {
            // 退出相机模式时，上层可能已统一还原 Volume；
            // 这里至少把环境光颜色还原，避免残留。
            ApplyEnvColor(null);
        }

        // 如果当前列表已生成过，则同步 Toggle 选中态
        if (_activeItems != null && _activeItems.Count > 0)
        {
            RefreshSelectedToggleState();
        }
    }

    private void RefreshFilterContent()
    {
        SetAllItemUnActive();
        EnsureToggleGroup();

        // 第 0 个“无”
        {
            var entry = GetResItemEntry();
            if (entry != null)
            {
                entry.go.transform.SetSiblingIndex(0);
                var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CameraModeAtlasPath, "Filter1", entry.go);
                BindItem(entry, 0, null, "原图", sp);
            }
        }

        List<CameraModeResource> list = null;
        try
        {
            var all = DataTables.GetCameraModeResourceList();
            if (all != null)
            {
                list = all.FindAll(r => r != null && string.Equals(r.type, "Filter", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch
        {
            list = null;
        }

        if (list != null && list.Count > 0)
        {
            list.Sort((a, b) => a.id.CompareTo(b.id));
            for (int i = 0; i < list.Count; i++)
            {
                var cfg = list[i];
                var entry = GetResItemEntry();
                if (entry == null) continue;
                entry.go.transform.SetSiblingIndex(i + 1);

                var icon = TryLoadIcon(cfg, entry.go);
                BindItem(entry, cfg.id, cfg, cfg.name, icon);
            }
        }

        if (emptyTip != null) emptyTip.gameObject.SetActive(false);
    }

    private void SelectFilter(int id, CameraModeResource cfg)
    {
        if (!IsCameraModeFeatureVersionSupported())
        {
            return;
        }

        _selectedFilterId = id;
        _selectedFilterCfg = cfg;

        ApplyFilter(cfg);
        RefreshSelectedToggleState();

        Message.MessageHelper.Broadcast(Message_CameraModeFilterSelected, id, cfg);
    }

    private void ApplyFilter(CameraModeResource cfg)
    {
        ApplyEnvColor(cfg);

        // “无”：还原默认 Volume
        if (cfg == null || string.IsNullOrEmpty(cfg.resourcePath))
        {
            PostProcessManager.Inst.RestoreDefaultVolume();
            return;
        }

        // 配置里 resourcePath 是 Volume 相关资源（VolumeProfile 或带 Volume 的 Prefab）
        PostProcessManager.Inst.TryOverrideVolume(cfg.resourcePath, gameObject);
    }

    private void EnsureDefaultEnvColor()
    {
        if (_defaultEnvColorInited) return;

        try
        {
            var comp = SkyboxManager.Inst != null ? SkyboxManager.Inst.GetComp() : null;
            var c = comp != null ? comp.skyboxColor : null;
            if (c != null)
            {
                _defaultEnvColor = new SkyboxColor()
                {
                    sky = c.sky,
                    equator = c.equator,
                    ground = c.ground,
                };
                _defaultEnvColorInited = true;
            }
        }
        catch
        {
            _defaultEnvColorInited = false;
        }
    }

    private void ApplyEnvColor(CameraModeResource cfg)
    {
        EnsureDefaultEnvColor();

        // “无” 或没配置：还原默认环境色
        if (cfg == null || string.IsNullOrEmpty(cfg.extraValue))
        {
            if (_defaultEnvColorInited && SkyboxManager.Inst != null && SkyboxManager.Inst.GetComp() != null)
            {
                SkyboxManager.Inst.SetSkyboxColor(_defaultEnvColor);
            }
            return;
        }

        try
        {
            // extraValue 对应 NormalSkyboxData 的 _Id
            var skyCfg = DataTables.GetNormalSkyboxData(cfg.extraValue);
            if (skyCfg == null) return;

            var color = new SkyboxColor()
            {
                sky = skyCfg.Sky,
                equator = skyCfg.Equator,
                ground = skyCfg.Ground,
            };

            if (SkyboxManager.Inst != null && SkyboxManager.Inst.GetComp() != null)
            {
                SkyboxManager.Inst.SetSkyboxColor(color);
            }
        }
        catch
        {
            // ignore
        }
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
        if (filterContent == null) return;
        if (_toggleGroup != null) return;
        _toggleGroup = filterContent.GetComponent<ToggleGroup>();
        if (_toggleGroup == null) _toggleGroup = filterContent.gameObject.AddComponent<ToggleGroup>();
        _toggleGroup.allowSwitchOff = false;
    }

    private ResItemEntry GetResItemEntry()
    {
        ResItemEntry entry;
        if (_inactiveItems.Count == 0)
        {
            if (resItemPrefab == null || filterContent == null) return null;
            var go = GameObject.Instantiate(resItemPrefab, filterContent);
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
            var images = go.GetComponentsInChildren<Image>(true);
            if (images != null && images.Length > 0) entry.iconImage = images[0];
        }
        var vipTf = FindChildByNameRecursive(go.transform, "Vip");
        entry.vipGo = vipTf != null ? vipTf.gameObject : null;
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
        if (entry.vipGo != null)
        {
            entry.vipGo.SetActive(cfg != null && cfg.isVIP);
        }

        if (entry.toggle != null)
        {
            entry.toggle.onValueChanged.RemoveAllListeners();
            entry.toggle.Init();
            entry.toggle.group = _toggleGroup;
            entry.toggle.onValueChanged.AddListener(isOn =>
            {
                if (_internalToggleChange) return;
                if (!isOn) return;
                if (!CanUseFilter(entry.cfg))
                {
                    // 非 VIP 点击 VIP 滤镜时，回滚到之前选中项
                    RefreshSelectedToggleState();
                    return;
                }
                SelectFilter(entry.id, entry.cfg);
            });

            _internalToggleChange = true;
            entry.toggle.isOn = (entry.id == _selectedFilterId);
            _internalToggleChange = false;
        }
    }

    private static bool CanUseFilter(CameraModeResource cfg)
    {
        if (cfg == null || !cfg.isVIP) return true;
        if (VipDataManager.Inst.isVip) return true;

        var joinVipType = new List<JoinVipType> { JoinVipType.VIP_CameraFilter };
        var joinVipTitle = "您正在使用的VIP功能：相机滤镜";
        UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        return false;
    }

    private static Transform FindChildByNameRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindChildByNameRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    private void RefreshSelectedToggleState()
    {
        _internalToggleChange = true;
        for (int i = 0; i < _activeItems.Count; i++)
        {
            var entry = _activeItems[i];
            if (entry?.toggle == null) continue;
            entry.toggle.isOn = (entry.id == _selectedFilterId);
        }
        _internalToggleChange = false;
    }

    private static Sprite TryLoadIcon(CameraModeResource cfg, GameObject refObj)
    {
        if (cfg == null || refObj == null) return null;

        if (!string.IsNullOrEmpty(cfg.iconString))
        {
            // 按需求：iconString 作为 spriteName，从指定图集里取
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CameraModeAtlasPath, cfg.iconString, refObj);
            if (sp != null) return sp;

            // 兜底：兼容老配置写了资源路径
            if (cfg.iconString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.iconString, refObj);
                if (sp != null) return sp;
            }
        }

        return null;
    }

    private bool IsCameraModeFeatureVersionSupported()
    {
        if (DeviceInfoManager.Inst != null
            && DeviceInfoManager.Inst.DeviceBaseData != null
            && DeviceInfoManager.Inst.CheckVersion_1_0_18())
        {
            return true;
        }
        UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
        //TipPanel.ShowToast("该功能仅在1.0.18及以上版本可用");
        return false;
    }
}
