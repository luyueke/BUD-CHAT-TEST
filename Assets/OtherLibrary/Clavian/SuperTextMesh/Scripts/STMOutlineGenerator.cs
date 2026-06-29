//Copyright (c) 2020-2021 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
/*
 
Make SURE that outlineParent is a gameObject that is above STM in the hierarchy for UI elements.
Script isn't perfect yet, sometimes gameobjects don't get deleted when they should, but it seems to work fine in Play mode.
 
*/
 
[ExecuteInEditMode]
public class STMOutlineGenerator : MonoBehaviour {
 
    public SuperTextMesh superTextMesh;
    [Tooltip("Make sure this is ABOVE your STM in the hierarchy for UI objects!!")]
    public Transform outlineParent;
    [Range(0,16)]
    public int detailLevel = 8;
    public float size = 0.05f;
    public Color32 color = Color.black;
    [Tooltip("Offset text with this")]
    public Vector3 offset = new Vector3(0f,0f,0.05f);
    public bool updateEveryFrame = true;
    //public float distance = 0.001f;
 
    private Mesh sharedOutlineMesh = null;
    private List<GameObject> outlineObjects = new List<GameObject>();
 
    [System.Serializable]
    public class OutlineRenderer
    {
        public Transform transform;
        public Vector3 offset;
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public CanvasRenderer canvasRenderer;
    }
    private List<OutlineRenderer> allRenderers = new List<OutlineRenderer>();
 
    private OutlineRenderer newRenderer;
 
    public void Reset()
    {
        superTextMesh = GetComponent<SuperTextMesh>();
        //outlineParent = this.transform;
    }
    public void OnEnable()
    {
        if(Application.isPlaying)
        {
            superTextMesh.OnRebuildEvent += GenerateOutlines;
        }
    }
    public void OnDisable()
    {
        if(Application.isPlaying)
        {
            superTextMesh.OnRebuildEvent -= GenerateOutlines;
        }
    }
    private bool validate;
    public void OnValidate()
    {
        validate = true;
    }
    public void Update()
    {   
        if(!Application.isPlaying && validate)
        {
            validate = false;
            GenerateOutlines();
        }
    }
    public void LateUpdate()
    {
        if(Application.isPlaying && updateEveryFrame)
        {
            RefreshOutlines();
        }
    }
    public void GenerateOutlines(Vector3[] verts, Vector3[] middles, Vector3[] positions)
    {
        GenerateOutlines();
    }
    public void RefreshOutlines()
    {   
        for(int i=0; i<detailLevel; i++)
        {
            if(superTextMesh.uiMode)
            {
                allRenderers[i].canvasRenderer.SetMesh(superTextMesh.textMesh);
            }
            else
            {
                CloneTextMesh();
                allRenderers[i].meshFilter.sharedMesh = sharedOutlineMesh;
            }
        }
    }
    private void CloneTextMesh()
    {
        sharedOutlineMesh = new Mesh();
        if(superTextMesh.textMesh == null)
            return;
        
        sharedOutlineMesh.vertices = superTextMesh.textMesh.vertices;
        sharedOutlineMesh.triangles = superTextMesh.textMesh.triangles;
        sharedOutlineMesh.normals = superTextMesh.textMesh.normals;
        sharedOutlineMesh.uv = superTextMesh.textMesh.uv;
        sharedOutlineMesh.uv2 = superTextMesh.textMesh.uv2;
        //colors
        colors = superTextMesh.textMesh.colors32;
        for(int j=0; j<colors.Length; j++)
        {
            colors[j] = color; //assign outline color
        }
        sharedOutlineMesh.colors32 = colors;
    }
 
    private Color32[] colors;
    public void GenerateOutlines()
    {
        if(superTextMesh != null && outlineParent != null)
        {
            sharedOutlineMesh = null; //clear last mesh
 
            for(int i=0; i<detailLevel; i++)
            {
                
                if(i >= allRenderers.Count)
                {
                    //create outline gameobjects
                    newRenderer = new OutlineRenderer();
 
                    allRenderers.Add(newRenderer);
 
                    //see if an outlineobject exists
                    if(i < outlineParent.childCount)
                    {
                        newRenderer.gameObject = outlineParent.GetChild(i).gameObject;
                        newRenderer.transform = newRenderer.gameObject.transform;
                        //outlineObjects[i] = newRenderer.gameObject;
                        outlineObjects.Add(newRenderer.gameObject);
 
                        if(superTextMesh.uiMode)
                        {
                            newRenderer.canvasRenderer = newRenderer.gameObject.GetComponent<CanvasRenderer>();
                        }
                        else
                        {
                            newRenderer.meshFilter = newRenderer.gameObject.GetComponent<MeshFilter>();
                            newRenderer.meshRenderer = newRenderer.gameObject.GetComponent<MeshRenderer>();
                        }
                    }
                    else
                    {
                        newRenderer.gameObject = new GameObject();
                        outlineObjects.Add(newRenderer.gameObject);
                        newRenderer.transform = newRenderer.gameObject.transform;
                        newRenderer.transform.name = "Outline";
                        newRenderer.transform.SetParent(outlineParent); //parent to STM
 
                        //UI text
                        if(superTextMesh.uiMode)
                        {
                            newRenderer.canvasRenderer = newRenderer.gameObject.AddComponent<CanvasRenderer>();
                        }
                        //regular text
                        else
                        {
                            
                            newRenderer.meshFilter = newRenderer.gameObject.AddComponent<MeshFilter>();
                            newRenderer.meshRenderer = newRenderer.gameObject.AddComponent<MeshRenderer>();
                
                        }
                    }
 
                    
 
                }
                else
                {
                    newRenderer = allRenderers[i];
                }
 
                outlineObjects[i].SetActive(true);
                //UI text
                if(superTextMesh.uiMode && superTextMesh.c.GetMaterial() != null)
                {
                    newRenderer.canvasRenderer.SetMesh(superTextMesh.textMesh);
                    if(superTextMesh.c.GetMaterial().HasProperty("_MainTex"))
                    {
                        newRenderer.canvasRenderer.SetTexture(superTextMesh.c.GetMaterial().GetTexture("_MainTex"));
                        
                        newRenderer.canvasRenderer.materialCount = 1;
                        newRenderer.canvasRenderer.SetMaterial(superTextMesh.c.GetMaterial(), 0);
                    }
                    newRenderer.canvasRenderer.SetColor(color);
                }
                //regular text
                else if(superTextMesh.r.sharedMaterials != null)
                {
                    if(sharedOutlineMesh == null)
                    {
                        CloneTextMesh();
                    }
 
                    newRenderer.meshFilter.sharedMesh = sharedOutlineMesh;
                    newRenderer.meshRenderer.sharedMaterials = superTextMesh.r.sharedMaterials;
                }
                //give it an offset... for now uhh
                newRenderer.offset.x = (superTextMesh.t.position.x + Mathf.Cos(Mathf.PI * 2f * ((float)i/detailLevel)) * size) + offset.x;
                newRenderer.offset.y = (superTextMesh.t.position.y + Mathf.Sin(Mathf.PI * 2f * ((float)i/detailLevel)) * size) + offset.y;
                newRenderer.offset.z = (superTextMesh.t.position.z) + offset.z;
 
                newRenderer.transform.position = newRenderer.offset;
                newRenderer.transform.rotation = superTextMesh.t.rotation;
                newRenderer.transform.localScale = superTextMesh.t.localScale;
 
                //newRenderer.transform
            }
            //disable extra renderers
            for(int i=detailLevel; i<outlineObjects.Count; i++)
            {
                outlineObjects[i].SetActive(false);
            }
        }
    }
}