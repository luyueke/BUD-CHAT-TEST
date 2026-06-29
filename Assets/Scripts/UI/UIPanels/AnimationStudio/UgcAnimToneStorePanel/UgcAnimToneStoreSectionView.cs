using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class UgcAnimToneStoreSectionView : MonoBehaviour
{
    private Transform _sectionContent;
    private GameObject _sectionItemPrefab;
    private List<SectionItem> _sectionItems = new List<SectionItem>();

    private string _curSectionId = "";
    public Action<String> SelectSectionAction;

    private bool InitOnceFlag = false;
    // Start is called before the first frame update
    void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        if (InitOnceFlag)
        {
            return;
        }
        _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
        _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
        InitOnceFlag = true;
    }
    
    public void Reload(UgcType ugcType = UgcType.AnimMusic)
    {
        InitUI();
        _curSectionId = "";
        RefreshData(ugcType,resultHandler: list =>
        {
            if (this == null) return;
            RefreshUI(list);
        });
    }

    private void RefreshUI(List<SectionItemData> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        
        foreach (var sectionItem in _sectionItems)
        {
            GameObject.Destroy(sectionItem.gameObject);
        }
        _sectionItems.Clear();
        
        for (int i = 0; i < datas.Count; i++)
        {
            var sectionItemItem =
                GameObject.Instantiate(_sectionItemPrefab, _sectionContent).GetComponent<SectionItem>();
            var curData = datas[i];
            sectionItemItem.InitItem(curData, i, OnSectionItemClick);
            _sectionItems.Add(sectionItemItem);
            sectionItemItem.gameObject.SetActive(true);
        }

        if (_sectionItems.Count > 0)
        {
            _sectionItems[0].Tog.isOn = true;
            _sectionItems[0].SetSelectState(true);
        }
    }

    private void OnSectionItemClick(string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId))
        {
            return;
        }
        if (_curSectionId == sectionId)
        {
            return;
        }

        _curSectionId = sectionId;
        SelectSectionAction?.Invoke(sectionId);
    }
    
    public void RefreshData(UgcType ugcType = UgcType.AnimMusic, Action<List<SectionItemData>> resultHandler = null)
    {
        JObject req = new JObject()
        {
            ["ugcType"] = (int)ugcType
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, 
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            arg0 =>
            {
                SectionListRsp sectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(arg0);
                var datas = sectionListRsp?.list;
                if (datas == null || datas.Count == 0)
                {
                    resultHandler?.Invoke(new List<SectionItemData>());
                    return;
                }
                
                resultHandler?.Invoke(datas);
            }, 
            onFail: arg0 =>
            {
                resultHandler?.Invoke(new List<SectionItemData>());
            },
            retryCount:3);
    }
}
