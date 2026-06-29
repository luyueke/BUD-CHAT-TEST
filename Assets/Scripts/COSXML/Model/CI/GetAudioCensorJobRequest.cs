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
    /// 查询音频审核任务
    /// <see href="https://cloud.tencent.com/document/product/436/54064"/>
    /// </summary>
    public sealed class GetAudioCensorJobRequest : CIRequest
    {
        public GetAudioCensorJobRequest(string bucket, string JobId)
            : base(bucket)
        {
            this.method = CosRequestMethod.GET;
            this.SetRequestPath("/audio/auditing/" + JobId);
        }
    }
}
