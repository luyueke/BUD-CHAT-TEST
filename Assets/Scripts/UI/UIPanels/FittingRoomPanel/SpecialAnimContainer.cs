using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using Sirenix.OdinInspector;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


[Serializable]
public class SpecialAnimButton {
    public SpecialAnim anim;
    public CButton btn;
}


public class SpecialAnimContainer : MonoBehaviour {

    [SerializeField]
    private Text selectedText;

    [SerializeField]
    private List<SpecialAnimButton> animBtns = new List<SpecialAnimButton>();

    [SerializeField]
    private CButton switchBtn;

    [SerializeField] private GameObject expandObj;

    private Action<SpecialAnim> callBack;

    private void Awake() {
        switchBtn.onClick.AddListener(() => {
            expandObj.SetActive(!expandObj.activeSelf);
        });
        var bgSprite = switchBtn.transform.Find("Image").GetComponent<Image>();
        string spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
        bgSprite.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "all_arrow", gameObject);
        //bgSprite.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "all_arrow", gameObject);
    }

    public void Start() {
        foreach (var animBtn in animBtns) {
            animBtn.btn.onClick.AddListener(() => {
                OnSpecialAnimChange(animBtn.anim);
            });
        }
    }

    public virtual void OnDisable() {
        callBack?.Invoke(SpecialAnim.Idle);
    }

    public void ResetIdle() {
        OnSpecialAnimChange(SpecialAnim.Idle);
    }


    public void SetCallBack(Action<SpecialAnim> action) {
        callBack = action;
    }


    public void OnSpecialAnimChange(SpecialAnim anim) {
        // 播放特殊动画
        selectedText.SetLocalText(animBtns.Find(tmp => tmp.anim == anim).btn.GetComponentInChildren<LocalizationComponent>(true).localizationKey);
        callBack?.Invoke(anim);
    }






}
