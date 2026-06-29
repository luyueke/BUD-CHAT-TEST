using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Common;
using Game.COSXML.Utils;

namespace Game.COSXML.Common
{
    public enum CosMetaDataDirective
    {
        [CosValue("Copy")]
        Copy = 0,

        [CosValue("Replaced")]
        Replaced
    }
}
