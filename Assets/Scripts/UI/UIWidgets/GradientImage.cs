using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 继承自 Image，支持在图片上叠加渐变颜色。
/// 渐变通过修改顶点色实现，与图片原色相乘混合。
/// </summary>
[AddComponentMenu("UI/Gradient Image")]
public class GradientImage : Image
{
    public enum GradientDir
    {
        Vertical,       // 垂直：上→下
        Horizontal,     // 水平：左→右
        DiagonalLTR,    // 对角：左上→右下
        DiagonalRTL,    // 对角：右上→左下
    }

    [SerializeField] private GradientDir _gradientDir = GradientDir.Vertical;
    [SerializeField] private Color _colorStart = Color.white;
    [SerializeField] private Color _colorEnd = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private bool _flip = false;

    public GradientDir Direction
    {
        get => _gradientDir;
        set { _gradientDir = value; SetVerticesDirty(); }
    }

    public Color ColorStart
    {
        get => _colorStart;
        set { _colorStart = value; SetVerticesDirty(); }
    }

    public Color ColorEnd
    {
        get => _colorEnd;
        set { _colorEnd = value; SetVerticesDirty(); }
    }

    public bool Flip
    {
        get => _flip;
        set { _flip = value; SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        base.OnPopulateMesh(toFill);
        ApplyGradient(toFill);
    }

    private void ApplyGradient(VertexHelper vh)
    {
        int count = vh.currentVertCount;
        if (count == 0) return;

        var vertices = new List<UIVertex>(count);
        for (int i = 0; i < count; i++)
        {
            var v = new UIVertex();
            vh.PopulateUIVertex(ref v, i);
            vertices.Add(v);
        }

        // 计算包围盒
        float minX = vertices[0].position.x, maxX = minX;
        float minY = vertices[0].position.y, maxY = minY;
        for (int i = 1; i < count; i++)
        {
            float x = vertices[i].position.x;
            float y = vertices[i].position.y;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        float width  = maxX - minX;
        float height = maxY - minY;

        for (int i = 0; i < count; i++)
        {
            UIVertex vertex = vertices[i];
            float t = GetT(vertex.position, minX, minY, width, height);
            if (_flip) t = 1f - t;

            // 渐变色与顶点原色相乘（保留 Image.color 的影响）
            Color gradientColor = Color.Lerp(_colorStart, _colorEnd, t);
            vertex.color = MultiplyColors(vertex.color, gradientColor);
            vh.SetUIVertex(vertex, i);
        }
    }

    private float GetT(Vector3 pos, float minX, float minY, float width, float height)
    {
        switch (_gradientDir)
        {
            case GradientDir.Horizontal:
                return width > 0 ? (pos.x - minX) / width : 0f;
            case GradientDir.DiagonalLTR:
                float wh = width + height;
                return wh > 0 ? ((pos.x - minX) + (pos.y - minY)) / wh : 0f;
            case GradientDir.DiagonalRTL:
                float wh2 = width + height;
                return wh2 > 0 ? ((pos.x - minX) + (height - (pos.y - minY))) / wh2 : 0f;
            default: // Vertical：顶部是 Start (t=0)，底部是 End (t=1)
                return height > 0 ? 1f - (pos.y - minY) / height : 0f;
        }
    }

    private static Color32 MultiplyColors(Color32 a, Color b)
    {
        return new Color32(
            (byte)(a.r * b.r),
            (byte)(a.g * b.g),
            (byte)(a.b * b.b),
            (byte)(a.a * b.a)
        );
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
