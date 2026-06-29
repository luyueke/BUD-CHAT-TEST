namespace xasset
{
    public abstract class LoadRequest : Request, IRecyclable
    {
        public int _refCount { private set; get; }
        public string path { get; set; }

        protected override void OnCompleted()
        {
            Logger.D($"Load {path} {result}.");
            Recorder.EndSample(path);
        }

        public virtual void Release()
        {
            if (_refCount == 0)
            {
                Logger.E($"Release {path} too many times {_refCount}.");
                return;
            }

            _refCount--;
            if (_refCount > 0) return;

            Recycler.RecycleAsync(this);
        }

        protected abstract void OnDispose();

        public void WaitForCompletion()
        {
            if (isDone) return;

            if (status == Status.Wait) Start();

            OnWaitForCompletion();
        }

        protected virtual void OnWaitForCompletion()
        {
        }

        protected virtual void LoadAsync()
        {
            if (_refCount > 0)
            {
                if (isDone) ActionRequest.CallAsync(Complete);
            }
            else
            {
                SendRequest();
                Recycler.CancelRecycle(this);
                Recorder.BeginSample(path, this);
            }

            _refCount++;
        }

        #region IRecyclable

        public void EndRecycle()
        {
            Logger.D($"Unload {path}.");
            OnDispose();
            Recorder.Unload(path);
        }

        public virtual bool CanRecycle()
        {
            return true;
        }

        public virtual void RecycleAsync()
        {
        }

        public virtual bool Recycling()
        {
            return false;
        }

        #endregion
    }
}