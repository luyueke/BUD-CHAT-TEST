
using BUD.AnimPose;
using Game.Avatar;
using Game.Store;
using GameData.PgcData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class CabinEmotePreviewPopPanel : BasePanel<CabinEmotePreviewPopPanel>
    {
        [SerializeField] private Transform characterRoot;
        [SerializeField] private AvatarCameraController avatarCameraController;
        [SerializeField] private Button closeBtn;

        private CharacterWrap _characterWrapper;
        private PlayerAnimationCtrl _animationCtrl;
        private CabinPgcUgcPlayController _playController;

        private CharacterData _characterData;
        private GoodsData _goodsData;

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (args.Length >= 2 && args[0] is CharacterData charData && args[1] is GoodsData goodsData)
            {
                _characterData = charData;
                _goodsData = goodsData;
            }

            closeBtn.onClick.AddListener(CloseSelf);
            InitCharacterWrapper();
        }

        private void InitCharacterWrapper()
        {
            if (_characterData == null)
                return;

            // callback 可能在 CreateUIAvatarWithIKController 内部同步触发（资源已缓存），
            // 也可能在加载完成后异步触发（需要下载）。
            // 用 callbackFired 标志区分两种情况，保证 PlayEmote 在 _playController 就绪后恰好调用一次。
            bool callbackFired = false;
            _characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(
                _characterData, characterRoot,
                callback: () =>
                {
                    callbackFired = true;
                    // 异步 case：_playController 已在下方初始化完毕，直接播放
                    if (_playController != null)
                    {
                        PlayEmote();
                    }
                });

            _animationCtrl = _characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            _playController = new CabinPgcUgcPlayController();
            _playController.Init(_animationCtrl, _characterWrapper, null, avatarCameraController);

            // 同步 case：callback 已经在上方触发并因 _playController 为 null 跳过了，在此补充调用
            if (callbackFired)
            {
                PlayEmote();
            }
        }

        private void PlayEmote()
        {
            if (_goodsData == null || _playController == null)
                return;

            var emoteData = BuildPEmoteData(_goodsData);

            if (emoteData == null)
                return;

            _playController.InitData(emoteData);
            _playController.PlayAnim();
        }

        private pEmoteData BuildPEmoteData(GoodsData goodsData)
        {
            if (goodsData == null || goodsData.Assets == null || goodsData.Assets.Count == 0)
                return null;

            var asset = goodsData.Assets[0];

            if (asset is EmoteAssetsData)
            {
                return new pEmoteData { emoteId = goodsData.Id };
            }

            if (asset is UgcAnimAssetsData)
            {
                return new pEmoteData
                {
                    emoteId = goodsData.Id,
                    ugcData = new UgcIdleData { id = goodsData.Id }
                };
            }

            return null;
        }

        public override void CloseSelf()
        {
            _playController?.CancelAnim();
            _playController = null;
            base.CloseSelf();
        }
    }
}
