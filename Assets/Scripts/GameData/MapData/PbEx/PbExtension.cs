// @Author: YangJie
// @Description:
// @Date:  2023/09/15
// @Modify:

using Basic.Extensions;
using Basic.Utils;
using Game.Config;
using Google.Protobuf;
using UnityEngine;

namespace Pb.Map
{
    public static class PbExtension
    {

        /// <summary>
        /// 若是素材ID为非空, 则返回素材 ID， 否则计算 MD5
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        public static string GetMD5(this PNodeData nodeData)
        {
            var tmpNodeData = nodeData.Clone();
            tmpNodeData.Pos = Vector3.zero.ToPB();
            tmpNodeData.Scale = Vector3.one.ToPB();
            tmpNodeData.Rotation = Vector3.zero.ToPB();
            tmpNodeData.Uid = 0;
            tmpNodeData.PropId = "";
            foreach (var childNodeData in tmpNodeData.Prims)
            {
                childNodeData.Uid = 0;
            }
            var nodeBytes = tmpNodeData.ToByteArray();
            return nodeBytes.GetMd5Hash();
        }
    }
}
