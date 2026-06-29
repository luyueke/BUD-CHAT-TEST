using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UpdateDraftGameItem : MonoBehaviour
{
    public RawImage mapCover;
    public Text mapName;
    public Image select;
    public Image _nameBg;

    public CButton button;
    public MapInfo uData;

    private Action<MapInfo> OnSelect;

    public HospitalStudioNpcItem npcItemPrefab;
    public Transform NpcContent;
    private List<HospitalStudioNpcItem> cachedNpcItems = new List<HospitalStudioNpcItem>();
    public CommonSpriteSwitch _spriteSwitch;
    public void Init(Action<MapInfo> selectAct,
        MapInfo data)
    {
        uData = data;
        OnSelect = selectAct;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => 
        {
            OnSelect?.Invoke(uData);
            _spriteSwitch.Switch((uint)data.gameType,false);
        });

        select.gameObject.SetActive(false);
        mapName.text = data.name;

        NpcContent.gameObject.SetActive(data.gameType == (int)GameType.AIGame);
        if (data.gameType == (int)GameType.AIGame)
        {
            if (ColorUtility.TryParseHtmlString(AIGameHospitalConfig.hospitalThemeColor, out Color color))
            {
                _nameBg.color = color;
            }
            InitNpcInfo(data.gameSetting);
        }
    }


    public void UpdateSelected(bool isSelect)
    {
        select.gameObject.SetActive(isSelect);
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

    private void OnDestroy()
    {
        cachedNpcItems.Clear();
    }
}
