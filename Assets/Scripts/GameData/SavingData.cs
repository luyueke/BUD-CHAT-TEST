using System;
using GameData.Config;

namespace GameData
{
    public class SavingData
    {
        [Serializable]
        public enum GenderType
        {
            Male = 1,
            Female = 2
        }

        [Serializable]
        public class MobileMediaFromNativeData
        {
            public string remoteUrl;
            public string localUrl;
            public string mediaType;
        }
    }

    public struct OpenSystemAlbumParams
    {
        public int albumType;// 0 视频  1 图片
        public int isCrop; //默认不裁剪
        public float cropAspectRatio; //裁剪时候传入, 裁剪的宽高比
        public bool orientation;//默认横屏
        public int length; // 视频长度， 秒为单位
    }


    public struct SaveMediaParams
    {
        public int mediaType; // 1 图片 2 视频
        public string mediaUrl;
    }

}
