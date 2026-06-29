using System;
using UI.Preview3D.Bean;

namespace UI.Preview3D.Base
{
    public abstract class IPreview3DDataConvertor<T> : IPreview3DDataConvertor where T : class
    {
        public abstract Preview3DData Convert(T srcData);
        public Preview3DData ConvertBase(Object srcData)
        {
            return Convert(srcData as T);
        }
    }

    public interface IPreview3DDataConvertor
    {
        Preview3DData ConvertBase(Object srcData);
    }
}