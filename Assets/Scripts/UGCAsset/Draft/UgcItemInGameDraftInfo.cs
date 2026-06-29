/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-06 11:03:15
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-06 18:57:40
 * @ Description: 游戏内上传素材的信息(不会保存为草稿)
 */

using GameData.BaseInfo;

namespace UGCAsset.Draft
{
    public class UgcItemInGameDraftInfo : BaseDraftInfo<UgcItemInGameDraftInfo, PropInfo>
    {
        protected sealed override string CoverRemoteFolder => $"UgcItemCover/{uid}";
        protected sealed override string MetaDataRemoteFolder => $"UgcItemMetadata/{uid}";
        public DetailInfo detailInfo;
        public UgcItemInGameDraftInfo()
        {
        }

        public UgcItemInGameDraftInfo(PropInfo propInfo): base(propInfo)
        {
        }

        public override PropInfo ToUgcInfo()
        {
            var ugcInfo = base.ToUgcInfo();
            if (detailInfo == null)
            {
                detailInfo = new DetailInfo();
            }
            ugcInfo.detailInfo = detailInfo;
            return ugcInfo;
        }

    }
}
