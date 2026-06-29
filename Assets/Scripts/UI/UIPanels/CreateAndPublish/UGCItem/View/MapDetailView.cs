using Es;
using GameData.BaseInfo;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI {
    public class MapDetailView : BaseDetailView {
        protected override int NameLimitCount => 50;
        protected override int DescLimitCount => 320;


        [SerializeField]
        protected CText playerLimitCountText;

        [SerializeField]
        protected CText hpCountText;

        [SerializeField]
        protected CText durationCountText;

        [SerializeField]
        protected CText winCountText;
        [SerializeField]
        protected Transform Bg3;

        private MapEditData mapEditData;

        public override void Show() {
            base.Show();

            playerLimitCountText.SetText("");
            winCountText.SetText("");
            Bg3.gameObject.SetActive(false);

            if (editData is MapEditData mapEditData) 
            {
                nextBtn.SetLocalText(mapEditData.isPublish ? "发布" : "更新");

                if (mapEditData.isCondition && (editData.GetInfo() is MapInfo mapInfo))
                {
                    string tips = LocalizationManager.Inst.GetLocalizedText("玩家人数:");
                    playerLimitCountText.SetText(tips + mapInfo.gameSetting.maxPlayer);
                    // hpCountText.text = $"生命值:{(mapInfo.gameSetting.limitHp <= 0 ? "无限制" : mapInfo.gameSetting.limitHp)}";
                    // durationCountText.text = $"游戏时长:{(mapInfo.gameSetting.limitDuration < 0 ? "无限制" : mapInfo.gameSetting.limitDuration + "s")}";
                    // winCountText.text = $"胜利条件:{(!mapInfo.IsPassLevelMap()? "无" : mapInfo.gameSetting.winCondition == 1 ? "到达终点" : "收集星星")}";
                    string winCondition = LocalizationManager.Inst.GetLocalizedText(!mapInfo.IsPassLevelMap() ? "无" : mapInfo.gameSetting.winCondition == 1 ? "到达终点" : "收集星星");
                    string winTips = LocalizationManager.Inst.GetLocalizedText("胜利条件:");
                    winCountText.SetText(winTips + winCondition);
                    Bg3.gameObject.SetActive(true);
                }
            }

        }

        protected override void OnNextBtnClick() {

            if (editData is not MapEditData mapEditData) {
                return;
            }
            ((LoadingButton)nextBtn).ShowLoading();

            mapEditData.draftInfo.UploadAndSave((info, isSuccess) => {
                if (gameObject == null) {
                    ((LoadingButton)nextBtn).HideLoading();
                    return;
                }
                if (!mapEditData.isPublish) {
                    ((LoadingButton)nextBtn).HideLoading();
                    base.OnNextBtnClick();
                    return;
                }

                if (isSuccess) {
                    if (string.IsNullOrEmpty(mapEditData.overwriteId) ) {
                        mapEditData.draftInfo.PublishDraftToServer((newInfo, tmpResult) => {
                            if (tmpResult) {
                                base.OnNextBtnClick();
                            } else {
                                ((LoadingButton)nextBtn).HideLoading();
                            }
                        });
                    } else {
                        mapEditData.draftInfo.PublishDraftToServer(mapEditData.overwriteId, (newInfo, tmpResult) => {
                            if (tmpResult) {
                                base.OnNextBtnClick();
                            } else {
                                ((LoadingButton)nextBtn).HideLoading();
                            }
                        });
                    }
                } else {
                    TipPanel.ShowToast($"{mapEditData.draftInfo.GetUploadMessage()}");
                    ((LoadingButton)nextBtn).HideLoading();
                }


            });
        }

    }
}
