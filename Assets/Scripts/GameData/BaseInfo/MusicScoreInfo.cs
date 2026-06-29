// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System.Collections.Generic;
using Basic.Utils;
using GameData.Base;
using GameData.OfflineRender;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData.BaseInfo {
    public class MusicScoreInfo : UgcBaseInfo {
        //音色类型 15，22音节
        public int toneType;
        /// 审核图 URL
        public string verifyUrl;
        public int isBan;
        public PaymentInfo paymentInfo;
        public List<MusicScorePartInfo> partList;
        public int bpm;
        public void AddPart()
        {
            if (partList==null)
            {
                partList = new List<MusicScorePartInfo>();
            }
            if (partList.Count>=4)
            {
                return;
            }
            var partInfo = new MusicScorePartInfo();
            partInfo.SetDefSyllableInfosList();
            partList.Add(partInfo);
        }
        public void DeletePart(int partId)
        {
            if (partList!=null)
            {
                if (partList.Count>partId)
                {
                    partList.RemoveAt(partId);
                }
            }
        }
    }
    public class MusicScorePartInfo
    {
        //单个音的同时播放的音节列表
        public List<MusicScoreSyllableInfo> syllableInfosList;

        public void SetDefSyllableInfosList()
        {
            if (syllableInfosList==null)
            {
                syllableInfosList = new List<MusicScoreSyllableInfo>();
                for (int i = 0; i < 16; i++)
                {
                    syllableInfosList.Add(new MusicScoreSyllableInfo());
                }
            }
            
        }
    }
    public class MusicScoreSyllableInfo
    {
        //单个音的同时播放的音节列表
        public List<int> syllablesList;
        public int syllableType;
    
        //是否被编辑过
        public bool HasEdit()
        {
            return !(syllableType == (int)MusicScoreSyllableType.Syllables &&(syllablesList == null || syllablesList.Count == 0));
        }
    }

    public enum MusicScoreSyllableType
    {
        Syllables,
        Long,
        Empty
    }
}
