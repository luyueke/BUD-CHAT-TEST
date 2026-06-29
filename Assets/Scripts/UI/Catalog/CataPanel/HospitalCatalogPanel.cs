using System;
using System.Collections;
using System.Collections.Generic;
using UI.Catalog;
using UnityEngine;
using Network;
using Network.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GameData;
using Game.Avatar;
using Es;

public class HospitalCatalogPanel : BaseCatalogPanel
{
    
    public GameObject redPointImage;

    static public HospitalCatalogPanel Instant;


    private void Start()
    {
        
    }

    

    public override void OnCreate()
    {
        Instant = this;
        base.OnCreate();
    }

    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    public override void OnWindowBeCovered(bool isCover)
    {
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowShow()
    {
    }
}
