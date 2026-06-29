using GameData.PgcData;

namespace Es
{
    public partial class DataTables {


        /// <summary>
        /// 融合宠物及角色 Avatar 接口
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AvatarCommonData GetAvatarData(string id) {
            var resType = GameData.PgcData.UniqueType.GetPgcResType(id);
            if (resType == ResourceType.Avatar || resType == ResourceType.UgcAvatar) {
                return s_instance.AvatarCommonDataTable.Get(id);
            } else if (resType == ResourceType.PGCPetAvatar || resType == ResourceType.UGCPetAvatar) {
                return s_instance.PetAvatarCommonDataTable.Get(id);
            } else {
                return null;
            }
        }
    }

}
