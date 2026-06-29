using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using UnityEngine;
using UnityEngine.UI;
using static CustomBodyTypeController;

namespace UI.UIPanels.FittingRoom
{
    public class ShapeAdapter : MonoBehaviour
    {
        [SerializeField] ToggleGroup shapeContent;
        [SerializeField] private ShapeItem shapeItem;

        private Action<int> onValueChanged;

        private List<ShapeData> shapeDataList;

        private Dictionary<int, ShapeItem> shapeDict = new();

        private Queue<ShapeItem> shapeItemPool = new();

        public void SetShapeInfo(BaseAvatarWrapper baseAvatar)
        {
            shapeDataList = ShapeDataMgr.Inst.shapeDataList;

            var bodyIndex = GetNowBodyTypeIndex(baseAvatar);

            for (int i = 0; i < shapeDataList.Count; i++)
            {
                if (shapeDict.ContainsKey(shapeDataList[i].Id)) continue;
                ShapeItem item;
                if(shapeItemPool.Count > 0)
                {
                    item = shapeItemPool.Dequeue();
                }
                else
                {
                    item = Instantiate(shapeItem, shapeContent.transform);
                }
                item.gameObject.SetActive(true);
                item.transform.SetSiblingIndex(2 + i);
                item.SetShape(shapeDataList[i]);
                item.SetToggleGroup(shapeContent);
                item.SetSelectedCallBack(OnSelecedShape);
                shapeDict.Add(shapeDataList[i].Id, item);
            }

            if(bodyIndex != 0 && shapeDict[bodyIndex] != null)
            {
                shapeDict[bodyIndex].GetComponent<Toggle>().isOn = true;
                shapeDict[bodyIndex].SetIsOn(true);
            }

        }

        private int GetNowBodyTypeIndex(BaseAvatarWrapper baseAvatar)
        {
            var bodyCtrl = baseAvatar.Avatar.GetComponent<CustomBodyTypeController>();

            var curBody = bodyCtrl.GetCurrentBodyType();
            for (int i = 0; i < ShapeDataMgr.Inst.shapeDataList.Count; i++)
            {
                if(curBody == (BodyType)ShapeDataMgr.Inst.shapeDataList[i].Id)
                {
                    return ShapeDataMgr.Inst.shapeDataList[i].Id;
                }
            }
            return 0;
        }

        public void OnSelecedShape(int id)
        {
            onValueChanged?.Invoke(id);
            if (!PlayerPrefs.HasKey(ShapeDataMgr.Inst.firstSelectShapeKey) && id != ShapeDataMgr.Inst.shapeDataList[0].Id)
            {
                PlayerPrefs.SetInt(ShapeDataMgr.Inst.firstSelectShapeKey, 1);
                PlayerPrefs.Save();
                UIManager.Inst.OpenPanel(PanelId.ShapeHintPanel);
            }
        }

        public void SetCallback(Action<int> action)
        {
            onValueChanged = action;
        }

    }

}


