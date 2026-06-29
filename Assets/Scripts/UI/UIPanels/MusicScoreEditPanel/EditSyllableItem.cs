using System;
using System.Collections.Generic;
using Game.Base;
using Game.CommunityGame;
using Game.PropStore;
using GameData;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class EditSyllableItem : MonoBehaviour
{
    [SerializeField]private CButton setButton;
    [SerializeField]private List<GameObject> syllableItems;
    [SerializeField]private GameObject noneItem;
    [SerializeField]private GameObject emptySyllableItem;
    [SerializeField]private GameObject longSyllableItem;
    public MusicScoreSyllableInfo curInfo;
    public int curId;
    public void Init(Action<int,MusicScoreSyllableInfo> onSetClick)
    {
        setButton.onClick.AddListener(()=>onSetClick?.Invoke(curId,curInfo));
    }
    public void SetId(int id)
    {
       curId = id;
    }
    public void SetShow(MusicScoreSyllableInfo info)
    {
        if (info==null)
        {
            return;
        }

        curInfo = info;
        SetAllHide();
        switch (info.syllableType)
        {
            case (int)MusicScoreSyllableType.Syllables:
                if (info.syllablesList==null||info.syllablesList.Count==0)
                {
                    noneItem.SetActive(true);
                    return;
                }
                var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
                if (info.syllablesList.Count == 1)
                {
                    syllableItems[1].SetActive(true);
                    var icon = GameObjectEx.FindComponentByName<Image>(syllableItems[1], "SyllableIcon");
                    icon.sprite =  XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, "Syllable_Icon_"+info.syllablesList[0], gameObject);
                }
                else
                {
                    for (int i = 0; i < info.syllablesList.Count; i++)
                    {
                        syllableItems[i].SetActive(true);
                        var icon = GameObjectEx.FindComponentByName<Image>(syllableItems[i], "SyllableIcon");
                        icon.sprite =  XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, "Syllable_Icon_"+info.syllablesList[i], gameObject);
                    }
                }
                break;
            case (int)MusicScoreSyllableType.Long:
                longSyllableItem.SetActive(true);
                break;
            case (int)MusicScoreSyllableType.Empty:
                emptySyllableItem.SetActive(true);
                break;
        }
    }

    public void SetAllHide()
    {
        for (int i = 0; i < syllableItems.Count; i++)
        {
            syllableItems[i].SetActive(false);
        }
        noneItem.SetActive(false);
        emptySyllableItem.SetActive(false);
        longSyllableItem.SetActive(false);
    }
}
