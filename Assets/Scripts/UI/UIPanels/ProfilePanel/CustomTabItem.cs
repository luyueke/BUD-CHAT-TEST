using UnityEngine.UI;

namespace UI.BaseWidgets
{
    public class CustomTabItem : TabItem
    {
        public Image ReddotBg;
        public Text ReddotText;

        private string sectionId;
        private int reddotNum;

        /// <summary>
        /// 设置红点数
        /// </summary>
        /// <param name="num"></param>
        public void SetRedDotNum(int num)
        {
            if (num <= 0)
            {
                ReddotBg.gameObject.SetActive(false);
            }
            else
            {
                ReddotBg.gameObject.SetActive(true);
                ReddotText.text = num.ToString();
                this.reddotNum = num;
            }
        }
        
        public void SetSectionId(string id)
        {
            sectionId = id;
        }

        public string GetSectionId()
        {
            return sectionId;
        }
    }
}