using System;
using System.Collections.Generic;
using Game.Audio;
using Game.Avatar;
using GameData.Rewards;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel.GashaponPages;
using UI.UIPanels.RewardPanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GashaponPanel
{
    /// <summary>
    /// 扭蛋抽奖动画
    /// </summary>
    public class GashaponTwistAnimView : MonoBehaviour
    {
        [SerializeField] private Transform TwistGashaponMachineRoot;
        [SerializeField] private Transform TwistCharacterRoot;
        [SerializeField] private CButton BackToPreviewStyleBtn;
        [SerializeField] private RawImage bgRawImage;
        private CharacterWrap _twistCharacterWrap;
        private PlayerAnimationCtrl _twistPlayerAnimationCtrl;
        private Animator _twistGashaMachineAnimator;

        private GashaponTwistBall _gashaponTwistBall;
        private BudTimer _onceAnimTimer;
        private BudTimer _tenTimesAnimTimer;
        private BudTimer _tenTimesAnimTimer2;
        private BudTimer _ballShowTimer;

        private Action _backAction;

        private string curGashaponId = "";

        private void Awake()
        {
            BackToPreviewStyleBtn.onClick.RemoveAllListeners();
            BackToPreviewStyleBtn.onClick.AddListener(OnBackBtnClick);
        }

        private void OnDisable()
        {
            StopAllTimer();
        }

        public void AddBackListener(Action callback)
        {
            _backAction = callback;
        }

        public void Show()
        {
            this.gameObject.SetActive(true);
        }

        public void Hide()
        {
            OnAnimationRunning(false);
            this.gameObject.SetActive(false);
        }

        public void SetBg(string path) {
            if (bgRawImage == null) {
                return;
            }

            if (string.IsNullOrEmpty(path)) {

                var viewCfg = GashaponDataManager.Inst.GetGashaponView(curGashaponId);
                if (viewCfg == null) {
                    return;
                }
                bgRawImage.gameObject.SetActive(true);
                bgRawImage.enabled = false;
                var itemObj = Loader
                    .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                    .Instantiate(bgRawImage.transform);
                var item = itemObj.GetComponent<ActivityCenterBgItem>();
                item.InitCustomBgItem(viewCfg.BgColor, viewCfg.AtlasPath, viewCfg.BgSpriteIds);
                item.gameObject.SetActive(true);
                return;
            }



            var bgTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>(path, gameObject);
            bgRawImage.texture = bgTexture;
            bgRawImage.enabled = true;
            bgRawImage.gameObject.SetActive(true);
        }


        public void InitTwistAnimStyle(string gashaponId)
        {
            if (_twistPlayerAnimationCtrl == null)
            {
                CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
                _twistCharacterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
                _twistCharacterWrap.SetParent(TwistCharacterRoot, true);
                _twistPlayerAnimationCtrl = _twistCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            }

            if (_twistGashaMachineAnimator == null || curGashaponId != gashaponId)
            {
                UpdateGashaponModle(gashaponId);
            }

            curGashaponId = gashaponId;

            BackToPreviewStyleBtn.gameObject.SetActive(true);
        }

        private void UpdateGashaponModle(string gashaponId)
        {
            if (_twistGashaMachineAnimator != null && _twistGashaMachineAnimator.gameObject != null)
            {
                Destroy(_twistGashaMachineAnimator.gameObject);
            }
            if (TwistGashaponMachineRoot == null) {return;}

            var wrapper = Loader.Load<GameObject>("Assets/Loadable/Model3D/UI_Props/CommonGashaponMachine/gashapon.prefab");
            var gMachine = wrapper.Instantiate(TwistGashaponMachineRoot);
            _twistGashaMachineAnimator = gMachine.GetComponent<Animator>();
            _twistGashaMachineAnimator.transform.localPosition = Vector3.zero;
            _twistGashaMachineAnimator.transform.localEulerAngles = new Vector3(0,241.3f,0);
            var bodyMeshRenderer = GameObjectEx.FindChildByName(gMachine.transform, "gashapon_body")?.GetComponent<MeshRenderer>();
            var renderMaterials = bodyMeshRenderer.materials;
            if (renderMaterials == null) { return; }
            var config = GashaponDataManager.Inst.GetGashaponView(gashaponId);

            for (int i = 0; i < renderMaterials.Length; i++)
            {
                if ( renderMaterials[i].name.Contains("mat_gashapon_01"))
                {
                    bodyMeshRenderer.materials[i].SetColor("_BaseColor", DataUtil.DeSerializeColorByHex(config.GashaColor1));
                }
                if ( bodyMeshRenderer.materials[i].name.Contains("mat_gashapon_02"))
                {
                    bodyMeshRenderer.materials[i].SetColor("_BaseColor", DataUtil.DeSerializeColorByHex(config.GashaColor2));
                }
            }
        }



        private void StopAllTimer()
        {
            TimerManager.Inst.Stop(_onceAnimTimer);
            TimerManager.Inst.Stop(_tenTimesAnimTimer);
            TimerManager.Inst.Stop(_tenTimesAnimTimer2);
            TimerManager.Inst.Stop(_ballShowTimer);
        }


        private void OnBackBtnClick()
        {
            Hide();
            _backAction?.Invoke();
        }

        public void OnAnimationRunning(bool isShow)
        {
            BackToPreviewStyleBtn.gameObject.SetActive(!isShow);
        }

        public void PlayOneTwistAnimation(List<RewardInfo> rewardInfoList, Action cb)
        {
            OnAnimationRunning(true);
            AkSoundManager.Inst.PlayUIEffectSound("Play_Gashapon_OneDraw_Twist");
            _twistGashaMachineAnimator.Play("Twist");
            _twistPlayerAnimationCtrl.SetPlayerAniState(PlayerAniState.Idle);
            _twistPlayerAnimationCtrl.SetPlayerAniState(PlayerAniState.GashaponOneTime);
            ShowTwistBall(rewardInfoList);
            TimerManager.Inst.Stop(_onceAnimTimer);
            _onceAnimTimer = TimerManager.Inst.RunOnce("twistOnce", 3f, () =>
            {
                AkSoundManager.Inst.PlayUIEffectSound("Play_Gashapon_OneDraw_ShowReward");
                _gashaponTwistBall.gameObject.SetActive(false);
                OnAnimationRunning(false);
                cb?.Invoke();
            });
        }


        public void PlayTenTwistAnimation(List<RewardInfo> rewardInfoList, Action cb)
        {
            OnAnimationRunning(true);
            AkSoundManager.Inst.PlayUIEffectSound("Play_Gashapon_TenDraws_Twist");
            _twistGashaMachineAnimator.Play("TenTimesTwist");
            _twistPlayerAnimationCtrl.SetPlayerAniState(PlayerAniState.Idle);
            _twistPlayerAnimationCtrl.SetPlayerAniState(PlayerAniState.GashaponTenTime);

            TimerManager.Inst.Stop(_tenTimesAnimTimer);
            TimerManager.Inst.Stop(_tenTimesAnimTimer2);
            _tenTimesAnimTimer = TimerManager.Inst.RunOnce("twistTenTimes", 8.0f, () =>
            {
                AkSoundManager.Inst.PlayUIEffectSound("Play_Gashapon_TenDraw_ShowReward");
                OnAnimationRunning(false);
                cb?.Invoke();
            });

            _tenTimesAnimTimer2 = TimerManager.Inst.RunOnce("twistTenTimes2", 3.0f, () =>
            {
                AkSoundManager.Inst.PlayUIEffectSound("Play_Gashapon_TenDraws_ExpectAndWin");
            });
        }


        #region 抽奖小球模型控制
        private void ShowTwistBall(List<RewardInfo> rewardInfoList)
        {
            if (_gashaponTwistBall == null)
            {
                var wrapper = Loader.Load<GameObject>("Assets/Loadable/Model3D/UI_Props/GashaponMachines/EMT_1P_Gashapon_prop_PREFAB.prefab");
                var pickNode = _twistCharacterWrap.GetBandNode((int)BodyNode.RightHand);
                var newBall = wrapper.Instantiate(pickNode);
                newBall.transform.localPosition = new Vector3(-0.0302f, 0.0484f, 0.0338f);
                _gashaponTwistBall = newBall.GetComponent<GashaponTwistBall>();
                _gashaponTwistBall.gameObject.SetActive(false);
            }

            TimerManager.Inst.Stop(_ballShowTimer);
            _ballShowTimer = TimerManager.Inst.RunOnce(nameof(_ballShowTimer), 1.8f, () =>
            {
                //显示最高级奖励的球颜色
                var maxLevel = RewardHelper.GetMaxRewardLevel(rewardInfoList);
                _gashaponTwistBall.SetLevelSizeShow(maxLevel);
                _gashaponTwistBall.transform.gameObject.SetActive(true);
            });
        }

        #endregion
    }
}
