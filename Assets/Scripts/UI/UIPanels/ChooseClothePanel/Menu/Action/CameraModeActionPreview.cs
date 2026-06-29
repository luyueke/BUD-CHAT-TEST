using Es;
using Game.Avatar;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeActionPreview : MonoBehaviour
{
    private const string LeftEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
    private const string RightEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";
    private const string SelfieStickPrefabPath = "Assets/Loadable/AnimationsExpress/Feat/selfiestick_effect/selfiestick_effect.prefab";

    [SerializeField] private Button closeBtn;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform selfieContent;
    [SerializeField] private GameObject cameraSelfieItemPrefab;
    [SerializeField] private CameraSelfieItem previewItem;

    private CharacterWrap characterWrap;
    private PlayerAnimationCtrl animationCtrl;
    private GameObject selfieNode;

    private bool isInit;
    private string selfiePoseId;

    public void Init(string selfiePoseId)
    {
        this.selfiePoseId = selfiePoseId;
        if (!isInit)
        {
            isInit = true;
            InitUI();
            InitPreviewAvatar();
        }

        RefreshSingleSelfieItem();
        ApplySelfiePreview(selfiePoseId);
    }

    private void InitUI()
    {
        closeBtn ??= GameObjectEx.FindComponentByName<Button>(transform, "CloseBtn");
        if (selfieContent == null)
        {
            var contentNode = GameObjectEx.FindChildByName(transform, "SelfieContent");
            if (contentNode != null) selfieContent = contentNode.transform;
        }
        if (characterRoot == null)
        {
            var rootNode = GameObjectEx.FindChildByName(transform, "CharacterRoot");
            if (rootNode != null) characterRoot = rootNode;
        }
        avatarCameraController ??= GetComponentInChildren<AvatarCameraController>(true);
        if (avatarCameraController != null && characterRoot != null)
        {
            avatarCameraController.RotateTarget = characterRoot;
            avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }
    }

    private void RefreshSingleSelfieItem()
    {
        if (string.IsNullOrEmpty(selfiePoseId) || selfieContent == null)
        {
            return;
        }

        var cfg = DataTables.GetCameraSelfiePose(selfiePoseId);
        if (cfg == null)
        {
            return;
        }

        EnsurePreviewItem();
        if (previewItem == null)
        {
            return;
        }

        // 预览页只显示当前点击的一个自拍姿势。
        var allItems = selfieContent.GetComponentsInChildren<CameraSelfieItem>(true);
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] == previewItem)
            {
                allItems[i].gameObject.SetActive(true);
                continue;
            }
            allItems[i].gameObject.SetActive(false);
        }

        previewItem.transform.SetAsLastSibling();
        previewItem.InitData(cfg, OnPreviewSelfieItemClick);
        previewItem.ForceOwnedForPreview(true);
        previewItem.SetBroadcastEnabled(false);
        previewItem.SetSelected(true);
    }

    private void EnsurePreviewItem()
    {
        if (previewItem != null)
        {
            return;
        }

        previewItem = selfieContent.GetComponentInChildren<CameraSelfieItem>(true);
        if (previewItem != null)
        {
            return;
        }

        if (cameraSelfieItemPrefab == null)
        {
            return;
        }

        var go = GameObject.Instantiate(cameraSelfieItemPrefab, selfieContent);
        previewItem = go.GetComponent<CameraSelfieItem>();
    }

    private void OnPreviewSelfieItemClick(bool _, string clickedId)
    {
        ApplySelfiePreview(string.IsNullOrEmpty(clickedId) ? selfiePoseId : clickedId);
    }

    private void InitPreviewAvatar()
    {
        var saveCharacterData = AccountDataManager.Inst?.UserInfo?.avatarInfo;
        if (saveCharacterData == null || characterRoot == null || characterWrap != null)
        {
            return;
        }

        characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
        characterWrap.SetParent(characterRoot, true);
        animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        if (animationCtrl != null)
        {
            animationCtrl.gameObject.SetActive(true);
            animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
        }
    }

    private void ApplySelfiePreview(string poseId)
    {
        if (string.IsNullOrEmpty(poseId))
        {
            return;
        }
        if (characterWrap == null || animationCtrl == null)
        {
            InitPreviewAvatar();
        }
        if (characterWrap == null || animationCtrl == null)
        {
            return;
        }

        var cfg = DataTables.GetCameraSelfiePose(poseId);
        if (cfg == null)
        {
            return;
        }

        selfiePoseId = poseId;
        characterWrap.Avatar.SetActive(true);
        ApplySelfieAnim(cfg);
        CreateOrRefreshSelfieStick(cfg);
    }

    private void ApplySelfieAnim(CameraSelfiePose cfg)
    {
        animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
        animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
        animationCtrl.OverrideAnimationClip("selfiestick_idle", null);

        if (!string.IsNullOrEmpty(cfg.resourcePath))
        {
            var clipWrapper = Loader.Load<AnimationClip>(cfg.resourcePath + ".anim");
            var clipRes = clipWrapper != null ? clipWrapper.RetainAsset(gameObject) : null;
            if (clipRes != null)
            {
                var clip = AnimationClip.Instantiate(clipRes, gameObject.transform);
                animationCtrl.OverrideAnimationClip("selfiestick_idle", clip);
            }
        }


        animationCtrl.SetPlayerState(PlayerState.CameraMode);
        animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
    }

    private void CreateOrRefreshSelfieStick(CameraSelfiePose cfg)
    {
        var parent = characterWrap.Avatar.transform.Find(cfg.stickHand == 1 ? RightEffectPath : LeftEffectPath);
        if (parent == null)
        {
            return;
        }

        if (selfieNode == null)
        {
            var selfiePrefab = Loader.Load<GameObject>(SelfieStickPrefabPath)?.RetainAsset(gameObject);
            if (selfiePrefab == null)
            {
                return;
            }
            selfieNode = GameObject.Instantiate(selfiePrefab, parent);
        }
        else if (selfieNode.transform.parent != parent)
        {
            selfieNode.transform.SetParent(parent, false);
        }

        var selfieTransform = selfieNode.transform;
        selfieTransform.localPosition = new Vector3(0f, 0f, 0.02f);
        selfieTransform.localScale = Vector3.one;
        selfieTransform.localEulerAngles = cfg.stickRot;
        selfieNode.SetActive(true);

        var selfieAnimator = selfieNode.GetComponent<Animator>();
        if (selfieAnimator != null)
        {
            selfieAnimator.SetInteger("BoardState", 2);
        }
    }

    private void OnCloseBtnClick()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (animationCtrl != null)
        {
            animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
            animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
            animationCtrl.OverrideAnimationClip("selfiestick_idle", null);
            animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
        }
    }
}
