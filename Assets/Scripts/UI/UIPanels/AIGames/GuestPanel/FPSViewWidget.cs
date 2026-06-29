using Cinemachine;
using DG.Tweening;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Utils;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine;

public class FPSViewWidget : MonoBehaviour
{
    public CButton changeTpsButton;
    public CButton screenModeBtn;
    public GameObject FpsNode;
    public GameObject TpsNode;
    private bool isTps = false;
    private const float ONE_CAM_FOLLOW_OFFSET = 0.1f;
    private const float THIRD_CAM_FOLLOW_OFFSET = -7.5f;
    private Vector3 TpsVec = new Vector3(30, 0, 0);
    private Vector3 FpsVec = new Vector3(0, -2.5f, 0);
    private Vector3 FpsPos = new Vector3(0, 0.114f, -0.09f);
    private Vector3 TpsPos = new Vector3(0, 0.7f, 0);
         //骨骼头部路径
    private const string BONE_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head";
    private GameObject headNode;
    private GameObject faceNode;
    private GameObject hairNode;
    protected KinematicCharacterController kinematicCharacter;
    
    public void InitCharacterNode(KinematicCharacterController kccCtr)
    {
        kinematicCharacter = kccCtr;
        headNode = AvatarController.Inst.SelfWrap.Avatar.transform.Find(BONE_PATH).gameObject;
        faceNode = AvatarController.Inst.SelfWrap.Avatar.transform.Find("body_face").gameObject;
        var hairAdapter = (HairPartAdapter)AvatarController.Inst.SelfWrap.GetPartAdapter(UniqueType.GetAvatar(AvatarSubType.Hair));
        if (hairAdapter != null)
        {
            hairNode = hairAdapter.curPart;
        }
        changeTpsButton.onClick.AddListener(OnTPSChangeClick);
    }

    private void SetPlayerHeadVisible(bool isVisible)
    {
        headNode.SetActive(isVisible);
        faceNode.SetActive(isVisible);
        if (hairNode != null)
        {
            hairNode.SetActive(isVisible);
        }
    }

    private void OnTPSChangeClick()
    {
        var virtualCamera = GameCameraUtils.Inst.GetPlayVirtualCamera();
        CinemachineTransposer transposer = null;
        if (!isTps) {
            FpsNode.SetActive(false);
            TpsNode.SetActive(true);
            SetPlayerHeadVisible(false);
            // playerCom.playModeCamCenter.localPosition = FpsPos;
            transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            virtualCamera.AddCinemachineComponent<CinemachineHardLockToTarget>();
            DOTween.To(() => transposer.m_FollowOffset.z, x => transposer.m_FollowOffset.z = x, ONE_CAM_FOLLOW_OFFSET, 0.3f).onComplete += () =>
            {
                screenModeBtn.gameObject.SetActive(false);
            };
            kinematicCharacter.SetCameraPos(new Vector3(0, 1.35f, 0));
            // playerCom.playModeCamCenter.DOLocalRotate(FpsVec, 0.3f);
            // playerCom.playerModel.transform.rotation = new Quaternion(0, 0, 0, 0);
            // playerCom.transform.DORotateQuaternion(modleEuler, 0.3f).onComplete += () =>
            // {
            // };
        }
        else
        {
            FpsNode.SetActive(true);
            TpsNode.SetActive(false);
            SetPlayerHeadVisible(true);
            // playerCom.playModeCamCenter.localPosition = TpsPos;
            transposer = virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
            transposer.m_XDamping = 0;
            transposer.m_YDamping = 0;
            transposer.m_ZDamping = 0;
            DOTween.To(() => transposer.m_FollowOffset.z, x => transposer.m_FollowOffset.z = x, THIRD_CAM_FOLLOW_OFFSET, 0.1f).onComplete += () =>
            {
                screenModeBtn.gameObject.SetActive(true);
            };
            kinematicCharacter.SetCameraPos(new Vector3(0, 1.8f, 0));
            // playerCom.playModeCamCenter.DOLocalRotate(TpsVec, 0.3f);
        }
        isTps = !isTps;
    }
}
