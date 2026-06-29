using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ShapeItem : MonoBehaviour
    {

        [SerializeField] Toggle toggle;
        [SerializeField] Image icon;
        [SerializeField] GameObject selectStatus;
        [SerializeField] GameObject vip;
        [SerializeField] SpriteAtlas atlas;

        private int selectId = 0;

        private Action<int> onValueChanged;
        public void SetSelectedCallBack(Action<int> action)
        {
            onValueChanged = action;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                onValueChanged?.Invoke(selectId);
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
            }
            selectStatus.gameObject.SetActive(isOn);
        }

        public void SetIsOn(bool isOn)
        {
            selectStatus.gameObject.SetActive(isOn);
            toggle.SetIsOnWithoutNotify(!isOn);
        }

        public void SetShape(ShapeData data)
        {
            selectId = data.Id;
            icon.sprite = atlas.GetSprite(data.SpriteName);
            vip.SetActive(data.SaleType == ShapeSaleType.Vip);
        }

        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            toggle.group = toggleGroup;
        }

    }
}

