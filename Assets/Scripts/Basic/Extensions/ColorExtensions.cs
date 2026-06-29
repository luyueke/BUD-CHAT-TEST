using UnityEngine;

namespace Game.Utils
{

    public static class ColorExtensions
    {
        public static Color32 ToGama(this Color32 color)
        {
            float r = color.r / 255f;
            float g = color.g / 255f;
            float b = color.b / 255f;
            float a = color.a / 255f;
            r = Mathf.LinearToGammaSpace(r);
            g = Mathf.LinearToGammaSpace(g);
            b = Mathf.LinearToGammaSpace(b);
            a = Mathf.LinearToGammaSpace(a);
            color = new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), (byte)(a * 255f));
            return color;
        }
    }
}

