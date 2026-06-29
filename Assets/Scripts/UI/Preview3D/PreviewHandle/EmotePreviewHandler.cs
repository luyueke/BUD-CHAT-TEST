using Es;
using GameData;
using UI.Preview3D.Bean;
using UI.Preview3D.Helper;
using UnityEngine;

namespace UI.Preview3D.PreviewHandle
{
    public class EmotePreviewHandler : PlayerBasicPreviewHandler
    {
        private PlayerAnimationCtrl _playerCharacterAniCtrl;
        private PlayerAnimationCtrl _copyCharacterAniCtrl;

        public override void HandlePreview(Preview3DData data, PreviewWrap wrap)
        {
            base.HandlePreview(data, wrap);

            PlayEmote(data);
        }

        public override void CancelPreview(Preview3DData data, PreviewWrap wrap)
        {
            base.CancelPreview(data, wrap);
            _playerCharacterAniCtrl.ResetEmoteForUICharacter();

            if (_copyCharacterAniCtrl)
            {
                GameObject.Destroy(_copyCharacterAniCtrl.gameObject);
            }
        }

        private void PlayEmote(Preview3DData data)
        {
            if (_playerCharacterAniCtrl == null)
            {
                _playerCharacterAniCtrl = _characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            }

            //emote type
            var emoteId = data.PgcIdStr;
            var emoteCfg = _playerCharacterAniCtrl.GetEmoteUIConfig(emoteId);
            if (emoteCfg.emoType == (int)UIEmoteType.SinglePlayer)
            {
                PlaySingleEmote(emoteId);
            }
            else if (emoteCfg.emoType == (int)UIEmoteType.DoublePlayer)
            {
                PlayInteractEmote(emoteId);
            }
        }

        //单人表情
        private void PlaySingleEmote(string emoteId)
        {
            _playerCharacterAniCtrl.PlaySingleEmoteForUICharacter(emoteId);
        }

        //双人表情
        private void PlayInteractEmote(string emoteId)
        {
            if (_copyCharacterAniCtrl == null)
            {
                var copyCharacter = GameObject.Instantiate(_characterWrap.Avatar, _characterWrap.Avatar.transform.parent);
                _copyCharacterAniCtrl = copyCharacter.GetComponent<PlayerAnimationCtrl>();
                _copyCharacterAniCtrl.Init(_characterWrap);
                _copyCharacterAniCtrl.PlayCurEyeAni();
                
                copyCharacter.gameObject.name = "CopyCharacter";
                var sourcePos = copyCharacter.transform.localPosition;
                copyCharacter.transform.localPosition = new Vector3(sourcePos.x + 10, sourcePos.y, sourcePos.z);
                copyCharacter.gameObject.SetActive(false);
            }

            _playerCharacterAniCtrl.PlayDoubleEmoteForUICharacter(emoteId, _copyCharacterAniCtrl);
        }

        protected override PreviewParams GetPreviewPrams(Preview3DData data)
        {
            var emoteId = data.PgcIdStr;
            var emoteUICfg = DataTables.GetEmoUIConfig(emoteId);
            if (emoteUICfg == null)
            {
                return null;
            }
            return PreviewParams.ParseFromExcel(emoteUICfg.previewParamsId);
        }
    }
}