using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Bucket
{
    public sealed class ListBucketVersionsResult : CosDataResult<ListBucketVersions>
    {
        public ListBucketVersions listBucketVersions {
            get {return _data; }
        }
    }
}
