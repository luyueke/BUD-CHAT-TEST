using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketWebsiteResult : CosDataResult<WebsiteConfiguration>
    {
        public WebsiteConfiguration websiteConfiguration {
            get {return _data; }
        }
    }
}
