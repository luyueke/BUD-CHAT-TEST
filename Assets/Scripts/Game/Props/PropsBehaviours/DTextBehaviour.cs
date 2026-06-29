
using Game.Base;
using Game.Utils;
using UnityEngine;
using TMPro;
using System;

namespace Game.Props.PropsBehaviours
{
    public class DTextBehaviour : NodeBaseBehaviour
    {
        SuperTextMesh textPro;
        SpriteRenderer selectSprite;
        BoxCollider textCollider;

        const float maxWidth = 20f;
        const float maxHeight = 2f;

		public override void OnInitByCreate()
		{
			base.OnInitByCreate();
            textPro = this.transform.Find("text").GetComponent<SuperTextMesh>();
#if PACKAGE_TYPE_US
            textPro.font = Loader.Load<Font>("Assets/Arts/Font/Sarabun-ExtraBold.ttf", gameObject);
#endif
            textCollider = textPro.GetComponent<BoxCollider>();
            selectSprite = this.transform.Find("select").GetComponent<SpriteRenderer>();
		}

		public override void HighLight(bool isHigh)
		{
			base.HighLight(isHigh);
            selectSprite.gameObject.SetActive(isHigh);
		}

        public void SetColor(Color color)
        {
            textPro.color = color;
            textPro.Rebuild();
        }

        public void SetText(string content)
        {
            textPro.text = content;
            textPro.alignment = SuperTextMesh.Alignment.Center;
            DTextHelper.RebuildUntilFontAtlasStable(textPro);
            var width = textPro.preferredWidth;
            var height = textPro.preferredHeight;
            if (width > maxWidth || height > maxHeight)
            {
                textPro.alignment = SuperTextMesh.Alignment.Left;
                DTextHelper.RebuildUntilFontAtlasStable(textPro);
            }
            width = Math.Min(textPro.preferredWidth, maxWidth);
            textCollider.size = new Vector3(width, textPro.preferredHeight, 0);
            selectSprite.size = new Vector3(width, textPro.preferredHeight, 0);
        }
    }
}

