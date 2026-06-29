using Game.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ActionItem : MonoBehaviour
    {
        [SerializeField] Image selectedImage;
        [SerializeField] Image assetsIcon;
        [SerializeField] Color bgColor;
        [SerializeField] Color selectColor;
        [SerializeField] Button closeButton;
        public RemoteImageBehaviour imageBehaviour;

        private Action<string> onItemSelected;
        public string mData { get; set; }
        public string CoverUrl{ get; set; }
        
        private void Awake()
        {
            closeButton.onClick.AddListener(OnItemClick);
        }

        private void OnItemClick()
        {
            onItemSelected?.Invoke(mData);
        }

        public void UpdateViews(string data, Action<string> action)
        {
            mData = data;
            onItemSelected = action;

            if (data == null)
            {
                selectedImage.color = bgColor;
                assetsIcon.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(false);
                return;
            }

            if(data.Equals("leisure") || data.Equals("default"))
            {
                var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
                var sprite1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, data, gameObject);
                assetsIcon.sprite = sprite1;
                selectedImage.color = selectColor;
                assetsIcon.gameObject.SetActive(true);
                closeButton.gameObject.SetActive(true);
                return;
            }
            
            selectedImage.color = selectColor;
            var sprite = PgcUtils.GetIconSpriteByPgcId(data, gameObject);
            if (sprite != null)
            {
                assetsIcon.gameObject.SetActive(true);
                assetsIcon.sprite = sprite;
            }
            else
            {
                assetsIcon.gameObject.SetActive(false);
            }
            closeButton.gameObject.SetActive(true);
        }
        
        public void UpdateViews(string data,string url, Action<string> action)
        {
            mData = data;
            CoverUrl = url;
            onItemSelected = action;

            if (data == null)
            {
                selectedImage.color = bgColor;
                closeButton.gameObject.SetActive(false);
                imageBehaviour.ResetRawImage();
                return;
            }
            selectedImage.color = selectColor;
            if (!string.IsNullOrEmpty(url))
            {
                imageBehaviour.Load(url);
            }
            closeButton.gameObject.SetActive(true);
        }
    }
    


    public class BagEmoteData : EmoteAssetsData
    {
        public bool Selected;
    }
}