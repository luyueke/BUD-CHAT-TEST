using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AIGame.Base;
using GameData.BaseInfo;

public class AIHospitalGameStudioItem : DraftsItem
{
    public HospitalStudioNpcItem npcItemPrefab;
    public Transform NpcContent;
    private List<HospitalStudioNpcItem> cachedNpcItems = new List<HospitalStudioNpcItem>();

    public SuperTextMesh _mapNametext;

    public override void Init(Action<DraftListItem> onSelect, Action<DraftListItem, DraftsItem> uploadAction, DraftListItem data, StudioSubType studioSubType)
    {
        base.Init(onSelect, uploadAction, data, studioSubType);

        if (data == null || data.mapInfo == null)
        {
            return;
        }
        InitMapName(data.mapInfo.name);
        var mapInfo = data.mapInfo;
        var gameSetting = mapInfo.gameSetting;

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
        Go_Visted.SetActive(false);
    }

    protected override void CreateNewMap()
    {
        UIManager.Inst.OpenPanel(PanelId.AIHospitalUgcEditPanel, EditType.Create);
    }
    
    // 在销毁时清理缓存的NPC项目
    private void OnDestroy()
    {
        cachedNpcItems.Clear();
    }

    private void InitMapName(string mapName)
    {
        _mapNametext.text = mapName;
    }
}
