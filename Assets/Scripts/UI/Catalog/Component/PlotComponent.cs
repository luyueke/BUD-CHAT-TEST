using System.Collections;
using System.Collections.Generic;
using UI.Catalog;
using UI.Catalog.Components;
using UnityEngine;
using UnityEngine.UI;
public class PlotComponent : CatalogComponentBase
{
    public Text descText;

    public override void updateUI(Gallery gallery)
    {
        descText.text = gallery.roleGallery.rolePlot;
    }

}
