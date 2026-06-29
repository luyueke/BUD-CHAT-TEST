using UnityEngine;

// [ExecuteInEditMode]
public class water_cubemap_animation : MonoBehaviour
{
    // Start is called before the first frame update
    // public Material Water_cubemap_urp;
    public GameObject WaterGameObject;
    public float Offset_Normal1_Speed;
    public float Offset_Normal2_Speed;
    private float timer = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        // WaterPlane.GetComponent<MeshRenderer>().material.SetFloat("_Offset_Normal1",timer);
        // Water_cubemap_urp.SetFloat("_Offset_NormalX_1", 10);
        // print(timer);
        WaterGameObject.GetComponent<MeshRenderer>().material.SetFloat("_BumpMap_1_Offset_X", timer * Offset_Normal1_Speed);
        WaterGameObject.GetComponent<MeshRenderer>().material.SetFloat("_BumpMap_1_Offset_Y", timer * Offset_Normal1_Speed);
        WaterGameObject.GetComponent<MeshRenderer>().material.SetFloat("_BumpMap_2_Offset_X", timer * Offset_Normal2_Speed);
        WaterGameObject.GetComponent<MeshRenderer>().material.SetFloat("_BumpMap_2_Offset_Y", timer * Offset_Normal2_Speed);
        //if(timer > 10){
            //timer = 0;
        //}
        
    }
}
