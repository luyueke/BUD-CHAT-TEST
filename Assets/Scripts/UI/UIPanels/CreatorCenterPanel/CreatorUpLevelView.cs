using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreatorUpLevelView : MonoBehaviour
{
    [SerializeField] private Transform transBg;
    [SerializeField] private GameObject level2;
    [SerializeField] private GameObject level3;
    [SerializeField] private GameObject level4;
    [SerializeField] private Button backBtn;
    public void Awake()
    {
        InitBGUI();
        backBtn.onClick.AddListener(()=>gameObject.SetActive(false));
    }
    public void Show(int level)
    {
        gameObject.SetActive(true);
        level2.SetActive(level == (int)CreatorTitleType.NewCreator);
        level3.SetActive(level == (int)CreatorTitleType.LightChaserCreator);
        level4.SetActive(level == (int)CreatorTitleType.PopularCreator);
    }
    private void InitBGUI()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#AA8DFE", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
        });
        itemObj.gameObject.SetActive(true);
    }

}
