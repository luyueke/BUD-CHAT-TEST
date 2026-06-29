using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 空，替代仅热区的透明Image
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class EmptyImage : MaskableGraphic
{
    protected EmptyImage()
    {
        useLegacyMeshGeneration = false;
    }
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();
    }
}