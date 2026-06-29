using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.AvatarImage
{
    public class AvatarCatagoryItem: MonoBehaviour
    {
        private Image NormalView;
        private GameObject RedDotView;
        private Image SelectedView;
        private string atlasPath = "Assets/Loadable/UI/UIPanel/NewbieRegister/Avatar/AvatarCategoryIcon.spriteatlas";
        private void Awake()
        {
            InitUIIfNeed();
        }

        private void InitUIIfNeed()
        {
            if (NormalView == null)
            {
                NormalView = GameObjectEx.FindChildByName(transform, "Normal").GetComponent<Image>();
                RedDotView = GameObjectEx.FindChildByName(transform, "RedDot").gameObject;
                SelectedView = GameObjectEx.FindChildByName(transform, "Selected").GetComponent<Image>();
            }
        }
        
        public void UpdateSelected(bool isShow)
        {
            SelectedView.gameObject.SetActive(isShow); 
        }

        public void UpdateRedDotActive(bool isShow)
        {
            RedDotView.SetActive(isShow);
        }

        public AvatarCategoryData cData;
        public void SetData(AvatarCategoryData item)
        {
            cData = item;
            InitUIIfNeed();
            var normalIcon = item.iconName(false);
            if (!string.IsNullOrEmpty(normalIcon))
            {
                LoggerUtils.Log($"xxxxx {normalIcon}");
                NormalView.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, normalIcon, gameObject);
            }
            var selectIcon =  item.iconName(true);
            if (!string.IsNullOrEmpty(selectIcon))
            {
                SelectedView.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, selectIcon, gameObject);
            }

            RedDotManager.Inst.CheckRedDot(transform, AvatarConfigTool.PartTypes(item.CurrentMenuType));
        }
    }
}