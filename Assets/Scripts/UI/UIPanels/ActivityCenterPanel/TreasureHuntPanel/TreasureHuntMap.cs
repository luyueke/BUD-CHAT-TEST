using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHuntMap : MonoBehaviour
{
    [SerializeField] private List<Image> ItemIcons;
    [SerializeField] private List<CButton> GridBtns;

    public void Setup(List<TreasureVec3> positions, List<TreasureVec3> rotations, int count,
                      List<int> showedGrids, Action<int> onGridClick,
                      List<int> treasureIds, string atlasPath)
    {
        for (int i = 0; i < ItemIcons.Count; i++)
        {
            bool active = i < count;
            ItemIcons[i].gameObject.SetActive(active);
            if (active)
            {
                string spriteName = $"ItemImg{treasureIds[i]}";
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
                if (sprite != null)
                {
                    ItemIcons[i].sprite = sprite;
                    ItemIcons[i].SetNativeSize();
                }
                ItemIcons[i].rectTransform.anchoredPosition = new Vector2(positions[i].x, positions[i].y);
                ItemIcons[i].rectTransform.localEulerAngles = new Vector3(rotations[i].x, rotations[i].y, rotations[i].z);
            }
        }

        foreach (var btn in GridBtns)
        {
            if (!int.TryParse(btn.gameObject.name, out int gridId)) continue;
            bool alreadyDug = showedGrids.Contains(gridId);
            btn.gameObject.SetActive(!alreadyDug);
            if (!alreadyDug)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onGridClick(gridId));
            }
        }
    }

    public void HideGrid(int gridId)
    {
        var btn = GridBtns.Find(b => b.gameObject.name == gridId.ToString());
        if (btn != null) btn.gameObject.SetActive(false);
    }

    public Transform GetGridTransform(int gridId)
    {
        var btn = GridBtns.Find(b => b.gameObject.name == gridId.ToString());
        return btn != null ? btn.transform : null;
    }

    public void HideAllGridBtns()
    {
        foreach (var btn in GridBtns)
            if (btn != null) btn.gameObject.SetActive(false);
    }
}
