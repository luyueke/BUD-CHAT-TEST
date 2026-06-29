using UnityEngine;

public class ColorViewUtils
{
    public static void ReloadColorList(AvatarMenuType avatarMenuType, IAvatarRoleItem avatarPgcRoleView)
    {
        switch (avatarMenuType)
        {
            case AvatarMenuType.Eyebrow:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.EYE_ALL, AvatarColorManager.EYE_COM);
            }
                break;
            case AvatarMenuType.Hair:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.HAIR_ALL, AvatarColorManager.HAIR_COM);
            }
                break;
            case AvatarMenuType.FacePainting:
            {
                avatarPgcRoleView
                    .ReloadColorList(AvatarColorManager.FACESTYLE_ALL, AvatarColorManager.FACESTYLE_COM);
            }
                break;
            case AvatarMenuType.Glasses:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.GLASSES_ALL, AvatarColorManager.GLASSES_COM);
            }
                break;
            case AvatarMenuType.Visor:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.VISOR_ALL, AvatarColorManager.VISOR_COM);
            }
                break;
            case AvatarMenuType.Bag:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.BAG_ALL, AvatarColorManager.BAG_COM);
            }
                break;
            case AvatarMenuType.Eye:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.EYE_ALL, AvatarColorManager.EYE_COM);
            }
                break;
            case AvatarMenuType.Headwear:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.HAT_ALL, AvatarColorManager.HAT_COM);
            }
                break;
            case AvatarMenuType.Earrings:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.EARRINGS_ALL, AvatarColorManager.EARRINGS_COM);
            }
                break;
            case AvatarMenuType.Blush:
            {
                avatarPgcRoleView.ReloadColorList(AvatarColorManager.FACESTYLE_ALL, AvatarColorManager.FACESTYLE_COM);

            }
                break;
        }
    }
}