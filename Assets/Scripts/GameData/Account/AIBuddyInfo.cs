using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.UGCData;
using UnityEngine;


namespace GameData.Account
{
    public enum AIBuddyOp
    {
        Create = 0,
        Delete = 1,
        EditProfile = 2,
        EditAvatar = 3,
        SetInLobby = 5, // 大厅切换AIBuddy
        SetHidden = 6, // 设置是否在大厅展示
        SetIdleData = 7, // 设置大厅的待机动画
    }
    public class AIBuddyInfo
    {
        public string uid;
        public string id;
        public string npcId;
        public string conversationId;
        public string homepageUrl;
        public string homepageColor;
        public int intimacyRate;
        public string intimacyRateDegree;
        public AINpcInfo npc;
        public List<string> summoningEffects;//召唤特效，第一个为当前使用特效，如果为空，则表示使用默认
        public int isHidden;
        public int setInLobby;


        /// <summary>
        /// 大厅待机动画
        /// </summary>
        public IdleData idleData;

        public int CompareTo(AIBuddyInfo other)
        {
            if (other == null) {
                return -1;
            }
            bool idEqual = this.id == other.id;
            bool uidEqual = this.uid == other.uid;
            bool npcEqual = this.npcId == other.npcId;
            bool isEqual = idEqual && uidEqual && npcEqual;
            // 只比较是否相等、不比较大小
            return isEqual ? 0 : -1;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public static AIBuddyInfo GetDefaultInfo() {
            return new AIBuddyInfo() {
                uid = "",
                id = "",
                npcId = "",
                conversationId = "",
                homepageUrl = "",
                homepageColor = "",
                intimacyRate = 0,
                intimacyRateDegree = "",
                npc = new AINpcInfo() {
                    npcAvatarJson = "{\"partDatas\":[]}"
                },
                isHidden = 1,
                idleData = null,
            };
        }

        public void CopyFrom(AIBuddyInfo other) {
            this.uid = other.uid;
            this.id = other.id;
            this.npcId = other.npcId;
            this.conversationId = other.conversationId;
            this.homepageUrl = other.homepageUrl;
            this.homepageColor = other.homepageColor;
            this.intimacyRate = other.intimacyRate;
            this.intimacyRateDegree = other.intimacyRateDegree;
            this.npc = other.npc.Clone();
            this.isHidden = other.isHidden;
            this.idleData = other.idleData;

        }

    }
}
