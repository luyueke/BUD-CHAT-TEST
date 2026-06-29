using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.Object
{
    /// <summary>
    /// 使用Append接口的返回Result.
    /// </summary>
    public sealed class AppendObjectResult : CosResult
    {
        /// <summary>
        /// Append结果信息
        /// <see href="Model.Tag.CopyObject"/>
        /// </summary>
        public long nextAppendPosition {get; set;}

        internal override void InternalParseResponseHeaders()
        {
            List<string> values;

            this.responseHeaders.TryGetValue("x-cos-next-append-position", out values);

            if (values != null && values.Count > 0)
            {
                long tmpPosition;
                if (long.TryParse(values[0], out tmpPosition))
                {
                    nextAppendPosition = tmpPosition;
                }
            }
        }
    }
}
