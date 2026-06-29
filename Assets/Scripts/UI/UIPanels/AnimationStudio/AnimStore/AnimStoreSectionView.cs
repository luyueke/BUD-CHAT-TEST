using System;
using System.Collections.Generic;
using Game.CommunityGame;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class AnimStoreSectionView : MonoBehaviour
{
    private Transform _sectionContent;
    private GameObject _sectionItemPrefab;
    private List<SectionItem> _sectionItems = new List<SectionItem>();

    private string _curSectionId = "";
    public Action<string> SelectSectionAction;

    private bool _initOnceFlag = false;

    void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        if (_initOnceFlag) return;
        _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
        _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
        _initOnceFlag = true;
    }

    // ugcType: UgcType.Anim=7 (UGC动画), UgcType.Pose=8 (UGC姿势)
    public void Reload(int ugcType)
    {
        _curSectionId = "";
        InitUI();
        FetchSectionList(ugcType, list =>
        {
            if (this == null) return;
            RefreshUI(list);
        });
    }

    private void RefreshUI(List<SectionItemData> datas)
    {
        if (datas == null || datas.Count == 0) return;

        foreach (var item in _sectionItems)
            GameObject.Destroy(item.gameObject);
        _sectionItems.Clear();

        for (int i = 0; i < datas.Count; i++)
        {
            var item = GameObject.Instantiate(_sectionItemPrefab, _sectionContent)
                .GetComponent<SectionItem>();
            item.InitItem(datas[i], i, OnSectionItemClick);
            _sectionItems.Add(item);
            item.gameObject.SetActive(true);
        }

        if (_sectionItems.Count > 0)
        {
            _sectionItems[0].Tog.isOn = false;
            _sectionItems[0].Tog.isOn = true;
        }
    }

    private void OnSectionItemClick(string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId) || _curSectionId == sectionId) return;
        _curSectionId = sectionId;
        SelectSectionAction?.Invoke(sectionId);
    }

    private void FetchSectionList(int ugcType, Action<List<SectionItemData>> resultHandler)
    {
        var jb = new JObject { ["ugcType"] = ugcType };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.sectionList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<SectionListRsp>(content);
                resultHandler?.Invoke(rsp?.list ?? new List<SectionItemData>());
            },
            _ => resultHandler?.Invoke(new List<SectionItemData>()),
            retryCount: 3);
    }
}
