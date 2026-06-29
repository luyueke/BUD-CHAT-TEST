using GameData.Base;
using Message;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinRolesPanel
    {
        public void RefreshBaseMsg()
        {
            var _netCabinCharacterUgcInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();
            if (_netCabinCharacterUgcInfo == null)
            {
                return;
            }

            // 显示作者头像和昵称：优先使用调用方传入的 creatorInfo，
            // 若未传入且创作者正是当前用户，则使用本地账号信息，
            // 始终请求角色详情接口以获取最新点赞数，若本地无 creatorInfo 则同步更新头像/昵称
            AccountUserInfo immediateCreatorInfo = currentPublishData?.creatorInfo;

            if (immediateCreatorInfo == null && _netCabinCharacterUgcInfo.creator == AccountDataManager.Inst.Uid)
                immediateCreatorInfo = AccountDataManager.Inst.UserInfo;

            if (immediateCreatorInfo != null)
            {
                SetCreatorView(immediateCreatorInfo);
            }

            SetupLikeBtn(_netCabinCharacterUgcInfo.id, currentPublishData?.interactInfo);

            var characterId = _netCabinCharacterUgcInfo.id;
            CabinNetManager.Inst.GetCabinCharacterCreatorInfo(characterId, (detail) =>
            {
                if (gameObject == null || detail == null)
                    return;

                if (immediateCreatorInfo == null && detail.creator != null)
                    SetCreatorView(detail.creator);

                if (txt_like != null)
                    txt_like.text = (detail.interactInfo?.likeAmount ?? 0).ToString();
                string ugcID = string.IsNullOrEmpty(detail.characterInfo.targetUgcId) ? detail.characterInfo.id : detail.characterInfo.targetUgcId;
                SetupLikeBtn(ugcID, detail.interactInfo);
                RefreshBuyEditButtons(detail.characterInfo, detail.interactInfo);
                // 服务端确认已拥有时，刷新皮肤锁和互动锁，作为 targetUgcId 方案的兜底保障
                if (detail.interactInfo?.consumed == 1)
                {
                    RefreshSkinItemsLock();
                    RefreshInteractItemsLock();
                }
            });
        }

        private void SetupLikeBtn(string ugcId, BaseInteractInfo interactInfo)
        {
            if (LikeBtn == null)
                return;

            LikeBtn.SetData(ugcId, interactInfo?.liked ?? 0, interactInfo?.likeAmount ?? 0);
            LikeBtn.likeNumChange = (likeAmount) =>
            {
                MessageHelper.Broadcast<int>(MessageName.OnCabinCharacterLikeChange, likeAmount);
            };
        }

        private void SetCreatorView(AccountUserInfo creator)
        {
            remoteImg?.Load(creator.portraitUrl);

            if (txt_user_name != null)
                txt_user_name.text = creator.nickname;
        }

        private void OnCabinCharacterLikeChange(int likeAmount)
        {
            if (txt_like != null)
                txt_like.text = likeAmount.ToString();
        }
    }
}
