namespace xasset
{
    public static class RawAsset
    {
        public static RawAssetRequest Load(string path)
        {
            var request = RawAssetRequest.GetAsync(path);
            request?.WaitForCompletion();
            return request;
        }

        public static RawAssetRequest LoadAsync(string path)
        {
            return RawAssetRequest.GetAsync(path);
        }
    }
}