namespace xasset.example
{
    public class PreloadRawAsset : Loadable
    {
        private RawAssetRequest _request;
        public override bool isDone => _request == null || _request.isDone;

        protected override void OnLoad()
        {
            _request = RawAsset.LoadAsync(path);
        }
    }
}