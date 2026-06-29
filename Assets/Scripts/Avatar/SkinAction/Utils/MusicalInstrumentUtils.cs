using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;


namespace Game.MusicalInstrument
{
    public static class MusicalInstrumentUtils
    {
        public const string InstrumentTemplateId = "52400001";
        public const string InstrumentDefaultMoveId = "1008";
        public static List<string> FreeMoveIdList = new List<string> { "1008", "1003" };
        public static SkinInfo GetDefaultMusicalInstrumentSkinInfo()
        {
            var skinInfo = new SkinInfo();
            skinInfo.templateId = InstrumentTemplateId;
            skinInfo.subType = (int)AvatarSubType.MusicalInstrument;
            skinInfo.isProp = true;

            return skinInfo;
        }
        
        public static SkinActionInfo GetDefaultSkinActionInfo()
        {
            SkinActionInfo skinActionInfo = new SkinActionInfo();
            skinActionInfo.instrumentInfo = new InstrumentInfo();

            skinActionInfo.instrumentInfo.moveId = InstrumentDefaultMoveId;

            skinActionInfo.instrumentInfo.toneInfo = GetDefaultToneInfo();

            skinActionInfo.instrumentInfo.animDetailInfo = GetDefaultInstrumentDetailInfo();

            return skinActionInfo;
        }
        
        public static ToneInfo GetDefaultToneInfo()
        {
            var pgcToneConfigList = Es.DataTables.GetInstrumentToneConfigList();
            var defaultConfig = pgcToneConfigList[0];
            return ConvertPgcToneConfigToToneInfo(defaultConfig);
        }
        
        public static ToneInfo ConvertPgcToneConfigToToneInfo(InstrumentToneConfig pgcConfig)
        {
            ToneInfo toneInfo = new ToneInfo();
            var curToneType =  pgcConfig.toneType == (int)ToneType.Both ? ToneType.TwentyTwo : ToneType.Fifteen;
            
            toneInfo.Init();
            toneInfo.SetToneType(curToneType);
            toneInfo.isPgc = 1;
            toneInfo.id = pgcConfig.pgcId;
            toneInfo.name = pgcConfig.toneName;
            return toneInfo;
        }

        public static ToneInfo GetPgcToneInfoByPgcToneId(string pgcId)
        {
            var pgcConfig = Es.DataTables.GetInstrumentToneConfig(pgcId);
            ToneInfo toneInfo = ConvertPgcToneConfigToToneInfo(pgcConfig);
            return toneInfo;
        }

        public static Color GetRandomSyllableItemSelectedColor()
        {
            var selectedColor = new List<string>() { "#FF76D0", "#63E3FF", "#FFD84E"};
            return DataUtil.DeSerializeColorCheckHash(selectedColor[Random.Range(0, selectedColor.Count)]);
        }

        public static Vector2 GetPreviewRange()
        {
            return new Vector2((int)SyllableType.Middle_1, (int)SyllableType.Middle_7);
        }

        public static bool IsPgcTone(string toneId)
        {
            return toneId.Contains("pgc");
        }
        
        public static List<SyllableType> High_Config = new List<SyllableType>() {
            SyllableType.High_1,
            SyllableType.High_2,
            SyllableType.High_3,
            SyllableType.High_4,
            SyllableType.High_5,
            SyllableType.High_6,
            SyllableType.High_7,
            SyllableType.Double_High
        };
    
        public static List<SyllableType> Middle_Config = new List<SyllableType>() {
            SyllableType.Middle_1,
            SyllableType.Middle_2,
            SyllableType.Middle_3,
            SyllableType.Middle_4,
            SyllableType.Middle_5,
            SyllableType.Middle_6,
            SyllableType.Middle_7,
        };
    
        public static List<SyllableType> Low_Config = new List<SyllableType>() {
            SyllableType.Low_1,
            SyllableType.Low_2,
            SyllableType.Low_3,
            SyllableType.Low_4,
            SyllableType.Low_5,
            SyllableType.Low_6,
            SyllableType.Low_7,
        };
        
        public static InstrumentDetailInfo GetDefaultInstrumentDetailInfo()
        {
            // 因为角色缩放了 1.7倍, 因此值需要缩小 1.7倍率
            var fixScale = 1 / 1.7f;
            var curDetailInfo = new InstrumentDetailInfo
            {
                pDef = Vector3.zero,
                rDef = Vector3.zero,
                sDef = Vector3.one * fixScale
            };

            return curDetailInfo;
        }

        public static InstrumentDetailInfo ConvertInstrumentDetailInfo(InstrumentDetailInfo info)
        {
            // 因为角色缩放了 1.7倍, 因此值需要缩小 1.7倍率
            var fixScale = 1 / 1.7f;
            
            if (info == null)
                return GetDefaultInstrumentDetailInfo();

            else
            {
                info.sDef = info.sDef * fixScale;
                return info;
            }
        }
    }
}
