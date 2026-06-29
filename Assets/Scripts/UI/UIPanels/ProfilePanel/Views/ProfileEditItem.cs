using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace UI.UIPanels.ProfilePanel
{
    public class ProfileEditItem : MonoBehaviour
    {
        [SerializeField] private CButton ActionBtn;
        [SerializeField] private Text ShowName;

        [HideInInspector]public ProfileEditType EditType; 
    

        public void AddClickListener(UnityAction callback)
        {
            ActionBtn.onClick.RemoveAllListeners();
            ActionBtn.onClick.AddListener(callback);
        }

        public void SetData(string nameStr,ProfileEditType editType)
        {
            ShowName.SetLocalText(nameStr);
            EditType = editType;
        }
        
    }
}
