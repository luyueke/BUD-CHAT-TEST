using DG.Tweening;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeSettingMenu: CameraModeMenuBase
{
    [SerializeField] private GameObject[] hide_objs;
    private bool[] hide_status_records;
    private Button reset_btn;
    private Button gallery_btn;
    private Button setting_btn;
    private Button hide_btn;
    private GameObject setting_panel;
    private CameraModeSettingPanel settingPanelCtrl;

    private bool isSettingShow = false;
    private bool isHideShow = false;

    protected override void OnInit(){
        reset_btn = GetComponentByName<Button>("Reset");
        gallery_btn = GetComponentByName<Button>("Gallery");
        setting_btn = GetComponentByName<Button>("Setting");
        hide_btn = GetComponentByName<Button>("Hide");

        setting_panel = transform.Find("SettingPanel").gameObject;
        setting_panel.SetActive(false);
        settingPanelCtrl = setting_panel.GetComponent<CameraModeSettingPanel>();
        if (settingPanelCtrl != null)
        {
            settingPanelCtrl.Init();
        }

        reset_btn.onClick.AddListener(OnResetClick);
        gallery_btn.onClick.AddListener(OnGalleryClick);
        setting_btn.onClick.AddListener(OnSettingClick);
        hide_btn.onClick.AddListener(OnHideClick);

        setting_btn.transform.Find("On").gameObject.SetActive(false);
        setting_btn.transform.Find("Off").gameObject.SetActive(true);
        hide_btn.transform.Find("On").gameObject.SetActive(false);
        hide_btn.transform.Find("Off").gameObject.SetActive(true);
        hide_status_records = new bool[hide_objs.Length];

        MessageHelper.AddListener<bool>(MessageName.UICameraModeHideUI, OnUICameraModeHideUI);
        MessageHelper.AddListener(MessageName.UICameraModeGlobalClose, OnUICameraModeGlobalClose);
    }

    private void OnDestroy(){
        MessageHelper.RemoveListener<bool>(MessageName.UICameraModeHideUI, OnUICameraModeHideUI);
        MessageHelper.RemoveListener(MessageName.UICameraModeGlobalClose, OnUICameraModeGlobalClose);
    }

    private void OnUICameraModeGlobalClose(){
        if(isSettingShow){
            OnSettingClick();
        }
    }
    private void OnUICameraModeHideUI(bool isHide){
        if(isHide){
            hide_btn.transform.Find("On").gameObject.SetActive(false);
            hide_btn.transform.Find("Off").gameObject.SetActive(true);
        }
    }

    private void OnResetClick(){
        MessageHelper.Broadcast(MessageName.UICameraModeReset);
    }

    private void OnGalleryClick(){
        //UIManager.Inst.ClosePanel(PanelId.CameraModePanel);
        UIManager.Inst.OpenPanel(PanelId.AlbumPanel);
    }

    private Tween setting_panel_tween;

    private void OnSettingClick(){
        if(isSettingShow){
            isSettingShow = false;
            setting_btn.transform.Find("On").gameObject.SetActive(false);
            setting_btn.transform.Find("Off").gameObject.SetActive(true);
            setting_panel_tween?.Kill();
            setting_panel_tween = setting_panel.transform.DOScale(0, 0.2f).SetEase(Ease.InBack);
            setting_panel_tween.onComplete = () => {
                setting_panel.SetActive(false);
            };
        }else{
            isSettingShow = true;
            setting_btn.transform.Find("On").gameObject.SetActive(true);
            setting_btn.transform.Find("Off").gameObject.SetActive(false);
            setting_panel.SetActive(true);
            setting_panel.transform.localScale = Vector3.zero;
            setting_panel_tween?.Kill();
            setting_panel_tween = setting_panel.transform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void OnHideClick(){
        if(isHideShow){
            isHideShow = false;
            hide_btn.transform.Find("On").gameObject.SetActive(false);
            hide_btn.transform.Find("Off").gameObject.SetActive(true);
            hide_btn.GetComponentInChildren<Text>().SetLocalText("隐藏");
            for(int i = 0; i < hide_objs.Length; i++){
                var obj = hide_objs[i];
                obj.SetActive(hide_status_records[i]);
            }
            MessageHelper.Broadcast(MessageName.UICameraModeHideUI, false);
        }else{
            isHideShow = true;
            hide_btn.transform.Find("On").gameObject.SetActive(true);
            hide_btn.transform.Find("Off").gameObject.SetActive(false);
            hide_btn.GetComponentInChildren<Text>().SetLocalText("显示");
            for(int i = 0; i < hide_objs.Length; i++){
                var obj = hide_objs[i];
                hide_status_records[i] = obj.activeSelf;
                obj.SetActive(false);
            }
            MessageHelper.Broadcast(MessageName.UICameraModeHideUI, true);
        }
    }

    protected override void OnShow()
    {

    }

    protected override void OnHide()
    {
        MessageHelper.RemoveListener<bool>(MessageName.UICameraModeHideUI, OnUICameraModeHideUI);
        MessageHelper.RemoveListener(MessageName.UICameraModeGlobalClose, OnUICameraModeGlobalClose);
    }
}
