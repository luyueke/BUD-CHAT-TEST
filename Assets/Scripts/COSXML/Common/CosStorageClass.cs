using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Common;
using Game.COSXML.Utils;

namespace Game.COSXML.Common
{
    public enum CosStorageClass
    {
        /// <summary>
        /// 标准储存
        /// </summary>
        [CosValue("Standard")]
        Standard = 0,

        /// <summary>
        /// 低频存储
        /// </summary>
        [CosValue("Standard_IA")]
        StandardIA = 1,

        /// <summary>
        /// 归档储存
        /// </summary>
        [CosValue("ARCHIVE")]
        Archive
    }
}
