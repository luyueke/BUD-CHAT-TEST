using UI.BaseWidgets;
using UnityEngine;
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
using UI.TopList;
using GameData.BaseInfo;
using System.Collections.Generic;

public class AIHospitalPopularMapViewsHolder : CellViewsHolder
{
    public RemoteImageBehaviour IconRemoteImageBehaviour;
    public AIHospitalPopularMapItem SectionInfoItem;

    public override void CollectViews()
    {
        base.CollectViews();
        IconRemoteImageBehaviour = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(views, "MapCover");
        root.TryGetComponent(out SectionInfoItem);
    }

    public virtual void UpdateViews(RankItem data)  // 改为RankItem
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

public class AIHospitalPopularMapItem : MonoBehaviour  // 改为MonoBehaviour
{
    public CButton _btn_view;
    public SuperTextMesh _mapNametext;
    public Text _txtRank;  // 新增排名显示
    public RemoteImageBehaviour _imgCover;  // 地图封面
    public HospitalStudioNpcItem npcItemPrefab;
    public Transform NpcContent;
    private List<HospitalStudioNpcItem> cachedNpcItems = new List<HospitalStudioNpcItem>();

    //public RemoteImageBehaviour _imgCreatorAvatar;  // 创作者头像
    //public Text _txtCreatorName;  // 创作者名称
    //public Text _txtLikes;  // 点赞数

    private RankItem _curData;  // 改为RankItem
    private Action _interactiveCallback;

    private void Awake()
    {
        _btn_view.onClick.AddListener(OnBtnViewClick);
    }

    public void InitData(RankItem data)  // 改为RankItem
    {
        if (data == null)
            return;

        _curData = data;

        // 更新UI显示
        if (_txtRank != null)
            _txtRank.text = data.rank.ToString();

        if (_mapNametext != null)
            _mapNametext.text = data.mapInfo?.name;

        if (_imgCover != null)
            _imgCover.Load(data.mapInfo?.cover);
        InitNpcInfo(data.mapInfo.gameSetting);
        //if (_imgCreatorAvatar != null)
        //_imgCreatorAvatar.SetImageUrl(data.creator?.avatar);

        //if (_txtCreatorName != null)
        //_txtCreatorName.text = data.creator?.nickname;

        //if (_txtLikes != null)
        //    _txtLikes.text = data.interactInfo?.likeCount.ToString();
    }

    private void OnBtnViewClick()
    {
        UIManager.Inst.SwapPanel(PanelId.AIHospitalUgcMapInfoPanel, _curData?.mapInfo?.id);
    }

    public void SetInteractiveCallback(Action callback)
    {
        _interactiveCallback = callback;
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