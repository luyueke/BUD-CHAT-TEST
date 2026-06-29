using System;

using System.Text;
using Game.COSXML.Common;
using Game.COSXML.Model.Tag;
using Game.COSXML.Network;
using Game.COSXML.CosException;

namespace Game.COSXML.Model.Bucket
{
    /// <summary>
    /// 设置 Bucket 生命周期
    /// <see href="https://cloud.tencent.com/document/product/436/8280"/>
    /// </summary>
    public sealed class GetBucketIntelligentTieringRequest : BucketRequest
    {

        public GetBucketIntelligentTieringRequest(string bucket)
            : base(bucket)
        {
            this.method = CosRequestMethod.GET;
            this.queryParameters.Add("intelligenttiering", null);
        }
    }
}
