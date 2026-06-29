using System.Collections.Generic;
using Basic.Utils;
using GameData.Base;
using GameData.OfflineRender;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData.BaseInfo
{
    public class InstrumentInfo
    {
        //动作Id
        public string moveId;
        //音色信息
        public ToneInfo toneInfo;
        //绑定信息
        public InstrumentDetailInfo animDetailInfo;
    }

    public class InstrumentDetailInfo
    {
        /// <summary>
        /// 默认位置
        /// </summary>
        public Vector3 pDef;

        /// <summary>
        /// 默认旋转
        /// </summary>
        public Vector3 rDef;

        /// <summary>
        /// 默认缩放
        /// </summary>
        public Vector3 sDef = Vector3.one;
    }

    public class ToneInfo : UgcBaseInfo
    {
        //是否PGC音色
        public int isPgc;
        //音色类型 15，22音节
        public int toneType;
        public Dictionary<int, SyllableData> toneDict = new Dictionary<int, SyllableData>();
        public int isDelete = 0;
        public PaymentInfo paymentInfo;

        public bool IsPgc()
        {
            if (id.Contains("pgc"))
                return true;
            
            return isPgc == 1;
        }
        
        //初始化 默认15音节
        public void Init()
        {
            SetToneType(ToneType.Fifteen);
        }
        
        public void SetToneType(ToneType type)
        {
            this.toneType = (int)type;
            InitToneDict(type);
        }

        public void SetToneDict(List<string> toneList)
        {
            toneDict = new Dictionary<int, SyllableData>();
            for (int i = 0; i < toneList.Count; i++)
            {
                int index = i + 1;
                toneDict.Add(index,new SyllableData{url = toneList[i]});
            }
        }
        public void InitToneDict(ToneType type)
        {
            toneDict = new Dictionary<int, SyllableData>();
            
            toneDict.Add((int)SyllableType.High_1, null);
            toneDict.Add((int)SyllableType.High_2, null);
            toneDict.Add((int)SyllableType.High_3, null);
            toneDict.Add((int)SyllableType.High_4, null);
            toneDict.Add((int)SyllableType.High_5, null);
            toneDict.Add((int)SyllableType.High_6, null);
            toneDict.Add((int)SyllableType.High_7, null);
            toneDict.Add((int)SyllableType.Double_High, null);
            
            toneDict.Add((int)SyllableType.Middle_1, null);
            toneDict.Add((int)SyllableType.Middle_2, null);
            toneDict.Add((int)SyllableType.Middle_3, null);
            toneDict.Add((int)SyllableType.Middle_4, null);
            toneDict.Add((int)SyllableType.Middle_5, null);
            toneDict.Add((int)SyllableType.Middle_6, null);
            toneDict.Add((int)SyllableType.Middle_7, null);
            
            if (type == ToneType.TwentyTwo)
            {
                toneDict.Add((int)SyllableType.Low_1, null);
                toneDict.Add((int)SyllableType.Low_2, null);
                toneDict.Add((int)SyllableType.Low_3, null);
                toneDict.Add((int)SyllableType.Low_4, null);
                toneDict.Add((int)SyllableType.Low_5, null);
                toneDict.Add((int)SyllableType.Low_6, null);
                toneDict.Add((int)SyllableType.Low_7, null);
            }
        }
    }

    public class ToneLanguageData
    {
        /// <summary>
        /// 中文为 0，英文为 1，日文为 2
        /// </summary>
        public int type;
        public string voiceId;
        public string voiceUrl;
    }


    public class CabinToneInfo : UgcBaseInfo
    {
        //是否PGC音色
        public int isPgc;
        public int isDelete = 0;
        public PaymentInfo paymentInfo;
        public List<ToneLanguageData> languageList = new List<ToneLanguageData>();

        public bool IsPgc()
        {
            if (id.Contains("pgc"))
                return true;

            return isPgc == 1;
        }
    }


    //单个音节数据
    public class SyllableData{
        public string url;
    }

    //音色类型 15，22音节
    public enum ToneType
    {
        Default = 0,
        Fifteen = 1,
        TwentyTwo = 2,
        Both = 3, //全都支持
    }
    
    //音节类型
    public enum SyllableType
    {
        High_1 = 1,
        High_2 = 2,
        High_3 = 3,
        High_4 = 4,
        High_5 = 5,
        High_6 = 6,
        High_7 = 7,
        Double_High = 8,
        
        Middle_1 = 9,
        Middle_2 = 10,
        Middle_3 = 11,
        Middle_4 = 12,
        Middle_5 = 13,
        Middle_6 = 14,
        Middle_7 = 15,
        
        Low_1 = 16,
        Low_2 = 17,
        Low_3 = 18,
        Low_4 = 19,
        Low_5 = 20,
        Low_6 = 21,
        Low_7 = 22,
    }

    public enum LongShortType
    {
        Short = 1,
        Long = 2,
        Both = 3,
    }

    public class EditToneInfoReq
    {
        public ToneInfo musicToneInfo;
        public int setType;
    }


}
