using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Catalog;
using UnityEngine;

public class UgcCataOpenItem : MonoBehaviour
{
    public PanelId cataPanelId;

    public static UgcCataOpenItem Instan;

    CButton clickButton;
    private List<Gallery> galleryList;
    public GameObject redpointItem;

    // Start is called before the first frame update
    private void Awake()
    {
        Instan = this;
    }
    void Start()
    {
        AIHospitalCataData.Inst.ugcActions += ReferenceData;
        clickButton = GetComponent<CButton>();
        clickButton.onClick.AddListener(OnClickButten);
    }
    void showRedPoint(bool isshow)
    {
        redpointItem.SetActive(isshow);
    }

    void ReferenceData(List<Gallery> _galleryList)
    {
        galleryList = _galleryList;
        UpdateRedPoint();
    }
    public void UpdateRedPoint()
    {
        // 遍历所有角色和动作
        int unclickedCount = 0;
        foreach (var gl in galleryList)
        {
            if(gl.isLock == 1) continue;

            var key = AccountDataManager.Inst.Uid + gl.galleryId;
            if (!PlayerPrefs.HasKey(key))
            {
                unclickedCount++;
            }
            
        }
        showRedPoint(unclickedCount > 0);
    }


    private void OnClickButten()
    {
        var cata = UIManager.Inst.OpenPanel<UgcCatalogPanel>(PanelId.UgcHospitalCatalogPanel);
        if(galleryList!=null)
        cata.SetCataData(galleryList , true);
        cata.ugcRedpointEvent += showRedPoint;
    }
    private void OnDestroy()
    {
        AIHospitalCataData.Inst.ugcActions -= ReferenceData;
    }


}
