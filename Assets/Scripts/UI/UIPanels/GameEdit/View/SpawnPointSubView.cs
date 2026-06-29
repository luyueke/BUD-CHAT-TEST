using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.UIPanels.GameEdit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpawnPointSubView :BasePropertyEditSubView
{
   public Toggle DefaultToggle;

   public UnityEvent<bool> SetDefault;
   protected override void OnInit()
   {
      DefaultToggle.onValueChanged.AddListener((val) =>
      {
         SetDefault?.Invoke(val);
      });
   }

   #if UNITY_EDITOR
   private void OnGUI()
   {
      if (GUI.Button(new Rect(100, 200, 200, 100), "添加"))
      {
         NodeBaseBehaviour behaviour;
         var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
         var lastIndex = sManager.GetLastIndex();
         if (lastIndex >= 16)
         {
            //TODO:文案待修改
            TipPanel.ShowToast("出生点不能超过16个");
            return;
         }

         GamePropNodeManager.Inst.TryCreateInEdit("20100024",out behaviour);
         var component = behaviour.entity.GetOrAddComp<SpawnPointComponent>();
         component.SpawnIndex = lastIndex + 1;
         var sBehaviour = behaviour as SpawnPointBehaviour;
         sBehaviour.SetIndex(component.SpawnIndex);
         sBehaviour.SetDefault(component.SpawnDefault);
      }

      if (GUI.Button(new Rect(400, 200, 200, 100), "删除"))
      {
         var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
         var lastIndex = sManager.GetLastIndex();
         if (lastIndex <= 1)
         {
            //TODO:文案待修改
            TipPanel.ShowToast("出生点不能少于1个");
            return;
         }
         var removes = sManager.GetSpawnPointByLast(1);
         removes.ForEach(x=>GamePropNodeManager.Inst.TryDeleteInEdit(x));
      }
   }
   #endif
}
