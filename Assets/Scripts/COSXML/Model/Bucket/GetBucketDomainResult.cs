using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketDomainResult : CosDataResult<DomainConfiguration>
    {
        public DomainConfiguration domainConfiguration {
            get{ return _data; }
        }
    }
}
