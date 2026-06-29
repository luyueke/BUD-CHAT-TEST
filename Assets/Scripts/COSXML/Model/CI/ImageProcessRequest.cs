using System;
using System.Collections.Generic;

using System.Text;
using Game.COSXML.Common;
using Game.COSXML.Model.Object;
using Game.COSXML.CosException;

namespace Game.COSXML.Model.CI
{
    public sealed class ImageProcessRequest : ObjectRequest
    {
        public ImageProcessRequest(string bucket, string key, string operationRules)
            : base(bucket, key)
        {

            if (operationRules == null)
            {
                throw new CosClientException((int)CosClientError.InvalidArgument, "operationRules = null");
            }

            this.method = CosRequestMethod.POST;
            this.queryParameters.Add("image_process", null);
            this.headers.Add("Pic-Operations", operationRules);
        }

    }
}
