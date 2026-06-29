using Es;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.MusicalInstrument;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;

namespace Game.MusicalInstrument
{
    public class GuestInstrumentOPManager : GameInstance<GuestInstrumentOPManager>
    {
        private string _curInstrumentId = "";
        private InstrumentInfo _curInstrumentInfo;

        public GuestInstrumentOPManager()
        {
            MessageHelper.AddListener(MessageName.OnExitPlayInstrumentAnim, OnExitPlayInstrumentAnim);
            AccountDataManager.Inst.AddAvatarChangeListener(OnAvatarChange);
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener(MessageName.OnExitPlayInstrumentAnim, OnExitPlayInstrumentAnim);
            AccountDataManager.Inst.RemoveAvatarChangeListener(OnAvatarChange);
        }

        public void AddListener()
        {
            AvatarController.Inst.AddAvatarCreateListener(OnAvatarCreate);
        }

        public void RemoveListener()
        {
            AvatarController.Inst.RemoveAvatarCreateListener(OnAvatarCreate);
        }

        private void OnAvatarCreate(string playerId, KinematicCharacterController kinematicCharacter)
        {
            if (playerId != AccountDataManager.Inst.Uid)
                return;
            bool isUgc = false;
            var part = GetCharacterPartData(out isUgc);
            OnChangeAvatar(part, isUgc);
        }

        private void OnAvatarChange(AccountUserInfo userInfo)
        {
            if (GameController.GetCurrentGameMode() != GameMode.Guest)
                return;

            if (userInfo != null && userInfo.avatarInfo != null)
            {
                bool isUgc = false;
                var part = GetCharacterPartData(userInfo.avatarInfo, out isUgc);
                OnChangeAvatar(part, isUgc);
            }
        }

        private void OnChangeAvatar(CharacterPartData part, bool isUgc)
        {
            if (part == null || part.IsNull())
            {
                _curInstrumentInfo = null;
                MessageHelper.Broadcast(MessageName.OnGetPlayerHoldInstrument, false);
                return;
            }

            if (isUgc)
            {
                _curInstrumentId = part.UId;
            }
            else
            {
                _curInstrumentId = part.Id;
            }

            if (string.IsNullOrEmpty(_curInstrumentId) || _curInstrumentId == "0")
            {
                MessageHelper.Broadcast(MessageName.OnGetPlayerHoldInstrument, false);
                return;
            }

            if (isUgc)
            {
                JObject req = new JObject()
                {
                    ["idList"] = _curInstrumentId,
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET,
                    JsonConvert.SerializeObject(req), (content) =>
                    {
                        BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
                        if (rspData.skinList == null || rspData.skinList.Count == 0 ||
                            rspData.skinList[0].skinActionInfo == null) return;
                        _curInstrumentInfo = rspData.skinList[0].skinActionInfo.instrumentInfo;
                        if (_curInstrumentInfo.toneInfo.IsPgc())
                        {
                            _curInstrumentInfo.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(_curInstrumentInfo.toneInfo.id);
                        }

                        MessageHelper.Broadcast(MessageName.OnGetPlayerHoldInstrument, true);
                    },
                    (msg) => { });
            }
            else
            {
                var iData = DataTables.GetInstrumentConfig(_curInstrumentId);
                if (iData != null)
                {
                    _curInstrumentInfo = new InstrumentInfo();
                    _curInstrumentInfo.moveId = iData.moveId;
                    _curInstrumentInfo.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(iData.toneId);
                    // PGC 乐器：像试衣间一样将部件穿到 SelfWrap，更新 avatarPartDatas 并加载背部视觉模型
                    // 同时更新 _chaData.partDatas（克隆副本），防止 RefreshAvatar 时因克隆缺失该部件而被清除
                    SyncPgcInstrumentToSelfWrap(_curInstrumentId);
                    MessageHelper.Broadcast(MessageName.OnGetPlayerHoldInstrument, true);
                }
            }
        }

        private void SyncPgcInstrumentToSelfWrap(string instrumentId)
        {
            var selfWrap = AvatarController.Inst.SelfWrap;
            if (selfWrap == null) return;
            int pgcType = UniqueType.GetAvatar(AvatarSubType.MusicalInstrument);
            int ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
            // 更新 _chaData.partDatas（克隆副本），使将来 RefreshAvatar 时也能正确包含 PGC 乐器
            var chaData = selfWrap.ChaData;
            if (chaData?.partDatas != null)
            {
                chaData.partDatas.RemoveAll(p => p.Type == pgcType || p.Type == ugcType);
                chaData.partDatas.Add(new CharacterPartData { Type = pgcType, Id = instrumentId });
            }
        }

        private CharacterPartData GetCharacterPartData(out bool isUGC)
        {
            return GetCharacterPartData(AvatarController.Inst.SelfWrap.ChaData, out isUGC);
        }

        private CharacterPartData GetCharacterPartData(CharacterData data, out bool isUGC)
        {
            var ugc = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
            var pgc = UniqueType.GetAvatar(AvatarSubType.MusicalInstrument);
            var ugcPart = data.GetPartData(ugc);
            if (ugcPart != null)
            {
                isUGC = true;
                return ugcPart;
            }

            var pgcPart = data.GetPartData(pgc);
            if (pgcPart != null)
            {
                isUGC = false;
                return pgcPart;
            }

            isUGC = false;
            return null;
        }

        public void EnterPlayMusicInstrumentState()
        {
            if(AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
            {
                TipPanel.ShowToast("牵手状态下不可以弹奏乐器哦");
                return;
            }
            
            if(string.IsNullOrEmpty(_curInstrumentId) || _curInstrumentInfo == null)
                return;
            
            //关闭UI
            if (UIManager.Inst.TryFindPanel(WindowId.GuestWindow, PanelId.GameGuestPanel, out GameGuestPanel gameGuestPanel))
            {
                gameGuestPanel.HidePanel();
            }
            
            var curDetailInfo = new InstrumentDetailInfo();
            if (_curInstrumentInfo.animDetailInfo == null)
            {
                _curInstrumentInfo.animDetailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
            }
            else
            {
                curDetailInfo = _curInstrumentInfo.animDetailInfo;
            }
            
            MusicalSyncParam param = new MusicalSyncParam
            {
                resId = _curInstrumentId,
                moveId = _curInstrumentInfo.moveId,
                detailInfo = curDetailInfo
            };
            AvatarController.Inst.SelfStateController.EnterState(PlayerState.MusicInstrumentPlay,param);
            MusicalInstrumentNetManager.Inst.SendChangeOp(MusicalHoldState.Play,param);
            
            UIManager.Inst.OpenPanel(PanelId.GuestInstrumentPlayPanel, _curInstrumentId, _curInstrumentInfo);
        }
        
        public void ExitPlayMusicInstrumentState()
        {
            if (string.IsNullOrEmpty(_curInstrumentId) || _curInstrumentInfo == null)
                return;
            
            var curDetailInfo = new InstrumentDetailInfo();
            if (_curInstrumentInfo.animDetailInfo == null)
            {
                _curInstrumentInfo.animDetailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
            }
            else
            {
                curDetailInfo = _curInstrumentInfo.animDetailInfo;
            }
            
            MusicalSyncParam param = new MusicalSyncParam
            {
                resId = _curInstrumentId,
                moveId = _curInstrumentInfo.moveId,
                detailInfo = curDetailInfo
            };
            AvatarController.Inst.SelfStateController.ExitState(PlayerState.MusicInstrumentPlay);
            MusicalInstrumentNetManager.Inst.SendChangeOp(MusicalHoldState.Back,param);
        }

        private void OnExitPlayInstrumentAnim()
        {
            //关闭UI
            if (UIManager.Inst.TryFindPanel(WindowId.GuestWindow, PanelId.GameGuestPanel, out GameGuestPanel gameGuestPanel))
            {
                gameGuestPanel.ShowPanel();
            }
            
            //关闭UI
            if (UIManager.Inst.TryFindPanel(WindowId.GuestWindow, PanelId.MusicScoreBagPanel, out MusicScoreBagPanel musicScoreBagPanel))
            {
                musicScoreBagPanel.CloseSelf();
            }
            
            if (UIManager.Inst.TryFindPanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel, out GuestInstrumentPlayPanel guestInstrumentPlayPanel))
            {
                guestInstrumentPlayPanel.CloseSelf();
            }
        }
    }
}