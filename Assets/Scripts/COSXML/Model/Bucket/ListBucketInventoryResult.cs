using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;


namespace Game.COSXML.Model.Bucket
{
    public sealed class ListBucketInventoryResult : CosDataResult<ListInventoryConfiguration>
    {
        public ListInventoryConfiguration listInventoryConfiguration {
            get {return _data; }
        }
    }
}
