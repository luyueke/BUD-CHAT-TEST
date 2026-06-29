using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using Newtonsoft.Json;
using UnityEngine;
namespace GameData.BaseInfo
{
    public enum AINpcAnimType
    {
        Idle = 0,
        Assist = 1,
    }
    
    public class AINpcInfo : UgcBaseInfo
    {
        public string npcName;
        public int npcAge;
        public int npcGender = 1;//1:男 2:女
        public string npcAvatarJson;
        public int animResType;// 0:pgc 1:ugc
        public List<AINpcIdleAnim> npcAnimations;
        public List<string> npcPetPhrases;
        public string npcPortraitUrl;
        public string npcDesc; // 资料卡的描述
        
        public int isBan;
        public PaymentInfo paymentInfo;
        // [JsonIgnore]
        // public Dictionary<int, AINpcIdleAnim> npcAnimDic = new Dictionary<int, AINpcIdleAnim>();

        // public void ConvertDirToArray()
        // {
        //     npcAnimations = new List<AINpcIdleAnim>();
        //     foreach (var keyValue in npcAnimDic)
        //     {
        //         npcAnimations.Add(keyValue.Value);
        //     }
        // }
        //
        // public void ConvertArrayToDir()
        // {
        //     if (npcAnimations != null)
        //     {
        //         npcAnimDic.Clear();
        //         foreach (var npcAnim in npcAnimations)
        //         {
        //             npcAnimDic.Add(npcAnim.npcAnimationType,npcAnim);
        //         }
        //     }
        // }
    }
    
    public class AINpcIdleAnim
    {
        public int npcAnimationType;
        public List<string> pgcIdleList;
        public List<NpcUgcIdleData> ugcIdleList;
    }
}
