using UnityEngine;
using System.Text.RegularExpressions;

[RequireComponent(typeof(SuperTextMesh))]
public class STMImageHandler : MonoBehaviour
{
    [SerializeField] private SuperTextMesh stm;
    [SerializeField] private Vector2 size = new Vector2(1.0f, 1.0f);
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.1f, 0f);
    [SerializeField] private float advance = 0.1f;  
    [SerializeField] private Texture[] textures;  
    private bool isInit = false;

    public void OnEnable()
    {
        Init();
    }

    private void OnDestroy()
    {
        Disable();
    }

    public void Init()
    {
        if (!isInit)
        {
            stm.OnPreParse += ReplaceImagesWithQuads;
            isInit = true;
        }
    }

    public void Disable()
    {
        if (isInit)
        {
            stm.OnPreParse -= ReplaceImagesWithQuads;
            isInit = false;
        }
    }
    
    void ReplaceImagesWithQuads(STMTextContainer x)
    {
        MatchCollection imageMatch = Regex.Matches(x.text, @"<q=.*?>");
        foreach (Match e in imageMatch)
        {
            string v = e.Value; 
            int startIndex = v.IndexOf('=') + 1;
            int endIndex = v.IndexOf('>'); 
            if (startIndex >= 0 && endIndex > startIndex)
            {
                string imageName = v.Substring(startIndex, endIndex - startIndex);
                AddImageQuadData(imageName);
            }
        }
    }

    private void AddImageQuadData(string imageName)
    {
        // if (stm.data.quads.ContainsKey(imageName))
        // {
        //     return;
        // }
        
        Texture texture = System.Array.Find(textures, tex => tex.name == imageName);
        
        if (texture != null)
        {
            STMQuadData imageQuad = ScriptableObject.CreateInstance<STMQuadData>();
            imageQuad.texture = texture;  
            imageQuad.name = imageName;
            imageQuad.silhouette = false;
            imageQuad.size = size;
            imageQuad.offset = offset;  
            imageQuad.advance = advance;  

            stm.data.quads[imageName] = imageQuad;
        }
        else
        {
            LoggerUtils.LogError("AddImageQuadData Texture not found: " + imageName);
        }
    }
}
