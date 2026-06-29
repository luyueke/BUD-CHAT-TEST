using System;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// 姿势商城运营栏目视图：从 FittingRoom 的 Ugc+姿势 流程迁出，结构与 ActorCardStoreSectionView 一致，
// 区别仅在于请求 sectionList 时 ugcType 传 Pose。
public class PoseStoreSectionView : MonoBehaviour
{
    private Transform _sectionContent;
    private GameObject _sectionItemPrefab;
    private List<SectionItem> _sectionItems = new List<SectionItem>();

    private string _curSectionId = "";
    public Action<String> SelectSectionAction;

    private bool InitOnceFlag = false;

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

    public void Reload()
    {
        InitUI();
        RefreshData(resultHandler: list =>
        {
            if (this == null)
            {
                return;
            }
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

    public void RefreshData(UgcType ugcType = UgcType.Pose, Action<List<SectionItemData>> resultHandler = null)
    {
        JObject req = new JObject()
        {
            ["ugcType"] = (int)UgcType.Pose
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
            retryCount: 3);
    }
}
