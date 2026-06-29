using System;
using System.IO;
using System.Collections.Generic;
using Game.COSXML.Model.Tag;
using Game.COSXML.Transfer;

namespace Game.COSXML.Model.CI
{
    /// <summary>
    /// 图片审核结果
    /// </summary>
    public sealed class SensitiveContentRecognitionResult : CosDataResult<SensitiveRecognitionResult>
    {

        /// <summary>
        /// 图片审核结果
        /// </summary>
        /// <value></value>
        public SensitiveRecognitionResult recognitionResult {
            get {return _data; }
        }
    }
}
