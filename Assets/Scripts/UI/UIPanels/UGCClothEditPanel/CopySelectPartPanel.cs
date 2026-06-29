using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{

    public class CopySelectPartPanel : MonoBehaviour
    {
        public Button backBtn;
        public GameObject item;
        public Transform partsContent;
        public List<RenderTexture> RenderTextures;
        public List<UgcPartData> ugcDatas;
        public Action<int> OnPartSelect;
        public Action _OnSelectReturn;
        private List<GameObject> items = new List<GameObject>();
        public bool isCharacter;
        private void Awake()
        {
            backBtn.onClick.AddListener(() =>
            {
                _OnSelectReturn?.Invoke();
                Hide();
            });
        }

        public void Show()
        {
            gameObject.SetActive(true);
            //只初始化一次
            if (items.Count > 0)
            {
                return;
            }

            for (int i = 0; i < RenderTextures.Count; i++)
            {
                GameObject obj = Instantiate(item);
                obj.gameObject.SetActive(true);
                obj.transform.SetParent(partsContent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                var selectPart = obj.GetComponent<CopySelectPartItem>();
                //材质面板，ugcData为null
                var ugcData = ugcDatas.Count > 0 ? ugcDatas[i] : null;
                int index = i;
                selectPart.Init(RenderTextures[index], index + 1, (id) =>
                {
                    OnPartSelect(id);
                    Hide();
                }, ugcData,isCharacter);
                items.Add(obj);
            }
        }

        public void Hide()
        {

            gameObject.SetActive(false);
        }
    }
}