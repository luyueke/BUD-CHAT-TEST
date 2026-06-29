/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-17 15:07:47
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-22 16:52:32
 * @ Description:音乐板配置
 */
 using System.Collections.Generic;
 using Game.Base;

namespace Game.Config
{
    public class MusicPadData : BasePropConfigData
    {
        public string NormalColor;
        public string LightColor;
        public string SoundName;
    }

    public static class MusicPadConfig
    {
        public static List<MusicPadData> All = new List<MusicPadData>()
        {
            new MusicPadData()
            {
                Id = "0",
                IconName = "",
                NormalColor = "#BFBFBF",
                LightColor = "#BFBFBF",
                SoundName = "",
            },
            // 低音
            new MusicPadData()
            {
                Id = "1",
                IconName = "icn_editor_musicborad03",
                NormalColor = "#FF001C",
                LightColor = "#FF524C",
                SoundName = "Play_MusicPad_Piano_L_1",
            },
            new MusicPadData()
            {
                Id = "2",
                IconName = "icn_editor_musicborad04",
                NormalColor = "#FF6900",
                LightColor = "#FF784C",
                SoundName = "Play_MusicPad_Piano_L_S1",
            },
            new MusicPadData()
            {
                Id = "3",
                IconName = "icn_editor_musicborad05",
                NormalColor = "#B09E00",
                LightColor = "#FFB200",
                SoundName = "Play_MusicPad_Piano_L_2",
            },
            new MusicPadData()
            {
                Id = "4",
                IconName = "icn_editor_musicborad06",
                NormalColor = "#80A800",
                LightColor = "#85FF00",
                SoundName = "Play_MusicPad_Piano_L_S2",
            },
            new MusicPadData()
            {
                Id = "5",
                IconName = "icn_editor_musicborad07",
                NormalColor = "#0030FF",
                LightColor = "#29A6FF",
                SoundName = "Play_MusicPad_Piano_L_3",
            },
            new MusicPadData()
            {
                Id = "6",
                IconName = "icn_editor_musicborad08",
                NormalColor = "#E300FF",
                LightColor = "#A361FF",
                SoundName = "Play_MusicPad_Piano_L_4",
            },
            new MusicPadData()
            {
                Id = "7",
                IconName = "icn_editor_musicborad09",
                NormalColor = "#FF54E0",
                LightColor = "#FF87FC",
                SoundName = "Play_MusicPad_Piano_L_S4",
            },
            new MusicPadData()
            {
                Id = "8",
                IconName = "icn_editor_musicborad10",
                NormalColor = "#FF3300",
                LightColor = "#FFBF78",
                SoundName = "Play_MusicPad_Piano_L_5",
            },
            new MusicPadData()
            {
                Id = "9",
                IconName = "icn_editor_musicborad11",
                NormalColor = "#FFED1A",
                LightColor = "#FF8052",
                SoundName = "Play_MusicPad_Piano_L_S5",
            },
            new MusicPadData()
            {
                Id = "10",
                IconName = "icn_editor_musicborad12",
                NormalColor = "#009E0F",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_L_6",
            },
            new MusicPadData()
            {
                Id = "11",
                IconName = "icn_editor_musicborad13",
                NormalColor = "#00995C",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_L_S6",
            },
            new MusicPadData()
            {
                Id = "12",
                IconName = "icn_editor_musicborad14",
                NormalColor = "#B52EFF",
                LightColor = "#DB96FF",
                SoundName = "Play_MusicPad_Piano_L_7",
            },
            // 中音
            new MusicPadData()
            {
                Id = "13",
                IconName = "icn_editor_musicborad15",
                NormalColor = "#FF001C",
                LightColor = "#FF524C",
                SoundName = "Play_MusicPad_Piano_1",
            },
            new MusicPadData()
            {
                Id = "14",
                IconName = "icn_editor_musicborad16",
                NormalColor = "#FF6900",
                LightColor = "#FF784C",
                SoundName = "Play_MusicPad_Piano_S1",
            },
            new MusicPadData()
            {
                Id = "15",
                IconName = "icn_editor_musicborad17",
                NormalColor = "#B09E00",
                LightColor = "#FFB200",
                SoundName = "Play_MusicPad_Piano_2",
            },
            new MusicPadData()
            {
                Id = "16",
                IconName = "icn_editor_musicborad18",
                NormalColor = "#80A800",
                LightColor = "#85FF00",
                SoundName = "Play_MusicPad_Piano_S2",
            },
            new MusicPadData()
            {
                Id = "17",
                IconName = "icn_editor_musicborad19",
                NormalColor = "#0030FF",
                LightColor = "#29A6FF",
                SoundName = "Play_MusicPad_Piano_3",
            },
            new MusicPadData()
            {
                Id = "18",
                IconName = "icn_editor_musicborad20",
                NormalColor = "#E300FF",
                LightColor = "#A361FF",
                SoundName = "Play_MusicPad_Piano_4",
            },
            new MusicPadData()
            {
                Id = "19",
                IconName = "icn_editor_musicborad21",
                NormalColor = "#FF54E0",
                LightColor = "#FF87FC",
                SoundName = "Play_MusicPad_Piano_S4",
            },
            new MusicPadData()
            {
                Id = "20",
                IconName = "icn_editor_musicborad22",
                NormalColor = "#FF3300",
                LightColor = "#FFBF78",
                SoundName = "Play_MusicPad_Piano_5",
            },
            new MusicPadData()
            {
                Id = "21",
                IconName = "icn_editor_musicborad23",
                NormalColor = "#FFED1A",
                LightColor = "#FF8052",
                SoundName = "Play_MusicPad_Piano_S5",
            },
            new MusicPadData()
            {
                Id = "22",
                IconName = "icn_editor_musicborad24",
                NormalColor = "#009E0F",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_6",
            },
            new MusicPadData()
            {
                Id = "23",
                IconName = "icn_editor_musicborad25",
                NormalColor = "#00995C",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_S6",
            },
            new MusicPadData()
            {
                Id = "24",
                IconName = "icn_editor_musicborad26",
                NormalColor = "#B52EFF",
                LightColor = "#DB96FF",
                SoundName = "Play_MusicPad_Piano_7",
            },
            // 高音
            new MusicPadData()
            {
                Id = "25",
                IconName = "icn_editor_musicborad27",
                NormalColor = "#FF001C",
                LightColor = "#FF524C",
                SoundName = "Play_MusicPad_Piano_H_1",
            },
            new MusicPadData()
            {
                Id = "26",
                IconName = "icn_editor_musicborad28",
                NormalColor = "#FF6900",
                LightColor = "#FF784C",
                SoundName = "Play_MusicPad_Piano_H_S1"
            },
            new MusicPadData()
            {
                Id = "27",
                IconName = "icn_editor_musicborad29",
                NormalColor = "#B09E00",
                LightColor = "#FFB200",
                SoundName = "Play_MusicPad_Piano_H_2",
            },
            new MusicPadData()
            {
                Id = "28",
                IconName = "icn_editor_musicborad30",
                NormalColor = "#80A800",
                LightColor = "#85FF00",
                SoundName = "Play_MusicPad_Piano_H_S2",
            },
            new MusicPadData()
            {
                Id = "29",
                IconName = "icn_editor_musicborad31",
                NormalColor = "#0030FF",
                LightColor = "#29A6FF",
                SoundName = "Play_MusicPad_Piano_H_3",
            },
            new MusicPadData()
            {
                Id = "30",
                IconName = "icn_editor_musicborad32",
                NormalColor = "#E300FF",
                LightColor = "#A361FF",
                SoundName = "Play_MusicPad_Piano_H_4",
            },
            new MusicPadData()
            {
                Id = "31",
                IconName = "icn_editor_musicborad33",
                NormalColor = "#FF54E0",
                LightColor = "#FF87FC",
                SoundName = "Play_MusicPad_Piano_H_S4",
            },
            new MusicPadData()
            {
                Id = "32",
                IconName = "icn_editor_musicborad34",
                NormalColor = "#FF3300",
                LightColor = "#FFBF78",
                SoundName = "Play_MusicPad_Piano_H_5",
            },
            new MusicPadData()
            {
                Id = "33",
                IconName = "icn_editor_musicborad35",
                NormalColor = "#FFED1A",
                LightColor = "#FF8052",
                SoundName = "Play_MusicPad_Piano_H_S5",
            },
            new MusicPadData()
            {
                Id = "34",
                IconName = "icn_editor_musicborad36",
                NormalColor = "#009E0F",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_H_6",
            },
            new MusicPadData()
            {
                Id = "35",
                IconName = "icn_editor_musicborad37",
                NormalColor = "#00995C",
                LightColor = "#5252D9",
                SoundName = "Play_MusicPad_Piano_H_S6",
            },
            new MusicPadData()
            {
                Id = "36",
                IconName = "icn_editor_musicborad02",
                NormalColor = "#B52EFF",
                LightColor = "#DB96FF",
                SoundName = "Play_MusicPad_Piano_H_7",
            },
        };
    
        public static MusicPadData GetMusicPadData(string id)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}