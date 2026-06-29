using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Object
{
    /// <summary>
    /// 查询特定分块上传中的已上传的块返回的结果
    /// <see href="https://cloud.tencent.com/document/product/436/7747"/>
    /// </summary>
    public sealed class ListPartsResult : CosDataResult<ListParts>
    {
        /// <summary>
        /// 已上传块的所有信息
        /// <see href="Model.Tag.ListParts"/>
        /// </summary>
        public ListParts listParts {
            get {return _data; }
        }
    }
}
