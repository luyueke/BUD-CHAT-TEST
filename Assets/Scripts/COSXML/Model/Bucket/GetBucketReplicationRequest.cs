using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Common;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketReplicationRequest : BucketRequest
    {
        public GetBucketReplicationRequest(string bucket)
            : base(bucket)
        {
            this.method = CosRequestMethod.GET;
            this.queryParameters.Add("replication", null);
        }
    }
}
