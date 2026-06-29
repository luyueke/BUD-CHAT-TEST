using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using Newtonsoft.Json;
using UnityEngine;
namespace GameData.BaseInfo
{
    public enum AnimResType
    {
        PGC = 0,
        UGC
    }

    
    public class AnimPropData
    {
        public int index;//对应道具列表索引(最多5个)
        public int bindIndex;
        public string id;
        public string metaDataUrl;
        /// <summary>封面图Url</summary>
        public string cover;
        // [JsonIgnore]
        // public PropInfo info;
    }

    public class AnimInfo : UgcBaseInfo
    {
        public int skinType;
        //1.单人动作
        //3.双人动作
        //5.宠物单人动作
        //7.宠物双人动作
        public int animType;

        public int frameFrequency = 10;

        // 1 循环 0 非循环
        public int loop;

        public List<AnimBgmTrackInfo> animBgmTrackInfos = new List<AnimBgmTrackInfo>();
        //动画用到的音效 UID
        public List<string> musicList;
        public float animationTime;
        //动画用到的素材 UID
        public List<AnimPropData> propList;  
        public int isBan;
        public PaymentInfo paymentInfo;
    }

    public class AnimBgmTrackInfo
    {
        //音轨Id
        public int trackId; 
        //当前音轨用到的BGM
        public List<AnimMusicInfo> animMusicList = new List<AnimMusicInfo>();
    }
}
