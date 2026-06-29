using UnityEngine;

namespace GameData.Base
{
    public class BaseCreator
    {
        public string uid;
        public string username;
        public string nickname;
        public int gender;
        public string portraitUrl;
        public string bio;
        public long birthday;
        public string avatarJson;
        public int avatarFrame;

        public string petAvatarJson;

        public RelationShipInfo relationShipInfo;

        public static implicit operator BaseCreator(AccountUserInfo userInfo) {
            return new BaseCreator() {
                uid = userInfo.uid,
                username = userInfo.username,
                nickname = userInfo.nickname,
                gender = userInfo.gender,
                portraitUrl = userInfo.portraitUrl,
                bio = userInfo.bio,
                birthday = userInfo.birthday,
                avatarJson = userInfo.avatarJson,
                avatarFrame = userInfo.avatarFrame,

            };
        }
        public static implicit operator AccountUserInfo(BaseCreator creator) {
            return new AccountUserInfo() {
                uid = creator.uid,
                username = creator.username,
                nickname = creator.nickname,
                gender = creator.gender,
                portraitUrl = creator.portraitUrl,
                bio = creator.bio,
                birthday = creator.birthday,
                avatarJson = creator.avatarJson,
                avatarFrame = creator.avatarFrame,
                
            };
        }

    }
}
