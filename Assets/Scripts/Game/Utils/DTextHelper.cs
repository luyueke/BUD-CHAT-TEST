using System;
using UnityEngine;

namespace Game.Utils {
    public static class DTextHelper {

        const float maxWidth = 20f;
        const float maxHeight = 2f;

        public static void SetContent(this SuperTextMesh textPro, string content) {
            textPro.text = content;
            textPro.alignment = SuperTextMesh.Alignment.Center;
            RebuildUntilFontAtlasStable(textPro);
            var width = textPro.preferredWidth;
            var height = textPro.preferredHeight;
            if (width > maxWidth || height > maxHeight)
            {
                textPro.alignment = SuperTextMesh.Alignment.Left;
                RebuildUntilFontAtlasStable(textPro);
            }
            var textCollider = textPro.GetComponent<BoxCollider>();
            textCollider.size = new Vector3(Math.Min(textPro.preferredWidth, maxWidth), textPro.preferredHeight, 0);
        }

        public static void SetColor(this SuperTextMesh textPro, Color color) {
            textPro.color = color;
            textPro.Rebuild();
        }

        // Calls Rebuild() repeatedly until no font atlas reorganization happens during the call.
        // When the atlas is reorganized (new glyphs are added), characters processed before the
        // reorganization get stale UV coordinates. A subsequent rebuild—after all glyphs are
        // already in the atlas—produces correct UVs for every character.
        // Font.textureRebuilt is a synchronous Unity event, so it fires inside Rebuild().
        internal static void RebuildUntilFontAtlasStable(SuperTextMesh textPro)
        {
            const int maxRetries = 3;
            bool atlasRebuilt = false;
            int retryCount = 0;
            System.Action<Font> onAtlasRebuilt = _ => atlasRebuilt = true;
            for (int i = 0; i < maxRetries; i++)
            {
                atlasRebuilt = false;
                Font.textureRebuilt += onAtlasRebuilt;
                textPro.Rebuild();
                Font.textureRebuilt -= onAtlasRebuilt;
                retryCount = i + 1;
                if (!atlasRebuilt)
                {
                    break;
                }
            }
            if (retryCount > 1)
            {
                Debug.Log($"[DTextHelper] 字体图集发生重建，共重试 {retryCount} 次，文字=\"{textPro.text}\"，节点={textPro.gameObject.name}");
            }
            if (atlasRebuilt)
            {
                Debug.LogWarning($"[DTextHelper] 字体图集在 {maxRetries} 次重试后仍不稳定！文字=\"{textPro.text}\"，节点={textPro.gameObject.name}");
                if (textPro.font != null)
                {
                    var failedChars = new System.Text.StringBuilder();
                    foreach (var c in textPro.text)
                    {
                        if (!textPro.font.GetCharacterInfo(c, out _, textPro.quality))
                        {
                            failedChars.Append(c);
                        }
                    }
                    if (failedChars.Length > 0)
                    {
                        var atlasTex = textPro.font.material?.mainTexture;
                        int aw = atlasTex != null ? atlasTex.width : -1;
                        int ah = atlasTex != null ? atlasTex.height : -1;
                        Debug.LogWarning($"[DTextHelper] 写入图集失败的字符：\"{failedChars}\"，字体={textPro.font.name}，图集尺寸={aw}x{ah}，文字=\"{textPro.text}\"，节点={textPro.gameObject.name}");
                    }
                }
            }
        }

    }
}
