using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using Game.Database;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using System;
public class RoleSkinItem : MonoBehaviour
{
    public Image iconImage;
    public Text nametext;


    public Button selectBtn;
    public GameObject selectActionGo;
 

    public RoleSkinData data;
    public RemoteImageBehaviour remoteAssetsIcon;
    public GameObject LockObj;
    public Action<RoleSkinItem> onSelectAction;

    void Awake()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
       
        selectActionGo?.SetActive(false);
    }

    public void Init(RoleSkinData data)
    {
        this.data = data;
        nametext.text = data.Name;
        remoteAssetsIcon.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(data.skinPackInfo.cover))
        {
            remoteAssetsIcon.Load(data.skinPackInfo.cover, onCompleted: (bool fromCache, bool success) =>
            {
                remoteAssetsIcon.gameObject.SetActive(true);
            });
        }
        RefreshLock();
    }

    public void RefreshLock()
    {
        CabinSkinLockHelper.ApplyLock(LockObj, data?.ugcId, data?.creator);
    }

    void OnSelectBtnClick()
    {
        selectActionGo.SetActive(true);
        onSelectAction?.Invoke(this);
    }

}

public class RoleSkinData
{
    public SkinPackInfo skinPackInfo;
    public string Name;
    public string ugcId;
    public string creator;

    public RoleSkinData(SkinPackInfo item, string name, string ugcId = null, string creator = null)
    {
        skinPackInfo = item;
        Name = name;
        this.ugcId = ugcId;
        this.creator = creator;
    }
}