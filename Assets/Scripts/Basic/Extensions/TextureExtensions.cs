
using Game.Utils;
using UnityEngine;

namespace Utils.Extensions
{
    public static class TextureExtensions
    {
        public static Color[] Scale(this Texture2D source, int targetWidth, int targetHeight)
        {
            if (source.width == targetWidth && source.height == targetHeight)
            {
                return source.GetPixels();
            }
            // var scaledTexture = new Texture2D(targetWidth, targetHeight, source.format, false);

            Color[] texturePixels = source.GetPixels();
            Color[] scaledPixels = new Color[targetWidth * targetHeight];

            float ratioX = 1.0f / ((float)targetWidth / (source.width - 1));
            float ratioY = 1.0f / ((float)targetHeight / (source.height - 1));

            for (int y = 0; y < targetHeight; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                int yCeiling = (int)Mathf.Ceil(y * ratioY);
                float yBlend = y * ratioY - yFloor;

                for (int x = 0; x < targetWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    int xCeiling = (int)Mathf.Ceil(x * ratioX);
                    float xBlend = x * ratioX - xFloor;

                    Color pixel1 = texturePixels[yFloor * source.width + xFloor];
                    Color pixel2 = texturePixels[yFloor * source.width + xCeiling];
                    Color pixel3 = texturePixels[yCeiling * source.width + xFloor];
                    Color pixel4 = texturePixels[yCeiling * source.width + xCeiling];

                    Color blendedPixel = Color.Lerp(Color.Lerp(pixel1, pixel2, xBlend), Color.Lerp(pixel3, pixel4, xBlend), yBlend);
                    scaledPixels[y * targetWidth + x] = blendedPixel;
                }
            }

            // scaledTexture.SetPixels(scaledPixels);
            // scaledTexture.Apply();
            UnityEngine.Object.Destroy(source);
            return scaledPixels;
        }
    

        /// <summary>
        /// 转换为 Gamma 颜色空间
        /// </summary>
        /// <param name="texture"></param>
        public static void Gamma(this Texture2D texture) {
            if (texture == null)
            {
                return;
            }
            try
            {
                var rawData = texture.GetRawTextureData<Color32>();
                for (int i = 0; i < rawData.Length; i++) {
                    rawData[i] = rawData[i].ToGama();
                }
                texture.Apply();
            }
            catch (System.Exception ex)
            {
                LoggerUtils.LogError("Texture Gamma Error:" + ex.Message);
            }
        
        }

    }
}