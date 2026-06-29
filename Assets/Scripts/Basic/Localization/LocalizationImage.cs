
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;



public class LocalizationImage : MonoBehaviour {
    [SerializeField]
    private List<ImageInfo> localizationImages = new List<ImageInfo>();

    [SerializeField]
    private bool isNativeSize = true;

    private Image imageCom;

    void Awake() {
        InitComponent();
        Translate();
    }

    private void InitComponent() {
        imageCom = GetComponent<Image>();
    }


    private void Translate() {
        var langCode = LocalizationManager.Inst.LangCode;
        var imageInfo = localizationImages.FirstOrDefault(x => x.langCode == langCode);
        if (imageInfo != null && imageInfo.sprite != null) {
            imageCom.sprite = imageInfo.sprite;
            if (isNativeSize) {
                imageCom.SetNativeSize();
            }
        } else {
            LoggerUtils.LogError("Localization --> Localization Sprite is not set...:" + langCode + "," + gameObject.name);
        }
    }

#if UNITY_EDITOR
    public void Reset() {
        imageCom = GetComponent<Image>();
        if (imageCom != null && localizationImages != null && localizationImages.Count == 0) {
            localizationImages.Add(new ImageInfo() {
                sprite = imageCom.sprite,
                langCode = LangCode.zh_Hans
            });
        }
    }
#endif



    [Serializable]
    public class ImageInfo {
        public LangCode langCode;
        public Sprite sprite;
    }


}
