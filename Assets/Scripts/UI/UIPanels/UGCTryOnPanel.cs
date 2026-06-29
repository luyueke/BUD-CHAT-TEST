using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData.PgcData;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UGCTryOnPanel : BasePanel<UGCTryOnPanel>
{
    [SerializeField] private CButton backBtn;
    [SerializeField] private GameObject loadingText;
    [SerializeField] private RemoteImageBehaviour outfitImg;
    [SerializeField] private CText outfitName;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private UIDragUtil dragUtil;

    private CharacterWrap characterWrap;

    public override void OnCreate()
    {
        base.OnCreate();

        backBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var clothesInfo = args[0] as SkinInfo;
        ShowUGCClothes(clothesInfo);
        ShowCharacter();

        characterWrap.ChangeUGCPart(clothesInfo);
    }

    private void ShowCharacter()
    {
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;

        characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        characterWrap.SetParent(characterRoot, true);

        dragUtil.RotateTarget = characterWrap.Avatar.transform;
    }

    private void ShowUGCClothes(SkinInfo clothesInfo)
    {
        outfitName.text = clothesInfo.name;
        outfitImg.Load(clothesInfo.cover, true, (bool fromCache, bool success) =>
        {
            if (success)
            {
                loadingText.SetActive(false);
            }
        });
    }
}
