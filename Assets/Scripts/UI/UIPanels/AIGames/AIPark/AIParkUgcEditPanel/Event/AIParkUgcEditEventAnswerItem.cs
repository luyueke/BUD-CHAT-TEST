using System.Collections;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditEventAnswerItem :MonoBehaviour
    {
        public Text Txt;
        public CButton DelBtn;
        public CButton Btn;

        private AIParkUgcEditEvent Root;
        private int Idx;
        [HideInInspector] public string Str;
        private void Awake()
        {
            DelBtn.onClick.AddListener(OnDelBtn);
            Btn.onClick.AddListener(OnBtn);
        }
        public void Init(AIParkUgcEditEvent root,int idx)
        {
            Root = root;
            Idx = idx;
        }
        public void SetData(string str) {
            gameObject.SetActive(true);
            Str = str;
            if (Str.Length > 5)
            {
                Txt.text = Str.Substring(0,5) + "...";
            }
            else
            {
                Txt.text = Str;
            }

            Txt.GetComponent<ContentSizeFitter>().SetLayoutHorizontal();
            var rect = transform as RectTransform;
            var txtRect = Txt.transform as RectTransform;
            rect.sizeDelta = new Vector2(120+ txtRect.sizeDelta.x, 88);
        }
        public void OnBtn()
        {
            Root.OpenAnswer(this,Str);
        }
        public void OnDelBtn()
        {
            Root.curItem.EventConfig.answers.Remove(Str);
            gameObject.SetActive(false);
            Str = string.Empty;
        }
    }
}