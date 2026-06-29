using System.Collections.Generic;
using Es;
using Game.UGCEditor;
using GameData.UGCData;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Basic;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.Pet;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.UI;
using UI.Base;

public class UGCEditTipsPanel : BasePanel<UGCEditTipsPanel>
{
    public Transform content;
    public GameObject point;
    public CButton leftBtn;
    public CButton rightBtn;
    public CButton closeBtn;
    public Image icon;
    string tipsSpriteAlxsPath = "Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/UGCEditTipsAlxs.spriteatlas";


    int currId = 0;

    public override void OnCreate()
    {
        base.OnCreate();
        leftBtn.onClick.AddListener(()=> {
            OnChanggeClick(-1);
        });
        rightBtn.onClick.AddListener(() => {
            OnChanggeClick(1);
        });
        closeBtn.onClick.AddListener(() => {
            CloseSelf();
        });
        SetData(currId);
    }
    void OnChanggeClick(int next)
    {
        currId += next;
        if (currId < 0)
        {
            currId = 0;
        }
        if (currId > 3)
        {
            currId = 3;
        }
        SetData(currId);
    }
    void SetData(int index)
    {
        icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tipsSpriteAlxsPath, index.ToString(), gameObject);
        if (content != null && content.childCount > 0)
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
        }
        for (int i = 0; i <= 3; i++)
        {
            var item = GameObject.Instantiate(point, content);
            if (i == currId)
            {
                item.GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tipsSpriteAlxsPath, "Point_Acticity", item);
            }
            else
            {
                item.GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tipsSpriteAlxsPath, "Point_Default", item);
            }
        }
    }

}
