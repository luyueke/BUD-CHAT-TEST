using System;
using System.IO;
using System.Collections.Generic;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.CI
{
    /// <summary>
    /// 获取媒体信息结果
    /// </summary>
    public sealed class GetMediaInfoResult : CosDataResult<MediaInfoResult>
    {
        public MediaInfoResult mediaInfoResult {
            get { return _data; }
        }
    }
}
