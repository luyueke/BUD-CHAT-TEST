using UI.Preview3D.Bean;

namespace UI.Preview3D.Base
{
    public interface IPreview3DHandler
    {
        void HandlePreview(Preview3DData data,PreviewWrap wrap);
        
        void CancelPreview(Preview3DData data,PreviewWrap wrap);
    }
}