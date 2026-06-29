using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketLoggingResult : CosDataResult<BucketLoggingStatus>
    {
        public BucketLoggingStatus bucketLoggingStatus {
            get{ return _data; }
        }
    }
}
