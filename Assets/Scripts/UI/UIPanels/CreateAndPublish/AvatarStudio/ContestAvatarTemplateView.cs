using System;
using System.Collections.Generic;
using Es;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class ContestAvatarTemplateView : AvatarStudioBaseView
{
    private const string tempItemPath =
        "Assets/Loadable/UI/UIPanel/CreateAndPublish/AvatarStudio/AvatarUgcTemplateItem.prefab";

    private const string tempSpriteatlasPath =
        "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

    #region 模板选择页面改版

    private Transform contentParent;

    #endregion

    public Action<ClothesTemplate, bool> ClickTemplateAction;
    private bool isPet = false;

    protected override void Init()
    {
        base.Init();
        contentParent = GameObjectEx.FindChildByName(transform, "ContentParent");
    }

    public void SetData(List<string> templateIds, bool isPet = false)
    {
        if (contentParent == null)
        {
            Init();
        }

        this.isPet = isPet;

        var iconAtlas =
            Loader.Load<SpriteAtlas>(
                tempSpriteatlasPath, this.gameObject);
        var configData = !isPet
            ? DataTables.GetClothesTemplateList()
            : DataTables.GetPetClothesTemplateList();

        List<ClothesTemplate> orderedData = new List<ClothesTemplate>();

        if (templateIds == null || templateIds.Count == 0)
        {
            orderedData = configData;
        }
        else
        {
            for (int i = 0; i < templateIds.Count; i++)
            {
                var item = configData.Find(x => x.Id == templateIds[i]);
                if (item != null)
                {
                    orderedData.Add(item);
                }
            }
        }

        for (int i = 0; i < orderedData.Count; i++)
        {
            var data = orderedData[i];
            var itemGo = Loader.Load<GameObject>(tempItemPath).Instantiate(contentParent);
            var itemComp = itemGo.GetComponent<AvatarUgcTemplateItem>();
            itemComp.OnItemCreate(data, OnSelectTempItem, iconAtlas , currentStyle == CharacterStyle.Avatar);
        }
    }

    private void OnSelectTempItem(ClothesTemplate cData)
    {
        ClickTemplateAction?.Invoke(cData, isPet);
    }
}