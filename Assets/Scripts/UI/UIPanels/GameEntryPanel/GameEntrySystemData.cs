using Game.CommunityGame;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using System.Collections.Generic;

namespace GameUI
{
    public class GameEntrySystemData
    {
        //乐园界面当前列表  收藏
        public ResInfoList CollectResInfoList;
        private List<ResInfo> _collectMapInfos = new List<ResInfo>();
        public List<ResInfo> CollectMapInfos
        {
            get { return _collectMapInfos; }
            set
            {
                _collectMapInfos = value;
                if (_collectMapInfos == null)
                {
                    _collectMapInfos = new List<ResInfo>();
                }
            }
        }

        //乐园界面当前列表  记录
        private List<MapInfo> _mapInfos = new List<MapInfo>();
        public List<MapInfo> MapInfos
        {
            get { return _mapInfos; }
            set
            {
                _mapInfos = value;
                if (_mapInfos == null)
                {
                    _mapInfos = new List<MapInfo>();
                }
            }
        }

        //乐园界面当前列表  精选
        public OfficalRecommendRsp SelectedRspInfo;
        private List<RecommendItemData> _selectedMapInfos = new List<RecommendItemData>();
        public List<RecommendItemData> SelectedMapInfos
        {
            get { return _selectedMapInfos; }
            set
            {
                _selectedMapInfos = value;
                if (_selectedMapInfos == null)
                {
                    _selectedMapInfos = new List<RecommendItemData>();
                }
            }
        }

        //乐园界面当前列表  搜索
        public string SearchStr;
        public SectionInfoRsp SearchInfoRsp;
        private List<RecommendItemData> _searchMapInfos = new List<RecommendItemData>();
        public List<RecommendItemData> SearchMapInfos
        {
            get { return _searchMapInfos; }
            set
            {
                _searchMapInfos = value;
                if (_searchMapInfos == null)
                {
                    _searchMapInfos = new List<RecommendItemData>();
                }
            }
        }

        public UgcBaseInfo LastPlayMapInfo;

        //剧本广场所有标签 
        public SectionListRsp SectionListRsp;
        public string SectionId;
        public SectionInfoRsp SectionInfoRsp;
        private List<RecommendItemData> _sectionMapInfos = new List<RecommendItemData>();
        public List<RecommendItemData> SectionMapInfos
        {
            get { return _sectionMapInfos; }
            set
            {
                _sectionMapInfos = value;
                if (_sectionMapInfos == null)
                {
                    _sectionMapInfos = new List<RecommendItemData>();
                }
            }
        }

        //草稿箱或已发布
        public MapListResponse MapListResponse;
        public MapListResponseType MapListType;
        private List<DraftListItem> _mapListInfos = new List<DraftListItem>();
        public List<DraftListItem> MapListInfos
        {
            get { return _mapListInfos; }
            set
            {
                _mapListInfos = value;
                if (_mapListInfos == null)
                {
                    _mapListInfos = new List<DraftListItem>();
                }
            }
        }

        public void ResetCollect()
        {
            CollectResInfoList = null;
            CollectMapInfos.Clear();

            SelectedRspInfo = null;
            SelectedMapInfos.Clear();
        }

    }

    //mapinfo 字段太多了 本地只存储有效的这几个字段
    public class GameMapSave
    {
        public string id;

        public string cover;

        public string name;

        public string plot;

        public List<AICommonGameConfig_NPC> npc;
    }
}