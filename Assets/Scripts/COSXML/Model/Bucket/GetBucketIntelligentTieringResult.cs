using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketIntelligentTieringResult : CosDataResult<IntelligentTieringConfiguration>
    {
        public IntelligentTieringConfiguration configuration {
            get{ return _data; }
        }
    }
}
