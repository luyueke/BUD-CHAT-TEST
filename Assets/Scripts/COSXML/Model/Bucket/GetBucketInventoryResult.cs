using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;
namespace Game.COSXML.Model.Bucket
{
    public sealed class GetBucketInventoryResult : CosDataResult<InventoryConfiguration>
    {

        public InventoryConfiguration inventoryConfiguration {
            get{ return _data; }
        }
    }
}
