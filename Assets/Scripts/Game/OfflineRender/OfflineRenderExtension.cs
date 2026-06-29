// @Author: YangJie
// @Description:
// @Date:  2023/09/21
// @Modify:

using Game.OfflineRender;
using Game.Props.PropsComponents;
using GameData;

namespace Game.Base
{
    public static class OfflineRenderExtension
    {
        public static MaterialComponent GetMatComponent(this UGCMatData matData)
        {
            var matComp = new MaterialComponent()
            {
                matId = new MaterialUnionID(matData.matId, matData.uMatId)
            };
            if (!string.IsNullOrEmpty(matData.color))
            {
                matComp.color = DataUtil.DeSerializeColor(matData.color);
            }
            if (!string.IsNullOrEmpty(matData.tile))
            {
                matComp.tile = DataUtil.DeSerializeVector2(matData.tile);
            }
            return matComp;
        }

    }
}
