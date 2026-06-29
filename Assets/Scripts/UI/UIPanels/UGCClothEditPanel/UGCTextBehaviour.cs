using Game.Utils;
using GameData.UGCData;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Game.UGCEditor
{
    public class UGCTextBehaviour: ElementBaseBehaviour
    {
        public TextData data;
        public Text self;
        private string LastStr;
        private int maxLength = 80;
        private const string defaultText = "输入文本...";
        [HideInInspector] 
        public string enterText = defaultText;
        private BoxCollider selfCollider;
        [HideInInspector] 
        public Transform mirParent;
        public UGCTextBehaviour textDynamicMir;
        public Action OnCopyAction;
        public Action OnDestoryBehaviour;
        
        public override void OnCreate(Transform mirrorParent)
        {
            base.OnCreate(mirrorParent);
            enterText = LocalizationManager.Inst.GetLocalizedText(defaultText);
            self = this.GetComponent<Text>();
            self.text = enterText;
            rectTrans = self.rectTransform;
            minSize =  414f;
            scalingRatio = 3;
            rectTrans.localPosition = Vector2.zero;
            rectTrans.localScale = Vector3.one;
            rectTrans.sizeDelta = new Vector2(minSize, minSize / scalingRatio);
            rectTrans.localEulerAngles = Vector3.zero;
            gameObject.SetActive(true);
            
            selfCollider = gameObject.AddComponent<BoxCollider>();
            selfCollider.size = new Vector3(rectTrans.sizeDelta.x,rectTrans.sizeDelta.y, 0);

            textDynamicMir = CreatMirrorText(self, mirrorParent);
            mirParent = mirrorParent;

            this.gameObject.SetActive(true);
            data = new TextData();
            SetSelfData();
            OnTransformChange();
            AddClickEvent();
        }
        
        public string GetOffetPosition()
        {
            return FormatUtils.Vector3ToString(rectTrans.localPosition + copyOffset);
        }
        
        public override void SetActive(bool isActive)
        {
            base.SetActive(isActive);
            this.gameObject.SetActive(isActive);
            textDynamicMir.gameObject.SetActive(isActive);
        }
        
        public UGCTextBehaviour CreatMirrorText(Text photoGo, Transform par)
        {
            var mirrorText = GameObject.Instantiate(photoGo, par);
            mirrorText.rectTransform.localPosition = photoGo.rectTransform.localPosition;
            mirrorText.rectTransform.localScale = photoGo.rectTransform.localScale;
            mirrorText.rectTransform.sizeDelta = photoGo.rectTransform.sizeDelta;
            mirrorText.rectTransform.localEulerAngles = photoGo.rectTransform.localEulerAngles;
            var behav = mirrorText.GetComponent<UGCTextBehaviour>();
            return behav;
        }
        
        public override void OnCopy()
        {
            base.OnCopy();
            data.pos = FormatUtils.Vector3ToString(rectTrans.localPosition + copyOffset);
            OnCopyAction?.Invoke();
        }
        


        public override GameObject GetDynamicMir()
        {
            if (textDynamicMir)
            {
                return textDynamicMir.gameObject;
            }
            return null;
        }

        public override void OnTransformChange()
        {
            if (textDynamicMir)
            {
                Copy(self, textDynamicMir.self);
            }
        }

        private void Copy(Text org,Text mir)
        {
            mir.rectTransform.localPosition = org.rectTransform.localPosition;
            mir.rectTransform.localScale = org.rectTransform.localScale;
            mir.rectTransform.sizeDelta = org.rectTransform.sizeDelta;
            mir.rectTransform.localEulerAngles = org.rectTransform.localEulerAngles;  
            mir.color = org.color;
            mir.text = org.text;
        }

        public override void SetSelfData()
        {
            data.pos = FormatUtils.Vector3ToString(transform.localPosition);
            data.sizeDelta = FormatUtils.Vector2ToString(self.rectTransform.sizeDelta);
            data.rot = FormatUtils.Vector3ToString(transform.localEulerAngles);
            data.hierarchy = hierarchy;
            data.content = self.text;
            data.color = FormatUtils.ColorToString(self.color);
        }
        

        public override void SetMirSiblingIndex()
        {
            if (textDynamicMir != null)
            {
                textDynamicMir.rectTrans.SetSiblingIndex(rectTrans.GetSiblingIndex());
            }
        }

        public override void OnClick()
        {
            base.OnClick();
            UGCImportTextManager.Inst.CurrentSelectBehaviour = this;
        }

        public override void Select()
        {
            base.Select();
            string str = self.text.Trim();
            str = str.Equals(enterText) ? "" : LastStr;
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = enterText,
                inputMode = 2,
                maxLength = 200,
                inputFlag = 0,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
                defaultText = str,
                returnKeyType = (int)ReturnType.Send,
                textSecurity = 0
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, ShowKeyBoard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }
        
        public override void SetData(UGCImportData pData)
        { 
            data = pData as TextData;
            rectTrans.localPosition = FormatUtils.StringToVector3(data.pos);
            rectTrans.sizeDelta = FormatUtils.StringToVector2(data.sizeDelta);
            rectTrans.localEulerAngles = FormatUtils.StringToVector3(data.rot);
            rectTrans.localScale = Vector3.one;
            hierarchy =data.hierarchy;
            self.text = data.content;
            self.color = FormatUtils.StringToColor(data.color);
            OnTransformChange();
        }
        
        public void ShowKeyBoard(string str)
        {
            string tempStr = FormatUtils.FilterNonStandardText(str);
            if (string.IsNullOrEmpty(tempStr.Trim()))
                tempStr = enterText;
            data.content = tempStr;
            LastStr = tempStr;
            SetTextContent(tempStr);
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            OnTransformChange();
        }

        private void SetTextContent(string str)
        {
            if (str.Length > maxLength)
            {
                string temp = str.Substring(0, maxLength);
                self.text = temp + "...";
                return;
            }
            self.text = str;
        }

        public void SetColor(Color col)
        {
            self.color = col;
            if (textDynamicMir)
            {
                textDynamicMir.self.color = col;
            }
            SetSelfData();
        }
        
        public override void OnDes()
        {
            base.OnDes();
            OnDestoryBehaviour?.Invoke();
        }
        
        
        public override GameObject ExcuteMirObj()
        {
            if (textDynamicMir)
            {
                return textDynamicMir.gameObject;
            }
            return null;
        }

        public override void RedoInfo(int partIndex)
        {
            UGCImportTextManager.Inst.AddElement(partIndex, this);
            UGCImportTextManager.Inst.CurrentSelectBehaviour = this;
        }
        
        public override void UndoInfo(int partIndex)
        {
            UGCImportTextManager.Inst.RemoveElement(partIndex, this);
        }
    }
}