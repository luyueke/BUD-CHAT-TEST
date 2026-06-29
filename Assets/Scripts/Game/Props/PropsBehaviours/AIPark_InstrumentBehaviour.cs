using Es;
using Game.AvatarTool;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using Message;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_InstrumentBehaviour : AIPark_BasePropBehaviour 
    {

        [HideInInspector] public AICommonGameConfig_Musical musical;
        [HideInInspector] public AIPark_StageBehaviour behaviour;
        [HideInInspector] public int index;
        [HideInInspector] public InstrumentInfo instrumentInfo;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
        }

        public void SetData(AICommonGameConfig_Musical _m, AIPark_StageBehaviour _b,int idx) {
            if (_m == null) {
                return;
            }
            index = idx;

            musical = _m;

            behaviour = _b;

            var config = DataTables.GetAvatarCommonData(musical.id);
            if (config != null)
            {
                var path = config.texDir + config.prefabName + ".prefab";
                Debug.Log("乐园乐器加载路径  " + path);
                Loader.LoadAsyncOrSync<GameObject>(path, (isSuc, warpper) => {
                    Debug.Log("乐园乐器加载路径  " + isSuc);
                    if (isSuc)
                    {
                        if (warpper.RetainAsset() == null)
                        {
                            Debug.Log("乐园乐器加载路径 is null");
                        }
                        var obj = warpper.Instantiate(transform);
                        obj.transform.localPosition = Vector3.zero;
                        obj.transform.localScale = Vector3.one * 2;
                        obj.transform.localRotation = Quaternion.Euler(0,180,0);
                    }
                });
                var iData = DataTables.GetInstrumentConfig(musical.id);
                if (iData != null)
                {
                    instrumentInfo = new InstrumentInfo();
                    instrumentInfo.moveId = iData.moveId;
                    instrumentInfo.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(iData.toneId);
                }
            }
            else
            {
                UIAgentManager.Inst.AssetGetInfo(4, musical.id, (t) =>
                {
                    instrumentInfo = t.skinActionInfo.instrumentInfo;
                    if (instrumentInfo.toneInfo.IsPgc())
                    {
                        instrumentInfo.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(instrumentInfo.toneInfo.id);
                    }
                    var obj = PropSkinPartCachePool.Inst.GetSkinPartObj(musical.id, t.skinInfo.metaDataUrl);
                    obj.transform.parent = transform;
                    obj.transform.localRotation = Quaternion.Euler(instrumentInfo.animDetailInfo.rDef);
                    obj.transform.localPosition = instrumentInfo.animDetailInfo.pDef;
                    obj.transform.localScale = instrumentInfo.animDetailInfo.sDef;
                });
            }
        }

        protected override void Play()
        {
            behaviour.SinglePlay(_playerStateController.PlayerID, this);
        }

        protected override void PlayWithBuddy()
        {

        }

        public override void OnReset()
        {
            base.OnReset();

        }

        protected override void StartPlaySound(string soundName)
        {

        }

        protected override void EndPlaySound(string soundName)
        {

        }

        public override void OnPropReset()
        {
            if (IsCanClick)
                return;

            IsCanClick = true;
        }
    }
}

