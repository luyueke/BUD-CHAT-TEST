using System;
using Es;
using Game.Database;
using Message;
using Product;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Camera 自拍姿势列表 Item（用于 UIEmoteType.CameraSelfiePose）。
/// 仅负责展示：图标/名字/价格/已拥有；点击逻辑后续再补。
/// </summary>
public class CameraSelfieItem : MonoBehaviour
{
    private const string CameraModeAtlasPath = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";

    [Header("UI Refs")]
    [SerializeField] private CButton btn;
    [SerializeField] private Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text priceText;
    [SerializeField] private GameObject notOwnedRoot;
    [SerializeField] private Text jumpText;
    [SerializeField] private GameObject selectedRoot;

    private CameraSelfiePose _data;
    private Action<bool, string> _onClick;
    private bool isOwned = true;
    private bool enableBroadcast = true;

    public string GetId(){
        return _data?.id;
    }

    public void InitData(CameraSelfiePose data, Action<bool,string> onClick = null)
    {
        _data = data;
        _onClick = onClick;
        enableBroadcast = true;

        if (nameText != null)
        {
            nameText.SetLocalText(_data != null ? _data.name : string.Empty);
        }

        if (iconImage != null)
        {
            var sp = TryLoadIcon(_data, gameObject);
            iconImage.sprite = sp;
            iconImage.enabled = sp != null;
        }

        if(selectedRoot != null){
            selectedRoot.SetActive(false);
        }

        RefreshOwnStatus();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    public void SetOwned(bool owned)
    {
        if (notOwnedRoot != null) notOwnedRoot.SetActive(!owned);
    }

    public void ForceOwnedForPreview(bool owned = true)
    {
        isOwned = owned;
        SetOwned(owned);
    }

    public void SetSelected(bool selected)
    {
        if (selectedRoot != null) selectedRoot.SetActive(selected);
    }

    public void InitDefaultData(string displayName, string iconSpriteName, Action<bool,string> onClick = null)
    {
        _data = null;
        _onClick = onClick;
        enableBroadcast = true;

        if (nameText != null)
        {
            nameText.SetLocalText(displayName ?? string.Empty);
        }

        if(selectedRoot != null){
            selectedRoot.SetActive(false);
        }

        if (iconImage != null)
        {
            Sprite sp = null;
            if (!string.IsNullOrEmpty(iconSpriteName))
            {
                sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CameraModeAtlasPath, iconSpriteName, gameObject);
            }
            iconImage.sprite = sp;
            iconImage.enabled = sp != null;
        }

        if (priceText != null)
        {
            priceText.SetLocalText("免费");
        }

        SetOwned(true);

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    public void RefreshOwnStatus()
    {
        if(_data != null){
            isOwned = BagDatabase.Inst.Select(_data.id) != null;
        }

        SetOwned(isOwned);
        if(!isOwned && _data.gainType != 1){
            jumpText.gameObject.SetActive(true);
            if(_data.gainType == (int)StoreSkipType.GiftPack){
                jumpText.SetLocalText("前往礼包获得");
            }else if(_data.gainType == (int)StoreSkipType.LimitedEvents){
                jumpText.SetLocalText("前往活动获得");
            }

        }
    }

    public void SetBroadcastEnabled(bool enabled)
    {
        enableBroadcast = enabled;
    }

    private void OnClick()
    {
        if(_data == null){
            _onClick?.Invoke(true, null);
        }else if(isOwned){
            _onClick?.Invoke(true, _data.id);
        }else{
            _onClick?.Invoke(false, _data.id);
            return;
        }
        SetSelected(true);

        if (enableBroadcast)
        {
            if(_data != null){
                MessageHelper.Broadcast(MessageName.UICameraModeSelfieRequest, _data.id);
            }else{
                MessageHelper.Broadcast(MessageName.UICameraModeSelfieRequest, true);
            }
        }
        
    }

    private static Sprite TryLoadIcon(CameraSelfiePose cfg, GameObject refObj)
    {
        if (cfg == null || refObj == null) return null;

        // iconString 优先：作为 spriteName 从图集取（与 Frame/Filter 菜单一致）
        if (!string.IsNullOrEmpty(cfg.iconString))
        {
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(CameraModeAtlasPath, cfg.iconString, refObj);
            if (sp != null) return sp;

            // 兜底：兼容有人填了资源路径
            if (cfg.iconString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.iconString, refObj);
                if (sp != null) return sp;
            }
        }

        // 兜底：resourcePath 如果也是 sprite 路径，也尝试加载
        if (!string.IsNullOrEmpty(cfg.resourcePath) && cfg.resourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            var sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(cfg.resourcePath, refObj);
            if (sp != null) return sp;
        }

        return null;
    }
}

