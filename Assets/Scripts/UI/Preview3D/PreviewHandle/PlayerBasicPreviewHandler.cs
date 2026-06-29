using Game.Avatar;
using UI.Preview3D.Base;
using UI.Preview3D.Bean;

namespace UI.Preview3D.PreviewHandle
{
    public class PlayerBasicPreviewHandler : BasicPreviewHandler
    {
        protected CharacterWrap _characterWrap;

        public override void HandlePreview(Preview3DData data, PreviewWrap wrap)
        {
            base.HandlePreview(data, wrap);
            wrap.PreviewRawImage.SetGestureEnable(true);
            CreateUIAvatar(wrap);
        }

        public override void CancelPreview(Preview3DData data, PreviewWrap wrap)
        {
            base.CancelPreview(data, wrap);
            // wrap.CharacterWrap?.Avatar.SetActive(false);
        }

        protected virtual void CreateUIAvatar(PreviewWrap wrap)
        {
            if (wrap.CharacterWrap == null)
            {
                CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
                var modelRoot = wrap.PreviewModelRoot.previewModel;
                wrap.CharacterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
                wrap.CharacterWrap.SetParent(modelRoot, true);
                wrap.CharacterWrap.Avatar.gameObject.name = "PlayerAvatarPreview";
            }

            _characterWrap = wrap.CharacterWrap;
            _characterWrap.Avatar.SetActive(true);
        }
    }
}