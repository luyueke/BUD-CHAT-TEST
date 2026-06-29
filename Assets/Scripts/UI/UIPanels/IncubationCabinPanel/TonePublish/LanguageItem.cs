using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class LanguageItem : MonoBehaviour
    {
        [SerializeField] private Button SwitchBtn;
        [SerializeField] private GameObject SelectObj;
        [SerializeField] private Button DeleteBtn;

        public void Init(UnityAction onSwitch = null, UnityAction onDelete = null)
        {
            if (SwitchBtn != null)
            {
                SwitchBtn.onClick.RemoveAllListeners();
                if (onSwitch != null)
                {
                    SwitchBtn.onClick.AddListener(onSwitch);
                }
            }

            if (DeleteBtn != null)
            {
                DeleteBtn.onClick.RemoveAllListeners();
                if (onDelete != null)
                {
                    DeleteBtn.onClick.AddListener(onDelete);
                }
            }
        }

        public void SetSelected(bool selected)
        {
            SelectObj.SetActive(selected);
        }
    }
}
