
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.Props.PropsComponents;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class TheatreTriggerBehaviour : ActorNodeBehaviour
    {
        private string _localAIBuddyID;
        private GameObject _avatarPlaceholder;
        private int _loadGeneration;
        private int _fetchGeneration;
        private OCTheatreInfo _theatreInfo;
        private GameObject _floatingIconObj;

        private const string AvatarPlaceholderPath = "Assets/Loadable/Avatar/CharacterBody/WhiteCharacter.prefab";

        public override bool IsCanClick
        {
            get
            {
                var comp = entity.GetComp<TheatreTriggerComponent>();
                return !string.IsNullOrEmpty(comp?.TheatreId);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(_localAIBuddyID))
                AIBuddyAvatarController.Inst.DestroyAIBuddy(_localAIBuddyID);
            ClearPlaceholder();
            ClearFloatingIcon();
        }

        public void LoadPlaceholder()
        {
            if (_avatarPlaceholder != null) return;
            var go = Loader.Load<GameObject>(AvatarPlaceholderPath, gameObject);
            _avatarPlaceholder = Instantiate(go);
            _avatarPlaceholder.transform.SetParent(transform);
            _avatarPlaceholder.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _avatarPlaceholder.transform.localScale = Vector3.one * 1.7f;
        }

        public void ClearPlaceholder()
        {
            if (_avatarPlaceholder == null) return;
            Destroy(_avatarPlaceholder);
            _avatarPlaceholder = null;
        }

        public void ClearActorCharacter()
        {
            _loadGeneration++;
            if (!string.IsNullOrEmpty(_localAIBuddyID))
            {
                AIBuddyAvatarController.Inst.DestroyAIBuddy(_localAIBuddyID);
                _localAIBuddyID = null;
            }
            // DestroyAIBuddy for "AINpcInMap_*" only removes from dict — destroy the GO explicitly
            var kcc = GetComponentInChildren<KinematicCharacterController>();
            if (kcc != null) Destroy(kcc.gameObject);
        }

        public void LoadFloatingIcon(string path)
        {
            ClearFloatingIcon();
            if (string.IsNullOrEmpty(path)) return;
            _floatingIconObj = ModelCachePool.Inst.Get(GetAssetId() + "_float", path);
            _floatingIconObj.transform.SetParent(transform);
            _floatingIconObj.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            _floatingIconObj.transform.localRotation = Quaternion.identity;
        }

        public void ClearFloatingIcon()
        {
            if (_floatingIconObj == null) return;
            Destroy(_floatingIconObj);
            _floatingIconObj = null;
        }

        public void FetchTheatreInfo(string theatreId)
        {
            if (string.IsNullOrEmpty(theatreId)) return;
            _fetchGeneration++;
            int myGen = _fetchGeneration;
            var jb = new JObject { ["id"] = theatreId };
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.TheatreInfo,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (this == null || _fetchGeneration != myGen) return;
                    var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                    if (rsp?.theaterInfo != null)
                        _theatreInfo = rsp.theaterInfo;
                },
                error => LoggerUtils.LogError($"TheatreTriggerBehaviour fetch theatre failed: {error}"));
        }

        public void LoadActorCharacter(string actorId, string actorName, int clothesIndex)
        {
            if (string.IsNullOrEmpty(actorId)) return;

            _loadGeneration++;
            int myGen = _loadGeneration;
            var jb = new JObject { ["id"] = actorId };
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.ActorInfo,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (this == null || _loadGeneration != myGen) return;
                    var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                    var clothesList = rsp?.actorInfo?.avatarClothes;
                    if (clothesList == null || clothesList.Count == 0) return;
                    var clothes = clothesList.Find(c => c.clothesIndex == clothesIndex) ?? clothesList[0];
                    if (string.IsNullOrEmpty(clothes.clothesJson)) return;
                    var charData = CharacterData.DeserializeObject(clothes.clothesJson);
                    if (charData == null) return;
                    _localAIBuddyID = AIBuddyAvatarController.Inst.CreateLocalGameAIBuddy(
                        actorId, actorName, charData, transform);
                },
                error => LoggerUtils.LogError($"TheatreTriggerBehaviour load actor failed: {error}"));
        }

        public override void OnTouchClick()
        {
            var comp = entity.GetComp<TheatreTriggerComponent>();
            if (string.IsNullOrEmpty(comp?.TheatreId)) return;

            if (_theatreInfo != null)
            {
                OpenTheatreInfoPanel(_theatreInfo);
                return;
            }

            // Async fetch hasn't completed yet — re-fetch on demand so the panel
            // receives complete data (cover, desc, etc.) instead of the bare fallback.
            var theatreId = comp.TheatreId;
            var theatreName = comp.TheatreName;
            _fetchGeneration++;
            int myGen = _fetchGeneration;
            var jb = new JObject { ["id"] = theatreId };
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.TheatreInfo,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (this == null || _fetchGeneration != myGen) return;
                    var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                    _theatreInfo = rsp?.theaterInfo ?? new OCTheatreInfo { id = theatreId, name = theatreName };
                    OpenTheatreInfoPanel(_theatreInfo);
                },
                _ => OpenTheatreInfoPanel(new OCTheatreInfo { id = theatreId, name = theatreName }));
        }

        private void OpenTheatreInfoPanel(OCTheatreInfo info)
        {
            UIAgentManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, info, (int)TheatreEnterType.Scene);
        }
    }
}
