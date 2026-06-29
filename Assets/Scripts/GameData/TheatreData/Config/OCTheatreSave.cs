using System.Collections.Generic;

namespace GameData.Config
{
    public class OCTheatreSave
    {
        /// <summary>
        /// 每个剧本的进度
        /// </summary>
        public Dictionary<string, int> theatreSave = new Dictionary<string, int>();
        /// <summary>
        /// 每个剧本被手动自定义的角色
        /// </summary>
        public Dictionary<string, OCAvatarReplace> avatarReplaceDict = new Dictionary<string, OCAvatarReplace>();
    
        /// <summary>
        /// 每个剧本的玩家的剧情游玩路径
        /// </summary>
        public Dictionary<string, List<int>> theatrePaths = new Dictionary<string, List<int>>();

        /// <summary>
        /// 每个剧本最后生效的背景图URL
        /// </summary>
        public Dictionary<string, string> theatreBackground = new Dictionary<string, string>();
    }

    public class OCAvatarReplace
    {
        public string avatarID; //角色卡id
        public int avatarClothesIndex; //角色卡中的衣柜穿着序号
    }

    [System.Serializable]
    public class TheatreActorSaveItem
    {
        public string playerId;
        public string avatarName;
        public string avatarURL;
        public int clothesIndex;
        public int origin; // 0=Default, 1=My, 2=Room
        public int siblingIndex;
    }

}