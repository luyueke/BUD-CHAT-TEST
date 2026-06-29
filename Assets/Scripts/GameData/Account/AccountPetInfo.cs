using Game.Avatar;
using Newtonsoft.Json;

namespace GameData.Account {
    public class AccountPetInfo {
        public string id;
        public string creator;
        public string avatarJson;
        public string nickname { get; set; }
        public int isHidden;
        public int isGameHidden;
        public IdleData idleData;
        [JsonIgnore]
        public PetData avatarInfo
        {
            get
            {
                if (string.IsNullOrEmpty(avatarJson))
                {
                    avatarJson = "{\"partDatas\":[{\"Id\":\"70400007\"},{\"Id\":\"72200001\",\"Cr\":\"#C192FF\"}]}";
                }

                var tempData = PetData.DeserializeObject(avatarJson);
                return tempData;

            }
        }

        public int CompareTo(AccountPetInfo other)
        {
            bool idEqual = this.id == other.id;
            bool creatorEqual = this.creator == other.creator;
            bool avatarEqual = this.avatarJson == other.avatarJson;
            bool idleEqual = this.idleData == other.idleData;
            bool nickEqual = this.nickname == other.nickname;
            bool visibleEqual = this.isHidden == other.isHidden;
            bool isEqual = idEqual && creatorEqual && avatarEqual && idleEqual && visibleEqual&&nickEqual;
            // 只比较是否相等、不比较大小
            return isEqual ? 0 : -1;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public static AccountPetInfo GetDefaultInfo() {
            return new AccountPetInfo() {
                id = "",
                creator = "",
                avatarJson = "{\"partDatas\":[{\"Id\":\"70400007\"},{\"Id\":\"72200001\",\"Cr\":\"#C192FF\"}]}",
                isHidden = 1 // 这里加上
            };
        }

    }
}
