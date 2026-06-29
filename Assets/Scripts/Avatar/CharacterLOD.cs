using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class CharacterLOD : MonoBehaviour
{
    public void Awake()
    {
        LODGroup = gameObject.GetOrAddComponent<LODGroup>();
        SetLodMesh(gameObject);
    }

    #region LOD
    
    private LODGroup LODGroup;
    private HashSet<Renderer> rendererList = new();
    
    public void SetLodMesh(List<GameObject> gos)
    {
        foreach (var go in gos) SetLodMesh(go);
    }

    public void SetLodMesh(List<GameObject> gos1, List<GameObject> gos2)
    {
        foreach (var go in gos1) SetLodMesh(go);
        foreach (var go in gos2) SetLodMesh(go);
    }

    public void SetLodMesh(GameObject go)
    {
        if (go == null) return;

        if (LODGroup == null) return;

        var Lods = LODGroup.GetLODs();

        if (Lods.Length == 0) return;

        var lod = Lods[0];

        for (int i = 0, L = lod.renderers.Length; i < L; i++)
        {
            if (lod.renderers[i] != null)
            {
                rendererList.Add(lod.renderers[i]);
            }
        }

        var Renderers = go.GetComponentsInChildren<MeshRenderer>();
        if (Renderers != null && Renderers.Length > 0)
        {
            for (int i = 0, L = Renderers.Length; i < L; i++)
            {
                rendererList.Add(Renderers[i]);
            }
        }

        var Renderers1 = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (Renderers1 != null && Renderers1.Length > 0)
        {
            for (int i = 0, L = Renderers1.Length; i < L; i++)
            {
                rendererList.Add(Renderers1[i]);
            }
        }

        CancelInvoke("ResetLodMeshSize");
        Invoke("ResetLodMeshSize", 0.01f);

    }

    private void ResetLodMeshSize()
    {
        var lod = new LOD()
        {
            renderers = rendererList.ToArray(),
            screenRelativeTransitionHeight = 0.02f
        };
        LODGroup.SetLODs(new LOD[1] { lod });
        LODGroup.RecalculateBounds();
        rendererList.Clear();
    }

    #endregion
}
