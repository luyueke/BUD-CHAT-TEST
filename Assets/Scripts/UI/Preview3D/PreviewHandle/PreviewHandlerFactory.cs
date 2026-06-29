using UI.Preview3D.Base;

namespace UI.Preview3D.PreviewHandle
{
    public static class PreviewHandlerFactory
    {
        public static BasicPreviewHandler GetHandlerByType(Preview3DType pType)
        {
            switch (pType)
            {
                case Preview3DType.Avatar:
                    return new ClothPreviewHandler();
                case Preview3DType.Emote:
                    return new EmotePreviewHandler();
                case Preview3DType.ThemeSkin:
                    return new ThemeSkinBasicHandler();
                //TODO:拓展其他类型
                default:
                    return null;
            }
        }
    }
}