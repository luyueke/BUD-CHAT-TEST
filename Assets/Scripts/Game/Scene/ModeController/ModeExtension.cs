// @Author: YangJie
// @Description:
// @Date:  2023/07/25
// @Modify:

using Game.Base;
using Game.Scene.EnterModelController;
using GameData;

namespace Game.Scene.ModeController {
    public static class ModeExtension {



        public static bool IsEdit(this IModeManager modeManager) {
            return IsEdit();
        }

        public static bool IsPlay(this IModeManager modeManager) {
            return IsPlay();
        }

        public static bool IsGuest(this IModeManager modeManager) {
            return IsGuest();
        }

        public static bool IsAnimPoseEdit(this BaseInstance baseInstance)
        {
            return IsPoseEdit();
        }

        public static bool IsVehicleEdit(this BaseInstance baseInstance)
        {
            return IsVehicleEdit();
        }

        public static bool IsEdit(this BaseInstance baseInstance) {
            return IsEdit();
        }

        public static bool IsPlay(this BaseInstance baseInstance) {
            return IsPlay();
        }

        public static bool IsGuest(this BaseInstance baseInstance) {
            return IsGuest();
        }
        
        public static bool IsAIGuest(this BaseInstance baseInstance) {
            return IsAIGuest();
        }

        public static bool IsEdit(this BaseGlobalInstance baseInstance) {
            return IsEdit();
        }

        public static bool IsPlay(this BaseGlobalInstance baseInstance) {
            return IsPlay();
        }

        public static bool IsGuest(this BaseGlobalInstance baseInstance) {
            return IsGuest();
        }

        public static bool IsGuest() {
            if (GameController.GetCurrentModeController() == null) {
                return GameController.enterGameModel == EnterGameModel.GuestScene;
            } else {
                return GameController.GetCurrentModeController() is GuestModeController;
            }
        }
        
        public static bool IsAIGuest() {
            if (GameController.GetCurrentModeController() == null) {
                return 
                    GameController.enterGameModel == EnterGameModel.AIYandere
                    || GameController.enterGameModel == EnterGameModel.AIHospital
                        || GameController.enterGameModel == EnterGameModel.AIPark;
            } else {
                return GameController.GetCurrentModeController() is AIGusetModeController;
            }
        }

        public static bool IsPlay() {
            if (GameController.GetCurrentModeController() == null) {
                return GameController.enterGameModel == EnterGameModel.PublishTest ||
                       GameController.enterGameModel == EnterGameModel.UpdatePublishTest;
            } else {
                return GameController.GetCurrentModeController() is PlayModeController;
            }
        }

        public static bool IsPoseEdit() {
            if (GameController.GetCurrentModeController() == null) {
                return GameController.enterGameModel == EnterGameModel.AnimPoseEmpty
                       || GameController.enterGameModel == EnterGameModel.AnimPoseContinueEdit;
            } else {
                return GameController.GetCurrentModeController() is EditModeController;
            }
        }

        public static bool IsVehicleEdit()
        {
            return GameController.enterGameModel == EnterGameModel.UgcVehicleEmptyContinueEdit || GameController.enterGameModel == EnterGameModel.UgcVehicleEmpty;
        }

        public static bool IsPropEdit(this IModeManager modeManager) {
            return (GameController.enterGameModel == EnterGameModel.UgcPropEmpty ||
                    GameController.enterGameModel == EnterGameModel.UgcPropContinueEdit);
        }

        public static bool IsSkinEdit(this IModeManager modeManager) {
            return (GameController.enterGameModel == EnterGameModel.UgcSkinEmpty ||
                    GameController.enterGameModel == EnterGameModel.UgcSkinContinueEdit);
        }

        public static bool IsMusicInstrumentEdit(this IModeManager modeManager) {
            return (GameController.enterGameModel == EnterGameModel.UgcMusicalInstrumentEmpty ||
                    GameController.enterGameModel == EnterGameModel.UgcMusicalInstrumentContinueEdit);
        }

        public static bool IsVehicleEdit(this IModeManager modeManager)
        {
            return (GameController.enterGameModel == EnterGameModel.UgcVehicleEmpty || GameController.enterGameModel == EnterGameModel.UgcVehicleEmptyContinueEdit);
        }

        public static bool IsEdit() {
            if (GameController.GetCurrentModeController() == null) {
                if (GameController.enterGameModel == EnterGameModel.UgcSkinEmpty ||
                    GameController.enterGameModel == EnterGameModel.UgcSkinContinueEdit) {

                    // 衣服编辑器 分为 3D 和 2D
                    var enterGameModelController =
                        GameController.GetEnterModelController<UgcSkinEnterModelController>(GameController
                            .enterGameModel);
                    return enterGameModelController.skinInfo.isProp;
                }

                return GameController.enterGameModel == EnterGameModel.CreateEmptyScene
                       || GameController.enterGameModel == EnterGameModel.ContinueEditScene
                       || GameController.enterGameModel == EnterGameModel.UgcPropEmpty
                       || GameController.enterGameModel == EnterGameModel.UgcPropContinueEdit
                       || GameController.enterGameModel == EnterGameModel.UgcMusicalInstrumentEmpty
                       || GameController.enterGameModel == EnterGameModel.UgcMusicalInstrumentContinueEdit
                       || GameController.enterGameModel == EnterGameModel.UgcVehicleEmpty
                       || GameController.enterGameModel == EnterGameModel.UgcVehicleEmptyContinueEdit;
            } else {
                return GameController.GetCurrentModeController() is EditModeController;
            }
        }
    }
}
