using System.Collections;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditStageBgmItem : MonoBehaviour
    {
        public Text Txt;
        public CButton DelBtn;
        public CButton Btn;

        private AIParkUgcEditStage Root;
        private int Idx;
        [HideInInspector] public string Str;

        private void Awake()
        {
            DelBtn.onClick.AddListener(OnDelBtn);
            Btn.onClick.AddListener(OnBtn);
        }
        public void Init(AIParkUgcEditStage root, int idx)
        {
            Root = root;
            Idx = idx;
            //Txt.text = "答案" + Idx;
        }
        public void SetData(string str)
        {
            gameObject.SetActive(true);
            Str = str;
        }
        public void OnBtn()
        {
            //Root.OpenAnswer(this, Str);
        }
        public void OnDelBtn()
        {
            //Root.CurStage.musicUrls.Remove(Str);
            gameObject.SetActive(false);
            Str = string.Empty;
        }
    }
}