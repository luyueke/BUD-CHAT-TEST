// @Author: YangJie
// @Description:
// @Date:  2023/07/20
// @Modify:

using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;

namespace GameData.MapData
{
    public class MapGlobalData
    {
        public UgcBaseInfo curUgcBaseInfo;
        public UgcBaseInfo originUgcBaseInfo;
        
        public BaseCreator Creator;
        public BaseInteractInfo InteractInfo;
        public SkinActionInfo skinActionInfo;
        public VehicleInfo vehicleInfo;
        
        /// <summary>
        /// 获取当前UGC信息,
        /// 1、对于素材编辑器来说，当前信息就是 素材信息 UgcItemInfo,
        /// 2、对于地图编辑器来说，当前信息就是 地图信息 MapInfo,
        /// 3、对于衣服编辑器来说，当前信息就是 衣服信息 UGCClothInfo,
        /// 4、对于材质编辑器来说，当前信息就是 材质信息 UGCMaterialInfo,
        /// 5. 对于载具编辑器来说，当前信息就是 载具信息 VehicleInfo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetCurInfo<T>() where T : UgcBaseInfo
        {
            return curUgcBaseInfo as T;
        }
        
        public T GetOriginInfo<T>() where T : UgcBaseInfo
        {
            return originUgcBaseInfo as T;
        }

        public void ClearData()
        {
            this.curUgcBaseInfo = null;
            this.originUgcBaseInfo = null;
            this.Creator = null;
            this.InteractInfo = null;
            this.skinActionInfo = null;
        }
    }
}