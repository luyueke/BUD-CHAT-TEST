using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderTextureExtensions
{
    public static Texture2D toTexture2D(this RenderTexture rt)
    {
        int width = rt.width;
        int height = rt.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        return tex;
         
    }
}
