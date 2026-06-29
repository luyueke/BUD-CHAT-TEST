using System;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 养成舱编辑基础信息
/// </summary>

namespace UI.UIPanels.IncubationCabin
{

    public class IncubationCabinPanel_EditBaseMsg : MonoBehaviour
    {
        public TextInputView eitNameBox; //编辑角色名
        public TextInputView editBackgroundStoryBox;//编辑角色背景故事
        void Awake()
        {
            DateTime currentDate = DateTime.Now;
            string formattedDate = currentDate.ToString("yyyy-MM-dd");
            eitNameBox.SetInputWithoutNotify("Npc-" + formattedDate);
        }





    }


}
