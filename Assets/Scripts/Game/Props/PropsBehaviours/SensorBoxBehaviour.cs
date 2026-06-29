
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Utils;
using TMPro;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SensorBoxBehaviour : NodeBaseBehaviour
    {
        private TextMeshPro _textMeshPro;
        private MeshRenderer[] meshRenders;
        public int UsedTimes = 0;//当前已使用次数
        public int SensorStatus = 0;//当前触发状态 0:未触发 1:已触发
        private Color[] originColor;
        
        public override void OnInitByCreate()
        {
            _textMeshPro = GetComponentInChildren<TextMeshPro>();
            meshRenders = GetComponentsInChildren<MeshRenderer>();
        }
        
        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            var renderers = GetComponentsInChildren<Renderer>(true);
            Color highColor = new Color(1, 1, 1);
            GamePropUtils.HighLight(isHigh,ref originColor,renderers,highColor);
        }
        
        public void RefreshIndex()
        {
            var tComp = entity.GetComp<SensorBoxComponent>();
            _textMeshPro.text = tComp.BoxIndex.ToString();
        }
        
        public void SetBoxVisiable(bool state)
        {
            if (meshRenders == null) return;
            foreach (var item in meshRenders)
            {
                item.enabled = state;
            }
        }

        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            var sComp = entity.GetComp<SensorBoxComponent>();
            int index = sComp.BoxIndex;
            LoggerUtils.Log("SensorBoxBehaviour OnBoxEnter:" + index);

            //已达使用次数
            if (sComp.BoxTimes > 0 && UsedTimes >= sComp.BoxTimes)
            {
                LoggerUtils.Log("该感应盒为一次性，已使用");
                return;
            }

            if (SensorStatus == 0)
            {
                SensorStatus = 1;
            }
            else
            {
                SensorStatus = 0;
            }
            UsedTimes ++;

            LocalShow();
            
            if (GlobalNodeManager.Inst.Get<SensorBoxManager>().IsGuest())
            {
                SendRequest();
            }
        }

        
        //本地预测展示
        private void LocalShow()
        {
            LoggerUtils.Log("SebsorBoxBehaviour LocalShow");
            GlobalNodeManager.Inst.Get<SensorBoxManager>().HandleSensorBoxTouch(this);
        }

        
        //TODO:@jaywill 联机发送数据
        private void SendRequest()
        {
            GlobalNodeManager.Inst.Get<SensorBoxManager>().SendRequest(this);
        }
        
    }
}
        
