using System;
using UI.Preview3D.Bean;
using UI.Preview3D.Mono;

namespace UI.Preview3D.Base
{
    public interface IPreview3D
    {
        void Attach(Preview3DRawImage rawImage);
        void Detach(Preview3DRawImage rawImage);
        void Preview(Preview3DRawImage preview3DRawImage, Preview3DData previewData);
        void CancelPreview(Preview3DRawImage preview3DRawImage);

        void RefreshRole(Preview3DRawImage rawImage);
        // void ChangeLight(RawImage rawImage,Object lightData);
        // void PlayChangeClothAnim(RawImage rawImage);
    }
}