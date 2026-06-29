// @Author: YangJie
// @Description:
// @Date:  2023/08/09
// @Modify:

using Game.MapSetting;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.Light
{
    public class DirLightAdvancedSubView : BasePropertyEditSubView
    {

        [SerializeField] private CButton lightColorBtn;
        
        [SerializeField] private CButton skyColorBtn;
        
        [SerializeField] private CButton equatorColorBtn;
        
        [SerializeField] private CButton groundColorBtn;


        [SerializeField] private GameColorEditSubView colorEditSubView;
        [SerializeField] private GameObject colorEditSubViewGo;
        
        private Button selectBtn;
        
        protected override void OnInit()
        {
            lightColorBtn.onClick.AddListener(() =>
            {
                OnColorBtnClicked(lightColorBtn);
            });
            
            skyColorBtn.onClick.AddListener(() =>
            {
                OnColorBtnClicked(skyColorBtn);
            });
            
            equatorColorBtn.onClick.AddListener(() =>
            {
                OnColorBtnClicked(equatorColorBtn);
            });
            
            groundColorBtn.onClick.AddListener(() =>
            {
                OnColorBtnClicked(groundColorBtn);
            });
            colorEditSubView.AddColorChangeListener(OnColorChange);
            colorEditSubView.SetEmbedStyle();
            colorEditSubView.EnableUnRedo(false);
        }

        protected override void OnStart()
        {
            base.OnStart();
            colorEditSubViewGo.SetActive(false);
        }

        private void OnColorChange(Color color)
        {
            if (selectBtn == lightColorBtn)
            {
                DirLightManager.Inst.SetLightColor(color);
            } else if (selectBtn == skyColorBtn)
            {
                SkyboxManager.Inst.GetComp().skyboxColor.sky = color;
                SkyboxManager.Inst.SetSkyboxColor();
            } else if (selectBtn == equatorColorBtn)
            {    
                SkyboxManager.Inst.GetComp().skyboxColor.equator = color;
                SkyboxManager.Inst.SetSkyboxColor();
                
            } else if (selectBtn == groundColorBtn)
            {
                SkyboxManager.Inst.GetComp().skyboxColor.ground = color;
                SkyboxManager.Inst.SetSkyboxColor();
            }
        }


        private void OnColorBtnClicked(Button btn)
        {
            if (selectBtn == btn)
            {
                selectBtn = null;
                colorEditSubViewGo.SetActive(false);
            }
            else
            {
                selectBtn = btn;
                colorEditSubViewGo.SetActive(true);
                var viewSiblingIndex = 4;
                if (selectBtn == lightColorBtn)
                {
                    colorEditSubView.SetColor(DirLightManager.Inst.GetDirLightComponent().lightColor);
                    viewSiblingIndex = 1;
                } else if (selectBtn == skyColorBtn)
                {
                    colorEditSubView.SetColor(SkyboxManager.Inst.GetComp().skyboxColor.sky);
                    viewSiblingIndex = 2;
                } else if (selectBtn == equatorColorBtn)
                {    
                    colorEditSubView.SetColor(SkyboxManager.Inst.GetComp().skyboxColor.equator);
                    viewSiblingIndex = 3;
                } else if (selectBtn == groundColorBtn)
                {
                    colorEditSubView.SetColor(SkyboxManager.Inst.GetComp().skyboxColor.ground);
                    viewSiblingIndex = 4;
                }
                
                colorEditSubViewGo.transform.SetSiblingIndex(viewSiblingIndex);
            }
        }
    }
}