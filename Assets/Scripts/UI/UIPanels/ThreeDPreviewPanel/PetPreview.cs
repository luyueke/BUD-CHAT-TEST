using Es;
using Game.Avatar;
using Game.Pet;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

public class PetPreview : MonoBehaviour
{
    public Transform petRoot;
    public PetWrap petWrap;
    public UIDragUtil dragUtil;

    public void StartPreview(SkinInfo ugcInfo)
    {
        this.gameObject.SetActive(true);
        
        PetData tempPetData =  AccountDataManager.Inst.PetInfo.avatarInfo.Clone();
        tempPetData.ChangeSkinData(ugcInfo);

        if (petWrap == null)
        {
            petWrap = PetAvatarController.Inst.CreateUIAvatar(tempPetData);
            petWrap.SetParent(petRoot, true);
            petWrap.Avatar.transform.localPosition = Vector3.zero;
        }
        else
        {
            petWrap.RefreshAvatar(tempPetData);
        }

        dragUtil.RotateTarget = petWrap.Avatar.transform;
    }
}
