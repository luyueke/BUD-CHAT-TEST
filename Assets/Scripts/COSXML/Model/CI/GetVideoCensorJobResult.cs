using System;
using System.IO;
using System.Collections.Generic;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.CI
{
    /// <summary>
    /// 视频审核结果
    /// </summary>
    public sealed class GetVideoCensorJobResult : CosDataResult<VideoCensorResult>
    {
        /// <summary>
        /// 视频审核结果
        /// </summary>
        /// <value></value>
        public VideoCensorResult resultStruct {
            get {return _data; }
        }
    }
}
