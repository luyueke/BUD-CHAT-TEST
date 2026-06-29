using Com.TheFallenGames.OSA.Core;
using Game.CommunityGame;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using frame8.Logic.Misc.Other.Extensions;
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using System.Collections.Generic;
using UnityEngine.UI;

public class AIHospitalRecommendedMapViewsHolder : CellViewsHolder
{
    public RemoteImageBehaviour IconRemoteImageBehaviour;
    public AIHospitalRecommendedMapItem SectionInfoItem;

    public override void CollectViews()
    {
        base.CollectViews();
        IconRemoteImageBehaviour = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(views, "MapCover");
        root.TryGetComponent(out SectionInfoItem);
    }

    public virtual void UpdateViews(RecommendItemData data)
    {
        if (SectionInfoItem == null)
            return;

        SectionInfoItem.InitData(data);
    }

    public void SetInteractiveCallback(Action act)
    {
        if (SectionInfoItem == null)
            return;

        SectionInfoItem.SetInteractiveCallback(act);
    }
}

public class AIHospitalRecommendedMapItem : BaseSectionInfoItem
{
    public CButton _btn_view;

    public HospitalStudioNpcItem npcItemPrefab;
    public Transform NpcContent;

    private RecommendItemData _curData;

    private List<HospitalStudioNpcItem> cachedNpcItems = new List<HospitalStudioNpcItem>();

    public SuperTextMesh _mapNametext;

    private void Awake()
    {
        _btn_view.onClick.AddListener(OnBtnViewClick);
    }

    public override void InitData(RecommendItemData data)
    {
        base.InitData(data);

        if (data == null)
            return;

        this._curData = data;
        InitMapName(data.ugcInfo.name);
        InitNpcInfo(data.ugcInfo.gameSetting);
    }

    public void InitNpcInfo(GameSetting gameSetting)
    {
        if (gameSetting != null && gameSetting.aIGameConfig != null)
        {
            var aIGameConfig = gameSetting.aIGameConfig;
            var hospitalNPCs = aIGameConfig.hospitalNPCs == null ? new List<HospitalNPCData>() : aIGameConfig.hospitalNPCs;

            // 确保缓存的NPC项目数量足够
            while (cachedNpcItems.Count < 3)
            {
                var npcItem = GameObject.Instantiate(npcItemPrefab, NpcContent);
                cachedNpcItems.Add(npcItem);
            }

            // 更新NPC项目状态
            for (int i = 0; i < 3; i++)
            {
                // 从缓存获取NPC项目
                var npcItem = cachedNpcItems[i];
                npcItem.gameObject.SetActive(true);

                if (i < hospitalNPCs.Count && hospitalNPCs[i] != null)
                {
                    var npcData = hospitalNPCs[i];
                    if (!string.IsNullOrEmpty(npcData.cover))
                    {
                        npcItem.SetProfile(npcData.cover);
                    }
                    else
                    {
                        npcItem.SetLearnMore();
                    }
                }
                else
                {
                    npcItem.SetLearnMore();
                }
            }
        }
    }

    private void OnBtnViewClick()
    {
        UIManager.Inst.SwapPanel(PanelId.AIHospitalUgcMapInfoPanel, this._curData?.ugcInfo?.id);
    }

    private void InitMapName(string mapName)
    {
        _mapNametext.text = mapName;
    }

    private void OnDestroy()
    {
        cachedNpcItems.Clear();
    }
}

