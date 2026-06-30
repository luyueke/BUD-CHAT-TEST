using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 详情(窗口)
    /// 展示角色档案：名字、角色设定、世界观、角色简介、开场白
    /// 每条目用 tempPrefabGo 实例化，挂在 prefabParentGo 下
    /// tempPrefabGo 子结构：Image/txt_title（标题）、txt_content（内容）
    /// </summary>
    public class ChatSelectionDetailCom : MonoBehaviour
    {
        public Button hideBtn;
        public GameObject tempPrefabGo;
        public GameObject prefabParentGo;

        void Awake()
        {
            hideBtn.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void SetData(CabinChatCreateBotProfileData data)
        {
            foreach (Transform child in prefabParentGo.transform)
                Destroy(child.gameObject);

            CreateItem("角色名字", data.name);
            CreateItem("角色设定", data.persona);
            CreateItem("世界观", data.world);
            CreateItem("角色简介", data.one_line_note);

            // if (data.greeting_candidates != null && data.greeting_candidates.Count > 0)
            //     CreateItem("开场白", string.Join("\n", data.greeting_candidates));

            UICommonUtils.RefreshLayout(prefabParentGo.transform);
        }

        void CreateItem(string title, string content)
        {
            var go = Instantiate(tempPrefabGo, prefabParentGo.transform);
            go.SetActive(true);

            var titleTxt = go.transform.Find("Image/txt_title")?.GetComponent<Text>();
            if (titleTxt != null) titleTxt.text = title;

            var contentTxt = go.transform.Find("txt_content")?.GetComponent<Text>();
            if (contentTxt != null) contentTxt.text = content ?? string.Empty;
        }
    }
}
