using System;
using System.Collections;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeViewWardrobelistItem : MonoBehaviour
{
    Transform img_add;
    Transform img_def;
    RemoteImageBehaviour Rm_Cover;
    Text txt_name;
    CButton btn_replace;
    CButton btn_del;
    CButton cbtn_rename;
    CButton btn_icon;
    Transform btns;
    CButton btnsMask;
    Transform img_select;

    private bool _initialized;
    private bool _btnsJustShown;
    public OTCAvatarClothes ClothesData => _clothesData;
    private OTCAvatarClothes _clothesData;
    public GameObject _loadingObj;

    private void Update()
    {
        if (!btns.gameObject.activeSelf) return;
        if (_btnsJustShown) { _btnsJustShown = false; return; }
        if (!Input.GetMouseButtonDown(0)) return;
        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = Input.mousePosition };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
        if (results.Exists(r => r.gameObject.transform.IsChildOf(btns))) return;
        btns.gameObject.SetActive(false);
        img_select.gameObject.SetActive(false);
        MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, null);
    }

    private void Awake() => EnsureInit();

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        img_select = GameObjectEx.FindChildByName(transform,"img_select");
        img_add = GameObjectEx.FindChildByName(transform, "img_add");
        img_def = GameObjectEx.FindChildByName(transform, "img_def");
        if (img_def != null) img_def.gameObject.SetActive(false);
        Rm_Cover = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "icon");
        txt_name = GameObjectEx.FindComponentByName<Text>(transform, "txt_name");
        btn_replace = GameObjectEx.FindComponentByName<CButton>(transform, "btn_replace");
        btn_del = GameObjectEx.FindComponentByName<CButton>(transform, "btn_del");
        cbtn_rename = GameObjectEx.FindComponentByName<CButton>(transform, "cbtn_rename");
        btn_icon = GameObjectEx.FindComponentByName<CButton>(transform, "icon");
        btns = GameObjectEx.FindChildByName(transform, "btns");
        btnsMask = GameObjectEx.FindComponentByName<CButton>(transform, "btnsMask");
        btns.gameObject.SetActive(false);
        img_select.gameObject.SetActive(false);
        btnsMask.gameObject.SetActive(false);
        if (_loadingObj != null) _loadingObj.SetActive(false);

        btn_icon.onClick.AddListener(OnIconClicked);
        btnsMask.onClick.AddListener(OnBtnsMaskClicked);
        btn_del.onClick.AddListener(() =>
        {
            if (_clothesData != null)
                OCTheatreActorEditorDataManager.Inst.RemoveClothes(_clothesData.clothesIndex);
            MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, null);
        });
        btn_replace.onClick.AddListener(() =>
        {
            btns.gameObject.SetActive(false);
            img_select.gameObject.SetActive(false);
            ShowLoading();
            UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeViewOpenFittingRoomPanel, this);
        });
        cbtn_rename.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.CommonSetNamePanel, new CommonSetNamePanelData
            {
                title = "重命名",
                inputTxt = "请输入衣服名称",
                maxLength = 7,
                btn_close_action = _ => UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel),
                btn_ok_action = txt =>
                {
                    if (string.IsNullOrEmpty(txt))
                    {
                        TipPanel.ShowToast("名字不能为空！");
                        return;
                    }
                    if (OCTheatreActorEditorDataManager.Inst != null)
                    {
                        var list = OCTheatreActorEditorDataManager.Inst.GetClothesList();
                        if (list.Exists(c => c.clothesName == txt))
                        {
                            TipPanel.ShowToast("已存在相同名字的衣服！");
                            return;
                        }
                    }
                    OCTheatreActorEditorDataManager.Inst.SetClothesName(_clothesData, txt);
                    UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                }
            });
        });
    }

    public void ShowLoading()
    {
        if (_loadingObj != null) _loadingObj.SetActive(true);
    }

    public void HideLoading()
    {
        if (_loadingObj != null) _loadingObj.SetActive(false);
    }

    private void OnIconClicked()
    {
        btns.gameObject.SetActive(true);
        img_select.gameObject.SetActive(true);
        _btnsJustShown = true;
        MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, this);
    }

    private void OnBtnsMaskClicked()
    {
        btns.gameObject.SetActive(false);
        img_select.gameObject.SetActive(false);
        btnsMask.gameObject.SetActive(false);
        MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, null);
    }

    public void UpdateData(OTCAvatarClothes clothes)
    {
        EnsureInit();
        _clothesData = clothes;
        gameObject.SetActive(true);
        txt_name.text = clothes.clothesName;
        img_add.gameObject.SetActive(false);
        Rm_Cover.gameObject.SetActive(true);
        Rm_Cover.ResetRawImage();
        HideLoading();
        ShowLoading();
        Rm_Cover.Load(clothes.clothesURL, true, (_, _) => HideLoading(), HideLoading);
        if (img_def != null) img_def.gameObject.SetActive(clothes.isDef == 1);
    }

    public void SetEmpty()
    {
        EnsureInit();
        HideLoading();
        txt_name.text = "";
        img_add.gameObject.SetActive(true);
        Rm_Cover.gameObject.SetActive(false);
        Rm_Cover.ResetRawImage();
    }

    public void InitData(OTCAvatarClothes clothes)
    {
        EnsureInit();
        _clothesData = clothes;
        UpdateData(clothes);
    }
}
