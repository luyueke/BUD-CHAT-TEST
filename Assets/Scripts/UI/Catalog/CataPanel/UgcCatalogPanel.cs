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

public class UgcCatalogPanel : BaseCatalogPanel
{

    public GameObject redPointImage;

    private void Start()
    {
        
    }

    

    public override void OnCreate()
    {
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
