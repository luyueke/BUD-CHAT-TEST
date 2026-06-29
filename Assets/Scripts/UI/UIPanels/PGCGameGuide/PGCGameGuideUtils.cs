using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PGCGameGuideUtils
{
    private static Dictionary<PGCGameType, PGCGameGuideConfig> guideConfigs = new Dictionary<PGCGameType, PGCGameGuideConfig>
    {
        {
            PGCGameType.AIYandere, new PGCGameGuideConfig
            {
                voiceId = "",
                talkerName = "机器人Yumi",
                talks = new List<string>
                {
                    "欢迎来到年兽公寓逃脱模拟器！",
                    "传说年兽曾肆虐人间，后被仙家封印在了一座神秘的宅院之中，随着岁月流转，这座宅院隐匿在世间，渐渐演变成了如今的公寓模样，鲜有人知。",
                    "每到春节前夕，天地间的年味气息会与封印产生奇妙的共鸣，使得公寓的禁制出现短暂的不稳定。",
                    "每到春节前夕，天地间的年味气息会与封印产生奇妙的共鸣，使得公寓的禁制出现短暂的不稳定。",
                    "而你，不小心闯入了这个公寓，让他黯淡的眼眸瞬间有了光亮。他静静地跟在你身后，目光紧紧锁住你的身影，那模样仿佛生怕你下一秒就会消失不见。",
                    "你察觉到他的意图，心里有些害怕，却也知晓必须得想办法离开。于是你一边观察着周围环境，寻得机会脱身，一边与年兽周旋，试图摆脱这被年兽渴望陪伴而困住的局面。"
                },
                guideDialogueBgImage = "bg_guide_dialogue9",
                guideNameImage = "bg_guide_name9",
                guidePeopleImage = "ic_guide9",
            }
        }
    };
    
    public static PGCGameGuideConfig GetGuideConfig(PGCGameType gameType)
    {
        return guideConfigs[gameType];
    }
    
    public static Sprite LoadGuideIcon(string iconName, GameObject go)
    {
        return XAssetLoaderMgr.Inst.LoadSpriteInAltas("Assets/Loadable/UI/UIPanel/PGCGameGuidePanel/PGCGameGuidePanel.spriteatlas", iconName, go);
    }
    
}

public enum PGCGameType
{
    AIYandere = 1,
    AIHospital = 2,
    AIPark = 3,
}

public class PGCGameGuideConfig
{
    public string voiceId;
    public string talkerName;
    public List<string> talks;
    public string guideDialogueBgImage;
    public string guideNameImage;
    public string guidePeopleImage;
}
