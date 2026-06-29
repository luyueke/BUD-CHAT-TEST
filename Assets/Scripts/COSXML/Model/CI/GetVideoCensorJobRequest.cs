using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Common;
using Game.COSXML.Model.Object;
using Game.COSXML.Model.Tag;
using Game.COSXML.CosException;
using Game.COSXML.Utils;

namespace Game.COSXML.Model.CI
{
    /// <summary>
    /// 查询视频审核任务
    /// <see href="https://cloud.tencent.com/document/product/436/47316"/>
    /// </summary>
    public sealed class GetVideoCensorJobRequest : CIRequest
    {
        public GetVideoCensorJobRequest(string bucket, string JobId)
            : base(bucket)
        {
            this.method = CosRequestMethod.GET;
            this.SetRequestPath("/video/auditing/" + JobId);
        }
    }
}
