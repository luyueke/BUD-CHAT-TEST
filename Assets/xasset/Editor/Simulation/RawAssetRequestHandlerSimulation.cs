using System.IO;

namespace xasset.editor
{
    public struct RawAssetRequestHandlerSimulation : RawAssetRequestHandler
    {
        private RawAssetRequest request { get; set; }

        public void OnStart()
        {
            if (File.Exists(request.info.path))
            {
                request.path = request.info.path;
                request.SetResult(Request.Result.Success);
                return;
            }

            request.SetResult(Request.Result.Failed, "File not found.");
        }

        public void Update()
        {
        }

        public void WaitForCompletion()
        {
        }

        public static RawAssetRequestHandler CreateInstance(RawAssetRequest rawAssetRequest)
        {
            return new RawAssetRequestHandlerSimulation {request = rawAssetRequest};
        }
    }
}