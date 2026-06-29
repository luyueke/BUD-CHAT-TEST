using Es;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Game.Pet;
using UnityEngine;

public class BundlePreview : MonoBehaviour
{
    public Transform characterRoot;
    public BaseAvatarWrapper characterWrap;
    public UIDragUtil dragUtil;
    public PreviewBundleItemsList list;

    private Action<SkinInfo> infoChange;

    public void StartPreview(SkinInfo ugcInfo, Action<SkinInfo> action)
    {
        infoChange = action;
        this.gameObject.SetActive(true);

        list.isOnAction = OnItemSelected;

        BaseAvatarData tempAvatarInfo = null;
        if (ugcInfo.skinType == (int)SkinType.Avatar)
        {
            tempAvatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        }
        else
        {
            tempAvatarInfo = AccountDataManager.Inst.PetInfo.avatarInfo.Clone();
        }
       
        List<SkinInfo> datas = new List<SkinInfo>();
        foreach(var str in ugcInfo.bundleItems)
        {
            var data = JsonConvert.DeserializeObject<SkinInfo>(str);
            datas.Add(data);
        }


        if (characterWrap == null)
        {
            if (ugcInfo.skinType == (int)SkinType.Avatar)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo as CharacterData);
            }
            else
            {
                characterWrap = PetAvatarController.Inst.CreateUIAvatar(tempAvatarInfo as PetData);
            }
            characterWrap.SetParent(characterRoot, true);
        }
        else
        {
            characterWrap.RefreshAvatar(tempAvatarInfo);
        }

        list.SetTarget(datas);
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
    }

    private void OnItemSelected(SkinInfo info)
    {
        infoChange?.Invoke(info);
        
        if (info.skinType != (int)SkinType.Avatar)
        {
            PetData tempAvatarInfo = AccountDataManager.Inst.PetInfo.avatarInfo.Clone();
            tempAvatarInfo.ChangeSkinData(info);
            characterWrap.RefreshAvatar(tempAvatarInfo);
        }
        else
        {
            CharacterData tempAvatarInfo =AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
            tempAvatarInfo.ChangeSkinData(info);
            characterWrap.RefreshAvatar(tempAvatarInfo);
        }
       
        
    }
}
