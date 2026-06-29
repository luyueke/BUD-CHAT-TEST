// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using System;
using System.Collections.Generic;

namespace GameData.UGCData
{
    
    public enum UgcShaderStyle
    {
        Normal = 0,
        Anime = 1
    }
    
    public class UGCResData
    {
        /// <summary>
        /// 模版ID
        /// </summary>
        public string id;

        public List<UGCPartData> parts;
    }

    public class UGCClothesData:UGCResData
    {
    }

    public class UGCPartData
    {
        public int type;
        public List<UGCPixelData> pixels;
        public List<TextData> texts;
        public List<PhotoData> photos;
    }
    
    public class UGCPixelData
    {
        public string p;
        public string col;
    }

    
    [Serializable]
    public class TextData:UGCImportData
    {
        public string pos;
        public string rot;
        public string sizeDelta;
        public string color;
        public string content;
        public int hierarchy;
        
        public TextData Clone()
        {
            return this.MemberwiseClone() as TextData;
        }

        
    }
    [Serializable]
    public class PhotoData:UGCImportData
    {
        public string pos;
        public string rot;
        public string sizeDelta;
        public string photoUrl;
        public int hierarchy;

        public PhotoData Clone()
        {
            return this.MemberwiseClone() as PhotoData;
        }

    }

    [Serializable]
    public class UGCImportData
    {
    }
}
