using UnityEngine;

public class CameraModeSettingPanel : MonoBehaviour
{
    [Header("Switch Toggles")]
    [SerializeField] private CameraModeToggle showNameToggle;
    [SerializeField] private CameraModeToggle showInteractionHintToggle;
    [SerializeField] private CameraModeToggle showSelfieStickerToggle;

    [Header("Target Toggles")]
    [SerializeField] private CameraModeToggle showSelfToggle;
    [SerializeField] private CameraModeToggle showFriendToggle;
    [SerializeField] private CameraModeToggle showStrangerToggle;

    private bool _inited;

    public void Init()
    {
        if (_inited) return;

        // 兜底自动查找，避免 prefab 忘记拖引用导致空指针。
        if (showNameToggle == null) showNameToggle = FindToggleByName("ToggleShowName");
        if (showInteractionHintToggle == null) showInteractionHintToggle = FindToggleByName("ToggleShowInteractHint");
        if (showSelfieStickerToggle == null) showSelfieStickerToggle = FindToggleByName("ToggleShowSelfieSticker");
        if (showSelfToggle == null) showSelfToggle = FindToggleByName("ToggleShowSelf");
        if (showFriendToggle == null) showFriendToggle = FindToggleByName("ToggleShowFriend");
        if (showStrangerToggle == null) showStrangerToggle = FindToggleByName("ToggleShowStranger");

        // showNameToggle 使用持久化的设置初始化，其余保持默认开启
        if (showNameToggle != null)
        {
            showNameToggle.Init();
            showNameToggle.isOn = PlayerPrefs.GetInt(CameraModeSettingUtils.ShowNameKey, 1) == 1;
        }
        InitAndEnable(showInteractionHintToggle);
        InitAndEnable(showSelfieStickerToggle);
        InitAndEnable(showSelfToggle);
        InitAndEnable(showFriendToggle);
        InitAndEnable(showStrangerToggle);

        showNameToggle.onValueChanged.AddListener(OnShowNameChange);
        showInteractionHintToggle.onValueChanged.AddListener(OnShowInteractionHintChange);
        showSelfieStickerToggle.onValueChanged.AddListener(OnShowSelfieStickerChange);
        showSelfToggle.onValueChanged.AddListener(OnShowSelfChange);
        showFriendToggle.onValueChanged.AddListener(OnShowFriendChange);
        showStrangerToggle.onValueChanged.AddListener(OnShowStrangerChange);

        _inited = true;
    }

    private CameraModeToggle FindToggleByName(string name)
    {
        var child = transform.Find(name);
        if (child == null) return null;
        return child.GetComponent<CameraModeToggle>();
    }

    private static void InitAndEnable(CameraModeToggle toggle)
    {
        if (toggle == null) return;
        toggle.Init();
        toggle.isOn = true;
    }

    private void OnShowNameChange(bool isOn)
    {
        CameraModeSettingUtils.Inst.SetNameShowState(isOn);
    }

    private void OnShowInteractionHintChange(bool isOn)
    {
        CameraModeSettingUtils.Inst.SetInteractionHintShowState(isOn);
    }

    private void OnShowSelfieStickerChange(bool isOn)
    {
        CameraModeSettingUtils.Inst.SetSelfieStickerShowState(isOn);
    }

    private void OnShowSelfChange(bool isOn)
    {
        CameraModeSettingUtils.Inst.SetShowState(0, isOn);
    }
    
    private void OnShowFriendChange(bool isOn)
    {
        if (isOn)
        {
            if(CameraModeSettingUtils.Inst.isCheckingFriendship){
                showFriendToggle.isOn = false;
                TipPanel.ShowToast("操作太频繁，请稍后再试");
            }else{
                CameraModeSettingUtils.Inst.SetShowState(1, isOn);
            }
        }else{
            if(!CameraModeSettingUtils.Inst.isCheckingFriendship){
                CameraModeSettingUtils.Inst.SetShowState(1, false);
            }
        }
    }
    
    private void OnShowStrangerChange(bool isOn)
    {
        if (isOn)
        {
            if(CameraModeSettingUtils.Inst.isCheckingFriendship){
                showFriendToggle.isOn = false;
                TipPanel.ShowToast("操作太频繁，请稍后再试");
            }else{
                CameraModeSettingUtils.Inst.SetShowState(2, isOn);
            }
        }else{
            if(!CameraModeSettingUtils.Inst.isCheckingFriendship){
                CameraModeSettingUtils.Inst.SetShowState(2, false);
            }
        }
    }   
}
