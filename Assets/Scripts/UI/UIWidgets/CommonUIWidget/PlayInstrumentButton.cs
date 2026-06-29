using Game.MusicalInstrument;
using Game.Avatar;
using GameData.BaseInfo;
using Message;
using UI.BaseWidgets;
using UnityEngine;

public class PlayInstrumentButton : MonoBehaviour
{
    public CButton Btn_Play;
    private bool _hasInstrument = false;
    private bool _isEnterCameraMode = false;
    private bool _allowShowByPanel = true;
    private bool _lastIsDrivingVehicle = false;

    private void Awake()
    {
        MessageHelper.AddListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.AddListener<bool>(MessageName.OnGetPlayerHoldInstrument, OnGetPlayerHoldInstrument);
        UIShowAbilityManager.Inst.AddBanChangeListener(OnUIAbilityBanChange);
        Btn_Play.onClick.AddListener(OnBtnPlayClick);
        
        this.Btn_Play.gameObject.SetActive(false);
        _lastIsDrivingVehicle = IsDrivingVehicle();
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.UICameraMode, OnCameraMode);
        MessageHelper.RemoveListener<bool>(MessageName.OnGetPlayerHoldInstrument, OnGetPlayerHoldInstrument);
        UIShowAbilityManager.Inst.RemoveBanChangeListener(OnUIAbilityBanChange);
    }
    
    private void Update()
    {
        bool isDrivingVehicle = IsDrivingVehicle();
        if (isDrivingVehicle != _lastIsDrivingVehicle)
        {
            _lastIsDrivingVehicle = isDrivingVehicle;
            RefreshBtnActive();
        }
    }

    public void OnGetPlayerHoldInstrument(bool hasInstrument)
    {
        this._hasInstrument = hasInstrument;
        RefreshBtnActive();
    }
    private void OnBtnPlayClick()
    {
        GuestInstrumentOPManager.Inst.EnterPlayMusicInstrumentState();
    }

    private void OnCameraMode(bool isEnterCameraMode)
    {
        _isEnterCameraMode = isEnterCameraMode;
        RefreshBtnActive();
    }
    
    private void OnUIAbilityBanChange(UIAbility ability, bool isBan)
    {
        if (ability == UIAbility.GuestInstrumentBtn)
        {
            RefreshBtnActive();
        }
    }

    public void ChangeBtnStatus(bool isShow)
    {
        _allowShowByPanel = isShow;
        RefreshBtnActive();
    }

    private void RefreshBtnActive()
    {
        if (!_allowShowByPanel || !_hasInstrument || _isEnterCameraMode)
        {
            Btn_Play.gameObject.SetActive(false);
            return;
        }

        bool isCanShowByAbility = !UIShowAbilityManager.Inst.GetBanBility(UIAbility.GuestInstrumentBtn);
        if (!isCanShowByAbility)
        {
            Btn_Play.gameObject.SetActive(false);
            return;
        }

        if (IsDrivingVehicle())
        {
            Btn_Play.gameObject.SetActive(false);
            return;
        }

        Btn_Play.gameObject.SetActive(true);
    }

    private static bool IsDrivingVehicle()
    {
        if (AvatarController.Inst != null &&
            AvatarController.Inst.SelfController != null &&
            AvatarController.Inst.SelfController.Motor != null &&
            AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
        {
            return true;
        }

        if (AvatarController.Inst != null &&
            AvatarController.Inst.SelfStateController != null &&
            AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.PGCVehicle))
        {
            return true;
        }

        return false;
    }
}
