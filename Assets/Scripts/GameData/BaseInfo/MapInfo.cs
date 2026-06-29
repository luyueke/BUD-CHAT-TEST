// @Author: YangJie
// @Description:
// @Date:  2023/07/13
// @Modify:

using System;
using System.Collections.Generic;
using Basic.Utils;
using GameData.Base;
using GameData.OfflineRender;
using Newtonsoft.Json;

namespace GameData.BaseInfo
{
    public class MapInfo : UgcBaseInfo
    {
        public override string templateId { get; set; } = "10000";
        //public GameSetting gameSetting = new GameSetting();


        public List<string> npcIds;
        public List<string> propIds;
        public List<string> materialIds;

        public OfflineRenderInfo renderInfo;
        public DateTime lastModifiedTime;

        [JsonConverter(typeof(StringObjectConverter))]
        public DetailInfo detailInfo;

        //public int gameType;
        public MapTitles mapTitles;
        /// <summary>
        /// 是否是通关地图
        /// </summary>
        public bool IsPassLevelMap()
        {
            return gameSetting is { winCondition: > 0 };
        }
    }

    public enum GameType
    {
        Normal = 0,
        AIGame = 1,
    }
    public class MapTitles
    {
        public string title;
        public int titleId;
        public int displayEndTime;
    }

    public class GameSetting
    {
        /// <summary>
        /// 默认有 通关棋子
        /// </summary>
        public int winCondition = 1; //通关条件 1 到达终点 2 收集星星
        public int playTestDuration = 0; //通关图发布前的自测通关时间
        public int timeLimited = 0; // 是否有时间限制 0 无 1 有

        public int limitDuration = -1; //限制时长 s 为单位

        public int limitHp = -1; //限制血量

        public int maxPlayer = 16; //最大玩家数
        public string gameHint;//游戏提示

        public string bgMusicUrl; // 游戏背景音乐
        public string bgName; // 背景音乐名称

        public AIGameConfig aIGameConfig = new AIGameConfig();

        public int aiGameId;  //1 病娇游戏、 2 医院逃脱 3 乐园

        public string aICommonGameConfig = "";

        [JsonIgnore] private AICommonGameConfig _AICommonGameConfig;
        [JsonIgnore] public AICommonGameConfig AICommonGameConfig
        {
            get
            {
                if (_AICommonGameConfig == null && !string.IsNullOrEmpty(aICommonGameConfig))
                {
                    _AICommonGameConfig = JsonConvert.DeserializeObject<AICommonGameConfig>(aICommonGameConfig);
                }
                if (_AICommonGameConfig == null)
                {
                    _AICommonGameConfig = new AICommonGameConfig();
                }
                return _AICommonGameConfig;
            }
        }

        public void SerializeData()
        {
            if (AICommonGameConfig != null)
            {
                aICommonGameConfig = JsonConvert.SerializeObject(AICommonGameConfig);
            }
        }

        public GameSetting Clone()
        {
            return new GameSetting()
            {
                winCondition = winCondition,
                playTestDuration = playTestDuration,
                timeLimited = timeLimited,
                limitDuration = limitDuration,
                maxPlayer = maxPlayer,
                limitHp = limitHp,
                bgMusicUrl = bgMusicUrl,
                aIGameConfig = aIGameConfig,
                bgName = bgName,
            };
        }
    }

    public class AIGameConfig
    {
        public int winRegulatorAmount; //说服监管者个数
        public string themeColor; //主题颜色
        public string plot; //场景内剧情
        public int purchasedRegulatorAmount; //购买的监管者NPC卡位数
        public int purchasedFugitiveAmount; //购买的逃亡者NPC卡位数
        public int gameAtmosphere;
        public List<HospitalNPCData> hospitalNPCs = new List<HospitalNPCData>(); //NPC信息
        public List<HospitalPhotoData> hospitalPhotos = new List<HospitalPhotoData>();//照片墙信息
    }

    public class HospitalNPCData
    {
        public string id; //NPCID
        public int role; //NPC角色 监管者 7 ，逃亡者 0
        public string name; //NPC名称   
        public string cover; //NPC头像
        public NPCConfig npcConfig; //NPC配置
    }

    public class ParkNPCData
    {
        public string id; //NPCID
        public int role; //NPC角色 监管者 7 ，逃亡者 0
        public string name; //NPC名称   
        public string cover; //NPC头像
        public NPCConfig npcConfig; //NPC配置
    }


    public class NPCConfig
    {
        public int winCondition; //1 说服，2回答问题
        public string persuadeContent; //说服方式：
        public string plot; //场景内NPC剧情（选填）
        public string question; //通关问题
        public string answer; //参考答案
    }

    public enum HospitalNPCType
    {
        Default = -1,
        Provost = 7,
        Runagate = 0,
    }

    public enum ParkNPCType
    {
        Default = -1,
        Provost = 7,
        Runagate = 0,
    }

    public class HospitalPhotoData
    {
        public int location;
        public List<string> urls = new List<string>();
    }

    public class ParkPhotoData
    {
        public int location;
        public List<string> urls = new List<string>();
    }
}
