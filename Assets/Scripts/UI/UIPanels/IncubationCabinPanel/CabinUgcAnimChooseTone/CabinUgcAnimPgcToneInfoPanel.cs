using GameData.BaseInfo;
using System;
using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;

public class CabinUgcAnimPgcToneInfoPanel : MonoBehaviour
{
    public Transform PgcToneContentParent;
    public IncubationToneShopItem Prefab_PgcToneItem;

    private List<IncubationToneShopItem> _pgcToneItems = new List<IncubationToneShopItem>();
    private List<string> _pgcToneIds = new List<string>();
    private List<CabinToneInfo> _pgcToneDataList = new List<CabinToneInfo>();
    private Action<CabinToneInfo> _onItemSelect;
    private bool isInit = false;

    private void Awake()
    {
        LoadPgcData();
    }

    public void OnSelectPanel()
    {
        if (!isInit)
            return;

        SetCurToneItemSelectState();
    }

    public void SetOnToneItemSelectAct(Action<CabinToneInfo> act)
    {
        this._onItemSelect = act;
    }

    private void OnToneItemSelectAct(CabinToneInfo toneInfo)
    {
        this._onItemSelect?.Invoke(toneInfo);
        _pgcToneItems.ForEach(x => x.SetSelectState(false));
    }

    private void OnTonePreview(CabinToneInfo info)
    {
        UgcAnimToneManager.Inst.PreviewUgcTone(info.metaDataUrl);
    }

    public string GetCurToneId()
    {
        string curToneId = null;
        var studioPanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
        if (studioPanel != null)
        {
            curToneId = studioPanel.GetCurToneId();
        }
        return curToneId;
    }

    private void LoadPgcData()
    {
        _pgcToneItems.Clear();
        _pgcToneIds.Clear();
        _pgcToneDataList.Clear();
        var list = CabinToneNetManager.Inst.GetPgcToneInfo();
        foreach (var item in list)
        {
            var toneItem = Instantiate(Prefab_PgcToneItem, PgcToneContentParent);
            toneItem.InitSelectMode(item, OnToneItemSelectAct, OnTonePreview);
            _pgcToneItems.Add(toneItem);
            _pgcToneIds.Add(item.id);
            _pgcToneDataList.Add(item);
        }
        SetCurToneItemSelectState();
        isInit = true;
    }

    public void SelectFirstItem()
    {
        if (_pgcToneItems.Count == 0 || _pgcToneDataList.Count == 0)
            return;

        _pgcToneItems.ForEach(x => x.SetSelectState(false));
        _pgcToneItems[0].SetSelectState(true);
        _onItemSelect?.Invoke(_pgcToneDataList[0]);
    }

    private void SetCurToneItemSelectState()
    {
        var curToneId = GetCurToneId();
        for (int i = 0; i < _pgcToneItems.Count; i++)
        {
            _pgcToneItems[i].SetSelectState(_pgcToneIds[i] == curToneId);
        }
    }
}

[Serializable]
public class PgcToneConfigEntry
{
    public string name;
    public string ugcId;
    public string previewUrl;
    public string voiceId;
}
