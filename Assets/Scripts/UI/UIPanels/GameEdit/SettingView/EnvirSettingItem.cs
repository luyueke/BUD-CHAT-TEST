// @Author: YangJie
// @Description:
// @Date:  2023/08/04
// @Modify:

using System;
using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.SettingView
{
    public class EnvirSettingItem : MonoBehaviour
    {
        private Image icon;
        private Text nameText;
        private GameObject selectObj;
        private Action<EnvirSettingItem> onClickedCallBack;
        private GameEnvSettingData settingData;

        private void Awake()
        {
            icon = GameObjectEx.FindComponentByName<Image>(transform, "Layout/Icon");
            nameText = GameObjectEx.FindComponentByName<Text>(transform, "Layout/Text");
            selectObj = GameObjectEx.FindChildByName(transform, "Layout/IconSelect").gameObject;
            selectObj.SetActive(false);
            icon.GetComponent<CButton>().onClick.AddListener(() =>
            {
                onClickedCallBack?.Invoke(this);
            });
        }

        public void InitItem(GameEnvSettingData data, Action<EnvirSettingItem> callBack)
        {
            settingData = data;
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.EditorPropSprite);
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, settingData.Icon, gameObject);
            icon.sprite = sp;
            nameText.SetLocalText(settingData.Name);
            selectObj.SetActive(false);
            AddOnSelectListener(callBack);
        }

        public GameEnvSettingData GetData()
        {
            return settingData;
        }

        public void SetSelect(bool isSelect)
        {
            selectObj.SetActive(isSelect);
            
        }

        public void AddOnSelectListener(Action<EnvirSettingItem> callback)
        {
            onClickedCallBack += callback;
        }

        public void RemoveOnSelectListener(Action<EnvirSettingItem> callback)
        {
            onClickedCallBack -= callback;
        }
        
        public void ClearOnSelectListener()
        {
            onClickedCallBack = null;
        }
    }
}