// @Author: YangJie
// @Description:
// @Date:  2023/08/29
// @Modify:

using GameData.Base;

namespace GameData.BaseInfo
{
    public class MaterialInfo: UgcBaseInfo
    {
        /// <summary>材质贴图Url</summary>
        public string materialUrl;

        public string[] imgs;
        
        public PaymentInfo paymentInfo;
        
        //画板格子数：32*32，64*64
        public int canvasType;
        public override string templateId { get; set; } = "990100001";

        public int ugcStyle;//0：正常 1：二次元风格
    }
}