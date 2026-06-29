using System.Collections;
using System.Collections.Generic;
using UI.Catalog;
using UI.Catalog.Components;
using UnityEngine;
using UnityEngine.UI;

public class ProfessionComponent : CatalogComponentBase
{
    public Text nameText;
    public Text ageText;
    public Text genderText;
    public Text avaterText;
    public Text descText;
    public RectTransform layout;

    public override void updateUI(Gallery gallery)
    {
        if (gallery != null) {
            nameText.text = gallery.name;
            ageText.text = "年龄：" + gallery.roleGallery.roleAge.ToString();
            genderText.text = "性别：" + (gallery.roleGallery.roleGender == 1 ? "男" : "女");
            avaterText.text = gallery.roleGallery.roleProfession;
            descText.text = gallery.roleGallery.roleDesc;
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        }
        
    }

}
