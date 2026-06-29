using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.BaseInfo
{
    public enum AIParkOfficialBgm
    {
        Bgm_S11Para_MainScene = 1,
        Bgm_Hall_S11,
        Bgm_Hall_S11_Loading,
        Bgm_Para_Punk,
        Bgm_Para_Funk,
        Bgm_Para_Jazz01,
        Bgm_Para_Jazz02,
    }
    public class AICommonGameConfigBase
    {
        public string plot;//场景内剧情
    }
    public class AICommonGameConfig : AICommonGameConfigBase
    {
        public List<AICommonGameConfig_NPC> npcData = new();//npc
        public List<AICommonGameConfig_Event> events;//事件
        public List<AICommonGameConfig_Ending> endings;//结局

        public AICommonGameConfig_Stage stage = new();//装饰

        public AICommonGameConfig_Billboard billboard = new();//公告牌

        public AICommonGameConfig_Book book = new();//事件簿

        public AICommonGameConfig_Scene scene = new();//场景

        public AICommonGameConfig_GamePop gamePop = new();//游戏内弹窗

        public string plantColor; //植物颜色

        public void DefaultConfig()
        {
            stage.DefaultInstrments();
            stage.DefaulBgm();
        }
    }

    //npc
    public class AICommonGameConfig_NPC
    {
        public string id; //NPCID
        public string plot; //NPC剧情
        public string name; //NPC名称   
        public string desc; //NPC描述
        public string cover; //NPC头像
        public string npcAvatarJson; //NPC模型
    }

    //事件
    public class AICommonGameConfig_Event
    {
        public string name;
        public string target;   // 事件目标
        public string desc;
        public int limitDuration;//限时时长 0不限时
        public int type;//事件类型 0普通事件 1选择事件
        public string question;
        public List<string> answers = new();

        public List<AICommonGameConfig_Action> actions = new();
    }
    //行为
    public class AICommonGameConfig_Action
    {
        public string id; //NPCID
        public string name; //名称
        public string desc; // 行为
    }


    //结局
    public class AICommonGameConfig_Ending
    {
        public string desc;
        public int type; //结局类型 0普通结局 1彩蛋结局
        public string triggerEvent;//彩蛋触发事件
    }

    //装饰
    public class AICommonGameConfig_Stage
    {
        public List<AICommonGameStageMusic> musicUrls = new(); // 最多四个
        public List<string> backgroundUrls = new() {"","","","" };
        public List<AICommonGameConfig_Musical> musicalInstruments = new() {null, null, null, null };

        public void DefaultInstrments() {
            musicalInstruments.Clear();
            musicalInstruments.Add(new AICommonGameConfig_Musical("12400014", "麦克风"));
            musicalInstruments.Add(new AICommonGameConfig_Musical("12400013", "键盘"));
            musicalInstruments.Add(new AICommonGameConfig_Musical("12400012", "电吉他"));
            musicalInstruments.Add(new AICommonGameConfig_Musical("12400011", "鼓"));
        }

        public void DefaulBgm() {
            musicUrls.Clear();
            musicUrls.Add(new AICommonGameStageMusic(AIParkOfficialBgm.Bgm_Para_Punk.ToString(),"朋克01"));
            musicUrls.Add(new AICommonGameStageMusic(AIParkOfficialBgm.Bgm_Para_Funk.ToString(), "朋克02"));
            musicUrls.Add(new AICommonGameStageMusic(AIParkOfficialBgm.Bgm_Para_Jazz01.ToString(), "爵士01"));
            musicUrls.Add(new AICommonGameStageMusic(AIParkOfficialBgm.Bgm_Para_Jazz02.ToString(), "爵士02"));
        }
    }

    public class AICommonGameStageMusic {
        public string url;
        public string name;
        public bool isLocal;
        public string musicName;  //音乐名字
        public AICommonGameStageMusic()
        {

        }
        public AICommonGameStageMusic(string _name,string _musicName) {
            name = _name;
            isLocal = true;
            musicName = _musicName;
        }
    }

    //乐器
    public class AICommonGameConfig_Musical
    {
        public string id;
        public string name;
        public bool isUgc;
        public string icon;

        public AICommonGameConfig_Musical()
        {

        }
        public AICommonGameConfig_Musical(string _id,string _name)
        {
            id = _id;
            name = _name;
            isUgc = false;
        }
    }

    //公告牌
    public class AICommonGameConfig_Billboard
    {
        public List<string> woodenhorseUrls = new() { "", "", "", "" };
        public List<string> fountainUrls = new() { "", "", "", "" };
        public List<string> parkUrls = new() { "", "", "", "" };
    }

    //场景
    public class AICommonGameConfig_Scene
    {
        public float width = 1;
        public string color;
        public List<AICommonGamePaster> pasterUrls = new();
    }

    public class AICommonGamePaster {
        public string url;
        public float x;
        public float y;
        public float scale = 1;
    }

    //游戏内弹窗
    public class AICommonGameConfig_GamePop
    {
        public string fontColor;
        public string bgColor1;
        public string bgColor2;
        public List<AICommonGamePaster> pasterUrls = new();
    }

    //事件簿
    public class AICommonGameConfig_Book
    {
        public string bgColor1;
        public string bgColor2;
        public string fontColor;
        public List<AICommonGamePaster> pasterUrls = new();
    }
}