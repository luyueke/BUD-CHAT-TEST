using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditMap : SettingContentBase
    {
        public Image Fill;
        public Slider Slider;

        public AIParkSelectColor Color;
        public Transform ColorParent;

        private List<AIParkSelectColor> Images = new List<AIParkSelectColor>();

        public Transform MapGroup;
        public Image MapBg;

        public Transform PasterParent;
        public AIParkUgcInputAnim InputAnim;
        private List<AIParkUgcEditPaster> PasterLs = new List<AIParkUgcEditPaster>();
        public RemoteImageBehaviour PasterPreview;
        public RectTransform RectPreview => PasterPreview.transform as RectTransform;
        public CButton DelBtn;
        public CButton ClickBtn;
        public Transform PitchCover;

        private AICommonGamePaster CurUrl;
        private int Idx;
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            if (curMapInfo.gameSetting.AICommonGameConfig.scene == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.scene = new AICommonGameConfig_Scene();
            }

            var ls = Es.DataTables.GetColorDataConfigList();

            foreach (var item in ls)
            {
                var tem = GameObject.Instantiate(Color, ColorParent).GetComponent<AIParkSelectColor>();
                tem.InitData(item.Color, OnSelect);
                Images.Add(tem);
            }
            Color.gameObject.SetActive(false);
            PasterPreview.ResetRawImage();
            PasterPreview.gameObject.SetActive(false);
            
            Slider.minValue = 0f;
            Slider.maxValue = 2f;
            Slider.value = curMapInfo.gameSetting.AICommonGameConfig.scene.width;
            Fill.fillAmount = Slider.value / (Slider.maxValue - Slider.minValue);

            var count = PasterParent.transform.childCount;
            var paster = curMapInfo.gameSetting.AICommonGameConfig.scene.pasterUrls;
            for (int i = 0; i < count; i++)
            {
                var idx = i;
                var v = PasterParent.GetChild(i).GetComponent<AIParkUgcEditPaster>();
                v.InitData(OnSelectPic, idx, OnUnSelectBtn, OnClickBtn);
                PasterLs.Add(v);

                if (i < paster.Count)
                {
                    PasterLs[i].SetData(paster[i]);
                    if (i == 0)
                    {
                        PasterLs[i].SetDefault(paster[i]);
                    }
                }
                else
                {
                    PasterLs[i].SetData(null);
                }
                PasterLs[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < PasterLs.Count; i++)
            {
                PasterLs[i].gameObject.SetActive(true);
                if (PasterLs[i].Config == null)
                {
                    break;
                }
            }


        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            Slider.onValueChanged.AddListener(OnSlider);

            DelBtn.onClick.AddListener(OnDelBtn);
            ClickBtn.onClick.AddListener(OnClickBtn);
        }

        public override void SaveData()
        {
            base.SaveData();
            if (CurUrl != null)
            {
                OnClickBtn(CurUrl, Idx);
            }
            curMapInfo.gameSetting.AICommonGameConfig.scene.pasterUrls = new();
            foreach (var v in PasterLs)
            {
                if (v.Config != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.scene.pasterUrls.Add(v.Config);
                }
            }
        }

        private void Start()
        {
            var ls = Es.DataTables.GetColorDataConfigList();
            var index = 0;
            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.scene.color);
            if (index >= 0)
            {
                Images[index].Toggle.isOn = true;
            }

            PitchCover.gameObject.SetActive(false);
        }

        private void OnSelect(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.scene.color = _color;
            if (ColorUtility.TryParseHtmlString("#" + _color, out var c))
            {
                MapBg.color = c;
            }
        }

        private void OnSlider(float _val)
        {
            Fill.fillAmount = _val / (Slider.maxValue - Slider.minValue);
            curMapInfo.gameSetting.AICommonGameConfig.scene.width = _val;
            var rect = MapBg.transform as RectTransform;
            var v2 = new Vector2(25, 25);
            rect.sizeDelta = new Vector2(1522.4f, 924.2f) + v2 * _val;
        }

        private void OnSelectPic(AICommonGamePaster _pic, int _idx)
        {
            if (CurUrl != null)
            {
                CurUrl.x = RectPreview.anchoredPosition.x;
                CurUrl.y = RectPreview.anchoredPosition.y;
                CurUrl.scale = RectPreview.localScale.x;
            }
            CurUrl = _pic;
            Idx = _idx;
            PasterPreview.Load(CurUrl.url);
            PasterPreview.gameObject.SetActive(true);
            RectPreview.anchoredPosition = new Vector2(CurUrl.x, CurUrl.y);
            RectPreview.localScale = Vector3.one * CurUrl.scale;
            for (int i = 0; i < PasterLs.Count; i++)
            {
                if (PasterLs[i].Config == null)
                {
                    PasterLs[i].gameObject.SetActive(true);
                    PasterLs[i].transform.SetAsLastSibling();
                    break;
                }
            }
            OnClickBtn(CurUrl, _idx);
            OnClickBtn();
        }

        private void OnUnSelectBtn(AICommonGamePaster _pic, int _idx)
        {
            PasterPreview.ResetRawImage();
            PasterPreview.gameObject.SetActive(false);

            bool bo = true;
            for (int i = 0; i < PasterLs.Count; i++)
            {
                if (PasterLs[i].Config != null)
                {
                    bo = false;
                    break;
                }
            }

            if (bo)
            {
                for (int i = 0; i < PasterLs.Count; i++)
                {
                    if (i == _idx)
                    {
                        PasterLs[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        PasterLs[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                //PasterLs[_idx].gameObject.SetActive(false);
            }

            CurUrl = null;
            Idx = 0;
        }

        private void OnClickBtn(AICommonGamePaster _pic, int _idx)
        {
            ClickBtn.enabled = true;
            if (CurUrl != null)
            {
                CurUrl.x = RectPreview.anchoredPosition.x;
                CurUrl.y = RectPreview.anchoredPosition.y;
                CurUrl.scale = RectPreview.localScale.x;
            }
            CurUrl = _pic;
            Idx = _idx;
            PasterPreview.Load(CurUrl.url);
            PasterPreview.gameObject.SetActive(true);
            RectPreview.anchoredPosition = new Vector2(CurUrl.x, CurUrl.y);
            RectPreview.localScale = Vector3.one * CurUrl.scale;
        }

        private void OnDelBtn()
        {
            PasterLs[Idx].OnCancelBtnClick();
            PitchCover.gameObject.SetActive(false);
        }

        private void OnClickBtn()
        {
            InputAnim.target = PasterPreview.transform;
            InputAnim.trigger = true;
            ClickBtn.enabled = false;
            PitchCover.gameObject.SetActive(true);
        }
    }
}