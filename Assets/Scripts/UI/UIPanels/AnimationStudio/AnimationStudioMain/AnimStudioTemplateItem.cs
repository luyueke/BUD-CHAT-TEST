using BUD.AnimPose;
using Game.Base;
using GameData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AnimationStudio
{
    public class AnimStudioTemplateItem : MonoBehaviour
    {
        public Text Txt_Title;
        public Image Img_Icon;
        public CButton Btn_Select;

        private AnimationStudioType _curType;
        private StudioTempConfig _curData;

        private string _atlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

        private void Awake()
        {
            Btn_Select.onClick.AddListener(EnterEditMode);
        }

        public void InitData(AnimationStudioType type, StudioTempConfig data)
        {
            this._curType = type;
            this._curData = data;

            Txt_Title.SetLocalText(this._curData.title);
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_atlasPath, _curData.iconName, this.gameObject);
            Img_Icon.sprite = sp;
        }

        private void EnterEditMode()
        {
            AnimDataManager.Inst.ClearData();
            
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            switch (_curType)
            {
                case AnimationStudioType.Animation:
                    var tempAnimInfo = _curData.animInfo.Clone();
                    p.Init(tempAnimInfo,null ,LoadingType.UGCAnim,s:Img_Icon.sprite);
                    tempAnimInfo.metaDataUrl = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/UgcAnimStudio/UgcAnimStudioData/UgcAnimDefData_" + tempAnimInfo.animType + ".json";
                    GameController.StartGame(EnterGameModel.UgcAnimEmpty, tempAnimInfo, true, null);
                    break;
                
                case AnimationStudioType.Pose:
                    var tempPoseInfo = _curData.poseInfo.Clone();
                    tempPoseInfo.poseData = JsonConvert.SerializeObject(new KeyFrameData());
                    AnimDataManager.Inst.enterMode = EnterPanelMode.Standard;
                    AnimDataManager.Inst.animPose =  tempPoseInfo;
                    p.Init(tempPoseInfo,null ,LoadingType.UGCAnim,s:Img_Icon.sprite);
                    GameController.StartGame(EnterGameModel.AnimPoseEmpty, tempPoseInfo, true, "AnimatedScene");
                    break;
            }
            MessageHelper.Broadcast(MessageName.OnUgcAnimStudioDraftListChange);
        }
    }
}