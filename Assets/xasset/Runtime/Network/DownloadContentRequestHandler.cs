namespace xasset
{
    public interface DownloadContentRequestHandler
    {
        void OnStart();
        void OnPause(bool paused);
        bool Update();
        void OnCancel();
    }
}