using UnityEngine;

public class RandomMoveGameObject : MonoBehaviour
{
    private readonly string TexturePath = "Assets/Loadable/AnimationsExpress/Textures/";

    public bool isChangeTexture;

    public void ChangeTexture(string name, int id)
    {

        Texture texture =
            XAssetLoaderMgr.Inst.LoadResource<Texture>(TexturePath + name + "/" + id + ".png",
                gameObject);
        MeshRenderer render = GetComponent<MeshRenderer>();
        if (render != null)
        {
            render.material.SetTexture("_BaseMap", texture);
            render.material.SetTexture("_MainTex", texture);
            render.material.SetTexture("_EmissionMap", texture);
            return;
        }

        SkinnedMeshRenderer skinnedRender = GetComponent<SkinnedMeshRenderer>();
        LoggerUtils.Log(skinnedRender);
        if (skinnedRender != null)
        {
            skinnedRender.material.mainTexture = texture;
            return;
        }

    }
}
