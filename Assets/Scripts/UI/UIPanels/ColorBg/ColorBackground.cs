using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ColorBg
{
    public class ColorBackground : MonoBehaviour
    {
        [SerializeField]
        private Image bgImg;

        public Sprite[] sprites;
        private Vector3 start = new Vector3(-2048, 1024);
        private int rowNum = 10;
        private int lineNum = 8;
        private int xDistance = 250;
        private int yDistance = 40;
        private float offset = 200;


        public void InitBgItem(string bgColor, List<int> bgSpriteIds)
        {
            bgImg.color = DataUtil.DeSerializeColorByHex(bgColor);

            if (bgSpriteIds.Count > 0)
            {
                sprites = new Sprite[bgSpriteIds.Count];
                var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.ColorBackground);
                for (int i = 0; i < bgSpriteIds.Count; i++)
                {
                    string iconName = $"color_bg_icon{bgSpriteIds[i]}";
                    sprites[i] = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
                }

                CreateItem();
            }
        }
        
        public void InitCustomBgItem(string bgColor, List<string> bgSpriteIds, string atlasPath)
        {
            if (!string.IsNullOrEmpty(bgColor))
            {
                bgImg.color = DataUtil.DeSerializeColorByHex(bgColor);
            }
            
            if (string.IsNullOrEmpty(atlasPath) || bgSpriteIds == null || bgSpriteIds.Count == 0)
            {
                return;
            }

            sprites = new Sprite[bgSpriteIds.Count];
            for (int i = 0; i < bgSpriteIds.Count; i++)
            {
                sprites[i] = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, bgSpriteIds[i], gameObject);
            }

            CreateItem();
        }

        private void CreateItem()
        {
            transform.eulerAngles = new Vector3(0, 0, 15);
            int index = 0;
            float currentPositionX = 0;
            float currentPositionY = sprites[0].rect.height;
            for (int i = 0; i < lineNum; i++)
            {
                for (int j = 0; j < rowNum; j++)
                {
                    var obj = new GameObject();
                    var img = obj.AddComponent<Image>();
                    obj.transform.SetParent(transform);
                    obj.transform.localEulerAngles = Vector3.zero;
                    obj.transform.localScale = Vector3.one;
                    img.sprite = sprites[index];
                    img.SetNativeSize();
                    currentPositionX += img.sprite.rect.width / 2;
                    obj.transform.localPosition = new Vector3(currentPositionX, -(currentPositionY + yDistance) * i);
                    obj.transform.localPosition += start;
                    currentPositionX += img.sprite.rect.width / 2 + xDistance;
                    index++;
                    index %= sprites.Length;
                }

                if (i % 2 == 0)
                {
                    currentPositionX = offset;
                }
                else
                {
                    currentPositionX = 0;
                }
            }
        }
    }
}