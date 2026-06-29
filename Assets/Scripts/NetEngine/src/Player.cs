
namespace NetEngine
{
    public class Player
    {
        public static string Id => AccountDataManager.Inst.Uid;

        public static string OpenId => AccountDataManager.Inst.Uid;

        public static string Name => AccountDataManager.Inst.UserInfo.nickname;

        public static string AvatarJson => AccountDataManager.Inst.UserInfo.avatarJson;

        public static string PetAvatarJson => AccountDataManager.Inst.PetInfo.avatarJson;

        public static string HeadUrl => AccountDataManager.Inst.UserInfo.portraitUrl;

        public static int HiddenPet => AccountDataManager.Inst.PetInfo.isGameHidden;
        
        public static string PetName => AccountDataManager.Inst.PetInfo.nickname;

        // public static string TeamId => GamePlayerInfo.GetInfo().TeamId;
        // public static ulong CustomPlayerStatus => GamePlayerInfo.GetInfo().CustomPlayerStatus;

        // public static string CustomProfile => GamePlayerInfo.GetInfo().CustomProfile;

        // public static NetworkState CommonNetworkState => GamePlayerInfo.GetInfo().CommonNetworkState;
        //
        // public static NetworkState RelayNetworkState => GamePlayerInfo.GetInfo().RelayNetworkState;

        // public static long Timestamp => GamePlayerInfo.GetInfo().Timestamp;
    }
}
