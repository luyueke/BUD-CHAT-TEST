using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Utils;
using UI.Base;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalNpcChatPanel : BasePanel<AIHospitalNpcChatPanel>
    {
        public Transform dialogRoot;
        private NpcDialogBox _npcDialogBox;
        private GameObject _createNpc;
        private string _txt_TalkContent;
        
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _createNpc = (GameObject)args[0];
            _txt_TalkContent = (string)args[1];
            
            if (_npcDialogBox == null)
            {
                var createNode = GameUtils.FindChildByName(_createNpc.transform, "dialogpos").gameObject;
                _npcDialogBox = CreateDialogBox(createNode);
            }
            _npcDialogBox.SetTextAndSpeak(YandereDataManager.Inst.TextAnimDuration, _txt_TalkContent, true, true);
        }
        
        private NpcDialogBox CreateDialogBox(GameObject createNode)
        {
            var dialogBox = NpcDialogBox.Create(createNode, new Vector3(0, 2.4f, 0));
            var cam = GameCameraUtils.Inst.GetMainCamera();
            dialogBox.SetCamera(cam);
            dialogBox.transform.SetParent(dialogRoot);
            return dialogBox;
        }
    }
}