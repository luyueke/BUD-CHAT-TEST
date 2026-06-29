using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class UGCEditorTool : MonoBehaviour
    {
        //与UI预制顺序一致
        private enum ImportEditToolType
        {
            Copy = 0,
            Transform = 1,
            Replace = 2,
            Delete = 3,
            AddNew = 4
        }
        public GridMapCanvas mapCanvas;
        public Action OnToolChange { get; set; }

        public Action OnFrameChange { get; set;}
        public Toggle PaintToggle { get; set; }
        public Action OnSelectPhoto{ get; set; }
        public Action OnCopyPhoto { get; set; }
        public Action OnDeletePhoto { get; set; }
        
        public Action OnReplacePhoto { get; set; }

        public Action OnTransformPhoto { get; set; }

        public Action OnSelectText { get; set; }
        public Action OnAddNew { get; set; }
        
        public Action OnCopyAct{ get; set; }

        public Action OnAdjustAct { get; set; }


        private Toggle scissorsToggle;
        private Toggle eraserToggle;
        private Transform brushTool;
        private Transform fillTool;
        private Transform editTool;
        private UGCTextHander textHandler;
        private UGCPhotoHander photoHandler;
        private UGCPaintingHandler paintingHandler;
        private UGCOilDrumHandler oilDrumHandler;
        private UGCCanvasToolHandler canvaHandler;
        private Button AddNewBtn;
        private Text AddText;
        private Toggle textToggle;
        private Toggle PictureToggle;
        private List<Transform> auxiliaryTool;
        private Button[] editButtons;
        private Button copyBtn;
        private Button adjustBtn;
        private Button flipBtn;
        private GameObject flipTool;
        private string componentPath = "Scroll View/Viewport/Content/";
        
        public void OnInit()
        {
            auxiliaryTool = new List<Transform>();
            var parent = transform.parent;
            var panelNode = parent;
            brushTool = panelNode.Find("BrushTool");
            fillTool = panelNode.Find("FillTool");
            editTool = panelNode.Find("EditTool");
            var rawNode = panelNode.Find("DrawingView/ZoomMask/ZoomPanel/RawImage");
            canvaHandler = rawNode.GetComponent<UGCCanvasToolHandler>();
            auxiliaryTool.Add(brushTool);
            auxiliaryTool.Add(fillTool);
            auxiliaryTool.Add(editTool);
            paintingHandler = new UGCPaintingHandler(mapCanvas);
            oilDrumHandler = new UGCOilDrumHandler(mapCanvas);
            photoHandler = new UGCPhotoHander(mapCanvas);
            textHandler = new UGCTextHander(mapCanvas);
            PaintToggle = this.transform.Find(componentPath + "PaintToggle").GetComponent<Toggle>();
            PaintToggle.onValueChanged.AddListener(OnPaintingClick);
            PaintToggle.isOn = true;
            PaintToggle.onValueChanged.Invoke(PaintToggle.isOn);
            
            scissorsToggle = this.transform.Find(componentPath + "ScissorsToggle").GetComponent<Toggle>();
            scissorsToggle.onValueChanged.AddListener(OnScissorsClick);

            //材质编辑器没有该选项
            var eraserNode = this.transform.Find(componentPath + "EraserToggle");
            if (eraserNode != null)
            {
                eraserToggle = eraserNode.GetComponent<Toggle>();
                eraserToggle.onValueChanged.AddListener(OnScissorsClick);
            }
            var mirrorBtn = this.transform.Find(componentPath + "MirrorToggle").GetComponent<Toggle>();
            mirrorBtn.onValueChanged.AddListener(OnMirrorClick);
            
            var oilDrumToggle = this.transform.Find(componentPath + "OilDrumToggle").GetComponent<Toggle>();
            oilDrumToggle.onValueChanged.AddListener(OnOilDrumClick);
            
            PictureToggle = this.transform.Find(componentPath + "ImageToggle").GetComponent<Toggle>();
            PictureToggle.onValueChanged.AddListener(OnImportPictureClick);
            
            textToggle = this.transform.Find(componentPath + "TextToggle").GetComponent<Toggle>();
            textToggle.onValueChanged.AddListener(OnImportTextClick);

            copyBtn = transform.Find(componentPath + "CopyToggle").GetComponent<Button>();
            copyBtn.onClick.AddListener(OnCopyBtnClick);

            var adjustNode = transform.Find(componentPath + "AdjustToggle");
            if (adjustNode != null)
            {
                adjustBtn  = adjustNode.GetComponent<Button>();
                adjustBtn.onClick.AddListener(OnAdjustBtnClick);
            }
          
            
            flipBtn = transform.Find(componentPath + "FlipToggle").GetComponent<Button>();
            flipBtn.onClick.AddListener(OnFlipBtnClick);
            
            flipTool = parent.Find("FlipTool").gameObject;
            flipTool.gameObject.SetActive(false);
            
            var flipVButton = flipTool.transform.Find("GameObject/bg/FlipVButton").GetComponent<Button>();
            var flipHButton =  flipTool.transform.Find("GameObject/bg/FlipHButton").GetComponent<Button>();
            
            //反转按钮
            flipVButton.onClick.AddListener(() =>
            {
                mapCanvas.SetFlip(true);
                flipTool.SetActive(false);
            });
             flipHButton.onClick.AddListener(() =>
            {
                mapCanvas.SetFlip();
                flipTool.SetActive(false);
            });
             var flipToolHideBtn =  flipTool.GetComponent<Button>();
             flipToolHideBtn.onClick.AddListener(()=> flipTool.SetActive(false));

            var brushToggles = brushTool.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < brushToggles.Length; i++)
            {
                int index = i + 1;
                brushToggles[i].onValueChanged.AddListener((val) =>
                {
                    if (val)
                    {
                        OnSizeChangeClick((UGCDrawSize)index);
                    }
                });
            }
            
            var fillToggles = fillTool.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < fillToggles.Length; i++)
            {
                int index = i;
                fillToggles[i].onValueChanged.AddListener((val) =>
                {
                    if (val)
                    {
                        OnFileTypeChangeClick((UGCFillType)index);
                    }
                });
            }
            
            editButtons = editTool.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < editButtons.Length; i++)
            {
                int index = i;
                if (index == (int) ImportEditToolType.AddNew)
                {
                    AddNewBtn = editButtons[i];
                    AddText = AddNewBtn.GetComponentInChildren<Text>();
                }

                editButtons[i].onClick.AddListener(() =>
                {
                    OnEditClick((ImportEditToolType)index);
                });
            }
        }


        public void SetAdjustVisible(bool isVisible)
        {
            if (adjustBtn != null)
            {
                adjustBtn.gameObject.SetActive(isVisible);
            }
        }

        public void SetScissorsToggle(bool visible)
        {
            scissorsToggle.gameObject.SetActive(visible);
        }
        public void SetEraserToggle(bool visible)
        {
            eraserToggle.gameObject.SetActive(visible);
        }

        private void OnEditClick(ImportEditToolType  eType)
        {
            switch (eType)
            {
                case ImportEditToolType.Copy:
                    OnCopyPhoto?.Invoke();
                    break;
                case ImportEditToolType.Delete:
                    OnDeletePhoto?.Invoke();
                    break;
                case ImportEditToolType.Replace:
                    OnReplacePhoto?.Invoke();
                    break;
                case ImportEditToolType.Transform:
                    
                    OnTransformPhoto?.Invoke();
                    break;
                case ImportEditToolType.AddNew:
                    OnAddNew?.Invoke();
                    break;
            }
        }

        private void OnPaintingClick(bool isPainting)
        {
            if (isPainting)
            {
                SetAuxiliaryToolInvisible();
                brushTool.gameObject.SetActive(true);
                UGCPaintToolSettings.Current.ToolType = EditorToolType.Painting;
                canvaHandler.RemoveAllHandle();
                canvaHandler.AddPaintingHandler(paintingHandler);
                OnToolChange?.Invoke();
            }
            OnFrameChange?.Invoke();
        }
        
        private void OnMirrorClick(bool isMirror)
        {
            UGCPaintToolSettings.Current.PaintingMode = isMirror ? UGCDrawMode.Mirror : UGCDrawMode.Normal;
            OnFrameChange?.Invoke();
        }

        private void OnOilDrumClick(bool isOilDrum)
        {
            if (isOilDrum)
            {
                UGCPaintToolSettings.Current.ToolType = EditorToolType.OilDrum;
                SetAuxiliaryToolInvisible();
                fillTool.gameObject.SetActive(true);
                canvaHandler.RemoveAllHandle();
                canvaHandler.AddPaintingHandler(oilDrumHandler);
                OnToolChange?.Invoke();
            }
            OnFrameChange?.Invoke();

        }

        private void OnScissorsClick(bool isScissors)
        {
            if (isScissors)
            {
                UGCPaintToolSettings.Current.ToolType = EditorToolType.Scissors;
                SetAuxiliaryToolInvisible();
                brushTool.gameObject.SetActive(true);
                canvaHandler.RemoveAllHandle();
                canvaHandler.AddPaintingHandler(paintingHandler);
                OnToolChange?.Invoke();
            }
            OnFrameChange?.Invoke();
        }

        private void OnImportPictureClick(bool val)
        {
          
            if (val)
            {
                // if (!VipDataManager.Inst.isVip)
                // {
                //     var panel = UIManager.Inst.OpenPanel<UsingVipPanel>(PanelId.UsingVipPanel);
                //     return;
                // }
                UGCPaintToolSettings.Current.ToolType = EditorToolType.Photo;
                ChangeEditBtnActive();
                SetAuxiliaryToolInvisible();
                editTool.gameObject.SetActive(true);
                canvaHandler.RemoveAllHandle();
                canvaHandler.AddPaintingHandler(photoHandler);
                OnToolChange?.Invoke();
                OnSelectPhoto?.Invoke();
            }
            OnFrameChange?.Invoke();

        }

        private void OnImportTextClick(bool val)
        {
            if (val)
            {
                UGCPaintToolSettings.Current.ToolType = EditorToolType.Text;
                ChangeEditBtnActive();
                SetAuxiliaryToolInvisible();
                editTool.gameObject.SetActive(true);
                canvaHandler.RemoveAllHandle();
                canvaHandler.AddPaintingHandler(textHandler);
                OnToolChange?.Invoke();
                OnSelectText?.Invoke();
            }
            OnFrameChange?.Invoke();

        }

        private void OnFlipBtnClick()
        {
            flipTool.SetActive(true);
        }

        private void OnCopyBtnClick()
        {
            if (textToggle.isOn || PictureToggle.isOn)
            {
                PaintToggle.isOn = true;
            }
            OnCopyAct?.Invoke();
        }

        private void OnAdjustBtnClick()
        {
            OnAdjustAct?.Invoke();
        }


        private void ChangeEditBtnActive()
        {
            var toolType = UGCPaintToolSettings.Current.ToolType;
          

            switch (toolType)
            {
                case EditorToolType.Text:
                    for (var i = 0; i < editButtons.Length; i++)
                    {
                        editButtons[i].gameObject.SetActive(false);
                    }
                    AddText.SetLocalText("添加文字");
                    AddNewBtn.gameObject.SetActive(true);
                    break;
                case EditorToolType.Photo:
                    AddText.SetLocalText("添加图片");
                    for (var i = 0; i < editButtons.Length; i++)
                    {
                        editButtons[i].gameObject.SetActive(true);
                    }
                    break;
            }
            
        }


        private void SetAuxiliaryToolInvisible()
        {
            for (var i = 0; i < auxiliaryTool.Count; i++)
            {
                auxiliaryTool[i].gameObject.SetActive(false);
            }
        }
        private void OnSizeChangeClick(UGCDrawSize uSize)
        {
            UGCPaintToolSettings.Current.PaintingSize = uSize;
        }


        private void OnFileTypeChangeClick(UGCFillType fType)
        {
            UGCPaintToolSettings.Current.oilDrumFileType = fType;
        }
        
        /// <summary>
        /// 外部调用 - 强制切换到油漆桶工具
        /// </summary>
        public void SwitchToBucketTool()
        {
            // 查找油漆桶Toggle组件
            var oilDrumToggle = this.transform.Find(componentPath + "OilDrumToggle").GetComponent<Toggle>();
            if (oilDrumToggle != null)
            {
                // 强制设置为true，触发油漆桶工具切换
                oilDrumToggle.isOn = true;
            }
        }
    }
}
