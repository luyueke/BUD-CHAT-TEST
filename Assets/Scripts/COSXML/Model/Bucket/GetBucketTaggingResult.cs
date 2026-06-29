using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketTaggingResult : CosDataResult<Tagging>
    {
        public Tagging tagging {
            get {return _data; }
        }
    }
}
